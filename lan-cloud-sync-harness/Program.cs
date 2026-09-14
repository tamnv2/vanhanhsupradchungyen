using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanCloudSync.Harness <edge.db>");
    return 2;
}

var databasePath = Path.GetFullPath(args[0]);
if (!File.Exists(databasePath))
{
    Console.Error.WriteLine($"Edge database not found: {databasePath}");
    return 2;
}

const string environment = "BETA";
const string clusterId = "PICK_PACK_1291";
const string compatibility = "VHDCHY_DOMAIN_V1";
const string actorUserId = "USER-SYNC-ACTOR";
const string machineKeyId = "lan-beta-harness-01";
const string machineKeyMaterial = "test-only-lan-cloud-reconciliation-key-material-2026";

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static async Task ExpectSyncError(string code, Func<Task> action)
{
    try
    {
        await action();
        throw new InvalidOperationException($"{code}_NOT_REJECTED");
    }
    catch (LanCloudSyncException error) when (error.Code == code)
    {
    }
}

var authorityStore = new AuthoritySnapshotStore(databasePath);
var authority = await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-CLOUD-SYNC-1",
        environment,
        clusterId,
        "cloud-sync-harness",
        compatibility,
        "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
        "{}"),
    environment,
    clusterId,
    compatibility);
Assert(authority.Activated, "AUTHORITY_NOT_ACTIVE");

var commandStore = new LocalCommandStore(databasePath, environment, clusterId, compatibility);
var syncStore = new CloudSyncQueueStore(databasePath);
var actorEvidenceStore = new EdgeActorEvidenceStore(databasePath);
var transportBuilder = new CloudSyncTransportEnvelopeBuilder(databasePath);

async Task<LanLocalCommandResult> Accept(string suffix, IReadOnlyList<LanGoogleProjectionWork>? projections = null)
{
    var requestId = $"REQ-SYNC-{suffix}";
    var idempotencyKey = $"IDEM-SYNC-{suffix}";
    await actorEvidenceStore.StageAsync(requestId, idempotencyKey, actorUserId);

    return await commandStore.ExecuteAsync(new LanLocalCommandEnvelope(
        RequestId: requestId,
        IdempotencyKey: idempotencyKey,
        ModuleId: "IDENTITY_EMPLOYEE_ATTENDANCE",
        CommandCode: "EMPLOYEE_CREATE",
        EventCode: "EMPLOYEE_CREATED",
        EntityType: "employee",
        EntityId: $"EMP-SYNC-{suffix}",
        StateKey: $"employee:EMP-SYNC-{suffix}",
        ExpectedBaseVersion: null,
        PayloadJson: $"{{\"employeeId\":\"EMP-SYNC-{suffix}\"}}",
        NextStateJson: $"{{\"employeeId\":\"EMP-SYNC-{suffix}\",\"status\":\"ACTIVE\"}}",
        GoogleProjectionWork: projections));
}

async Task InsertCompletedProjectionReceipt(string logicalKey)
{
    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadWrite,
        Pooling = false
    }.ToString();
    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    string Meta(string key)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT meta_value FROM edge_meta WHERE meta_key=$key";
        command.Parameters.AddWithValue("$key", key);
        return Convert.ToString(command.ExecuteScalar()) ?? throw new InvalidOperationException($"META_MISSING_{key}");
    }

    await using var insert = connection.CreateCommand();
    insert.CommandText = """
        INSERT INTO integration_receipts(
          receipt_id, logical_key, target_kind, provider_object_id, content_hash,
          checkpoint, readback_evidence_json, producer_edge_instance_id,
          producer_edge_epoch, completed_at, status
        ) VALUES (
          $receiptId, $logicalKey, 'GOOGLE_SHEETS', 'TEST-ROW', NULL,
          'TEST-CHECKPOINT', '{"fixture":true}', $instanceId,
          $edgeEpoch, $completedAt, 'COMPLETED'
        )
        """;
    insert.Parameters.AddWithValue("$receiptId", $"receipt-{Guid.NewGuid():N}");
    insert.Parameters.AddWithValue("$logicalKey", logicalKey);
    insert.Parameters.AddWithValue("$instanceId", Meta("instance_id"));
    insert.Parameters.AddWithValue("$edgeEpoch", Meta("edge_epoch"));
    insert.Parameters.AddWithValue("$completedAt", DateTimeOffset.UtcNow.ToString("O"));
    await insert.ExecuteNonQueryAsync();
}

