using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class LanReadinessEvaluator
{
    private readonly string _connectionString;
    private readonly string _expectedEnvironment;
    private readonly string _expectedClusterId;
    private readonly string _expectedDomainContractVersion;
    private readonly string[] _requiredModules;
    private readonly LanAuthorizationEvaluator _authorizationEvaluator;

    public LanReadinessEvaluator(
        string databasePath,
        string expectedEnvironment,
        string expectedClusterId,
        string expectedDomainContractVersion,
        IEnumerable<string> requiredModules)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        if (string.IsNullOrWhiteSpace(expectedEnvironment)) throw new ArgumentException("Environment is required", nameof(expectedEnvironment));
        if (string.IsNullOrWhiteSpace(expectedClusterId)) throw new ArgumentException("Cluster ID is required", nameof(expectedClusterId));
        if (string.IsNullOrWhiteSpace(expectedDomainContractVersion)) throw new ArgumentException("Domain contract version is required", nameof(expectedDomainContractVersion));

        _requiredModules = requiredModules
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (_requiredModules.Length == 0) throw new ArgumentException("At least one required module is required", nameof(requiredModules));

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
        _expectedEnvironment = expectedEnvironment;
        _expectedClusterId = expectedClusterId;
        _expectedDomainContractVersion = expectedDomainContractVersion;
        _authorizationEvaluator = new LanAuthorizationEvaluator(databasePath, expectedDomainContractVersion);
    }

    public async Task<LanReadinessReport> EvaluateAsync(CancellationToken cancellationToken = default)
    {
        var blockers = new List<LanReadinessBlocker>();
        string? authorityVersion = null;
        string? operationalVersion = null;
        string? operationalAuthorityVersion = null;

        await using var connection = await OpenAsync(cancellationToken);
        var meta = await ReadMetaAsync(connection, cancellationToken);
        ValidateRuntimeMeta(meta, blockers);

        LanAuthoritySnapshotInspection authorityInspection;
        try
        {
            authorityInspection = await _authorizationEvaluator.InspectActiveSnapshotAsync(cancellationToken);
        }
        catch (LanAuthorityModelException error)
        {
            authorityInspection = new LanAuthoritySnapshotInspection(false, null, null, null, error.Code);
        }

        authorityVersion = authorityInspection.AuthoritySnapshotVersion;
        if (!authorityInspection.Ready)
        {
            blockers.Add(new LanReadinessBlocker(
                authorityInspection.Code,
                "Synchronized authority snapshot is missing, incompatible, or invalid."));
        }

        ActiveOperationalSnapshot? operational = null;
        try
        {
            operational = await ReadActiveOperationalAsync(connection, cancellationToken);
        }
        catch (LanReadinessException error)
        {
            blockers.Add(new LanReadinessBlocker(error.Code, error.Message));
        }

        if (operational is null)
        {
            if (!blockers.Any(blocker => blocker.Code == "OPERATIONAL_ACTIVE_SET_INVALID"))
            {
                blockers.Add(new LanReadinessBlocker(
                    "OPERATIONAL_SNAPSHOT_REQUIRED",
                    "No active operational snapshot is available."));
            }
        }
        else
        {
            operationalVersion = operational.SnapshotVersion;
            if (!string.Equals(operational.CompatibilityVersion, _expectedDomainContractVersion, StringComparison.Ordinal))
            {
                blockers.Add(new LanReadinessBlocker(
                    "OPERATIONAL_INCOMPATIBLE",
                    "Active operational snapshot is incompatible with this LAN runtime."));
            }

            try
            {
                var modules = ReadOperationalModules(operational.PayloadJson);
                foreach (var requiredModule in _requiredModules)
                {
                    if (!modules.Contains(requiredModule, StringComparer.Ordinal))
                    {
                        blockers.Add(new LanReadinessBlocker(
                            "OPERATIONAL_REQUIRED_MODULE_MISSING",
                            $"Active operational snapshot does not cover required module: {requiredModule}."));
                    }
                }
            }
            catch (LanReadinessException error)
            {
                blockers.Add(new LanReadinessBlocker(error.Code, error.Message));
            }
        }

        meta.TryGetValue("operational_snapshot_authority_version", out operationalAuthorityVersion);
        if (authorityVersion is not null && operationalVersion is not null)
        {
            if (string.IsNullOrWhiteSpace(operationalAuthorityVersion))
            {
                blockers.Add(new LanReadinessBlocker(
                    "OPERATIONAL_AUTHORITY_LINK_REQUIRED",
                    "Operational snapshot does not record the authority generation under which it was activated."));
            }
            else if (!string.Equals(operationalAuthorityVersion, authorityVersion, StringComparison.Ordinal))
            {
                blockers.Add(new LanReadinessBlocker(
                    "OPERATIONAL_AUTHORITY_STALE",
                    "Operational snapshot was activated under a different authority generation and must be refreshed."));
            }
        }

        var snapshotPrerequisitesReady = blockers.Count == 0;

        // This gate is deliberately not caller-overridable. A future reviewed Slice-1 business
        // adapter must be linked here by code and CI evidence before EDGE_READY can ever be returned.
        blockers.Add(new LanReadinessBlocker(
            "SLICE_ADAPTER_REQUIRED",
            "A reviewed Slice-1 business/domain adapter is not yet linked to the readiness gate."));

        return new LanReadinessReport(
            Ready: false,
            Readiness: "EDGE_NOT_READY",
            SnapshotPrerequisitesReady: snapshotPrerequisitesReady,
            AuthoritySnapshotVersion: authorityVersion,
            OperationalSnapshotVersion: operationalVersion,
            OperationalAuthoritySnapshotVersion: operationalAuthorityVersion,
            RequiredModules: _requiredModules,
            Blockers: blockers);
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

    private void ValidateRuntimeMeta(
        IReadOnlyDictionary<string, string?> meta,
        ICollection<LanReadinessBlocker> blockers)
    {
        if (!meta.TryGetValue("environment", out var environment) || !string.Equals(environment, _expectedEnvironment, StringComparison.Ordinal))
        {
            blockers.Add(new LanReadinessBlocker("RUNTIME_ENVIRONMENT_MISMATCH", "Edge environment metadata does not match runtime configuration."));
        }
        if (!meta.TryGetValue("cluster_id", out var cluster) || !string.Equals(cluster, _expectedClusterId, StringComparison.Ordinal))
        {
            blockers.Add(new LanReadinessBlocker("RUNTIME_CLUSTER_MISMATCH", "Edge cluster metadata does not match runtime configuration."));
        }
        if (!meta.TryGetValue("domain_contract_version", out var domain) || !string.Equals(domain, _expectedDomainContractVersion, StringComparison.Ordinal))
        {
            blockers.Add(new LanReadinessBlocker("RUNTIME_DOMAIN_INCOMPATIBLE", "Edge domain contract metadata is incompatible."));
        }
    }

    private static async Task<Dictionary<string, string?>> ReadMetaAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT meta_key, meta_value
            FROM edge_meta
            WHERE meta_key IN (
              'environment',
              'cluster_id',
              'domain_contract_version',
              'operational_snapshot_authority_version'
            )
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result[reader.GetString(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
        }
        return result;
    }

    private static async Task<ActiveOperationalSnapshot?> ReadActiveOperationalAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT snapshot_version, compatibility_version, payload_json
            FROM operational_snapshot_state
            WHERE status='ACTIVE'
            ORDER BY imported_at DESC
            LIMIT 2
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var snapshot = new ActiveOperationalSnapshot(reader.GetString(0), reader.GetString(1), reader.GetString(2));
        if (await reader.ReadAsync(cancellationToken))
        {
            throw new LanReadinessException("OPERATIONAL_ACTIVE_SET_INVALID", "More than one active operational snapshot exists.");
        }
        return snapshot;
    }

    private static string[] ReadOperationalModules(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("scope", out var scope) ||
                scope.ValueKind != JsonValueKind.Object ||
                !scope.TryGetProperty("modules", out var modules) ||
                modules.ValueKind != JsonValueKind.Array)
            {
                throw new LanReadinessException(
                    "OPERATIONAL_SCOPE_INVALID",
                    "Active operational snapshot evidence does not contain a valid scope.modules array.");
            }

            var result = new List<string>();
            foreach (var module in modules.EnumerateArray())
            {
                if (module.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(module.GetString()))
                {
                    throw new LanReadinessException("OPERATIONAL_SCOPE_INVALID", "Operational module identifier is invalid.");
                }
                result.Add(module.GetString()!);
            }
            return result.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }
        catch (LanReadinessException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new LanReadinessException("OPERATIONAL_SCOPE_INVALID", "Operational snapshot evidence JSON is invalid.", error);
        }
    }

    private sealed record ActiveOperationalSnapshot(
        string SnapshotVersion,
        string CompatibilityVersion,
        string PayloadJson);
}

public sealed record LanReadinessReport(
    bool Ready,
    string Readiness,
    bool SnapshotPrerequisitesReady,
    string? AuthoritySnapshotVersion,
    string? OperationalSnapshotVersion,
    string? OperationalAuthoritySnapshotVersion,
    IReadOnlyList<string> RequiredModules,
    IReadOnlyList<LanReadinessBlocker> Blockers);

public sealed record LanReadinessBlocker(string Code, string Message);

public sealed class LanReadinessException : InvalidOperationException
{
    public LanReadinessException(string code, string message) : base(message) => Code = code;
    public LanReadinessException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
