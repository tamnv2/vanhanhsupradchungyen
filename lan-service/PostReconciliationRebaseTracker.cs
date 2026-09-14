using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class PostReconciliationRebaseTracker
{
    public const string ContractVersion = "VHDCHY_POST_RECONCILIATION_REBASE_V1";
    public const string CursorMetaKey = "post_reconciliation_rebase_cursor_v1";

    private const int MaxCoverageIds = 10_000;
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

    public async Task<PostReconciliationRebaseInspection> InspectAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var reconciled = await ReadReconciledEventsAsync(connection, transaction: null, cancellationToken);
        var cursor = await ReadCursorAsync(connection, transaction: null, cancellationToken);
        var pending = PendingAfterCursor(reconciled, cursor);
        return new PostReconciliationRebaseInspection(
            Required: pending.Count != 0,
            PendingCanonicalEventCount: pending.Count,
            RequiredAt: pending.Count == 0 ? null : pending.Max(item => item.ReconciledAt),
            CanonicalEventIds: pending.Select(item => item.CanonicalEventId).ToArray());
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
        if (covered.Length == 0 || covered.Length > MaxCoverageIds || covered.Distinct(StringComparer.Ordinal).Count() != covered.Length)
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_COVERAGE_INVALID",
                "Canonical coverage evidence must be non-empty, bounded, and contain no duplicate event IDs.");
        }

        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var reconciled = await ReadReconciledEventsAsync(connection, transaction, cancellationToken);
        var currentCursor = await ReadCursorAsync(connection, transaction, cancellationToken);
        var pending = PendingAfterCursor(reconciled, currentCursor);
        if (pending.Count == 0)
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

        var latestRequiredAt = pending.Max(item => item.ReconciledAt);
        if (snapshot.ImportedAt < latestRequiredAt)
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_SNAPSHOT_STALE",
                "The active operational snapshot predates reconciliation evidence that still requires local rebase.");
        }

        var coverage = covered.ToHashSet(StringComparer.Ordinal);
        var missing = pending
            .Select(item => item.CanonicalEventId)
            .Where(id => !coverage.Contains(id))
            .ToArray();
        if (missing.Length != 0)
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_COVERAGE_MISSING",
                "The operational refresh does not prove coverage of every canonical event awaiting local rebase.");
        }

        var boundaryIds = reconciled
            .Where(item => item.ReconciledAt == latestRequiredAt)
            .Select(item => item.EdgeEventId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (boundaryIds.Length == 0 || boundaryIds.Length > MaxCoverageIds)
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_CURSOR_INVALID",
                "Unable to persist a bounded post-reconciliation rebase cursor.");
        }

        var newCursor = new PostReconciliationRebaseCursor(
            ContractVersion,
            latestRequiredAt,
            boundaryIds);
        var now = DateTimeOffset.UtcNow;
        await UpsertMetaAsync(
            connection,
            transaction,
            CursorMetaKey,
            JsonSerializer.Serialize(newCursor, JsonOptions),
            now,
            cancellationToken);
        await UpsertMetaAsync(connection, transaction, "post_reconciliation_rebase_last_snapshot_version", snapshotVersion, now, cancellationToken);
        await UpsertMetaAsync(connection, transaction, "post_reconciliation_rebase_last_source_checkpoint", sourceCheckpoint, now, cancellationToken);
        await UpsertMetaAsync(connection, transaction, "post_reconciliation_rebase_last_completed_at", now.ToString("O"), now, cancellationToken);
        await UpsertMetaAsync(connection, transaction, "post_reconciliation_rebase_last_covered_count", pending.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), now, cancellationToken);
        transaction.Commit();

        return new PostReconciliationRebaseConfirmResult(true, false, pending.Count, snapshotVersion);
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

    private static async Task<IReadOnlyList<PostReconciliationReconciledEvent>> ReadReconciledEventsAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var result = new List<PostReconciliationReconciledEvent>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT event_id, canonical_event_id, canonical_committed_at, updated_at
            FROM edge_reconciliation_state
            WHERE state='RECONCILED'
              AND canonical_event_id IS NOT NULL
              AND canonical_committed_at IS NOT NULL
            ORDER BY updated_at, event_id
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!DateTimeOffset.TryParse(reader.GetString(2), out var canonicalCommittedAt) ||
                !DateTimeOffset.TryParse(reader.GetString(3), out var reconciledAt))
            {
                throw new PostReconciliationRebaseException(
                    "POST_RECONCILIATION_REBASE_STATE_INVALID",
                    "Persisted reconciliation timestamp evidence is invalid.");
            }
            result.Add(new PostReconciliationReconciledEvent(
                reader.GetString(0),
                reader.GetString(1),
                canonicalCommittedAt,
                reconciledAt));
        }
        return result;
    }

    private static IReadOnlyList<PostReconciliationReconciledEvent> PendingAfterCursor(
        IReadOnlyList<PostReconciliationReconciledEvent> reconciled,
        PostReconciliationRebaseCursor? cursor)
    {
        if (cursor is null) return reconciled.ToArray();
        var boundaryIds = cursor.CoveredEdgeEventIdsAtBoundary.ToHashSet(StringComparer.Ordinal);
        return reconciled
            .Where(item =>
                item.ReconciledAt > cursor.CoveredThroughReconciliationAt ||
                (item.ReconciledAt == cursor.CoveredThroughReconciliationAt && !boundaryIds.Contains(item.EdgeEventId)))
            .ToArray();
    }

    private static async Task<PostReconciliationRebaseCursor?> ReadCursorAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT meta_value FROM edge_meta WHERE meta_key=$key LIMIT 1";
        command.Parameters.AddWithValue("$key", CursorMetaKey);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null or DBNull) return null;

        try
        {
            var cursor = JsonSerializer.Deserialize<PostReconciliationRebaseCursor>(Convert.ToString(value)!, JsonOptions);
            if (cursor is null ||
                !string.Equals(cursor.Version, ContractVersion, StringComparison.Ordinal) ||
                cursor.CoveredEdgeEventIdsAtBoundary is null ||
                cursor.CoveredEdgeEventIdsAtBoundary.Count == 0 ||
                cursor.CoveredEdgeEventIdsAtBoundary.Count > MaxCoverageIds ||
                cursor.CoveredEdgeEventIdsAtBoundary.Distinct(StringComparer.Ordinal).Count() != cursor.CoveredEdgeEventIdsAtBoundary.Count)
            {
                throw new InvalidOperationException();
            }
            return cursor;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            throw new PostReconciliationRebaseException(
                "POST_RECONCILIATION_REBASE_CURSOR_INVALID",
                "Persisted post-reconciliation rebase cursor is invalid.",
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

    private static string RequireText(string? value, string name, int max)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0 || normalized.Length > max)
            throw new ArgumentException($"{name} is required and must be <= {max} characters.", name);
        return normalized;
    }

    private sealed record ActiveSnapshotEvidence(string SourceCheckpoint, DateTimeOffset ImportedAt);
}

public sealed record PostReconciliationReconciledEvent(
    string EdgeEventId,
    string CanonicalEventId,
    DateTimeOffset CanonicalCommittedAt,
    DateTimeOffset ReconciledAt);

public sealed record PostReconciliationRebaseCursor(
    string Version,
    DateTimeOffset CoveredThroughReconciliationAt,
    IReadOnlyList<string> CoveredEdgeEventIdsAtBoundary);

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