// 1) Claim -> signed HTTP envelope -> retry wait -> reclaim -> reconcile, including completed Google receipt evidence.
const string projectionKey = "PROJ-CLOUD-SYNC-1";
var firstAccepted = await Accept("1", new[]
{
    new LanGoogleProjectionWork(projectionKey, "SHEET:EMPLOYEES", "{\"employeeId\":\"EMP-SYNC-1\"}")
});
Assert(firstAccepted.CommitStatus == "LAN_ACCEPTED_PENDING_SYNC", "FIRST_ACCEPT_FAILED");
await InsertCompletedProjectionReceipt(projectionKey);

var firstClaims = await syncStore.ClaimDueAsync(10);
Assert(firstClaims.Count == 1, "FIRST_CLAIM_COUNT_WRONG");
var first = firstClaims[0];
Assert(first.Envelope.EventId == firstAccepted.EventId, "FIRST_CLAIM_EVENT_WRONG");
Assert(first.Envelope.DomainContractVersion == compatibility, "DOMAIN_CONTRACT_NOT_PRESERVED");
Assert(first.Envelope.CompletedIntegrationReceipts.Count == 1, "COMPLETED_RECEIPT_NOT_ATTACHED");
Assert(first.Envelope.CompletedIntegrationReceipts[0].LogicalKey == projectionKey, "RECEIPT_LOGICAL_KEY_WRONG");

var firstTransport = await transportBuilder.BuildAsync(first);
Assert(firstTransport.ActorUserId == actorUserId, "ACTOR_EVIDENCE_NOT_PRESERVED");
Assert(firstTransport.EdgeSchemaVersion == "VHDCHY_EDGE_V2", "EDGE_SCHEMA_NOT_PRESERVED");
Assert(firstTransport.PayloadJson == first.Envelope.PayloadJson, "PAYLOAD_NOT_PRESERVED");
Assert(firstTransport.CompletedIntegrationReceipts.Count == 1, "TRANSPORT_RECEIPT_NOT_PRESERVED");

var signingHandler = new ReconciliationSigningHandler(environment, machineKeyId, machineKeyMaterial);
using (var signingClient = new HttpClient(signingHandler))
{
    var sender = new CloudReconciliationHttpSender(
        signingClient,
        new Uri("https://beta.example/api/v1/reconciliation/events"),
        environment,
        machineKeyId,
        machineKeyMaterial);
    var sendResult = await sender.SendAsync(firstTransport);
    Assert(sendResult.Outcome == CloudReconciliationHttpOutcome.Received, "SIGNED_HTTP_RESULT_WRONG");
    Assert(signingHandler.SignatureValid, "SIGNED_HTTP_SIGNATURE_INVALID");
    Assert(signingHandler.BodyHashValid, "SIGNED_HTTP_BODY_HASH_INVALID");
    Assert(signingHandler.CamelCaseEnvelope, "SIGNED_HTTP_BODY_NOT_CAMEL_CASE");
}

await syncStore.MarkRetryAsync(first.OutboxId, "TEST_RETRY", DateTimeOffset.UtcNow.AddMilliseconds(200));
Assert((await syncStore.ClaimDueAsync(10)).Count == 0, "RETRY_BACKOFF_NOT_RESPECTED");
await Task.Delay(300);
var retried = await syncStore.ClaimDueAsync(10);
Assert(retried.Count == 1 && retried[0].AttemptCount == 2, "RETRY_RECLAIM_FAILED");
await syncStore.MarkReconciledAsync(retried[0].OutboxId, "CANONICAL-SYNC-1", DateTimeOffset.UtcNow);
await ExpectSyncError("SYNC_CLAIM_STATE_INVALID", () =>
    syncStore.MarkReconciledAsync(retried[0].OutboxId, "CANONICAL-DUP", DateTimeOffset.UtcNow));

// 2) Explicit conflict evidence is durable and leaves no silent overwrite path.
var conflictAccepted = await Accept("2");
var conflictClaims = await syncStore.ClaimDueAsync(10);
Assert(conflictClaims.Count == 1 && conflictClaims[0].Envelope.EventId == conflictAccepted.EventId, "CONFLICT_CLAIM_FAILED");
var conflictId = await syncStore.MarkConflictAsync(
    conflictClaims[0].OutboxId,
    "ENTITY_VERSION_CONFLICT",
    "{\"canonicalVersion\":2,\"edgeBaseVersion\":1}");
Assert(!string.IsNullOrWhiteSpace(conflictId), "CONFLICT_ID_MISSING");

