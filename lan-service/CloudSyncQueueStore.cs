using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class CloudSyncQueueStore
{
    private readonly string _connectionString;

    public CloudSyncQueueStore(string databasePath)
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

    public async Task<int> RecoverInterruptedClaimsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE cloud_sync_outbox
            SET state='RETRY_WAIT',
                next_attempt_at=$now,
                last_error_code='SYNC_INTERRUPTED_RESTART',
                updated_at=$now
            WHERE state='SYNCHRONIZING'
            """;
        command.Parameters.AddWithValue("$now", now);
        var recovered = await command.ExecuteNonQueryAsync(cancellationToken);

        await using var reconciliation = connection.CreateCommand();
        reconciliation.Transaction = transaction;
        reconciliation.CommandText = """
            UPDATE edge_reconciliation_state
            SET state='PENDING',
                last_error_code='SYNC_INTERRUPTED_RESTART',
                updated_at=$now
            WHERE state='SYNCHRONIZING'
            """;
        reconciliation.Parameters.AddWithValue("$now", now);
        await reconciliation.ExecuteNonQueryAsync(cancellationToken);

        transaction.Commit();
        return recovered;
    }

    public async Task<IReadOnlyList<LanCloudSyncClaim>> ClaimDueAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 100) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100.");

        var now = DateTimeOffset.UtcNow.ToString("O");
        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        var candidateIds = new List<string>();
        await using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText = """
                SELECT outbox_id
                FROM cloud_sync_outbox
                WHERE state IN ('PENDING','RETRY_WAIT')
                  AND (next_attempt_at IS NULL OR next_attempt_at <= $now)
                ORDER BY created_at, outbox_id
                LIMIT $limit
                """;
            select.Parameters.AddWithValue("$now", now);
            select.Parameters.AddWithValue("$limit", limit);
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) candidateIds.Add(reader.GetString(0));
        }

        var claims = new List<LanCloudSyncClaim>(candidateIds.Count);
        foreach (var outboxId in candidateIds)
        {
            await using var claim = connection.CreateCommand();
            claim.Transaction = transaction;
            claim.CommandText = """
                UPDATE cloud_sync_outbox
                SET state='SYNCHRONIZING',
                    attempt_count=attempt_count+1,
                    last_attempt_at=$now,
                    next_attempt_at=NULL,
                    last_error_code=NULL,
                    updated_at=$now
                WHERE outbox_id=$outboxId
                  AND state IN ('PENDING','RETRY_WAIT')
                  AND (next_attempt_at IS NULL OR next_attempt_at <= $now)
                """;
            claim.Parameters.AddWithValue("$now", now);
            claim.Parameters.AddWithValue("$outboxId", outboxId);
            if (await claim.ExecuteNonQueryAsync(cancellationToken) != 1) continue;

            await using var reconcile = connection.CreateCommand();
            reconcile.Transaction = transaction;
            reconcile.CommandText = """
                UPDATE edge_reconciliation_state
                SET state='SYNCHRONIZING', last_attempt_at=$now, last_error_code=NULL, updated_at=$now
                WHERE event_id=(SELECT event_id FROM cloud_sync_outbox WHERE outbox_id=$outboxId)
                  AND state='PENDING'
                """;
            reconcile.Parameters.AddWithValue("$now", now);
            reconcile.Parameters.AddWithValue("$outboxId", outboxId);
            await reconcile.ExecuteNonQueryAsync(cancellationToken);

            claims.Add(await ReadClaimAsync(connection, transaction, outboxId, cancellationToken));
        }

        transaction.Commit();
        return claims;
    }

    public async Task MarkRetryAsync(
        string outboxId,
        string errorCode,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default)
    {
        RequireText(outboxId, nameof(outboxId), 240);
        RequireText(errorCode, nameof(errorCode), 160);
        var now = DateTimeOffset.UtcNow;
        if (nextAttemptAt <= now) throw new ArgumentOutOfRangeException(nameof(nextAttemptAt), "Retry time must be in the future.");

        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var eventId = await ReadSynchronizingEventIdAsync(connection, transaction, outboxId, cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE cloud_sync_outbox
                SET state='RETRY_WAIT', next_attempt_at=$next, last_error_code=$error, updated_at=$now
                WHERE outbox_id=$outboxId AND state='SYNCHRONIZING'
                """;
            command.Parameters.AddWithValue("$next", nextAttemptAt.ToUniversalTime().ToString("O"));
            command.Parameters.AddWithValue("$error", errorCode);
            command.Parameters.AddWithValue("$now", now.ToString("O"));
            command.Parameters.AddWithValue("$outboxId", outboxId);
            EnsureOne(await command.ExecuteNonQueryAsync(cancellationToken), "SYNC_CLAIM_STATE_INVALID");
        }

        await SetReconciliationPendingAsync(connection, transaction, eventId, errorCode, now, cancellationToken);
        transaction.Commit();
    }

    public async Task MarkReconciledAsync(
        string outboxId,
        string canonicalEventId,
        DateTimeOffset canonicalCommittedAt,
        CancellationToken cancellationToken = default)
    {
        RequireText(outboxId, nameof(outboxId), 240);
        RequireText(canonicalEventId, nameof(canonicalEventId), 240);
        var now = DateTimeOffset.UtcNow;

        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var eventId = await ReadSynchronizingEventIdAsync(connection, transaction, outboxId, cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE cloud_sync_outbox
                SET state='RECONCILED', next_attempt_at=NULL, last_error_code=NULL, updated_at=$now
                WHERE outbox_id=$outboxId AND state='SYNCHRONIZING'
                """;
            command.Parameters.AddWithValue("$now", now.ToString("O"));
            command.Parameters.AddWithValue("$outboxId", outboxId);
            EnsureOne(await command.ExecuteNonQueryAsync(cancellationToken), "SYNC_CLAIM_STATE_INVALID");
        }

        await using (var reconciliation = connection.CreateCommand())
        {
            reconciliation.Transaction = transaction;
            reconciliation.CommandText = """
                UPDATE edge_reconciliation_state
                SET state='RECONCILED', canonical_event_id=$canonicalEventId,
                    canonical_committed_at=$canonicalCommittedAt, last_error_code=NULL, updated_at=$now
                WHERE event_id=$eventId AND state='SYNCHRONIZING'
                """;
            reconciliation.Parameters.AddWithValue("$canonicalEventId", canonicalEventId);
            reconciliation.Parameters.AddWithValue("$canonicalCommittedAt", canonicalCommittedAt.ToUniversalTime().ToString("O"));
            reconciliation.Parameters.AddWithValue("$now", now.ToString("O"));
            reconciliation.Parameters.AddWithValue("$eventId", eventId);
            EnsureOne(await reconciliation.ExecuteNonQueryAsync(cancellationToken), "RECONCILIATION_STATE_INVALID");
        }

        await UpsertMetaAsync(connection, transaction, "last_cloud_sync_at", now.ToString("O"), cancellationToken);
        await UpsertMetaAsync(connection, transaction, "last_cloud_sync_event_id", eventId, cancellationToken);
        transaction.Commit();
    }

    public async Task<string> MarkConflictAsync(
        string outboxId,
        string conflictCode,
        string contextJson,
        CancellationToken cancellationToken = default)
    {
        RequireText(outboxId, nameof(outboxId), 240);
        RequireText(conflictCode, nameof(conflictCode), 160);
        ValidateJsonObject(contextJson);
        var now = DateTimeOffset.UtcNow;
        var conflictId = $"edge-conflict-{Guid.NewGuid():N}";

        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var eventId = await ReadSynchronizingEventIdAsync(connection, transaction, outboxId, cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE cloud_sync_outbox
                SET state='CONFLICT', next_attempt_at=NULL, last_error_code=$code, updated_at=$now
                WHERE outbox_id=$outboxId AND state='SYNCHRONIZING'
                """;
            command.Parameters.AddWithValue("$code", conflictCode);
            command.Parameters.AddWithValue("$now", now.ToString("O"));
            command.Parameters.AddWithValue("$outboxId", outboxId);
            EnsureOne(await command.ExecuteNonQueryAsync(cancellationToken), "SYNC_CLAIM_STATE_INVALID");
        }

        await using (var reconciliation = connection.CreateCommand())
        {
            reconciliation.Transaction = transaction;
            reconciliation.CommandText = """
                UPDATE edge_reconciliation_state
                SET state='CONFLICT', last_error_code=$code, updated_at=$now
                WHERE event_id=$eventId AND state='SYNCHRONIZING'
                """;
            reconciliation.Parameters.AddWithValue("$code", conflictCode);
            reconciliation.Parameters.AddWithValue("$now", now.ToString("O"));
            reconciliation.Parameters.AddWithValue("$eventId", eventId);
            EnsureOne(await reconciliation.ExecuteNonQueryAsync(cancellationToken), "RECONCILIATION_STATE_INVALID");
        }

        await using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO edge_conflicts(conflict_id, event_id, conflict_code, context_json, state, detected_at)
                VALUES ($conflictId, $eventId, $code, $context, 'OPEN', $now)
                """;
            insert.Parameters.AddWithValue("$conflictId", conflictId);
            insert.Parameters.AddWithValue("$eventId", eventId);
            insert.Parameters.AddWithValue("$code", conflictCode);
            insert.Parameters.AddWithValue("$context", contextJson);
            insert.Parameters.AddWithValue("$now", now.ToString("O"));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        transaction.Commit();
        return conflictId;
    }

    public async Task<LanCloudSyncQueueInspection> InspectAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*),
                   SUM(CASE WHEN state='PENDING' THEN 1 ELSE 0 END),
                   SUM(CASE WHEN state='SYNCHRONIZING' THEN 1 ELSE 0 END),
                   SUM(CASE WHEN state='RETRY_WAIT' THEN 1 ELSE 0 END),
                   SUM(CASE WHEN state='RECONCILED' THEN 1 ELSE 0 END),
                   SUM(CASE WHEN state='CONFLICT' THEN 1 ELSE 0 END),
                   SUM(CASE WHEN state='REVIEW_REQUIRED' THEN 1 ELSE 0 END)
            FROM cloud_sync_outbox
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new LanCloudSyncQueueInspection(
            Total: reader.GetInt64(0),
            Pending: reader.IsDBNull(1) ? 0 : reader.GetInt64(1),
            Synchronizing: reader.IsDBNull(2) ? 0 : reader.GetInt64(2),
            RetryWait: reader.IsDBNull(3) ? 0 : reader.GetInt64(3),
            Reconciled: reader.IsDBNull(4) ? 0 : reader.GetInt64(4),
            Conflict: reader.IsDBNull(5) ? 0 : reader.GetInt64(5),
            ReviewRequired: reader.IsDBNull(6) ? 0 : reader.GetInt64(6));
    }

    private async Task<LanCloudSyncClaim> ReadClaimAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string outboxId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT o.outbox_id, o.attempt_count,
                   e.event_id, e.request_id, e.idempotency_key, e.environment, e.cluster_id,
                   e.device_id, e.device_seq, e.edge_instance_id, e.edge_epoch,
                   e.command_code, e.event_code, e.entity_type, e.entity_id,
                   e.base_version, e.resulting_version, e.payload_json, e.payload_hash,
                   e.accepted_at, e.authority_snapshot_version,
                   (SELECT meta_value FROM edge_meta WHERE meta_key='domain_contract_version')
            FROM cloud_sync_outbox o
            JOIN edge_events e ON e.event_id=o.event_id
            WHERE o.outbox_id=$outboxId AND o.state='SYNCHRONIZING'
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$outboxId", outboxId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new LanCloudSyncException("SYNC_CLAIM_NOT_FOUND", "Claimed Cloud sync item was not found.");

        var eventId = reader.GetString(2);
        var envelope = new LanReconciliationEnvelope(
            EventId: eventId,
            RequestId: reader.GetString(3),
            IdempotencyKey: reader.GetString(4),
            Environment: reader.GetString(5),
            ClusterId: reader.GetString(6),
            DeviceId: reader.IsDBNull(7) ? null : reader.GetString(7),
            DeviceSeq: reader.IsDBNull(8) ? null : reader.GetInt64(8),
            EdgeInstanceId: reader.GetString(9),
            EdgeEpoch: reader.GetString(10),
            CommandCode: reader.GetString(11),
            EventCode: reader.GetString(12),
            EntityType: reader.GetString(13),
            EntityId: reader.GetString(14),
            BaseVersion: reader.IsDBNull(15) ? null : reader.GetInt64(15),
            ResultingVersion: reader.GetInt64(16),
            PayloadJson: reader.GetString(17),
            PayloadHash: reader.GetString(18),
            AcceptedAt: DateTimeOffset.Parse(reader.GetString(19)),
            AuthoritySnapshotVersion: reader.GetString(20),
            DomainContractVersion: reader.IsDBNull(21) ? "" : reader.GetString(21),
            CompletedIntegrationReceipts: Array.Empty<LanIntegrationReceiptEvidence>());
        var attemptCount = reader.GetInt64(1);
        await reader.DisposeAsync();

        var receipts = await ReadCompletedReceiptsAsync(connection, transaction, eventId, cancellationToken);
        return new LanCloudSyncClaim(outboxId, attemptCount, envelope with { CompletedIntegrationReceipts = receipts });
    }

    private static async Task<IReadOnlyList<LanIntegrationReceiptEvidence>> ReadCompletedReceiptsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string eventId,
        CancellationToken cancellationToken)
    {
        var result = new List<LanIntegrationReceiptEvidence>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            WITH event_keys(logical_key) AS (
              SELECT projection_key FROM google_projection_outbox WHERE event_id=$eventId
              UNION
              SELECT logical_file_key FROM drive_upload_outbox WHERE event_id=$eventId
            )
            SELECT r.receipt_id, r.logical_key, r.target_kind, r.provider_object_id,
                   r.content_hash, r.checkpoint, r.readback_evidence_json,
                   r.completed_at, r.status
            FROM integration_receipts r
            JOIN event_keys k ON k.logical_key=r.logical_key
            WHERE r.status='COMPLETED'
            ORDER BY r.target_kind, r.logical_key
            """;
        command.Parameters.AddWithValue("$eventId", eventId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new LanIntegrationReceiptEvidence(
                ReceiptId: reader.GetString(0),
                LogicalKey: reader.GetString(1),
                TargetKind: reader.GetString(2),
                ProviderObjectId: reader.IsDBNull(3) ? null : reader.GetString(3),
                ContentHash: reader.IsDBNull(4) ? null : reader.GetString(4),
                Checkpoint: reader.IsDBNull(5) ? null : reader.GetString(5),
                ReadbackEvidenceJson: reader.GetString(6),
                CompletedAt: DateTimeOffset.Parse(reader.GetString(7)),
                Status: reader.GetString(8)));
        }
        return result;
    }

    private static async Task<string> ReadSynchronizingEventIdAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string outboxId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT event_id FROM cloud_sync_outbox WHERE outbox_id=$outboxId AND state='SYNCHRONIZING' LIMIT 1";
        command.Parameters.AddWithValue("$outboxId", outboxId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull
            ? throw new LanCloudSyncException("SYNC_CLAIM_STATE_INVALID", "Cloud sync item is not currently claimed.")
            : Convert.ToString(value)!;
    }

    private static async Task SetReconciliationPendingAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string eventId,
        string errorCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE edge_reconciliation_state
            SET state='PENDING', last_error_code=$error, updated_at=$now
            WHERE event_id=$eventId AND state='SYNCHRONIZING'
            """;
        command.Parameters.AddWithValue("$error", errorCode);
        command.Parameters.AddWithValue("$now", now.ToString("O"));
        command.Parameters.AddWithValue("$eventId", eventId);
        EnsureOne(await command.ExecuteNonQueryAsync(cancellationToken), "RECONCILIATION_STATE_INVALID");
    }

    private static async Task UpsertMetaAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO edge_meta(meta_key, meta_value, updated_at)
            VALUES ($key, $value, $now)
            ON CONFLICT(meta_key) DO UPDATE SET meta_value=excluded.meta_value, updated_at=excluded.updated_at
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
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

    private static void ValidateJsonObject(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("Object required.");
        }
        catch (JsonException error)
        {
            throw new LanCloudSyncException("SYNC_CONFLICT_CONTEXT_INVALID", "Conflict context must be a JSON object.", error);
        }
    }

    private static void RequireText(string value, string name, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > max) throw new ArgumentException($"{name} is required and must be <= {max} characters.", name);
    }

    private static void EnsureOne(int changed, string code)
    {
        if (changed != 1) throw new LanCloudSyncException(code, "Cloud sync state transition did not match exactly one row.");
    }
}

public sealed record LanCloudSyncClaim(
    string OutboxId,
    long AttemptCount,
    LanReconciliationEnvelope Envelope);

public sealed record LanReconciliationEnvelope(
    string EventId,
    string RequestId,
    string IdempotencyKey,
    string Environment,
    string ClusterId,
    string? DeviceId,
    long? DeviceSeq,
    string EdgeInstanceId,
    string EdgeEpoch,
    string CommandCode,
    string EventCode,
    string EntityType,
    string EntityId,
    long? BaseVersion,
    long ResultingVersion,
    string PayloadJson,
    string PayloadHash,
    DateTimeOffset AcceptedAt,
    string AuthoritySnapshotVersion,
    string DomainContractVersion,
    IReadOnlyList<LanIntegrationReceiptEvidence> CompletedIntegrationReceipts);

public sealed record LanIntegrationReceiptEvidence(
    string ReceiptId,
    string LogicalKey,
    string TargetKind,
    string? ProviderObjectId,
    string? ContentHash,
    string? Checkpoint,
    string ReadbackEvidenceJson,
    DateTimeOffset CompletedAt,
    string Status);

public sealed record LanCloudSyncQueueInspection(
    long Total,
    long Pending,
    long Synchronizing,
    long RetryWait,
    long Reconciled,
    long Conflict,
    long ReviewRequired);

public sealed class LanCloudSyncException : InvalidOperationException
{
    public LanCloudSyncException(string code, string message, Exception? innerException = null) : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
