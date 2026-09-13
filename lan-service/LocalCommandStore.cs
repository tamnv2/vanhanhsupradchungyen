using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class LocalCommandStore
{
    private readonly string _connectionString;
    private readonly string _expectedEnvironment;
    private readonly string _expectedClusterId;
    private readonly string _expectedDomainContractVersion;

    public LocalCommandStore(
        string databasePath,
        string expectedEnvironment,
        string expectedClusterId,
        string expectedDomainContractVersion)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        if (string.IsNullOrWhiteSpace(expectedEnvironment)) throw new ArgumentException("Environment is required", nameof(expectedEnvironment));
        if (string.IsNullOrWhiteSpace(expectedClusterId)) throw new ArgumentException("Cluster ID is required", nameof(expectedClusterId));
        if (string.IsNullOrWhiteSpace(expectedDomainContractVersion)) throw new ArgumentException("Domain contract version is required", nameof(expectedDomainContractVersion));

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
    }

    public async Task<LanLocalCommandResult> ExecuteAsync(
        LanLocalCommandEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ValidateEnvelope(envelope);
        var normalizedPayload = CanonicalizeJsonObject(envelope.PayloadJson, "COMMAND_PAYLOAD_INVALID");
        var normalizedState = CanonicalizeJsonObject(envelope.NextStateJson, "COMMAND_STATE_INVALID");
        var payloadHash = Sha256Hex(normalizedPayload);
        var acceptedAt = DateTimeOffset.UtcNow.ToString("O");

        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var runtime = await ReadAndValidateRuntimeAsync(connection, transaction, cancellationToken);
            var authority = await ReadActiveAuthorityAsync(connection, transaction, cancellationToken);

            var replay = await ReadByIdempotencyAsync(connection, transaction, envelope.IdempotencyKey, cancellationToken);
            if (replay is not null)
            {
                if (!MatchesLogicalCommand(replay, envelope, payloadHash))
                {
                    throw new LanLocalCommandException(
                        "IDEMPOTENCY_PAYLOAD_CONFLICT",
                        "The idempotency key already belongs to a different logical command or payload.");
                }

                transaction.Commit();
                return new LanLocalCommandResult(
                    EventId: replay.EventId,
                    RequestId: replay.RequestId,
                    IdempotencyKey: envelope.IdempotencyKey,
                    EntityType: replay.EntityType,
                    EntityId: replay.EntityId,
                    ResultingVersion: replay.ResultingVersion,
                    AuthoritySnapshotVersion: replay.AuthoritySnapshotVersion,
                    CommitStatus: "LAN_ACCEPTED_PENDING_SYNC",
                    GoogleOutputStatus: await ReadGoogleOutputStatusAsync(connection, replay.EventId, cancellationToken),
                    AlreadyAccepted: true);
            }

            if (envelope.DeviceId is not null && envelope.DeviceSeq is not null)
            {
                var bySequence = await ReadByDeviceSequenceAsync(
                    connection,
                    transaction,
                    envelope.DeviceId,
                    envelope.DeviceSeq.Value,
                    cancellationToken);
                if (bySequence is not null)
                {
                    throw new LanLocalCommandException(
                        "DEVICE_SEQUENCE_CONFLICT",
                        "The device sequence is already bound to a different accepted command.");
                }
            }

            var current = await ReadCurrentStateAsync(connection, transaction, envelope.StateKey, cancellationToken);
            var conflictingIdentity = await ReadStateKeyByEntityAsync(
                connection,
                transaction,
                envelope.ModuleId,
                envelope.EntityType,
                envelope.EntityId,
                cancellationToken);

            if (current is null && conflictingIdentity is not null && !string.Equals(conflictingIdentity, envelope.StateKey, StringComparison.Ordinal))
            {
                throw new LanLocalCommandException(
                    "STATE_IDENTITY_CONFLICT",
                    "The entity already exists under a different state key.");
            }

            long resultingVersion;
            if (current is null)
            {
                if (envelope.ExpectedBaseVersion is not null)
                {
                    throw new LanLocalCommandException(
                        "ENTITY_VERSION_CONFLICT",
                        "The command expected an existing entity version, but no current state exists.");
                }
                resultingVersion = 1;
            }
            else
            {
                if (!string.Equals(current.ModuleId, envelope.ModuleId, StringComparison.Ordinal) ||
                    !string.Equals(current.EntityType, envelope.EntityType, StringComparison.Ordinal) ||
                    !string.Equals(current.EntityId, envelope.EntityId, StringComparison.Ordinal))
                {
                    throw new LanLocalCommandException(
                        "STATE_IDENTITY_CONFLICT",
                        "The state key is already bound to a different entity identity.");
                }

                if (envelope.ExpectedBaseVersion is null || envelope.ExpectedBaseVersion.Value != current.EntityVersion)
                {
                    throw new LanLocalCommandException(
                        "ENTITY_VERSION_CONFLICT",
                        $"Expected entity version {envelope.ExpectedBaseVersion?.ToString() ?? "<none>"}, current version is {current.EntityVersion}.");
                }
                resultingVersion = checked(current.EntityVersion + 1);
            }

            await ApplyCurrentStateAsync(
                connection,
                transaction,
                envelope,
                normalizedState,
                current,
                resultingVersion,
                acceptedAt,
                cancellationToken);

            var eventId = $"edge-{Guid.NewGuid():N}";
            await InsertEdgeEventAsync(
                connection,
                transaction,
                envelope,
                runtime,
                authority.AuthorityVersion,
                eventId,
                normalizedPayload,
                payloadHash,
                resultingVersion,
                acceptedAt,
                cancellationToken);

            await InsertReconciliationAsync(connection, transaction, eventId, acceptedAt, cancellationToken);
            await InsertCloudOutboxAsync(connection, transaction, eventId, acceptedAt, cancellationToken);

            if (envelope.GoogleProjectionWork is not null)
            {
                foreach (var work in envelope.GoogleProjectionWork)
                {
                    await InsertGoogleProjectionAsync(connection, transaction, eventId, work, acceptedAt, cancellationToken);
                }
            }

            if (envelope.DriveUploadWork is not null)
            {
                foreach (var work in envelope.DriveUploadWork)
                {
                    await InsertDriveUploadAsync(connection, transaction, eventId, work, acceptedAt, cancellationToken);
                }
            }

            transaction.Commit();
            var googleOutputStatus =
                (envelope.GoogleProjectionWork?.Count > 0 || envelope.DriveUploadWork?.Count > 0)
                    ? "PENDING"
                    : "NOT_REQUIRED";

            return new LanLocalCommandResult(
                EventId: eventId,
                RequestId: envelope.RequestId,
                IdempotencyKey: envelope.IdempotencyKey,
                EntityType: envelope.EntityType,
                EntityId: envelope.EntityId,
                ResultingVersion: resultingVersion,
                AuthoritySnapshotVersion: authority.AuthorityVersion,
                CommitStatus: "LAN_ACCEPTED_PENDING_SYNC",
                GoogleOutputStatus: googleOutputStatus,
                AlreadyAccepted: false);
        }
        catch (LanLocalCommandException)
        {
            transaction.Rollback();
            throw;
        }
        catch (SqliteException error)
        {
            transaction.Rollback();
            throw new LanLocalCommandException("LOCAL_COMMAND_COMMIT_FAILED", "The local command transaction did not commit.", error);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<LanCurrentState?> ReadCurrentStateAsync(
        string stateKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await ReadCurrentStateAsync(connection, transaction: null, stateKey, cancellationToken);
    }

    public async Task<long> ReadAcceptedEventCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM edge_events";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value ?? 0L);
    }

    public async Task<long> ReadCloudOutboxCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM cloud_sync_outbox";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value ?? 0L);
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

    private void ValidateEnvelope(LanLocalCommandEnvelope envelope)
    {
        if (envelope is null) throw new LanLocalCommandException("COMMAND_REQUIRED", "Command envelope is required.");
        RequireBounded(envelope.RequestId, "REQUEST_ID_REQUIRED", 1, 200);
        RequireBounded(envelope.IdempotencyKey, "IDEMPOTENCY_KEY_REQUIRED", 1, 240);
        RequireBounded(envelope.ModuleId, "MODULE_ID_REQUIRED", 1, 120);
        RequireBounded(envelope.CommandCode, "COMMAND_CODE_REQUIRED", 1, 120);
        RequireBounded(envelope.EventCode, "EVENT_CODE_REQUIRED", 1, 120);
        RequireBounded(envelope.EntityType, "ENTITY_TYPE_REQUIRED", 1, 120);
        RequireBounded(envelope.EntityId, "ENTITY_ID_REQUIRED", 1, 240);
        RequireBounded(envelope.StateKey, "STATE_KEY_REQUIRED", 1, 360);

        if (envelope.ExpectedBaseVersion is < 1)
            throw new LanLocalCommandException("ENTITY_VERSION_INVALID", "Expected base version must be at least 1 when supplied.");
        if ((envelope.DeviceId is null) != (envelope.DeviceSeq is null))
            throw new LanLocalCommandException("DEVICE_SEQUENCE_IDENTITY_INVALID", "Device ID and device sequence must be supplied together.");
        if (envelope.DeviceId is not null) RequireBounded(envelope.DeviceId, "DEVICE_ID_INVALID", 1, 240);
        if (envelope.DeviceSeq is < 0)
            throw new LanLocalCommandException("DEVICE_SEQUENCE_INVALID", "Device sequence must be non-negative.");

        if (envelope.GoogleProjectionWork is not null)
        {
            foreach (var work in envelope.GoogleProjectionWork)
            {
                RequireBounded(work.ProjectionKey, "PROJECTION_KEY_REQUIRED", 1, 360);
                RequireBounded(work.TargetKey, "PROJECTION_TARGET_REQUIRED", 1, 360);
                CanonicalizeJsonObject(work.PayloadJson, "PROJECTION_PAYLOAD_INVALID");
            }
        }

        if (envelope.DriveUploadWork is not null)
        {
            foreach (var work in envelope.DriveUploadWork)
            {
                RequireBounded(work.LogicalFileKey, "LOGICAL_FILE_KEY_REQUIRED", 1, 360);
                RequireBounded(work.LocalPath, "LOCAL_FILE_PATH_REQUIRED", 1, 1200);
                RequireBounded(work.ContentHash, "CONTENT_HASH_REQUIRED", 1, 240);
                if (work.SizeBytes < 0) throw new LanLocalCommandException("CONTENT_SIZE_INVALID", "Content size must be non-negative.");
            }
        }
    }

    private async Task<RuntimeIdentity> ReadAndValidateRuntimeAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        var values = await ReadMetaAsync(
            connection,
            transaction,
            new[] { "environment", "cluster_id", "domain_contract_version", "instance_id", "edge_epoch" },
            cancellationToken);

        var environment = RequireMeta(values, "environment");
        var cluster = RequireMeta(values, "cluster_id");
        var domain = RequireMeta(values, "domain_contract_version");
        var instance = RequireMeta(values, "instance_id");
        var epoch = RequireMeta(values, "edge_epoch");

        if (!string.Equals(environment, _expectedEnvironment, StringComparison.Ordinal))
            throw new LanLocalCommandException("RUNTIME_ENVIRONMENT_MISMATCH", "Edge environment does not match the local command runtime.");
        if (!string.Equals(cluster, _expectedClusterId, StringComparison.Ordinal))
            throw new LanLocalCommandException("RUNTIME_CLUSTER_MISMATCH", "Edge cluster does not match the local command runtime.");
        if (!string.Equals(domain, _expectedDomainContractVersion, StringComparison.Ordinal))
            throw new LanLocalCommandException("RUNTIME_DOMAIN_INCOMPATIBLE", "Edge domain contract version is incompatible.");

        return new RuntimeIdentity(environment, cluster, instance, epoch);
    }

    private async Task<ActiveAuthority> ReadActiveAuthorityAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT authority_version, compatibility_version FROM authority_snapshots WHERE status='ACTIVE' ORDER BY imported_at DESC LIMIT 2";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new LanLocalCommandException("AUTHORITY_SNAPSHOT_REQUIRED", "No active authority snapshot is available for local command acceptance.");

        var version = reader.GetString(0);
        var compatibility = reader.GetString(1);
        if (await reader.ReadAsync(cancellationToken))
            throw new LanLocalCommandException("AUTHORITY_ACTIVE_SET_INVALID", "More than one active authority snapshot exists.");
        if (!string.Equals(compatibility, _expectedDomainContractVersion, StringComparison.Ordinal))
            throw new LanLocalCommandException("AUTHORITY_INCOMPATIBLE", "The active authority snapshot is not compatible with this domain contract.");

        return new ActiveAuthority(version, compatibility);
    }

    private async Task<AcceptedEvent?> ReadByIdempotencyAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT event_id, request_id, device_id, device_seq, command_code, event_code,
                   entity_type, entity_id, base_version, resulting_version, payload_hash,
                   authority_snapshot_version
            FROM edge_events
            WHERE environment=$environment AND cluster_id=$cluster AND idempotency_key=$idempotency
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$environment", _expectedEnvironment);
        command.Parameters.AddWithValue("$cluster", _expectedClusterId);
        command.Parameters.AddWithValue("$idempotency", idempotencyKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return ReadAcceptedEvent(reader);
    }

    private static async Task<AcceptedEvent?> ReadByDeviceSequenceAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string deviceId,
        long deviceSeq,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT event_id, request_id, device_id, device_seq, command_code, event_code,
                   entity_type, entity_id, base_version, resulting_version, payload_hash,
                   authority_snapshot_version
            FROM edge_events
            WHERE device_id=$deviceId AND device_seq=$deviceSeq
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$deviceId", deviceId);
        command.Parameters.AddWithValue("$deviceSeq", deviceSeq);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return ReadAcceptedEvent(reader);
    }

    private static AcceptedEvent ReadAcceptedEvent(SqliteDataReader reader) =>
        new(
            EventId: reader.GetString(0),
            RequestId: reader.GetString(1),
            DeviceId: reader.IsDBNull(2) ? null : reader.GetString(2),
            DeviceSeq: reader.IsDBNull(3) ? null : reader.GetInt64(3),
            CommandCode: reader.GetString(4),
            EventCode: reader.GetString(5),
            EntityType: reader.GetString(6),
            EntityId: reader.GetString(7),
            BaseVersion: reader.IsDBNull(8) ? null : reader.GetInt64(8),
            ResultingVersion: reader.GetInt64(9),
            PayloadHash: reader.GetString(10),
            AuthoritySnapshotVersion: reader.GetString(11));

    private static bool MatchesLogicalCommand(AcceptedEvent existing, LanLocalCommandEnvelope envelope, string payloadHash) =>
        string.Equals(existing.CommandCode, envelope.CommandCode, StringComparison.Ordinal) &&
        string.Equals(existing.EventCode, envelope.EventCode, StringComparison.Ordinal) &&
        string.Equals(existing.EntityType, envelope.EntityType, StringComparison.Ordinal) &&
        string.Equals(existing.EntityId, envelope.EntityId, StringComparison.Ordinal) &&
        existing.BaseVersion == envelope.ExpectedBaseVersion &&
        string.Equals(existing.PayloadHash, payloadHash, StringComparison.Ordinal) &&
        string.Equals(existing.DeviceId, envelope.DeviceId, StringComparison.Ordinal) &&
        existing.DeviceSeq == envelope.DeviceSeq;

    private static async Task<LanCurrentState?> ReadCurrentStateAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string stateKey,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT state_key, module_id, entity_type, entity_id, entity_version, state_json, updated_at
            FROM module_current_state
            WHERE state_key=$stateKey
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$stateKey", stateKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new LanCurrentState(
            StateKey: reader.GetString(0),
            ModuleId: reader.GetString(1),
            EntityType: reader.GetString(2),
            EntityId: reader.GetString(3),
            EntityVersion: reader.GetInt64(4),
            StateJson: reader.GetString(5),
            UpdatedAt: reader.GetString(6));
    }

    private static async Task<string?> ReadStateKeyByEntityAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string moduleId,
        string entityType,
        string entityId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT state_key
            FROM module_current_state
            WHERE module_id=$moduleId AND entity_type=$entityType AND entity_id=$entityId
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$moduleId", moduleId);
        command.Parameters.AddWithValue("$entityType", entityType);
        command.Parameters.AddWithValue("$entityId", entityId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }

    private static async Task ApplyCurrentStateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        LanLocalCommandEnvelope envelope,
        string normalizedState,
        LanCurrentState? current,
        long resultingVersion,
        string acceptedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        if (current is null)
        {
            command.CommandText = """
                INSERT INTO module_current_state(
                  state_key, module_id, entity_type, entity_id, entity_version, state_json, updated_at
                ) VALUES ($stateKey, $moduleId, $entityType, $entityId, $version, $state, $updatedAt)
                """;
        }
        else
        {
            command.CommandText = """
                UPDATE module_current_state
                SET entity_version=$version, state_json=$state, updated_at=$updatedAt
                WHERE state_key=$stateKey AND entity_version=$baseVersion
                """;
            command.Parameters.AddWithValue("$baseVersion", current.EntityVersion);
        }
        command.Parameters.AddWithValue("$stateKey", envelope.StateKey);
        command.Parameters.AddWithValue("$moduleId", envelope.ModuleId);
        command.Parameters.AddWithValue("$entityType", envelope.EntityType);
        command.Parameters.AddWithValue("$entityId", envelope.EntityId);
        command.Parameters.AddWithValue("$version", resultingVersion);
        command.Parameters.AddWithValue("$state", normalizedState);
        command.Parameters.AddWithValue("$updatedAt", acceptedAt);
        var changed = await command.ExecuteNonQueryAsync(cancellationToken);
        if (changed != 1)
            throw new LanLocalCommandException("ENTITY_VERSION_CONFLICT", "The guarded local current-state transition did not match exactly one row.");
    }

    private static async Task InsertEdgeEventAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        LanLocalCommandEnvelope envelope,
        RuntimeIdentity runtime,
        string authorityVersion,
        string eventId,
        string normalizedPayload,
        string payloadHash,
        long resultingVersion,
        string acceptedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO edge_events(
              event_id, request_id, idempotency_key, environment, cluster_id,
              device_id, device_seq, edge_instance_id, edge_epoch,
              command_code, event_code, entity_type, entity_id,
              base_version, resulting_version, payload_json, payload_hash,
              accepted_at, authority_snapshot_version
            ) VALUES (
              $eventId, $requestId, $idempotency, $environment, $cluster,
              $deviceId, $deviceSeq, $instanceId, $edgeEpoch,
              $commandCode, $eventCode, $entityType, $entityId,
              $baseVersion, $resultingVersion, $payload, $payloadHash,
              $acceptedAt, $authorityVersion
            )
            """;
        command.Parameters.AddWithValue("$eventId", eventId);
        command.Parameters.AddWithValue("$requestId", envelope.RequestId);
        command.Parameters.AddWithValue("$idempotency", envelope.IdempotencyKey);
        command.Parameters.AddWithValue("$environment", runtime.Environment);
        command.Parameters.AddWithValue("$cluster", runtime.ClusterId);
        command.Parameters.AddWithValue("$deviceId", (object?)envelope.DeviceId ?? DBNull.Value);
        command.Parameters.AddWithValue("$deviceSeq", (object?)envelope.DeviceSeq ?? DBNull.Value);
        command.Parameters.AddWithValue("$instanceId", runtime.EdgeInstanceId);
        command.Parameters.AddWithValue("$edgeEpoch", runtime.EdgeEpoch);
        command.Parameters.AddWithValue("$commandCode", envelope.CommandCode);
        command.Parameters.AddWithValue("$eventCode", envelope.EventCode);
        command.Parameters.AddWithValue("$entityType", envelope.EntityType);
        command.Parameters.AddWithValue("$entityId", envelope.EntityId);
        command.Parameters.AddWithValue("$baseVersion", (object?)envelope.ExpectedBaseVersion ?? DBNull.Value);
        command.Parameters.AddWithValue("$resultingVersion", resultingVersion);
        command.Parameters.AddWithValue("$payload", normalizedPayload);
        command.Parameters.AddWithValue("$payloadHash", payloadHash);
        command.Parameters.AddWithValue("$acceptedAt", acceptedAt);
        command.Parameters.AddWithValue("$authorityVersion", authorityVersion);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertReconciliationAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string eventId,
        string now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO edge_reconciliation_state(event_id, state, updated_at)
            VALUES ($eventId, 'PENDING', $now)
            """;
        command.Parameters.AddWithValue("$eventId", eventId);
        command.Parameters.AddWithValue("$now", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertCloudOutboxAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string eventId,
        string now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO cloud_sync_outbox(outbox_id, event_id, state, created_at, updated_at)
            VALUES ($outboxId, $eventId, 'PENDING', $now, $now)
            """;
        command.Parameters.AddWithValue("$outboxId", $"cloud-{Guid.NewGuid():N}");
        command.Parameters.AddWithValue("$eventId", eventId);
        command.Parameters.AddWithValue("$now", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertGoogleProjectionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string eventId,
        LanGoogleProjectionWork work,
        string now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO google_projection_outbox(
              projection_id, event_id, projection_key, target_key, payload_json,
              state, created_at, updated_at
            ) VALUES (
              $projectionId, $eventId, $projectionKey, $targetKey, $payload,
              'PENDING', $now, $now
            )
            """;
        command.Parameters.AddWithValue("$projectionId", $"gproj-{Guid.NewGuid():N}");
        command.Parameters.AddWithValue("$eventId", eventId);
        command.Parameters.AddWithValue("$projectionKey", work.ProjectionKey);
        command.Parameters.AddWithValue("$targetKey", work.TargetKey);
        command.Parameters.AddWithValue("$payload", CanonicalizeJsonObject(work.PayloadJson, "PROJECTION_PAYLOAD_INVALID"));
        command.Parameters.AddWithValue("$now", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertDriveUploadAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string eventId,
        LanDriveUploadWork work,
        string now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO drive_upload_outbox(
              upload_id, event_id, logical_file_key, local_path, content_hash,
              content_type, size_bytes, state, created_at, updated_at
            ) VALUES (
              $uploadId, $eventId, $logicalFileKey, $localPath, $contentHash,
              $contentType, $sizeBytes, 'PENDING', $now, $now
            )
            """;
        command.Parameters.AddWithValue("$uploadId", $"drive-{Guid.NewGuid():N}");
        command.Parameters.AddWithValue("$eventId", eventId);
        command.Parameters.AddWithValue("$logicalFileKey", work.LogicalFileKey);
        command.Parameters.AddWithValue("$localPath", work.LocalPath);
        command.Parameters.AddWithValue("$contentHash", work.ContentHash);
        command.Parameters.AddWithValue("$contentType", (object?)work.ContentType ?? DBNull.Value);
        command.Parameters.AddWithValue("$sizeBytes", work.SizeBytes);
        command.Parameters.AddWithValue("$now", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string> ReadGoogleOutputStatusAsync(
        SqliteConnection connection,
        string eventId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              (SELECT COUNT(*) FROM google_projection_outbox WHERE event_id=$eventId) +
              (SELECT COUNT(*) FROM drive_upload_outbox WHERE event_id=$eventId)
            """;
        command.Parameters.AddWithValue("$eventId", eventId);
        var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        return count > 0 ? "PENDING" : "NOT_REQUIRED";
    }

    private static async Task<Dictionary<string, string?>> ReadMetaAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT meta_key, meta_value FROM edge_meta WHERE meta_key IN ({string.Join(',', keys.Select((_, index) => $"$k{index}"))})";
        var i = 0;
        foreach (var key in keys) command.Parameters.AddWithValue($"$k{i++}", key);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result[reader.GetString(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
        return result;
    }

    private static string RequireMeta(IReadOnlyDictionary<string, string?> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new LanLocalCommandException("RUNTIME_DEPENDENCY_UNAVAILABLE", $"Required edge metadata is missing: {key}.");
        return value;
    }

    private static void RequireBounded(string? value, string code, int min, int max)
    {
        var length = value?.Length ?? 0;
        if (length < min || length > max) throw new LanLocalCommandException(code, code);
    }

    private static string CanonicalizeJsonObject(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new LanLocalCommandException(code, code);
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new LanLocalCommandException(code, "JSON value must be an object.");
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                WriteCanonical(document.RootElement, writer, code);
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (LanLocalCommandException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new LanLocalCommandException(code, "JSON value is invalid.", error);
        }
    }

    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer, string code)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                var properties = element.EnumerateObject().ToArray();
                if (properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length)
                    throw new LanLocalCommandException(code, "JSON object contains duplicate property names.");
                foreach (var property in properties.OrderBy(p => p.Name, StringComparer.Ordinal))
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
                throw new LanLocalCommandException(code, "Unsupported JSON token.");
        }
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record RuntimeIdentity(string Environment, string ClusterId, string EdgeInstanceId, string EdgeEpoch);
    private sealed record ActiveAuthority(string AuthorityVersion, string CompatibilityVersion);
    private sealed record AcceptedEvent(
        string EventId,
        string RequestId,
        string? DeviceId,
        long? DeviceSeq,
        string CommandCode,
        string EventCode,
        string EntityType,
        string EntityId,
        long? BaseVersion,
        long ResultingVersion,
        string PayloadHash,
        string AuthoritySnapshotVersion);
}

