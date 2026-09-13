using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class AuthoritySnapshotStore
{
    private readonly string _connectionString;

    public AuthoritySnapshotStore(string databasePath)
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

    public async Task<AuthoritySnapshotImportResult> ImportAsync(
        AuthoritySnapshotEnvelope envelope,
        string expectedEnvironment,
        string expectedClusterId,
        string expectedDomainContractVersion,
        CancellationToken cancellationToken = default)
    {
        ValidateEnvelope(envelope, expectedEnvironment, expectedClusterId, expectedDomainContractVersion);
        var payloadHash = Sha256Hex(envelope.PayloadJson);
        var scopeHash = Sha256Hex(envelope.ScopeJson);
        var importedAt = DateTimeOffset.UtcNow.ToString("O");

        await using var connection = await OpenAsync(cancellationToken);
        await EnsureActiveIndexAsync(connection, cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var existing = await ReadSnapshotAsync(connection, transaction, envelope.AuthorityVersion, cancellationToken);
            if (existing is not null)
            {
                var same =
                    string.Equals(Sha256Hex(existing.PayloadJson), payloadHash, StringComparison.Ordinal) &&
                    string.Equals(Sha256Hex(existing.ScopeJson), scopeHash, StringComparison.Ordinal) &&
                    string.Equals(existing.SourceCheckpoint, envelope.SourceCheckpoint, StringComparison.Ordinal) &&
                    string.Equals(existing.CompatibilityVersion, envelope.CompatibilityVersion, StringComparison.Ordinal);

                if (!same)
                {
                    throw new AuthoritySnapshotException(
                        "AUTHORITY_VERSION_PAYLOAD_CONFLICT",
                        "The same authority version already exists with different evidence.");
                }

                transaction.Commit();
                return new AuthoritySnapshotImportResult(
                    envelope.AuthorityVersion,
                    existing.Status,
                    payloadHash,
                    AlreadyKnown: true,
                    Activated: string.Equals(existing.Status, "ACTIVE", StringComparison.Ordinal));
            }

            await ExecuteAsync(connection, transaction, """
                INSERT INTO authority_snapshots(
                  authority_version, scope_json, source_checkpoint,
                  imported_at, compatibility_version, status, payload_json
                ) VALUES ($version, $scope, $checkpoint, $importedAt, $compatibility, 'STAGING', $payload)
                """,
                cancellationToken,
                ("$version", envelope.AuthorityVersion),
                ("$scope", envelope.ScopeJson),
                ("$checkpoint", envelope.SourceCheckpoint),
                ("$importedAt", importedAt),
                ("$compatibility", envelope.CompatibilityVersion),
                ("$payload", envelope.PayloadJson));

            await ExecuteAsync(connection, transaction,
                "UPDATE authority_snapshots SET status='VERIFIED' WHERE authority_version=$version AND status='STAGING'",
                cancellationToken,
                ("$version", envelope.AuthorityVersion));

            await ExecuteAsync(connection, transaction,
                "UPDATE authority_snapshots SET status='REPLACED' WHERE status='ACTIVE'",
                cancellationToken);

            await ExecuteAsync(connection, transaction,
                "UPDATE authority_snapshots SET status='ACTIVE' WHERE authority_version=$version AND status='VERIFIED'",
                cancellationToken,
                ("$version", envelope.AuthorityVersion));

            await UpsertMetaAsync(connection, transaction, "authority_snapshot_version", envelope.AuthorityVersion, importedAt, cancellationToken);
            await UpsertMetaAsync(connection, transaction, "authority_snapshot_checkpoint", envelope.SourceCheckpoint, importedAt, cancellationToken);
            await UpsertMetaAsync(connection, transaction, "authority_snapshot_imported_at", importedAt, importedAt, cancellationToken);
            await UpsertMetaAsync(connection, transaction, "authority_snapshot_payload_sha256", payloadHash, importedAt, cancellationToken);

            var active = await ReadActiveVersionAsync(connection, transaction, cancellationToken);
            if (!string.Equals(active, envelope.AuthorityVersion, StringComparison.Ordinal))
            {
                throw new AuthoritySnapshotException("AUTHORITY_ACTIVATION_FAILED", "Authority snapshot did not become the sole active generation.");
            }

            transaction.Commit();
            return new AuthoritySnapshotImportResult(
                envelope.AuthorityVersion,
                "ACTIVE",
                payloadHash,
                AlreadyKnown: false,
                Activated: true);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<string?> ReadActiveVersionAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await ReadActiveVersionAsync(connection, transaction: null, cancellationToken);
    }

    public async Task<string?> ReadStatusAsync(string authorityVersion, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT status FROM authority_snapshots WHERE authority_version=$version LIMIT 1";
        command.Parameters.AddWithValue("$version", authorityVersion);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000;";
        await pragma.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }

    private static void ValidateEnvelope(
        AuthoritySnapshotEnvelope envelope,
        string expectedEnvironment,
        string expectedClusterId,
        string expectedDomainContractVersion)
    {
        if (envelope is null) throw new AuthoritySnapshotException("AUTHORITY_ENVELOPE_REQUIRED", "Authority snapshot envelope is required.");
        RequireBounded(envelope.AuthorityVersion, "AUTHORITY_VERSION_REQUIRED", 1, 160);
        RequireBounded(envelope.SourceCheckpoint, "AUTHORITY_CHECKPOINT_REQUIRED", 1, 300);
        RequireBounded(envelope.CompatibilityVersion, "AUTHORITY_COMPATIBILITY_REQUIRED", 1, 160);
        RequireBounded(envelope.Environment, "AUTHORITY_ENVIRONMENT_REQUIRED", 1, 32);
        RequireBounded(envelope.ClusterId, "AUTHORITY_CLUSTER_REQUIRED", 1, 160);

        if (!string.Equals(envelope.Environment, expectedEnvironment, StringComparison.Ordinal))
            throw new AuthoritySnapshotException("AUTHORITY_ENVIRONMENT_MISMATCH", "Snapshot environment does not match LAN runtime.");
        if (!string.Equals(envelope.ClusterId, expectedClusterId, StringComparison.Ordinal))
            throw new AuthoritySnapshotException("AUTHORITY_CLUSTER_MISMATCH", "Snapshot cluster does not match LAN runtime.");
        if (!string.Equals(envelope.CompatibilityVersion, expectedDomainContractVersion, StringComparison.Ordinal))
            throw new AuthoritySnapshotException("AUTHORITY_INCOMPATIBLE", "Snapshot compatibility version is not executable by this LAN runtime.");

        ValidateJsonObject(envelope.ScopeJson, "AUTHORITY_SCOPE_INVALID");
        ValidateJsonObject(envelope.PayloadJson, "AUTHORITY_PAYLOAD_INVALID");
    }

    private static void RequireBounded(string? value, string code, int min, int max)
    {
        var length = value?.Length ?? 0;
        if (length < min || length > max) throw new AuthoritySnapshotException(code, code);
    }

    private static void ValidateJsonObject(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new AuthoritySnapshotException(code, code);
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new AuthoritySnapshotException(code, "Snapshot JSON must be an object.");
        }
        catch (AuthoritySnapshotException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw new AuthoritySnapshotException(code, "Snapshot JSON is invalid.");
        }
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static async Task EnsureActiveIndexAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS ux_authority_snapshot_active ON authority_snapshots(status) WHERE status='ACTIVE'";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<AuthoritySnapshotRow?> ReadSnapshotAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string authorityVersion,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT scope_json, source_checkpoint, compatibility_version, status, payload_json
            FROM authority_snapshots
            WHERE authority_version=$version
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$version", authorityVersion);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new AuthoritySnapshotRow(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4));
    }

    private static async Task<string?> ReadActiveVersionAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT authority_version FROM authority_snapshots WHERE status='ACTIVE' LIMIT 1";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertMetaAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string key,
        string value,
        string updatedAt,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(connection, transaction, """
            INSERT INTO edge_meta(meta_key, meta_value, updated_at)
            VALUES ($key, $value, $updatedAt)
            ON CONFLICT(meta_key) DO UPDATE SET meta_value=excluded.meta_value, updated_at=excluded.updated_at
            """,
            cancellationToken,
            ("$key", key),
            ("$value", value),
            ("$updatedAt", updatedAt));
    }

    private sealed record AuthoritySnapshotRow(
        string ScopeJson,
        string SourceCheckpoint,
        string CompatibilityVersion,
        string Status,
        string PayloadJson);
}

public sealed record AuthoritySnapshotEnvelope(
    string AuthorityVersion,
    string Environment,
    string ClusterId,
    string SourceCheckpoint,
    string CompatibilityVersion,
    string ScopeJson,
    string PayloadJson);

public sealed record AuthoritySnapshotImportResult(
    string AuthorityVersion,
    string Status,
    string PayloadSha256,
    bool AlreadyKnown,
    bool Activated);

public sealed class AuthoritySnapshotException : InvalidOperationException
{
    public AuthoritySnapshotException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}
