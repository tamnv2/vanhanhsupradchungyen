using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class CloudSyncTransportEnvelopeBuilder
{
    private readonly string _connectionString;

    public CloudSyncTransportEnvelopeBuilder(string databasePath)
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

    public async Task<LanCloudReconciliationTransportEnvelope> BuildAsync(
        LanCloudSyncClaim claim,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        var envelope = claim.Envelope ?? throw new LanCloudSyncException("SYNC_ENVELOPE_REQUIRED", "Cloud sync claim envelope is required.");

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
            await pragma.ExecuteNonQueryAsync(cancellationToken);
        }

        var actorUserId = await ScalarAsync(
            connection,
            "SELECT actor_user_id FROM edge_event_actor_evidence WHERE event_id=$eventId LIMIT 1",
            ("$eventId", envelope.EventId),
            cancellationToken);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            throw new LanCloudSyncException(
                "SYNC_ACTOR_EVIDENCE_MISSING",
                "Immutable authenticated actor evidence is required before an edge event can be sent to Cloud reconciliation.");
        }

        var edgeSchemaVersion = await ReadMetaAsync(connection, "edge_schema_version", cancellationToken);
        if (string.IsNullOrWhiteSpace(edgeSchemaVersion))
        {
            throw new LanCloudSyncException("SYNC_EDGE_SCHEMA_MISSING", "Edge schema version metadata is required for Cloud reconciliation.");
        }

        var currentDomainContract = await ReadMetaAsync(connection, "domain_contract_version", cancellationToken);
        if (!string.Equals(currentDomainContract, envelope.DomainContractVersion, StringComparison.Ordinal))
        {
            throw new LanCloudSyncException(
                "SYNC_DOMAIN_CONTRACT_MISMATCH",
                $"Cloud sync claim domain contract {envelope.DomainContractVersion} does not match local metadata {currentDomainContract}.");
        }

        return new LanCloudReconciliationTransportEnvelope(
            EventId: envelope.EventId,
            RequestId: envelope.RequestId,
            IdempotencyKey: envelope.IdempotencyKey,
            Environment: envelope.Environment,
            ClusterId: envelope.ClusterId,
            DeviceId: envelope.DeviceId,
            DeviceSeq: envelope.DeviceSeq,
            EdgeInstanceId: envelope.EdgeInstanceId,
            EdgeEpoch: envelope.EdgeEpoch,
            CommandCode: envelope.CommandCode,
            EventCode: envelope.EventCode,
            EntityType: envelope.EntityType,
            EntityId: envelope.EntityId,
            BaseVersion: envelope.BaseVersion,
            ResultingVersion: envelope.ResultingVersion,
            PayloadJson: envelope.PayloadJson,
            PayloadHash: envelope.PayloadHash,
            AcceptedAt: envelope.AcceptedAt,
            AuthoritySnapshotVersion: envelope.AuthoritySnapshotVersion,
            DomainContractVersion: envelope.DomainContractVersion,
            EdgeSchemaVersion: edgeSchemaVersion,
            ActorUserId: actorUserId,
            CompletedIntegrationReceipts: envelope.CompletedIntegrationReceipts);
    }

    private static async Task<string?> ReadMetaAsync(
        SqliteConnection connection,
        string key,
        CancellationToken cancellationToken)
    {
        return await ScalarAsync(
            connection,
            "SELECT meta_value FROM edge_meta WHERE meta_key=$key LIMIT 1",
            ("$key", key),
            cancellationToken);
    }

    private static async Task<string?> ScalarAsync(
        SqliteConnection connection,
        string sql,
        (string Name, object Value) parameter,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }
}

public sealed record LanCloudReconciliationTransportEnvelope(
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
    string EdgeSchemaVersion,
    string ActorUserId,
    IReadOnlyList<LanIntegrationReceiptEvidence> CompletedIntegrationReceipts);
