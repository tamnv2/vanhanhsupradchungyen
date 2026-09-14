using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class LanUserSessionStore
{
    public const string SessionSchemaVersion = "VHDCHY_LAN_USER_SESSION_V1";
    private readonly string _connectionString;

    public LanUserSessionStore(string databasePath)
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

    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
    }

    public async Task<LanUserSessionIssueResult> IssueFromAuthenticatedEvidenceAsync(
        string userId,
        string deviceId,
        string securityEpoch,
        string authoritySnapshotVersion,
        bool mustChangePassword,
        DateTimeOffset? now = null,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        Require(userId, "USER_ID_REQUIRED", 1, 240);
        Require(deviceId, "DEVICE_ID_REQUIRED", 1, 200);
        Require(securityEpoch, "SECURITY_EPOCH_REQUIRED", 16, 128);
        Require(authoritySnapshotVersion, "AUTHORITY_VERSION_REQUIRED", 1, 160);
        var issuedAt = now ?? DateTimeOffset.UtcNow;
        var sessionTtl = ttl ?? TimeSpan.FromHours(8);
        if (sessionTtl < TimeSpan.FromMinutes(15)) sessionTtl = TimeSpan.FromMinutes(15);
        if (sessionTtl > TimeSpan.FromHours(24)) sessionTtl = TimeSpan.FromHours(24);

        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        var deviceError = await ValidateDeviceAsync(connection, deviceId, securityEpoch, cancellationToken);
        if (deviceError is not null) throw new LanUserSessionException(deviceError, deviceError);

        var activeAuthority = await ReadActiveAuthorityVersionAsync(connection, cancellationToken);
        if (!string.Equals(activeAuthority, authoritySnapshotVersion, StringComparison.Ordinal))
            throw new LanUserSessionException("AUTHORITY_REFRESH_REAUTH_REQUIRED", "Session issuance requires the current active authority generation.");

        var userStatus = await ReadAuthorityUserStatusAsync(connection, authoritySnapshotVersion, userId, cancellationToken);
        if (userStatus is null)
            throw new LanUserSessionException("ACCOUNT_NOT_FOUND", "Authenticated user is absent from the active authority snapshot.");
        if (!string.Equals(userStatus, "ACTIVE", StringComparison.Ordinal))
            throw new LanUserSessionException("ACCOUNT_NOT_ACTIVE", "Authenticated user is not active in the current authority snapshot.");

        var sessionId = Guid.NewGuid().ToString("N");
        var rawToken = Base64Url(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var expiresAt = issuedAt.Add(sessionTtl);

        await using var transaction = connection.BeginTransaction();
        await using (var revoke = connection.CreateCommand())
        {
            revoke.Transaction = transaction;
            revoke.CommandText = "UPDATE lan_auth_sessions SET status='REVOKED',revoked_at=$at WHERE user_id=$userId AND device_id=$deviceId AND status='ACTIVE'";
            revoke.Parameters.AddWithValue("$at", issuedAt.ToString("O"));
            revoke.Parameters.AddWithValue("$userId", userId);
            revoke.Parameters.AddWithValue("$deviceId", deviceId);
            await revoke.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO lan_auth_sessions(
                  auth_session_id,user_id,device_id,security_epoch,authority_snapshot_version,
                  token_hash,status,issued_at,expires_at,revoked_at,must_change_password
                ) VALUES ($id,$userId,$deviceId,$epoch,$authority,$hash,'ACTIVE',$issued,$expires,NULL,$mustChange)
                """;
            insert.Parameters.AddWithValue("$id", sessionId);
            insert.Parameters.AddWithValue("$userId", userId);
            insert.Parameters.AddWithValue("$deviceId", deviceId);
            insert.Parameters.AddWithValue("$epoch", securityEpoch);
            insert.Parameters.AddWithValue("$authority", authoritySnapshotVersion);
            insert.Parameters.AddWithValue("$hash", tokenHash);
            insert.Parameters.AddWithValue("$issued", issuedAt.ToString("O"));
            insert.Parameters.AddWithValue("$expires", expiresAt.ToString("O"));
            insert.Parameters.AddWithValue("$mustChange", mustChangePassword ? 1 : 0);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
        transaction.Commit();

        return new LanUserSessionIssueResult(sessionId, rawToken, userId, deviceId, securityEpoch, authoritySnapshotVersion, mustChangePassword, expiresAt);
    }

    public async Task<LanUserSessionDecision> AuthenticateAsync(
        string token,
        string deviceId,
        string securityEpoch,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        Require(token, "AUTH_TOKEN_REQUIRED", 32, 256);
        Require(deviceId, "DEVICE_ID_REQUIRED", 1, 200);
        Require(securityEpoch, "SECURITY_EPOCH_REQUIRED", 16, 128);
        var asOf = now ?? DateTimeOffset.UtcNow;

        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        var deviceError = await ValidateDeviceAsync(connection, deviceId, securityEpoch, cancellationToken);
        if (deviceError is not null) return Deny(deviceError);

        var tokenHash = Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT auth_session_id,user_id,device_id,security_epoch,authority_snapshot_version,status,expires_at,must_change_password
            FROM lan_auth_sessions WHERE token_hash=$hash LIMIT 1
            """;
        command.Parameters.AddWithValue("$hash", tokenHash);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return Deny("SESSION_NOT_FOUND");
        if (reader.GetString(5) != "ACTIVE") return Deny("SESSION_NOT_ACTIVE");
        if (!string.Equals(reader.GetString(2), deviceId, StringComparison.Ordinal)) return Deny("SESSION_DEVICE_MISMATCH");
        if (!string.Equals(reader.GetString(3), securityEpoch, StringComparison.Ordinal)) return Deny("SECURITY_EPOCH_MISMATCH");
        if (!DateTimeOffset.TryParse(reader.GetString(6), out var expiresAt) || expiresAt <= asOf) return Deny("SESSION_EXPIRED");

        var authorityVersion = reader.GetString(4);
        var activeAuthority = await ReadActiveAuthorityVersionAsync(connection, cancellationToken);
        if (!string.Equals(activeAuthority, authorityVersion, StringComparison.Ordinal)) return Deny("AUTHORITY_REFRESH_REAUTH_REQUIRED");

        var userStatus = await ReadAuthorityUserStatusAsync(connection, authorityVersion, reader.GetString(1), cancellationToken);
        if (userStatus is null) return Deny("ACCOUNT_NOT_FOUND");
        if (!string.Equals(userStatus, "ACTIVE", StringComparison.Ordinal)) return Deny("ACCOUNT_NOT_ACTIVE");

        return new LanUserSessionDecision(
            true,
            "SESSION_AUTHENTICATED",
            new LanUserSessionPrincipal(
                reader.GetString(0),
                reader.GetString(1),
                deviceId,
                securityEpoch,
                authorityVersion,
                reader.GetInt64(7) == 1,
                expiresAt));
    }

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

    private static async Task<string?> ValidateDeviceAsync(SqliteConnection connection, string deviceId, string securityEpoch, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.security_epoch,d.status,d.paired_security_epoch
            FROM lan_security_state s
            LEFT JOIN lan_paired_devices d ON d.device_id=$deviceId
            WHERE s.singleton_id=1 LIMIT 1
            """;
        command.Parameters.AddWithValue("$deviceId", deviceId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return "SECURITY_STATE_REQUIRED";
        var currentEpoch = reader.GetString(0);
        if (!string.Equals(currentEpoch, securityEpoch, StringComparison.Ordinal)) return "SECURITY_EPOCH_MISMATCH";
        if (reader.IsDBNull(1)) return "DEVICE_NOT_PAIRED";
        var status = reader.GetString(1);
        if (status != "ACTIVE") return status == "REPAIR_REQUIRED" ? "DEVICE_REPAIR_REQUIRED" : "DEVICE_NOT_ACTIVE";
        if (reader.IsDBNull(2) || !string.Equals(reader.GetString(2), currentEpoch, StringComparison.Ordinal)) return "DEVICE_REPAIR_REQUIRED";
        return null;
    }

    private static async Task<string?> ReadActiveAuthorityVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT authority_version FROM authority_snapshots WHERE status='ACTIVE' LIMIT 2";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var value = reader.GetString(0);
        if (await reader.ReadAsync(cancellationToken)) return null;
        return value;
    }

    private static async Task<string?> ReadAuthorityUserStatusAsync(
        SqliteConnection connection,
        string authoritySnapshotVersion,
        string userId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT payload_json FROM authority_snapshots WHERE authority_version=$version AND status='ACTIVE' LIMIT 1";
        command.Parameters.AddWithValue("$version", authoritySnapshotVersion);
        var payload = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
        if (string.IsNullOrWhiteSpace(payload)) return null;
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("users", out var users) || users.ValueKind != JsonValueKind.Array) return null;
            foreach (var user in users.EnumerateArray())
            {
                if (user.ValueKind != JsonValueKind.Object) continue;
                if (!user.TryGetProperty("userId", out var id) || id.ValueKind != JsonValueKind.String) continue;
                if (!string.Equals(id.GetString(), userId, StringComparison.Ordinal)) continue;
                if (!user.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String) return null;
                return status.GetString()?.Trim().ToUpperInvariant();
            }
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+','-').Replace('/','_');

    private static void Require(string? value, string code, int min, int max)
    {
        var length = value?.Trim().Length ?? 0;
        if (length < min || length > max) throw new LanUserSessionException(code, code);
    }

    private static LanUserSessionDecision Deny(string code) => new(false, code, null);

    private const string SchemaSql = """
CREATE TABLE IF NOT EXISTS lan_auth_sessions (
  auth_session_id TEXT PRIMARY KEY,
  user_id TEXT NOT NULL,
  device_id TEXT NOT NULL,
  security_epoch TEXT NOT NULL,
  authority_snapshot_version TEXT NOT NULL,
  token_hash TEXT NOT NULL UNIQUE,
  status TEXT NOT NULL CHECK(status IN ('ACTIVE','REVOKED')),
  issued_at TEXT NOT NULL,
  expires_at TEXT NOT NULL,
  revoked_at TEXT,
  must_change_password INTEGER NOT NULL DEFAULT 0 CHECK(must_change_password IN (0,1)),
  FOREIGN KEY(authority_snapshot_version) REFERENCES authority_snapshots(authority_version) ON UPDATE CASCADE ON DELETE RESTRICT,
  FOREIGN KEY(device_id) REFERENCES lan_paired_devices(device_id) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS ix_lan_auth_session_user_device ON lan_auth_sessions(user_id,device_id,status,issued_at);
CREATE INDEX IF NOT EXISTS ix_lan_auth_session_expiry ON lan_auth_sessions(status,expires_at);
""";
}

public sealed record LanUserSessionIssueResult(
    string SessionId,
    string Token,
    string UserId,
    string DeviceId,
    string SecurityEpoch,
    string AuthoritySnapshotVersion,
    bool MustChangePassword,
    DateTimeOffset ExpiresAt);

public sealed record LanUserSessionDecision(bool Authenticated, string Code, LanUserSessionPrincipal? Principal);

public sealed record LanUserSessionPrincipal(
    string SessionId,
    string UserId,
    string DeviceId,
    string SecurityEpoch,
    string AuthoritySnapshotVersion,
    bool MustChangePassword,
    DateTimeOffset ExpiresAt);

public sealed class LanUserSessionException : InvalidOperationException
{
    public LanUserSessionException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}
