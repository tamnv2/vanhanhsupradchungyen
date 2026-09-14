using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class LanClientSecurityStore
{
    public const string SecuritySchemaVersion = "VHDCHY_LAN_CLIENT_SECURITY_V1";
    public const string SignatureAlgorithm = "ECDSA_P256_SHA256_P1363";
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    private readonly string _connectionString;

    public LanClientSecurityStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
    }

    public async Task<LanClientSecurityInspection> EnsureAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await using var transaction = connection.BeginTransaction();
        await EnsureStateAsync(connection, transaction, cancellationToken);
        transaction.Commit();
        return await InspectAsync(cancellationToken);
    }

    public async Task<LanClientSecurityInspection> InspectAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        var epoch = await ScalarTextAsync(
            connection,
            "SELECT security_epoch FROM lan_security_state WHERE singleton_id=1 LIMIT 1",
            cancellationToken);
        if (string.IsNullOrWhiteSpace(epoch))
            return new(false, SecuritySchemaVersion, null, 0, 0, 0, "SECURITY_STATE_REQUIRED");

        var active = await CountAsync(connection, "SELECT COUNT(*) FROM lan_paired_devices WHERE status='ACTIVE'", cancellationToken);
        var repair = await CountAsync(connection, "SELECT COUNT(*) FROM lan_paired_devices WHERE status='REPAIR_REQUIRED'", cancellationToken);
        var revoked = await CountAsync(connection, "SELECT COUNT(*) FROM lan_paired_devices WHERE status='REVOKED'", cancellationToken);
        return new(true, SecuritySchemaVersion, epoch, active, repair, revoked, "READY");
    }

    public async Task<LanPairingResult> RegisterPairedDeviceAsync(
        string deviceId,
        string publicKeySpkiBase64,
        string pairedByUserId,
        CancellationToken cancellationToken = default)
    {
        Require(deviceId, "DEVICE_ID_REQUIRED", 1, 200);
        Require(publicKeySpkiBase64, "PUBLIC_KEY_REQUIRED", 32, 4096);
        Require(pairedByUserId, "PAIRING_ACTOR_REQUIRED", 1, 240);
        ValidateP256PublicKey(publicKeySpkiBase64);

        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await using var transaction = connection.BeginTransaction();
        var epoch = await EnsureStateAsync(connection, transaction, cancellationToken);

        string? existingKey = null;
        string? existingStatus = null;
        string? existingEpoch = null;
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText = "SELECT public_key_spki_base64, status, paired_security_epoch FROM lan_paired_devices WHERE device_id=$deviceId LIMIT 1";
            read.Parameters.AddWithValue("$deviceId", deviceId);
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                existingKey = reader.GetString(0);
                existingStatus = reader.GetString(1);
                existingEpoch = reader.GetString(2);
            }
        }

        if (existingStatus == "REVOKED")
        {
            transaction.Rollback();
            throw new LanClientSecurityException("PAIRING_REVOKED_DEVICE", "Revoked device identity cannot be reactivated silently.");
        }
        if (existingStatus == "ACTIVE" &&
            (!string.Equals(existingKey, publicKeySpkiBase64, StringComparison.Ordinal) ||
             !string.Equals(existingEpoch, epoch, StringComparison.Ordinal)))
        {
            transaction.Rollback();
            throw new LanClientSecurityException("PAIRING_CONFLICT", "Active device identity already has different pairing evidence.");
        }

        var now = DateTimeOffset.UtcNow.ToString("O");
        await using var write = connection.CreateCommand();
        write.Transaction = transaction;
        write.CommandText = """
            INSERT INTO lan_paired_devices(
              device_id, public_key_spki_base64, signature_algorithm, status,
              paired_by_user_id, paired_security_epoch, paired_at, revoked_at, updated_at
            ) VALUES ($deviceId, $publicKey, $algorithm, 'ACTIVE', $actor, $epoch, $now, NULL, $now)
            ON CONFLICT(device_id) DO UPDATE SET
              public_key_spki_base64=excluded.public_key_spki_base64,
              signature_algorithm=excluded.signature_algorithm,
              status='ACTIVE',
              paired_by_user_id=excluded.paired_by_user_id,
              paired_security_epoch=excluded.paired_security_epoch,
              paired_at=excluded.paired_at,
              revoked_at=NULL,
              updated_at=excluded.updated_at
            """;
        write.Parameters.AddWithValue("$deviceId", deviceId);
        write.Parameters.AddWithValue("$publicKey", publicKeySpkiBase64);
        write.Parameters.AddWithValue("$algorithm", SignatureAlgorithm);
        write.Parameters.AddWithValue("$actor", pairedByUserId);
        write.Parameters.AddWithValue("$epoch", epoch);
        write.Parameters.AddWithValue("$now", now);
        await write.ExecuteNonQueryAsync(cancellationToken);
        await AppendAuditAsync(connection, transaction, "DEVICE_PAIRED", pairedByUserId, deviceId, epoch, null, cancellationToken);
        transaction.Commit();
        return new(deviceId, epoch, "ACTIVE", SignatureAlgorithm);
    }

    public async Task RevokeDeviceAsync(string deviceId, string actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        Require(deviceId, "DEVICE_ID_REQUIRED", 1, 200);
        Require(actorUserId, "SECURITY_ACTOR_REQUIRED", 1, 240);
        Require(reason, "SECURITY_REASON_REQUIRED", 1, 500);

        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await using var transaction = connection.BeginTransaction();
        var epoch = await EnsureStateAsync(connection, transaction, cancellationToken);
        var now = DateTimeOffset.UtcNow.ToString("O");
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE lan_paired_devices SET status='REVOKED', revoked_at=$now, updated_at=$now WHERE device_id=$deviceId AND status <> 'REVOKED'";
        command.Parameters.AddWithValue("$now", now);
        command.Parameters.AddWithValue("$deviceId", deviceId);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            transaction.Rollback();
            throw new LanClientSecurityException("DEVICE_NOT_ACTIVE", "Device is absent or already revoked.");
        }
        await AppendAuditAsync(connection, transaction, "DEVICE_REVOKED", actorUserId, deviceId, epoch, reason, cancellationToken);
        transaction.Commit();
    }

    public async Task<string> RotateSecurityEpochAsync(string actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        Require(actorUserId, "SECURITY_ACTOR_REQUIRED", 1, 240);
        Require(reason, "SECURITY_REASON_REQUIRED", 1, 500);

        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await using var transaction = connection.BeginTransaction();
        var oldEpoch = await EnsureStateAsync(connection, transaction, cancellationToken);
        var nextEpoch = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow.ToString("O");

        await using (var state = connection.CreateCommand())
        {
            state.Transaction = transaction;
            state.CommandText = "UPDATE lan_security_state SET security_epoch=$epoch, rotated_at=$now, updated_at=$now WHERE singleton_id=1";
            state.Parameters.AddWithValue("$epoch", nextEpoch);
            state.Parameters.AddWithValue("$now", now);
            await state.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var devices = connection.CreateCommand())
        {
            devices.Transaction = transaction;
            devices.CommandText = "UPDATE lan_paired_devices SET status='REPAIR_REQUIRED', updated_at=$now WHERE status='ACTIVE'";
            devices.Parameters.AddWithValue("$now", now);
            await devices.ExecuteNonQueryAsync(cancellationToken);
        }
        await AppendAuditAsync(connection, transaction, "SECURITY_EPOCH_ROTATED", actorUserId, null, nextEpoch, $"{reason}; previousEpoch={oldEpoch}", cancellationToken);
        transaction.Commit();
        return nextEpoch;
    }

    public async Task<LanClientSecurityDecision> VerifySignedRequestAsync(
        LanClientSignedRequestProof proof,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        ValidateProof(proof);
        var asOf = now ?? DateTimeOffset.UtcNow;
        DateTimeOffset sentAt;
        try
        {
            sentAt = DateTimeOffset.FromUnixTimeMilliseconds(proof.TimestampUnixMs);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Deny("REQUEST_TIMESTAMP_INVALID", proof.DeviceId, proof.SecurityEpoch);
        }
        if ((asOf - sentAt).Duration() > MaxClockSkew)
            return Deny("REQUEST_TIMESTAMP_OUT_OF_WINDOW", proof.DeviceId, proof.SecurityEpoch);

        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        var currentEpoch = await ReadCurrentEpochAsync(connection, cancellationToken);
        if (currentEpoch is null) return Deny("SECURITY_STATE_REQUIRED", proof.DeviceId, null);
        if (!string.Equals(currentEpoch, proof.SecurityEpoch, StringComparison.Ordinal))
            return Deny("SECURITY_EPOCH_MISMATCH", proof.DeviceId, currentEpoch);

        var device = await ReadDeviceAsync(connection, proof.DeviceId, cancellationToken);
        if (device is null) return Deny("DEVICE_NOT_PAIRED", proof.DeviceId, currentEpoch);
        if (device.Status != "ACTIVE")
            return Deny(device.Status == "REPAIR_REQUIRED" ? "DEVICE_REPAIR_REQUIRED" : "DEVICE_NOT_ACTIVE", proof.DeviceId, currentEpoch);
        if (!string.Equals(device.PairedSecurityEpoch, currentEpoch, StringComparison.Ordinal))
            return Deny("DEVICE_REPAIR_REQUIRED", proof.DeviceId, currentEpoch);
        if (device.SignatureAlgorithm != SignatureAlgorithm)
            return Deny("SIGNATURE_ALGORITHM_UNSUPPORTED", proof.DeviceId, currentEpoch);

        var canonical = BuildCanonicalRequest(proof);
        if (!VerifySignature(device.PublicKeySpkiBase64, canonical, proof.SignatureBase64))
            return Deny("REQUEST_SIGNATURE_INVALID", proof.DeviceId, currentEpoch);

        await using var transaction = connection.BeginTransaction();
        try
        {
            await using var nonce = connection.CreateCommand();
            nonce.Transaction = transaction;
            nonce.CommandText = "INSERT INTO lan_request_nonces(device_id, security_epoch, nonce, request_digest, accepted_at) VALUES ($deviceId,$epoch,$nonce,$digest,$acceptedAt)";
            nonce.Parameters.AddWithValue("$deviceId", proof.DeviceId);
            nonce.Parameters.AddWithValue("$epoch", currentEpoch);
            nonce.Parameters.AddWithValue("$nonce", proof.Nonce);
            nonce.Parameters.AddWithValue("$digest", Sha256Hex(canonical));
            nonce.Parameters.AddWithValue("$acceptedAt", asOf.ToString("O"));
            await nonce.ExecuteNonQueryAsync(cancellationToken);
            transaction.Commit();
        }
        catch (SqliteException error) when (error.SqliteErrorCode == 19)
        {
            transaction.Rollback();
            return Deny("REQUEST_REPLAY", proof.DeviceId, currentEpoch);
        }
        return new(true, "CHANNEL_AUTHORIZED", proof.DeviceId, currentEpoch);
    }

    public static string BuildCanonicalRequest(LanClientSignedRequestProof proof)
    {
        ValidateProof(proof, requireSignature: false);
        return string.Join('\n', new[]
        {
            SecuritySchemaVersion,
            proof.DeviceId,
            proof.SecurityEpoch,
            proof.TimestampUnixMs.ToString(System.Globalization.CultureInfo.InvariantCulture),
            proof.Nonce,
            proof.Method.Trim().ToUpperInvariant(),
            proof.RequestTarget.Trim(),
            proof.BodySha256.Trim().ToLowerInvariant()
        });
    }

    public static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        await pragma.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SchemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string> EnsureStateAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        await using var read = connection.CreateCommand();
        read.Transaction = transaction;
        read.CommandText = "SELECT security_epoch FROM lan_security_state WHERE singleton_id=1 LIMIT 1";
        var existing = Convert.ToString(await read.ExecuteScalarAsync(cancellationToken));
        if (!string.IsNullOrWhiteSpace(existing)) return existing;

        var epoch = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow.ToString("O");
        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT INTO lan_security_state(singleton_id,schema_version,security_epoch,created_at,rotated_at,updated_at) VALUES (1,$schema,$epoch,$now,NULL,$now)";
        insert.Parameters.AddWithValue("$schema", SecuritySchemaVersion);
        insert.Parameters.AddWithValue("$epoch", epoch);
        insert.Parameters.AddWithValue("$now", now);
        await insert.ExecuteNonQueryAsync(cancellationToken);
        return epoch;
    }

    private static async Task<string?> ReadCurrentEpochAsync(SqliteConnection connection, CancellationToken cancellationToken) =>
        await ScalarTextAsync(connection, "SELECT security_epoch FROM lan_security_state WHERE singleton_id=1 LIMIT 1", cancellationToken);

    private static async Task<PairedDevice?> ReadDeviceAsync(SqliteConnection connection, string deviceId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT public_key_spki_base64,signature_algorithm,status,paired_security_epoch FROM lan_paired_devices WHERE device_id=$deviceId LIMIT 1";
        command.Parameters.AddWithValue("$deviceId", deviceId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
    }

    private static async Task AppendAuditAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string eventType,
        string actorUserId,
        string? deviceId,
        string securityEpoch,
        string? reason,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO lan_security_audit(audit_event_id,event_type,actor_user_id,device_id,security_epoch,reason,occurred_at) VALUES ($id,$type,$actor,$device,$epoch,$reason,$at)";
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$type", eventType);
        command.Parameters.AddWithValue("$actor", actorUserId);
        command.Parameters.AddWithValue("$device", (object?)deviceId ?? DBNull.Value);
        command.Parameters.AddWithValue("$epoch", securityEpoch);
        command.Parameters.AddWithValue("$reason", (object?)reason ?? DBNull.Value);
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> CountAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    }

    private static async Task<string?> ScalarTextAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }

    private static void ValidateP256PublicKey(string value)
    {
        try
        {
            var bytes = Convert.FromBase64String(value);
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(bytes, out var read);
            if (read != bytes.Length || key.KeySize != 256)
                throw new LanClientSecurityException("PUBLIC_KEY_INVALID", "P-256 SPKI key is required.");
        }
        catch (LanClientSecurityException) { throw; }
        catch (Exception error) when (error is FormatException or CryptographicException)
        {
            throw new LanClientSecurityException("PUBLIC_KEY_INVALID", "Paired client public key is invalid.", error);
        }
    }

    private static bool VerifySignature(string publicKey, string canonical, string signature)
    {
        try
        {
            var keyBytes = Convert.FromBase64String(publicKey);
            var signatureBytes = Convert.FromBase64String(signature);
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(keyBytes, out var read);
            return read == keyBytes.Length && key.KeySize == 256 && key.VerifyData(
                Encoding.UTF8.GetBytes(canonical),
                signatureBytes,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (Exception error) when (error is FormatException or CryptographicException)
        {
            return false;
        }
    }

    private static void ValidateProof(LanClientSignedRequestProof proof, bool requireSignature = true)
    {
        ArgumentNullException.ThrowIfNull(proof);
        Require(proof.DeviceId, "DEVICE_ID_REQUIRED", 1, 200);
        Require(proof.SecurityEpoch, "SECURITY_EPOCH_REQUIRED", 16, 128);
        Require(proof.Nonce, "REQUEST_NONCE_REQUIRED", 16, 128);
        Require(proof.Method, "REQUEST_METHOD_REQUIRED", 1, 16);
        Require(proof.RequestTarget, "REQUEST_TARGET_REQUIRED", 1, 2048);
        if (!proof.RequestTarget.StartsWith("/", StringComparison.Ordinal))
            throw new LanClientSecurityException("REQUEST_TARGET_INVALID", "Signed request target must begin with '/'.");
        if (!IsLowerHexSha256(proof.BodySha256))
            throw new LanClientSecurityException("BODY_HASH_INVALID", "Signed body hash must be a lowercase 64-character SHA-256 hex value.");
        if (requireSignature) Require(proof.SignatureBase64, "REQUEST_SIGNATURE_REQUIRED", 16, 1024);
    }

    private static bool IsLowerHexSha256(string? value)
    {
        if (value is null || value.Length != 64) return false;
        return value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private static void Require(string? value, string code, int min, int max)
    {
        var length = value?.Trim().Length ?? 0;
        if (length < min || length > max) throw new LanClientSecurityException(code, code);
    }

    private static LanClientSecurityDecision Deny(string code, string deviceId, string? epoch) => new(false, code, deviceId, epoch);

    private sealed record PairedDevice(string PublicKeySpkiBase64, string SignatureAlgorithm, string Status, string PairedSecurityEpoch);

    private const string SchemaSql = """
CREATE TABLE IF NOT EXISTS lan_security_state (
  singleton_id INTEGER PRIMARY KEY CHECK(singleton_id=1),
  schema_version TEXT NOT NULL,
  security_epoch TEXT NOT NULL,
  created_at TEXT NOT NULL,
  rotated_at TEXT,
  updated_at TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS lan_paired_devices (
  device_id TEXT PRIMARY KEY,
  public_key_spki_base64 TEXT NOT NULL,
  signature_algorithm TEXT NOT NULL,
  status TEXT NOT NULL CHECK(status IN ('ACTIVE','REPAIR_REQUIRED','REVOKED')),
  paired_by_user_id TEXT NOT NULL,
  paired_security_epoch TEXT NOT NULL,
  paired_at TEXT NOT NULL,
  revoked_at TEXT,
  updated_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_lan_paired_devices_status ON lan_paired_devices(status,updated_at);
CREATE TABLE IF NOT EXISTS lan_request_nonces (
  device_id TEXT NOT NULL,
  security_epoch TEXT NOT NULL,
  nonce TEXT NOT NULL,
  request_digest TEXT NOT NULL,
  accepted_at TEXT NOT NULL,
  PRIMARY KEY(device_id,security_epoch,nonce),
  FOREIGN KEY(device_id) REFERENCES lan_paired_devices(device_id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE TABLE IF NOT EXISTS lan_security_audit (
  audit_event_id TEXT PRIMARY KEY,
  event_type TEXT NOT NULL,
  actor_user_id TEXT NOT NULL,
  device_id TEXT,
  security_epoch TEXT NOT NULL,
  reason TEXT,
  occurred_at TEXT NOT NULL
);
CREATE TRIGGER IF NOT EXISTS trg_lan_security_audit_no_update BEFORE UPDATE ON lan_security_audit BEGIN SELECT RAISE(ABORT,'lan security audit is immutable'); END;
CREATE TRIGGER IF NOT EXISTS trg_lan_security_audit_no_delete BEFORE DELETE ON lan_security_audit BEGIN SELECT RAISE(ABORT,'lan security audit is immutable'); END;
""";
}

public sealed record LanClientSignedRequestProof(
    string DeviceId,
    string SecurityEpoch,
    long TimestampUnixMs,
    string Nonce,
    string Method,
    string RequestTarget,
    string BodySha256,
    string SignatureBase64);

public sealed record LanClientSecurityDecision(bool Authorized, string Code, string DeviceId, string? SecurityEpoch);
public sealed record LanClientSecurityInspection(bool Ready, string SchemaVersion, string? SecurityEpoch, long ActiveDeviceCount, long RepairRequiredDeviceCount, long RevokedDeviceCount, string Code);
public sealed record LanPairingResult(string DeviceId, string SecurityEpoch, string Status, string SignatureAlgorithm);

public sealed class LanClientSecurityException : InvalidOperationException
{
    public LanClientSecurityException(string code, string message) : base(message) => Code = code;
    public LanClientSecurityException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
