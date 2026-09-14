using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class OperationalSnapshotStore
{
    private readonly string _connectionString;

    public OperationalSnapshotStore(string databasePath)
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

    public async Task<OperationalSnapshotImportResult> ImportAsync(
        OperationalSnapshotEnvelope envelope,
        string expectedEnvironment,
        string expectedClusterId,
        string expectedDomainContractVersion,
        IReadOnlyCollection<string> requiredModules,
        CancellationToken cancellationToken = default)
    {
        var validated = ValidateEnvelope(
            envelope,
            expectedEnvironment,
            expectedClusterId,
            expectedDomainContractVersion,
            requiredModules);
        var importedAt = DateTimeOffset.UtcNow.ToString("O");

        await using var connection = await OpenAsync(cancellationToken);
        await EnsureActiveIndexAsync(connection, cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var activeAuthority = await ReadCompatibleActiveAuthorityAsync(
                connection,
                transaction,
                expectedDomainContractVersion,
                cancellationToken);
            if (activeAuthority is null)
            {
                throw new OperationalSnapshotException(
                    "OPERATIONAL_AUTHORITY_REQUIRED",
                    "A compatible active authority snapshot is required before operational state can be activated.");
            }

            var existing = await ReadSnapshotAsync(connection, transaction, envelope.SnapshotVersion, cancellationToken);
            if (existing is not null)
            {
                var same =
                    string.Equals(existing.SourceCheckpoint, envelope.SourceCheckpoint, StringComparison.Ordinal) &&
                    string.Equals(existing.CompatibilityVersion, envelope.CompatibilityVersion, StringComparison.Ordinal) &&
                    string.Equals(Sha256Hex(existing.PayloadJson), validated.EvidenceHash, StringComparison.Ordinal);

                if (!same)
                {
                    throw new OperationalSnapshotException(
                        "OPERATIONAL_VERSION_PAYLOAD_CONFLICT",
                        "The same operational snapshot version already exists with different evidence.");
                }

                transaction.Commit();
                return new OperationalSnapshotImportResult(
                    SnapshotVersion: envelope.SnapshotVersion,
                    Status: existing.Status,
                    EvidenceSha256: validated.EvidenceHash,
                    ScopeSha256: validated.ScopeHash,
                    StateSha256: validated.StateHash,
                    AuthoritySnapshotVersion: activeAuthority,
                    AlreadyKnown: true,
                    Activated: string.Equals(existing.Status, "ACTIVE", StringComparison.Ordinal));
            }

            int? materializedStateCount = null;
            if (validated.ScopeModules.Contains(Slice1OperationalStateMaterializer.ModuleId, StringComparer.Ordinal))
            {
                materializedStateCount = await Slice1OperationalStateMaterializer.MaterializeAsync(
                    connection,
                    transaction,
                    validated.CanonicalStateJson,
                    importedAt,
                    cancellationToken);
            }

            await ExecuteAsync(connection, transaction, """
                INSERT INTO operational_snapshot_state(
                  snapshot_version, source_checkpoint, imported_at,
                  compatibility_version, status, payload_json
                ) VALUES (
                  $version, $checkpoint, $importedAt,
                  $compatibility, 'STAGING', $payload
                )
                """,
                cancellationToken,
                ("$version", envelope.SnapshotVersion),
                ("$checkpoint", envelope.SourceCheckpoint),
                ("$importedAt", importedAt),
                ("$compatibility", envelope.CompatibilityVersion),
                ("$payload", validated.EvidenceJson));

            await ExecuteAsync(connection, transaction,
                "UPDATE operational_snapshot_state SET status='VERIFIED' WHERE snapshot_version=$version AND status='STAGING'",
                cancellationToken,
                ("$version", envelope.SnapshotVersion));

            await ExecuteAsync(connection, transaction,
                "UPDATE operational_snapshot_state SET status='REPLACED' WHERE status='ACTIVE'",
                cancellationToken);

            await ExecuteAsync(connection, transaction,
                "UPDATE operational_snapshot_state SET status='ACTIVE' WHERE snapshot_version=$version AND status='VERIFIED'",
                cancellationToken,
                ("$version", envelope.SnapshotVersion));

            await UpsertMetaAsync(connection, transaction, "operational_snapshot_version", envelope.SnapshotVersion, importedAt, cancellationToken);
            await UpsertMetaAsync(connection, transaction, "operational_snapshot_checkpoint", envelope.SourceCheckpoint, importedAt, cancellationToken);
            await UpsertMetaAsync(connection, transaction, "operational_snapshot_imported_at", importedAt, importedAt, cancellationToken);
            await UpsertMetaAsync(connection, transaction, "operational_snapshot_evidence_sha256", validated.EvidenceHash, importedAt, cancellationToken);
            await UpsertMetaAsync(connection, transaction, "operational_snapshot_scope_sha256", validated.ScopeHash, importedAt, cancellationToken);
            await UpsertMetaAsync(connection, transaction, "operational_snapshot_state_sha256", validated.StateHash, importedAt, cancellationToken);
            await UpsertMetaAsync(connection, transaction, "operational_snapshot_authority_version", activeAuthority, importedAt, cancellationToken);

            if (materializedStateCount is not null)
            {
                await UpsertMetaAsync(
                    connection,
                    transaction,
                    "slice1_materialized_snapshot_version",
                    envelope.SnapshotVersion,
                    importedAt,
                    cancellationToken);
                await UpsertMetaAsync(
                    connection,
                    transaction,
                    "slice1_materialized_state_count",
                    materializedStateCount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    importedAt,
                    cancellationToken);
            }

            var active = await ReadActiveVersionAsync(connection, transaction, cancellationToken);
            if (!string.Equals(active, envelope.SnapshotVersion, StringComparison.Ordinal))
            {
                throw new OperationalSnapshotException(
                    "OPERATIONAL_ACTIVATION_FAILED",
                    "Operational snapshot did not become the sole active generation.");
            }

            // Snapshot activation and Slice-1 state materialization prove only synchronized state.
            // Readiness remains fail-closed until the reviewed business adapter is linked and proven.
            transaction.Commit();
            return new OperationalSnapshotImportResult(
                SnapshotVersion: envelope.SnapshotVersion,
                Status: "ACTIVE",
                EvidenceSha256: validated.EvidenceHash,
                ScopeSha256: validated.ScopeHash,
                StateSha256: validated.StateHash,
                AuthoritySnapshotVersion: activeAuthority,
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

    public async Task<string?> ReadStatusAsync(string snapshotVersion, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT status FROM operational_snapshot_state WHERE snapshot_version=$version LIMIT 1";
        command.Parameters.AddWithValue("$version", snapshotVersion);
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

    private static ValidatedSnapshot ValidateEnvelope(
        OperationalSnapshotEnvelope envelope,
        string expectedEnvironment,
        string expectedClusterId,
        string expectedDomainContractVersion,
        IReadOnlyCollection<string> requiredModules)
    {
        if (envelope is null) throw new OperationalSnapshotException("OPERATIONAL_ENVELOPE_REQUIRED", "Operational snapshot envelope is required.");
        RequireBounded(envelope.SnapshotVersion, "OPERATIONAL_VERSION_REQUIRED", 1, 160);
        RequireBounded(envelope.SourceCheckpoint, "OPERATIONAL_CHECKPOINT_REQUIRED", 1, 300);
        RequireBounded(envelope.CompatibilityVersion, "OPERATIONAL_COMPATIBILITY_REQUIRED", 1, 160);
        RequireBounded(envelope.Environment, "OPERATIONAL_ENVIRONMENT_REQUIRED", 1, 32);
        RequireBounded(envelope.ClusterId, "OPERATIONAL_CLUSTER_REQUIRED", 1, 160);

        if (!string.Equals(envelope.Environment, expectedEnvironment, StringComparison.Ordinal))
            throw new OperationalSnapshotException("OPERATIONAL_ENVIRONMENT_MISMATCH", "Snapshot environment does not match LAN runtime.");
        if (!string.Equals(envelope.ClusterId, expectedClusterId, StringComparison.Ordinal))
            throw new OperationalSnapshotException("OPERATIONAL_CLUSTER_MISMATCH", "Snapshot cluster does not match LAN runtime.");
        if (!string.Equals(envelope.CompatibilityVersion, expectedDomainContractVersion, StringComparison.Ordinal))
            throw new OperationalSnapshotException("OPERATIONAL_INCOMPATIBLE", "Snapshot compatibility version is not executable by this LAN runtime.");

        var canonicalScope = CanonicalizeJsonObject(envelope.ScopeJson, "OPERATIONAL_SCOPE_INVALID");
        var canonicalState = CanonicalizeJsonObject(envelope.StateJson, "OPERATIONAL_STATE_INVALID");
        var scopeModules = ReadScopeModules(canonicalScope);
        var required = requiredModules
            .Where(module => !string.IsNullOrWhiteSpace(module))
            .Select(module => module.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(module => module, StringComparer.Ordinal)
            .ToArray();

        foreach (var requiredModule in required)
        {
            if (!scopeModules.Contains(requiredModule, StringComparer.Ordinal))
            {
                throw new OperationalSnapshotException(
                    "OPERATIONAL_REQUIRED_MODULE_MISSING",
                    $"Operational snapshot scope does not contain required module: {requiredModule}.");
            }
        }

        var evidenceJson = BuildEvidenceJson(
            envelope.Environment,
            envelope.ClusterId,
            canonicalScope,
            canonicalState);

        return new ValidatedSnapshot(
            EvidenceJson: evidenceJson,
            EvidenceHash: Sha256Hex(evidenceJson),
            ScopeHash: Sha256Hex(canonicalScope),
            StateHash: Sha256Hex(canonicalState),
            CanonicalStateJson: canonicalState,
            ScopeModules: scopeModules);
    }

    private static string[] ReadScopeModules(string canonicalScope)
    {
        using var document = JsonDocument.Parse(canonicalScope);
        if (!document.RootElement.TryGetProperty("modules", out var modules) || modules.ValueKind != JsonValueKind.Array)
            throw new OperationalSnapshotException("OPERATIONAL_SCOPE_MODULES_REQUIRED", "Operational snapshot scope must contain a modules array.");

        var result = new List<string>();
        foreach (var module in modules.EnumerateArray())
        {
            if (module.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(module.GetString()))
                throw new OperationalSnapshotException("OPERATIONAL_SCOPE_MODULE_INVALID", "Every operational module identifier must be a non-empty string.");
            result.Add(module.GetString()!.Trim());
        }

        if (result.Count == 0)
            throw new OperationalSnapshotException("OPERATIONAL_SCOPE_MODULES_REQUIRED", "Operational snapshot must declare at least one module.");
        if (result.Distinct(StringComparer.Ordinal).Count() != result.Count)
            throw new OperationalSnapshotException("OPERATIONAL_SCOPE_MODULE_DUPLICATE", "Operational snapshot scope contains duplicate module identifiers.");
        return result.OrderBy(module => module, StringComparer.Ordinal).ToArray();
    }

    private static string BuildEvidenceJson(
        string environment,
        string clusterId,
        string canonicalScope,
        string canonicalState)
    {
        using var scope = JsonDocument.Parse(canonicalScope);
        using var state = JsonDocument.Parse(canonicalState);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("clusterId", clusterId);
            writer.WriteString("environment", environment);
            writer.WritePropertyName("scope");
            scope.RootElement.WriteTo(writer);
            writer.WritePropertyName("state");
            state.RootElement.WriteTo(writer);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CanonicalizeJsonObject(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new OperationalSnapshotException(code, code);
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new OperationalSnapshotException(code, "Snapshot JSON must be an object.");
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream)) WriteCanonical(document.RootElement, writer, code);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (OperationalSnapshotException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new OperationalSnapshotException(code, "Snapshot JSON is invalid.", error);
        }
    }

    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer, string code)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                var properties = element.EnumerateObject().ToArray();
                if (properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length)
                    throw new OperationalSnapshotException(code, "JSON object contains duplicate property names.");
                foreach (var property in properties.OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer, code);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteCanonical(item, writer, code);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText(), skipInputValidation: true);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new OperationalSnapshotException(code, "Unsupported JSON token.");
        }
    }

    private static async Task<string?> ReadCompatibleActiveAuthorityAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string expectedDomainContractVersion,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT authority_version, compatibility_version
            FROM authority_snapshots
            WHERE status='ACTIVE'
            ORDER BY imported_at DESC
            LIMIT 2
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var version = reader.GetString(0);
        var compatibility = reader.GetString(1);
        if (await reader.ReadAsync(cancellationToken))
            throw new OperationalSnapshotException("OPERATIONAL_AUTHORITY_SET_INVALID", "More than one active authority snapshot exists.");
        if (!string.Equals(compatibility, expectedDomainContractVersion, StringComparison.Ordinal))
            throw new OperationalSnapshotException("OPERATIONAL_AUTHORITY_INCOMPATIBLE", "The active authority snapshot is incompatible.");
        return version;
    }

    private static async Task EnsureActiveIndexAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS ux_operational_snapshot_active ON operational_snapshot_state(status) WHERE status='ACTIVE'";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<OperationalSnapshotRow?> ReadSnapshotAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string snapshotVersion,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT source_checkpoint, compatibility_version, status, payload_json
            FROM operational_snapshot_state
            WHERE snapshot_version=$version
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$version", snapshotVersion);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new OperationalSnapshotRow(
            SourceCheckpoint: reader.GetString(0),
            CompatibilityVersion: reader.GetString(1),
            Status: reader.GetString(2),
            PayloadJson: reader.GetString(3));
    }

    private static async Task<string?> ReadActiveVersionAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT snapshot_version FROM operational_snapshot_state WHERE status='ACTIVE' LIMIT 1";
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

    private static void RequireBounded(string? value, string code, int min, int max)
    {
        var length = value?.Length ?? 0;
        if (length < min || length > max) throw new OperationalSnapshotException(code, code);
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record ValidatedSnapshot(
        string EvidenceJson,
        string EvidenceHash,
        string ScopeHash,
        string StateHash,
        string CanonicalStateJson,
        IReadOnlyList<string> ScopeModules);

    private sealed record OperationalSnapshotRow(
        string SourceCheckpoint,
        string CompatibilityVersion,
        string Status,
        string PayloadJson);
}

public sealed record OperationalSnapshotEnvelope(
    string SnapshotVersion,
    string Environment,
    string ClusterId,
    string SourceCheckpoint,
    string CompatibilityVersion,
    string ScopeJson,
    string StateJson);

public sealed record OperationalSnapshotImportResult(
    string SnapshotVersion,
    string Status,
    string EvidenceSha256,
    string ScopeSha256,
    string StateSha256,
    string AuthoritySnapshotVersion,
    bool AlreadyKnown,
    bool Activated);

public sealed class OperationalSnapshotException : InvalidOperationException
{
    public OperationalSnapshotException(string code, string message) : base(message) => Code = code;
    public OperationalSnapshotException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