public sealed record LanLocalCommandEnvelope(
    string RequestId,
    string IdempotencyKey,
    string ModuleId,
    string CommandCode,
    string EventCode,
    string EntityType,
    string EntityId,
    string StateKey,
    long? ExpectedBaseVersion,
    string PayloadJson,
    string NextStateJson,
    string? DeviceId = null,
    long? DeviceSeq = null,
    IReadOnlyList<LanGoogleProjectionWork>? GoogleProjectionWork = null,
    IReadOnlyList<LanDriveUploadWork>? DriveUploadWork = null);

public sealed record LanGoogleProjectionWork(
    string ProjectionKey,
    string TargetKey,
    string PayloadJson);

public sealed record LanDriveUploadWork(
    string LogicalFileKey,
    string LocalPath,
    string ContentHash,
    string? ContentType,
    long SizeBytes);

public sealed record LanLocalCommandResult(
    string EventId,
    string RequestId,
    string IdempotencyKey,
    string EntityType,
    string EntityId,
    long ResultingVersion,
    string AuthoritySnapshotVersion,
    string CommitStatus,
    string GoogleOutputStatus,
    bool AlreadyAccepted);

public sealed record LanCurrentState(
    string StateKey,
    string ModuleId,
    string EntityType,
    string EntityId,
    long EntityVersion,
    string StateJson,
    string UpdatedAt);

public sealed class LanLocalCommandException : InvalidOperationException
{
    public LanLocalCommandException(string code, string message) : base(message) => Code = code;
    public LanLocalCommandException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
