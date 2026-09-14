using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class PostReconciliationRebaseTracker
{
    public const string ContractVersion = "VHDCHY_POST_RECONCILIATION_REBASE_V1";
    public const string RequiredMetaKey = "post_reconciliation_rebase_required_v1";

    private const int MaxPendingCanonicalEvents = 10_000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public PostReconciliationRebaseTracker(string databasePath)
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

    public async Task MarkRequiredAsync(
        string edgeEventId,
        string canonicalEventId,
        DateTimeOffset canonicalCommittedAt,
        CancellationToken cancellationToken = default)
    {
        edgeEventId = RequireText(edgeEventId, nameof(edgeEventId), 240);
        canonicalEventId = RequireText(canonicalEventId, nameof(canonicalEventId), 240);
        var now = DateTimeOffset.UtcNow;

        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        var existing = await ReadRequiredStateAsync(connection, transaction, cancellationToken);
        var pending = existing?.Events.ToList() ?? new List<PostReconciliationPendingCanonicalEvent>();
        var sameEdge = pending.SingleOrDefault(item => string.Equals(item.EdgeEventId, edgeEventId, StringComparison.Ordinal));
        if (sameEdge is not null)
        {
            if (!string.Equals(sameEdge.CanonicalEventId, canonicalEventId, StringComparison.Ordinal) ||
                sameEdge.CanonicalCommittedAt.ToUniversalTime() != canonicalCommittedAt.ToUniversalTime())
            {
                throw new PostReconciliationRebaseException(
                    "POST_RECONCILIATION_REBASE_IDENTITY_CONFLICT",
                    "The same edge event was observed with different canonical reconciliation evidence.");
            }
        }
        else
        {
            pending.Add(new PostReconciliationPendingCanonicalEvent(
                edgeEventId,
                canonicalEventId,
                canonicalCommittedAt.ToUniversalTime()));
        }

        if (pending.Count > MaxPendingCanonicalEvents)
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_BACKLOG_LIMIT",
                "Post-reconciliation rebase backlog exceeded the reviewed evidence bound.");
        }

        var state = new PostReconciliationRequiredState(
            ContractVersion,
            now,
            pending
                .OrderBy(item => item.CanonicalCommittedAt)
                .ThenBy(item => item.CanonicalEventId, StringComparer.Ordinal)
                .ToArray());

        await UpsertMetaAsync(
            connection,
            transaction,
            RequiredMetaKey,
            JsonSerializer.Serialize(state, JsonOptions),
            now,
            cancellationToken);
        transaction.Commit();
    }

    public async Task<PostReconciliationRebaseInspection> InspectAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var state = await ReadRequiredStateAsync(connection, transaction: null, cancellationToken);
        return state is null
            ? new PostReconciliationRebaseInspection(false, 0, null, Array.Empty<string>())
            : new PostReconciliationRebaseInspection(
                true,
                state.Events.Count,
                state.RequiredAt,
                state.Events.Select(item => item.CanonicalEventId).ToArray());
    }

    public async Task<PostReconciliationRebaseConfirmResult> ConfirmAsync(
        PostReconciliationRebaseEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var snapshotVersion = RequireText(evidence.SnapshotVersion, nameof(evidence.SnapshotVersion), 160);
        var sourceCheckpoint = RequireText(evidence.SourceCheckpoint, nameof(evidence.SourceCheckpoint), 300);
        var covered = (evidence.CoveredCanonicalEventIds ?? Array.Empty<string>())
            .Select(value => RequireText(value, nameof(evidence.CoveredCanonicalEventIds), 240))
            .ToArray();
        if (covered.Length == 0 || covered.Length > MaxPendingCanonicalEvents || covered.Distinct(StringComparer.Ordinal).Count() != covered.Length)
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_COVERAGE_INVALID",
                "Canonical coverage evidence must be non-empty, bounded, and contain no duplicate event IDs.");
        }

        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var required = await ReadRequiredStateAsync(connection, transaction, cancellationToken);
        if (required is null)
        {
            transaction.Commit();
            return new PostReconciliationRebaseConfirmResult(false, true, 0, snapshotVersion);
        }

        var snapshot = await ReadActiveSnapshotAsync(connection, transaction, snapshotVersion, cancellationToken)
            ?? throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_ACTIVE_SNAPSHOT_REQUIRED",
                "The supplied operational snapshot is not the active LAN snapshot.");

        if (!string.Equals(snapshot.SourceCheckpoint, sourceCheckpoint, StringComparison.Ordinal))
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_CHECKPOINT_MISMATCH",
                "The supplied source checkpoint does not match the active operational snapshot evidence.");
        }

        if (snapshot.ImportedAt < required.RequiredAt)
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_SNAPSHOT_STALE",
                "The active operational snapshot predates the latest canonical reconciliation acknowledgement and cannot clear rebase-required state.");
        }

        var coverage = covered.ToHashSet(StringComparer.Ordinal);
        var missing = required.Events
            .Select(item => item.CanonicalEventId)
            .Where(id => !coverage.Contains(id))
            .ToArray();
        if (missing.Length != 0)
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_COVERAGE_MISSING",
                "The operational refresh does not prove coverage of every canonical event awaiting local rebase.");
        }

        var now = DateTimeOffset.UtcNow;
        await DeleteMetaAsync(connection, transaction, RequiredMetaKey, cancellationToken);
        await UpsertMetaAsync(connection, transaction, "post_reconciliation_rebase_last_snapshot_version", snapshotVersion, now, cancellationToken);
        await UpsertMetaAsync(connection, transaction, "post_reconciliation_rebase_last_source_checkpoint", sourceCheckpoint, now, cancellationToken);
        await UpsertMetaAsync(connection, transaction, "post_reconciliation_rebase_last_completed_at", now.ToString("O"), now, cancellationToken);
        await UpsertMetaAsync(connection, transaction, "post_reconciliation_rebase_last_covered_count", required.Events.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), now, cancellationToken);
        transaction.Commit();

        return new PostReconciliationRebaseConfirmResult(true, false, required.Events.Count, snapshotVersion);
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

    private static async Task<PostReconciliationRequiredState?> ReadRequiredStateAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT meta_value FROM edge_meta WHERE meta_key=$key LIMIT 1";
        command.Parameters.AddWithValue("$key", RequiredMetaKey);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null or DBNull) return null;

        try
        {
            var state = JsonSerializer.Deserialize<PostReconciliationRequiredState>(Convert.ToString(value)!, JsonOptions);
            if (state is null ||
                !string.Equals(state.Version, ContractVersion, StringComparison.Ordinal) ||
                state.Events is null ||
                state.Events.Count == 0 ||
                state.Events.Count > MaxPendingCanonicalEvents ||
                state.Events.Select(item => item.EdgeEventId).Distinct(StringComparer.Ordinal).Count() != state.Events.Count ||
                state.Events.Select(item => item.CanonicalEventId).Distinct(StringComparer.Ordinal).Count() != state.Events.Count)
            {
                throw new InvalidOperationException();
            }
            return state;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_STATE_INVALID",
                "Persisted post-reconciliation rebase evidence is invalid.",
                error);
        }
    }

    private static async Task<ActiveSnapshotEvidence?> ReadActiveSnapshotAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string snapshotVersion,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT source_checkpoint, imported_at
            FROM operational_snapshot_state
            WHERE snapshot_version=$version AND status='ACTIVE'
            LIMIT 2
            """;
        command.Parameters.AddWithValue("$version", snapshotVersion);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var sourceCheckpoint = reader.GetString(0);
        if (!DateTimeOffset.TryParse(reader.GetString(1), out var importedAt))
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_SNAPSHOT_TIME_INVALID",
                "Active operational snapshot imported_at evidence is invalid.");
        }
        if (await reader.ReadAsync(cancellationToken))
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_ACTIVE_SET_INVALID",
                "More than one active operational snapshot matches the requested version.");
        }
        return new ActiveSnapshotEvidence(sourceCheckpoint, importedAt);
    }

    private static async Task UpsertMetaAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string key,
        string value,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO edge_meta(meta_key, meta_value, updated_at)
            VALUES ($key, $value, $updatedAt)
            ON CONFLICT(meta_key) DO UPDATE SET meta_value=excluded.meta_value, updated_at=excluded.updated_at
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.Parameters.AddWithValue("$updatedAt", updatedAt.ToUniversalTime().ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteMetaAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string key,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM edge_meta WHERE meta_key=$key";
        command.Parameters.AddWithValue("$key", key);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string RequireText(string? value, string name, int max)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0 || normalized.Length > max)
            throw new ArgumentException($"{name} is required and must be <= {max} characters.", name);
        return normalized;
    }

    private sealed record ActiveSnapshotEvidence(string SourceCheckpoint, DateTimeOffset ImportedAt);
}

public sealed record PostReconciliationPendingCanonicalEvent(
    string EdgeEventId,
    string CanonicalEventId,
    DateTimeOffset CanonicalCommittedAt);

public sealed record PostReconciliationRequiredState(
    string Version,
    DateTimeOffset RequiredAt,
    IReadOnlyList<PostReconciliationPendingCanonicalEvent> Events);

public sealed record PostReconciliationRebaseInspection(
    bool Required,
    int PendingCanonicalEventCount,
    DateTimeOffset? RequiredAt,
    IReadOnlyList<string> CanonicalEventIds);

public sealed record PostReconciliationRebaseEvidence(
    string SnapshotVersion,
    string SourceCheckpoint,
    IReadOnlyList<string> CoveredCanonicalEventIds);

public sealed record PostReconciliationRebaseConfirmResult(
    bool Cleared,
    bool AlreadyComplete,
    int CoveredPendingCanonicalEventCount,
    string SnapshotVersion);

public sealed class PostReconciliationRebaseException : InvalidOperationException
{
    public PostReconciliationRebaseException(string code, string message) : base(message) => Code = code;
    public PostReconciliationRebaseException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
