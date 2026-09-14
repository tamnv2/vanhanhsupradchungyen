using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class LanIntegrationOutputStore
{
    public const string GoogleSheetsTarget = "GOOGLE_SHEETS";
    public const string GoogleDriveTarget = "GOOGLE_DRIVE";

    private const int MaxClaimLimit = 100;
    private readonly string _connectionString;

    public LanIntegrationOutputStore(string databasePath)
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

    public async Task<IReadOnlyList<LanIntegrationWorkClaim>> ClaimDueAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > MaxClaimLimit) throw new ArgumentOutOfRangeException(nameof(limit));

        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(deferred: false);
        var now = DateTimeOffset.UtcNow;
        var claims = new List<LanIntegrationWorkClaim>(limit);

        for (var i = 0; i < limit; i++)
        {
            var candidate = await ReadNextDueAsync(connection, transaction, now, cancellationToken);
            if (candidate is null) break;

            var processingState = candidate.TargetKind == GoogleSheetsTarget ? "SENDING" : "UPLOADING";
            var table = candidate.TargetKind == GoogleSheetsTarget ? "google_projection_outbox" : "drive_upload_outbox";
            var idColumn = candidate.TargetKind == GoogleSheetsTarget ? "projection_id" : "upload_id";

            await using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = $"""
                UPDATE {table}
                SET state=$processingState,
                    attempt_count=attempt_count + 1,
                    next_attempt_at=NULL,
                    last_error_code=NULL,
                    updated_at=$now
                WHERE {idColumn}=$workId
                  AND state=$expectedState
                """;
            update.Parameters.AddWithValue("$processingState", processingState);
            update.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O"));
            update.Parameters.AddWithValue("$workId", candidate.WorkId);
            update.Parameters.AddWithValue("$expectedState", candidate.State);
            var changed = await update.ExecuteNonQueryAsync(cancellationToken);
            if (changed != 1)
            {
                throw new LanIntegrationOutputException(
                    "INTEGRATION_CLAIM_RACE",
                    "Integration output claim did not match exactly one due row.");
            }

            claims.Add(candidate with
            {
                State = processingState,
                AttemptCount = checked(candidate.AttemptCount + 1)
            });
        }

        transaction.Commit();
        return claims;
    }

    public async Task<int> RecoverInterruptedClaimsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(deferred: false);
        var now = DateTimeOffset.UtcNow.ToUniversalTime().ToString("O");
        var changed = 0;

        changed += await RecoverTableAsync(
            connection,
            transaction,
            "google_projection_outbox",
            "SENDING",
            now,
            cancellationToken);
        changed += await RecoverTableAsync(
            connection,
            transaction,
            "drive_upload_outbox",
            "UPLOADING",
            now,
            cancellationToken);

        transaction.Commit();
        return changed;
    }

    public async Task MarkRetryAsync(
        string targetKind,
        string workId,
        string errorCode,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default)
    {
        var target = RequireTarget(targetKind);
        RequireText(workId, nameof(workId), 240);
        RequireText(errorCode, nameof(errorCode), 128);

        var table = target == GoogleSheetsTarget ? "google_projection_outbox" : "drive_upload_outbox";
        var idColumn = target == GoogleSheetsTarget ? "projection_id" : "upload_id";
        var processingState = target == GoogleSheetsTarget ? "SENDING" : "UPLOADING";
        var now = DateTimeOffset.UtcNow;

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            UPDATE {table}
            SET state='RETRY_WAIT',
                next_attempt_at=$nextAttemptAt,
                last_error_code=$errorCode,
                updated_at=$now
            WHERE {idColumn}=$workId
              AND state=$processingState
            """;
        command.Parameters.AddWithValue("$nextAttemptAt", nextAttemptAt.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$errorCode", errorCode.Trim());
        command.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$workId", workId.Trim());
        command.Parameters.AddWithValue("$processingState", processingState);
        var changed = await command.ExecuteNonQueryAsync(cancellationToken);
        if (changed != 1)
        {
            throw new LanIntegrationOutputException(
                "INTEGRATION_RETRY_STATE_INVALID",
                "Integration retry transition requires exactly one processing work item.");
        }
    }

    public async Task<LanIntegrationCompletionResult> CompleteAsync(
        LanIntegrationCompletion completion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(completion);
        var target = RequireTarget(completion.TargetKind);
        var workId = RequireText(completion.WorkId, nameof(completion.WorkId), 240);
        var providerObjectId = NormalizeOptional(completion.ProviderObjectId, nameof(completion.ProviderObjectId), 720);
        var contentHash = NormalizeOptional(completion.ContentHash, nameof(completion.ContentHash), 240);
        var checkpoint = NormalizeOptional(completion.Checkpoint, nameof(completion.Checkpoint), 720);
        var readback = CanonicalizeNonEmptyJsonObject(completion.ReadbackEvidenceJson);

        if (providerObjectId is null && checkpoint is null)
        {
            throw new LanIntegrationOutputException(
                "INTEGRATION_PROVIDER_EVIDENCE_REQUIRED",
                "Provider object or checkpoint evidence is required before integration work can complete.");
        }

        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(deferred: false);
        var work = await ReadWorkAsync(connection, transaction, target, workId, cancellationToken)
            ?? throw new LanIntegrationOutputException("INTEGRATION_WORK_NOT_FOUND", "Integration output work was not found.");

        var runtime = await ReadRuntimeIdentityAsync(connection, transaction, cancellationToken);
        var existing = await ReadReceiptAsync(connection, transaction, target, work.LogicalKey, cancellationToken);
        var supplied = new ReceiptEvidence(providerObjectId, contentHash, checkpoint, readback);

        if (target == GoogleDriveTarget &&
            (!string.Equals(contentHash, work.ExpectedContentHash, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(contentHash)))
        {
            var result = await PersistReviewRequiredAsync(
                connection,
                transaction,
                work,
                runtime,
                existing,
                supplied,
                "INTEGRATION_DRIVE_HASH_MISMATCH",
                cancellationToken);
            transaction.Commit();
            return result;
        }

        if (string.Equals(work.State, "REVIEW_REQUIRED", StringComparison.Ordinal))
        {
            transaction.Commit();
            return new LanIntegrationCompletionResult(
                Completed: false,
                AlreadyCompleted: false,
                ReviewRequired: true,
                TargetKind: target,
                WorkId: work.WorkId,
                LogicalKey: work.LogicalKey,
                ReceiptId: existing?.ReceiptId,
                Code: "INTEGRATION_REVIEW_REQUIRED");
        }

        if (existing is not null)
        {
            if (string.Equals(existing.Status, "COMPLETED", StringComparison.Ordinal) && EvidenceMatches(existing, supplied))
            {
                if (!string.Equals(work.State, "COMPLETED", StringComparison.Ordinal))
                {
                    await MarkWorkStateAsync(
                        connection,
                        transaction,
                        work,
                        "COMPLETED",
                        lastErrorCode: null,
                        cancellationToken);
                }
                transaction.Commit();
                return new LanIntegrationCompletionResult(
                    Completed: true,
                    AlreadyCompleted: true,
                    ReviewRequired: false,
                    TargetKind: target,
                    WorkId: work.WorkId,
                    LogicalKey: work.LogicalKey,
                    ReceiptId: existing.ReceiptId,
                    Code: "INTEGRATION_ALREADY_COMPLETED");
            }

            var review = await PersistReviewRequiredAsync(
                connection,
                transaction,
                work,
                runtime,
                existing,
                supplied,
                "INTEGRATION_RECEIPT_EVIDENCE_CONFLICT",
                cancellationToken);
            transaction.Commit();
            return review;
        }

        var expectedProcessingState = target == GoogleSheetsTarget ? "SENDING" : "UPLOADING";
        if (!string.Equals(work.State, expectedProcessingState, StringComparison.Ordinal))
        {
            throw new LanIntegrationOutputException(
                "INTEGRATION_COMPLETE_STATE_INVALID",
                $"Integration work must be {expectedProcessingState} before first completion evidence is accepted.");
        }

        var receiptId = $"receipt-{Guid.NewGuid():N}";
        var completedAt = DateTimeOffset.UtcNow;
        await InsertReceiptAsync(
            connection,
            transaction,
            receiptId,
            work.LogicalKey,
            target,
            supplied,
            runtime,
            completedAt,
            "COMPLETED",
            cancellationToken);
        await MarkWorkStateAsync(connection, transaction, work, "COMPLETED", lastErrorCode: null, cancellationToken);
        transaction.Commit();

        return new LanIntegrationCompletionResult(
            Completed: true,
            AlreadyCompleted: false,
            ReviewRequired: false,
            TargetKind: target,
            WorkId: work.WorkId,
            LogicalKey: work.LogicalKey,
            ReceiptId: receiptId,
            Code: "INTEGRATION_COMPLETED");
    }

    public async Task<LanIntegrationOutputInspection> InspectAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return new LanIntegrationOutputInspection(
            Pending: await CountAsync(connection, "SELECT (SELECT COUNT(*) FROM google_projection_outbox WHERE state='PENDING') + (SELECT COUNT(*) FROM drive_upload_outbox WHERE state='PENDING')", cancellationToken),
            Processing: await CountAsync(connection, "SELECT (SELECT COUNT(*) FROM google_projection_outbox WHERE state='SENDING') + (SELECT COUNT(*) FROM drive_upload_outbox WHERE state='UPLOADING')", cancellationToken),
            RetryWait: await CountAsync(connection, "SELECT (SELECT COUNT(*) FROM google_projection_outbox WHERE state='RETRY_WAIT') + (SELECT COUNT(*) FROM drive_upload_outbox WHERE state='RETRY_WAIT')", cancellationToken),
            Completed: await CountAsync(connection, "SELECT (SELECT COUNT(*) FROM google_projection_outbox WHERE state='COMPLETED') + (SELECT COUNT(*) FROM drive_upload_outbox WHERE state='COMPLETED')", cancellationToken),
            ReviewRequired: await CountAsync(connection, "SELECT (SELECT COUNT(*) FROM google_projection_outbox WHERE state='REVIEW_REQUIRED') + (SELECT COUNT(*) FROM drive_upload_outbox WHERE state='REVIEW_REQUIRED')", cancellationToken),
            CompletedReceipts: await CountAsync(connection, "SELECT COUNT(*) FROM integration_receipts WHERE status='COMPLETED'", cancellationToken),
            ReviewReceipts: await CountAsync(connection, "SELECT COUNT(*) FROM integration_receipts WHERE status='REVIEW_REQUIRED'", cancellationToken));
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

    private static async Task<LanIntegrationWorkClaim?> ReadNextDueAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT target_kind, work_id, event_id, logical_key, target_key, payload_json,
                   local_path, content_hash, content_type, size_bytes, state, attempt_count
            FROM (
              SELECT 'GOOGLE_SHEETS' AS target_kind,
                     projection_id AS work_id,
                     event_id,
                     projection_key AS logical_key,
                     target_key,
                     payload_json,
                     NULL AS local_path,
                     NULL AS content_hash,
                     NULL AS content_type,
                     NULL AS size_bytes,
                     state,
                     attempt_count,
                     created_at
              FROM google_projection_outbox
              WHERE state IN ('PENDING','RETRY_WAIT')
                AND (next_attempt_at IS NULL OR next_attempt_at <= $now)
              UNION ALL
              SELECT 'GOOGLE_DRIVE' AS target_kind,
                     upload_id AS work_id,
                     event_id,
                     logical_file_key AS logical_key,
                     NULL AS target_key,
                     NULL AS payload_json,
                     local_path,
                     content_hash,
                     content_type,
                     size_bytes,
                     state,
                     attempt_count,
                     created_at
              FROM drive_upload_outbox
              WHERE state IN ('PENDING','RETRY_WAIT')
                AND (next_attempt_at IS NULL OR next_attempt_at <= $now)
            ) due
            ORDER BY created_at, target_kind, work_id
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new LanIntegrationWorkClaim(
            TargetKind: reader.GetString(0),
            WorkId: reader.GetString(1),
            EventId: reader.IsDBNull(2) ? null : reader.GetString(2),
            LogicalKey: reader.GetString(3),
            TargetKey: reader.IsDBNull(4) ? null : reader.GetString(4),
            PayloadJson: reader.IsDBNull(5) ? null : reader.GetString(5),
            LocalPath: reader.IsDBNull(6) ? null : reader.GetString(6),
            ContentHash: reader.IsDBNull(7) ? null : reader.GetString(7),
            ContentType: reader.IsDBNull(8) ? null : reader.GetString(8),
            SizeBytes: reader.IsDBNull(9) ? null : reader.GetInt64(9),
            State: reader.GetString(10),
            AttemptCount: reader.GetInt64(11));
    }

    private static async Task<int> RecoverTableAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string table,
        string processingState,
        string now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            UPDATE {table}
            SET state='RETRY_WAIT',
                next_attempt_at=$now,
                last_error_code='PROCESS_RESTART',
                updated_at=$now
            WHERE state=$processingState
            """;
        command.Parameters.AddWithValue("$now", now);
        command.Parameters.AddWithValue("$processingState", processingState);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<WorkRow?> ReadWorkAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string targetKind,
        string workId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        if (targetKind == GoogleSheetsTarget)
        {
            command.CommandText = """
                SELECT projection_id, event_id, projection_key, state, NULL
                FROM google_projection_outbox
                WHERE projection_id=$workId
                LIMIT 1
                """;
        }
        else
        {
            command.CommandText = """
                SELECT upload_id, event_id, logical_file_key, state, content_hash
                FROM drive_upload_outbox
                WHERE upload_id=$workId
                LIMIT 1
                """;
        }
        command.Parameters.AddWithValue("$workId", workId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new WorkRow(
            TargetKind: targetKind,
            WorkId: reader.GetString(0),
            EventId: reader.IsDBNull(1) ? null : reader.GetString(1),
            LogicalKey: reader.GetString(2),
            State: reader.GetString(3),
            ExpectedContentHash: reader.IsDBNull(4) ? null : reader.GetString(4));
    }

    private static async Task<RuntimeIdentity> ReadRuntimeIdentityAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        string Read(string key)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT meta_value FROM edge_meta WHERE meta_key=$key LIMIT 1";
            command.Parameters.AddWithValue("$key", key);
            var value = Convert.ToString(command.ExecuteScalar());
            if (string.IsNullOrWhiteSpace(value)) throw new LanIntegrationOutputException("INTEGRATION_RUNTIME_IDENTITY_MISSING", $"Missing edge metadata: {key}.");
            return value;
        }

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        return new RuntimeIdentity(Read("instance_id"), Read("edge_epoch"));
    }

    private static async Task<ReceiptRow?> ReadReceiptAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string targetKind,
        string logicalKey,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT receipt_id, provider_object_id, content_hash, checkpoint,
                   readback_evidence_json, status
            FROM integration_receipts
            WHERE target_kind=$targetKind AND logical_key=$logicalKey
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$targetKind", targetKind);
        command.Parameters.AddWithValue("$logicalKey", logicalKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new ReceiptRow(
            ReceiptId: reader.GetString(0),
            ProviderObjectId: reader.IsDBNull(1) ? null : reader.GetString(1),
            ContentHash: reader.IsDBNull(2) ? null : reader.GetString(2),
            Checkpoint: reader.IsDBNull(3) ? null : reader.GetString(3),
            ReadbackEvidenceJson: reader.GetString(4),
            Status: reader.GetString(5));
    }

    private static async Task InsertReceiptAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string receiptId,
        string logicalKey,
        string targetKind,
        ReceiptEvidence evidence,
        RuntimeIdentity runtime,
        DateTimeOffset completedAt,
        string status,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO integration_receipts(
              receipt_id, logical_key, target_kind, provider_object_id, content_hash,
              checkpoint, readback_evidence_json, producer_edge_instance_id,
              producer_edge_epoch, completed_at, status
            ) VALUES (
              $receiptId, $logicalKey, $targetKind, $providerObjectId, $contentHash,
              $checkpoint, $readback, $instanceId,
              $edgeEpoch, $completedAt, $status
            )
            """;
        command.Parameters.AddWithValue("$receiptId", receiptId);
        command.Parameters.AddWithValue("$logicalKey", logicalKey);
        command.Parameters.AddWithValue("$targetKind", targetKind);
        command.Parameters.AddWithValue("$providerObjectId", (object?)evidence.ProviderObjectId ?? DBNull.Value);
        command.Parameters.AddWithValue("$contentHash", (object?)evidence.ContentHash ?? DBNull.Value);
        command.Parameters.AddWithValue("$checkpoint", (object?)evidence.Checkpoint ?? DBNull.Value);
        command.Parameters.AddWithValue("$readback", evidence.ReadbackEvidenceJson);
        command.Parameters.AddWithValue("$instanceId", runtime.EdgeInstanceId);
        command.Parameters.AddWithValue("$edgeEpoch", runtime.EdgeEpoch);
        command.Parameters.AddWithValue("$completedAt", completedAt.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$status", status);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkWorkStateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        WorkRow work,
        string state,
        string? lastErrorCode,
        CancellationToken cancellationToken)
    {
        var table = work.TargetKind == GoogleSheetsTarget ? "google_projection_outbox" : "drive_upload_outbox";
        var idColumn = work.TargetKind == GoogleSheetsTarget ? "projection_id" : "upload_id";
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            UPDATE {table}
            SET state=$state,
                next_attempt_at=NULL,
                last_error_code=$lastErrorCode,
                updated_at=$now
            WHERE {idColumn}=$workId
            """;
        command.Parameters.AddWithValue("$state", state);
        command.Parameters.AddWithValue("$lastErrorCode", (object?)lastErrorCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$workId", work.WorkId);
        var changed = await command.ExecuteNonQueryAsync(cancellationToken);
        if (changed != 1)
            throw new LanIntegrationOutputException("INTEGRATION_WORK_STATE_INVALID", "Integration work state update did not match exactly one row.");
    }

    private static async Task<LanIntegrationCompletionResult> PersistReviewRequiredAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        WorkRow work,
        RuntimeIdentity runtime,
        ReceiptRow? existing,
        ReceiptEvidence supplied,
        string code,
        CancellationToken cancellationToken)
    {
        string receiptId;
        if (existing is null)
        {
            receiptId = $"receipt-{Guid.NewGuid():N}";
            await InsertReceiptAsync(
                connection,
                transaction,
                receiptId,
                work.LogicalKey,
                work.TargetKind,
                supplied,
                runtime,
                DateTimeOffset.UtcNow,
                "REVIEW_REQUIRED",
                cancellationToken);
        }
        else
        {
            receiptId = existing.ReceiptId;
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE integration_receipts
                SET status='REVIEW_REQUIRED'
                WHERE receipt_id=$receiptId
                """;
            command.Parameters.AddWithValue("$receiptId", receiptId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await MarkWorkStateAsync(connection, transaction, work, "REVIEW_REQUIRED", code, cancellationToken);
        return new LanIntegrationCompletionResult(
            Completed: false,
            AlreadyCompleted: false,
            ReviewRequired: true,
            TargetKind: work.TargetKind,
            WorkId: work.WorkId,
            LogicalKey: work.LogicalKey,
            ReceiptId: receiptId,
            Code: code);
    }

    private static bool EvidenceMatches(ReceiptRow existing, ReceiptEvidence supplied) =>
        string.Equals(existing.ProviderObjectId, supplied.ProviderObjectId, StringComparison.Ordinal) &&
        string.Equals(existing.ContentHash, supplied.ContentHash, StringComparison.Ordinal) &&
        string.Equals(existing.Checkpoint, supplied.Checkpoint, StringComparison.Ordinal) &&
        string.Equals(CanonicalizeNonEmptyJsonObject(existing.ReadbackEvidenceJson), supplied.ReadbackEvidenceJson, StringComparison.Ordinal);

    private static string RequireTarget(string value)
    {
        var normalized = RequireText(value, nameof(value), 40);
        if (normalized != GoogleSheetsTarget && normalized != GoogleDriveTarget)
            throw new LanIntegrationOutputException("INTEGRATION_TARGET_INVALID", "Integration target must be GOOGLE_SHEETS or GOOGLE_DRIVE.");
        return normalized;
    }

    private static string RequireText(string? value, string name, int max)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0 || normalized.Length > max)
            throw new ArgumentException($"{name} is required and must be <= {max} characters.", name);
        return normalized;
    }

    private static string? NormalizeOptional(string? value, string name, int max)
    {
        if (value is null) return null;
        var normalized = value.Trim();
        if (normalized.Length == 0) return null;
        if (normalized.Length > max) throw new ArgumentException($"{name} must be <= {max} characters.", name);
        return normalized;
    }

    private static string CanonicalizeNonEmptyJsonObject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new LanIntegrationOutputException("INTEGRATION_READBACK_REQUIRED", "Readback evidence is required.");
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.EnumerateObject().Any())
                throw new LanIntegrationOutputException("INTEGRATION_READBACK_INVALID", "Readback evidence must be a non-empty JSON object.");
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream)) WriteCanonical(document.RootElement, writer);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (LanIntegrationOutputException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new LanIntegrationOutputException("INTEGRATION_READBACK_INVALID", "Readback evidence must be valid JSON.", error);
        }
    }

    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                var properties = element.EnumerateObject().ToArray();
                if (properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length)
                    throw new LanIntegrationOutputException("INTEGRATION_READBACK_INVALID", "Readback evidence contains duplicate property names.");
                foreach (var property in properties.OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteCanonical(item, writer);
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
                throw new LanIntegrationOutputException("INTEGRATION_READBACK_INVALID", "Unsupported JSON token in readback evidence.");
        }
    }

    private static async Task<long> CountAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value ?? 0L);
    }

    private sealed record WorkRow(
        string TargetKind,
        string WorkId,
        string? EventId,
        string LogicalKey,
        string State,
        string? ExpectedContentHash);

    private sealed record ReceiptRow(
        string ReceiptId,
        string? ProviderObjectId,
        string? ContentHash,
        string? Checkpoint,
        string ReadbackEvidenceJson,
        string Status);

    private sealed record ReceiptEvidence(
        string? ProviderObjectId,
        string? ContentHash,
        string? Checkpoint,
        string ReadbackEvidenceJson);

    private sealed record RuntimeIdentity(string EdgeInstanceId, string EdgeEpoch);
}

public sealed record LanIntegrationWorkClaim(
    string TargetKind,
    string WorkId,
    string? EventId,
    string LogicalKey,
    string? TargetKey,
    string? PayloadJson,
    string? LocalPath,
    string? ContentHash,
    string? ContentType,
    long? SizeBytes,
    string State,
    long AttemptCount);

public sealed record LanIntegrationCompletion(
    string TargetKind,
    string WorkId,
    string? ProviderObjectId,
    string? ContentHash,
    string? Checkpoint,
    string ReadbackEvidenceJson);

public sealed record LanIntegrationCompletionResult(
    bool Completed,
    bool AlreadyCompleted,
    bool ReviewRequired,
    string TargetKind,
    string WorkId,
    string LogicalKey,
    string? ReceiptId,
    string Code);

public sealed record LanIntegrationOutputInspection(
    long Pending,
    long Processing,
    long RetryWait,
    long Completed,
    long ReviewRequired,
    long CompletedReceipts,
    long ReviewReceipts);

public sealed class LanIntegrationOutputException : InvalidOperationException
{
    public LanIntegrationOutputException(string code, string message, Exception? innerException = null) : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