// 3) A process restart recovers an interrupted SYNCHRONIZING claim and retries it.
var interruptedAccepted = await Accept("3");
var interruptedClaim = await syncStore.ClaimDueAsync(1);
Assert(interruptedClaim.Count == 1 && interruptedClaim[0].Envelope.EventId == interruptedAccepted.EventId, "INTERRUPTED_CLAIM_FAILED");
var restartedStore = new CloudSyncQueueStore(databasePath);
Assert(await restartedStore.RecoverInterruptedClaimsAsync() == 1, "INTERRUPTED_RECOVERY_COUNT_WRONG");
var recoveredClaim = await restartedStore.ClaimDueAsync(1);
Assert(recoveredClaim.Count == 1 && recoveredClaim[0].AttemptCount == 2, "INTERRUPTED_RECLAIM_FAILED");
await restartedStore.MarkReconciledAsync(recoveredClaim[0].OutboxId, "CANONICAL-SYNC-3", DateTimeOffset.UtcNow);

// 4) Two workers racing for one due item cannot both own it.
var raceAccepted = await Accept("4");
var raceAStore = new CloudSyncQueueStore(databasePath);
var raceBStore = new CloudSyncQueueStore(databasePath);
var raceResults = await Task.WhenAll(raceAStore.ClaimDueAsync(1), raceBStore.ClaimDueAsync(1));
var claimed = raceResults.SelectMany(value => value).ToArray();
Assert(claimed.Length == 1 && claimed[0].Envelope.EventId == raceAccepted.EventId, "CLAIM_RACE_NOT_SERIALIZED");
await raceAStore.MarkReconciledAsync(claimed[0].OutboxId, "CANONICAL-SYNC-4", DateTimeOffset.UtcNow);

var inspection = await syncStore.InspectAsync();
Assert(inspection.Total == 4, "QUEUE_TOTAL_WRONG");
Assert(inspection.Reconciled == 3, "QUEUE_RECONCILED_WRONG");
Assert(inspection.Conflict == 1, "QUEUE_CONFLICT_WRONG");
Assert(inspection.Pending == 0 && inspection.Synchronizing == 0 && inspection.RetryWait == 0, "QUEUE_NOT_DRAINED");

Console.WriteLine("LAN_CLOUD_SYNC_QUEUE_PASS claim=PASS receiptEnvelope=PASS actorTransport=PASS edgeSchemaTransport=PASS signedHttp=PASS bodyHash=PASS camelCase=PASS retry=PASS reconcile=PASS conflict=PASS restartRecovery=PASS claimRace=PASS total=4 reconciled=3 conflict=1");
return 0;

sealed class ReconciliationSigningHandler : HttpMessageHandler
{
    private readonly string _environment;
    private readonly string _keyId;
    private readonly byte[] _keyMaterial;

    public ReconciliationSigningHandler(string environment, string keyId, string keyMaterial)
    {
        _environment = environment;
        _keyId = keyId;
        _keyMaterial = Encoding.UTF8.GetBytes(keyMaterial);
    }

    public bool SignatureValid { get; private set; }
    public bool BodyHashValid { get; private set; }
    public bool CamelCaseEnvelope { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = await request.Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        CamelCaseEnvelope = document.RootElement.TryGetProperty("eventId", out _) && !document.RootElement.TryGetProperty("EventId", out _);

        var suppliedBodyHash = request.Headers.GetValues("X-VHDCHY-Content-SHA256").Single();
        var calculatedBodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        BodyHashValid = string.Equals(suppliedBodyHash, calculatedBodyHash, StringComparison.Ordinal);

        var timestamp = request.Headers.GetValues("X-VHDCHY-Machine-Timestamp").Single();
        var nonce = request.Headers.GetValues("X-VHDCHY-Machine-Nonce").Single();
        var keyId = request.Headers.GetValues("X-VHDCHY-Machine-Key-Id").Single();
        var suppliedSignature = request.Headers.GetValues("X-VHDCHY-Machine-Signature").Single();
        var canonical = string.Join('\n',
            CloudReconciliationHttpSender.AuthVersion,
            "POST",
            request.RequestUri!.AbsolutePath,
            timestamp,
            nonce,
            calculatedBodyHash,
            _environment,
            _keyId);
        var expectedSignature = Convert.ToHexString(HMACSHA256.HashData(_keyMaterial, Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        SignatureValid = keyId == _keyId && CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(expectedSignature),
            Convert.FromHexString(suppliedSignature));

        return new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent("{\"ok\":true,\"reconciliationStatus\":\"RECEIVED\",\"edgeEventId\":\"edge-test\",\"duplicate\":false}", Encoding.UTF8, "application/json")
        };
    }
}
