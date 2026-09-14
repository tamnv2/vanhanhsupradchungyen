using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
var integrationStore = new LanIntegrationOutputStore(databasePath);
var actorEvidenceStore = new EdgeActorEvidenceStore(databasePath);
var transportBuilder = new CloudSyncTransportEnvelopeBuilder(databasePath);

async Task<LanLocalCommandResult> Accept(
    string suffix,
    IReadOnlyList<LanGoogleProjectionWork>? projections = null,
    IReadOnlyList<LanDriveUploadWork>? driveUploads = null)
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
        GoogleProjectionWork: projections,
        DriveUploadWork: driveUploads));
}

static LanIntegrationCompletion SheetCompletion(
    LanIntegrationWorkClaim claim,
    string providerObjectId,
    string checkpoint,
    string readbackJson) =>
    new(
        LanIntegrationOutputStore.GoogleSheetsTarget,
        claim.WorkId,
        providerObjectId,
        null,
        checkpoint,
        readbackJson);

static LanIntegrationCompletion DriveCompletion(
    LanIntegrationWorkClaim claim,
    string providerObjectId,
    string checkpoint,
    string readbackJson) =>
    new(
        LanIntegrationOutputStore.GoogleDriveTarget,
        claim.WorkId,
        providerObjectId,
        claim.ContentHash,
        checkpoint,
        readbackJson);

// 1) Projection + Drive work are durable, retryable and must have verified receipt/readback evidence before Cloud reconciliation carries them.
const string projectionKey = "PROJ-CLOUD-SYNC-1";
const string driveKey = "DRIVE-CLOUD-SYNC-1";
const string driveHash = "sha256-drive-cloud-sync-1";
var firstAccepted = await Accept(
    "1",
    new[]
    {
        new LanGoogleProjectionWork(projectionKey, "SHEET:EMPLOYEES", "{\"employeeId\":\"EMP-SYNC-1\"}")
    },
    new[]
    {
        new LanDriveUploadWork(driveKey, "/tmp/vhdchy-drive-sync-1.media", driveHash, "image/jpeg", 1234)
    });
Assert(firstAccepted.CommitStatus == "LAN_ACCEPTED_PENDING_SYNC", "FIRST_ACCEPT_FAILED");

var firstIntegrationClaims = await integrationStore.ClaimDueAsync(10);
Assert(firstIntegrationClaims.Count == 2, "FIRST_INTEGRATION_CLAIM_COUNT_WRONG");
var firstSheet = firstIntegrationClaims.Single(item => item.TargetKind == LanIntegrationOutputStore.GoogleSheetsTarget);
var firstDrive = firstIntegrationClaims.Single(item => item.TargetKind == LanIntegrationOutputStore.GoogleDriveTarget);
Assert(firstSheet.LogicalKey == projectionKey && firstSheet.AttemptCount == 1, "FIRST_SHEET_CLAIM_WRONG");
Assert(firstDrive.LogicalKey == driveKey && firstDrive.ContentHash == driveHash, "FIRST_DRIVE_CLAIM_WRONG");

await integrationStore.MarkRetryAsync(
    firstSheet.TargetKind,
    firstSheet.WorkId,
    "GATEWAY_TEMPORARY_FAILURE",
    DateTimeOffset.UtcNow.AddMilliseconds(200));
var firstDriveDone = await integrationStore.CompleteAsync(DriveCompletion(
    firstDrive,
    "drive-object-1",
    "drive-checkpoint-1",
    "{\"provider\":\"drive\",\"size\":1234,\"verified\":true}"));
Assert(firstDriveDone.Completed && !firstDriveDone.AlreadyCompleted, "FIRST_DRIVE_COMPLETION_FAILED");
Assert((await integrationStore.ClaimDueAsync(10)).Count == 0, "INTEGRATION_RETRY_BACKOFF_NOT_RESPECTED");
await Task.Delay(300);
var retriedIntegration = await integrationStore.ClaimDueAsync(10);
Assert(retriedIntegration.Count == 1, "INTEGRATION_RETRY_RECLAIM_COUNT_WRONG");
var retriedSheet = retriedIntegration[0];
Assert(retriedSheet.WorkId == firstSheet.WorkId && retriedSheet.AttemptCount == 2, "INTEGRATION_RETRY_RECLAIM_FAILED");
var firstSheetDone = await integrationStore.CompleteAsync(SheetCompletion(
    retriedSheet,
    "sheet-row:employee:EMP-SYNC-1",
    "sheet-checkpoint-1",
    "{\"key\":\"EMP-SYNC-1\",\"row\":7,\"verified\":true}"));
Assert(firstSheetDone.Completed && !firstSheetDone.AlreadyCompleted, "FIRST_SHEET_COMPLETION_FAILED");

// Same provider evidence is replay-safe even when JSON property order changes.
var firstSheetReplay = await integrationStore.CompleteAsync(SheetCompletion(
    retriedSheet,
    "sheet-row:employee:EMP-SYNC-1",
    "sheet-checkpoint-1",
    "{\"verified\":true,\"row\":7,\"key\":\"EMP-SYNC-1\"}"));
Assert(firstSheetReplay.Completed && firstSheetReplay.AlreadyCompleted, "SHEET_RECEIPT_REPLAY_NOT_IDEMPOTENT");

var firstClaims = await syncStore.ClaimDueAsync(10);
Assert(firstClaims.Count == 1, "FIRST_CLAIM_COUNT_WRONG");
var first = firstClaims[0];
Assert(first.Envelope.EventId == firstAccepted.EventId, "FIRST_CLAIM_EVENT_WRONG");
Assert(first.Envelope.DomainContractVersion == compatibility, "DOMAIN_CONTRACT_NOT_PRESERVED");
Assert(first.Envelope.CompletedIntegrationReceipts.Count == 2, "COMPLETED_RECEIPTS_NOT_ATTACHED");
Assert(first.Envelope.CompletedIntegrationReceipts.Any(item => item.TargetKind == LanIntegrationOutputStore.GoogleSheetsTarget && item.LogicalKey == projectionKey), "SHEET_RECEIPT_NOT_ATTACHED");
Assert(first.Envelope.CompletedIntegrationReceipts.Any(item => item.TargetKind == LanIntegrationOutputStore.GoogleDriveTarget && item.LogicalKey == driveKey && item.ContentHash == driveHash), "DRIVE_RECEIPT_NOT_ATTACHED");

var firstTransport = await transportBuilder.BuildAsync(first);
Assert(firstTransport.ActorUserId == actorUserId, "ACTOR_EVIDENCE_NOT_PRESERVED");
Assert(firstTransport.EdgeSchemaVersion == "VHDCHY_EDGE_V2", "EDGE_SCHEMA_NOT_PRESERVED");
Assert(firstTransport.PayloadJson == first.Envelope.PayloadJson, "PAYLOAD_NOT_PRESERVED");
Assert(firstTransport.CompletedIntegrationReceipts.Count == 2, "TRANSPORT_RECEIPTS_NOT_PRESERVED");

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

// 2) Conflicting provider receipt evidence never silently overwrites the first completion; it becomes REVIEW_REQUIRED.
const string conflictProjectionKey = "PROJ-CLOUD-SYNC-CONFLICT";
var conflictAccepted = await Accept("2", new[]
{
    new LanGoogleProjectionWork(conflictProjectionKey, "SHEET:EMPLOYEES", "{\"employeeId\":\"EMP-SYNC-2\"}")
});
var conflictIntegrationClaim = await integrationStore.ClaimDueAsync(10);
Assert(conflictIntegrationClaim.Count == 1, "CONFLICT_INTEGRATION_CLAIM_WRONG");
var conflictSheet = conflictIntegrationClaim[0];
var conflictInitial = await integrationStore.CompleteAsync(SheetCompletion(
    conflictSheet,
    "sheet-row:employee:EMP-SYNC-2",
    "sheet-checkpoint-conflict-v1",
    "{\"key\":\"EMP-SYNC-2\",\"row\":8,\"verified\":true}"));
Assert(conflictInitial.Completed, "CONFLICT_INITIAL_COMPLETION_FAILED");
var receiptConflict = await integrationStore.CompleteAsync(SheetCompletion(
    conflictSheet,
    "sheet-row:employee:EMP-SYNC-2",
    "sheet-checkpoint-conflict-v2",
    "{\"key\":\"EMP-SYNC-2\",\"row\":9,\"verified\":true}"));
Assert(receiptConflict.ReviewRequired && !receiptConflict.Completed, "RECEIPT_CONFLICT_NOT_REVIEW_REQUIRED");

var conflictClaims = await syncStore.ClaimDueAsync(10);
Assert(conflictClaims.Count == 1 && conflictClaims[0].Envelope.EventId == conflictAccepted.EventId, "CONFLICT_CLAIM_FAILED");
Assert(conflictClaims[0].Envelope.CompletedIntegrationReceipts.Count == 0, "REVIEW_RECEIPT_LEAKED_INTO_RECONCILIATION");
var conflictId = await syncStore.MarkConflictAsync(
    conflictClaims[0].OutboxId,
    "ENTITY_VERSION_CONFLICT",
    "{\"canonicalVersion\":2,\"edgeBaseVersion\":1}");
Assert(!string.IsNullOrWhiteSpace(conflictId), "CONFLICT_ID_MISSING");

// 3) Process restart recovers both integration work and Cloud reconciliation claims without losing idempotency.
const string restartProjectionKey = "PROJ-CLOUD-SYNC-RESTART";
var interruptedAccepted = await Accept("3", new[]
{
    new LanGoogleProjectionWork(restartProjectionKey, "SHEET:EMPLOYEES", "{\"employeeId\":\"EMP-SYNC-3\"}")
});
var interruptedIntegrationClaim = await integrationStore.ClaimDueAsync(1);
Assert(interruptedIntegrationClaim.Count == 1 && interruptedIntegrationClaim[0].LogicalKey == restartProjectionKey, "INTERRUPTED_INTEGRATION_CLAIM_FAILED");
var restartedIntegrationStore = new LanIntegrationOutputStore(databasePath);
Assert(await restartedIntegrationStore.RecoverInterruptedClaimsAsync() == 1, "INTERRUPTED_INTEGRATION_RECOVERY_COUNT_WRONG");
var recoveredIntegrationClaim = await restartedIntegrationStore.ClaimDueAsync(1);
Assert(recoveredIntegrationClaim.Count == 1 && recoveredIntegrationClaim[0].AttemptCount == 2, "INTERRUPTED_INTEGRATION_RECLAIM_FAILED");
var recoveredIntegrationDone = await restartedIntegrationStore.CompleteAsync(SheetCompletion(
    recoveredIntegrationClaim[0],
    "sheet-row:employee:EMP-SYNC-3",
    "sheet-checkpoint-restart",
    "{\"key\":\"EMP-SYNC-3\",\"row\":10,\"verified\":true}"));
Assert(recoveredIntegrationDone.Completed, "INTERRUPTED_INTEGRATION_COMPLETION_FAILED");

var interruptedClaim = await syncStore.ClaimDueAsync(1);
Assert(interruptedClaim.Count == 1 && interruptedClaim[0].Envelope.EventId == interruptedAccepted.EventId, "INTERRUPTED_CLAIM_FAILED");
Assert(interruptedClaim[0].Envelope.CompletedIntegrationReceipts.Count == 1, "RESTART_RECEIPT_NOT_ATTACHED");
var restartedStore = new CloudSyncQueueStore(databasePath);
Assert(await restartedStore.RecoverInterruptedClaimsAsync() == 1, "INTERRUPTED_RECOVERY_COUNT_WRONG");
var recoveredClaim = await restartedStore.ClaimDueAsync(1);
Assert(recoveredClaim.Count == 1 && recoveredClaim[0].AttemptCount == 2, "INTERRUPTED_RECLAIM_FAILED");
await restartedStore.MarkReconciledAsync(recoveredClaim[0].OutboxId, "CANONICAL-SYNC-3", DateTimeOffset.UtcNow);

// 4) Two workers racing for one integration item or one Cloud item cannot both own it.
const string raceProjectionKey = "PROJ-CLOUD-SYNC-RACE";
var raceAccepted = await Accept("4", new[]
{
    new LanGoogleProjectionWork(raceProjectionKey, "SHEET:EMPLOYEES", "{\"employeeId\":\"EMP-SYNC-4\"}")
});
var integrationRaceAStore = new LanIntegrationOutputStore(databasePath);
var integrationRaceBStore = new LanIntegrationOutputStore(databasePath);
var integrationRaceResults = await Task.WhenAll(
    integrationRaceAStore.ClaimDueAsync(1),
    integrationRaceBStore.ClaimDueAsync(1));
var integrationClaimed = integrationRaceResults.SelectMany(value => value).ToArray();
Assert(integrationClaimed.Length == 1 && integrationClaimed[0].LogicalKey == raceProjectionKey, "INTEGRATION_CLAIM_RACE_NOT_SERIALIZED");
var raceIntegrationDone = await integrationStore.CompleteAsync(SheetCompletion(
    integrationClaimed[0],
    "sheet-row:employee:EMP-SYNC-4",
    "sheet-checkpoint-race",
    "{\"key\":\"EMP-SYNC-4\",\"row\":11,\"verified\":true}"));
Assert(raceIntegrationDone.Completed, "RACE_INTEGRATION_COMPLETION_FAILED");

var raceAStore = new CloudSyncQueueStore(databasePath);
var raceBStore = new CloudSyncQueueStore(databasePath);
var raceResults = await Task.WhenAll(raceAStore.ClaimDueAsync(1), raceBStore.ClaimDueAsync(1));
var claimed = raceResults.SelectMany(value => value).ToArray();
Assert(claimed.Length == 1 && claimed[0].Envelope.EventId == raceAccepted.EventId, "CLAIM_RACE_NOT_SERIALIZED");
Assert(claimed[0].Envelope.CompletedIntegrationReceipts.Count == 1, "RACE_RECEIPT_NOT_ATTACHED");
await raceAStore.MarkReconciledAsync(claimed[0].OutboxId, "CANONICAL-SYNC-4", DateTimeOffset.UtcNow);

var integrationInspection = await integrationStore.InspectAsync();
Assert(integrationInspection.Pending == 0, "INTEGRATION_PENDING_NOT_DRAINED");
Assert(integrationInspection.Processing == 0, "INTEGRATION_PROCESSING_NOT_DRAINED");
Assert(integrationInspection.RetryWait == 0, "INTEGRATION_RETRY_NOT_DRAINED");
Assert(integrationInspection.Completed == 4, "INTEGRATION_COMPLETED_COUNT_WRONG");
Assert(integrationInspection.ReviewRequired == 1, "INTEGRATION_REVIEW_COUNT_WRONG");
Assert(integrationInspection.CompletedReceipts == 4, "COMPLETED_RECEIPT_COUNT_WRONG");
Assert(integrationInspection.ReviewReceipts == 1, "REVIEW_RECEIPT_COUNT_WRONG");

var inspection = await syncStore.InspectAsync();
Assert(inspection.Total == 4, "QUEUE_TOTAL_WRONG");
Assert(inspection.Reconciled == 3, "QUEUE_RECONCILED_WRONG");
Assert(inspection.Conflict == 1, "QUEUE_CONFLICT_WRONG");
Assert(inspection.Pending == 0 && inspection.Synchronizing == 0 && inspection.RetryWait == 0, "QUEUE_NOT_DRAINED");

Console.WriteLine("LAN_CLOUD_SYNC_QUEUE_PASS integrationClaim=PASS integrationRetry=PASS receiptReadback=PASS receiptReplay=PASS receiptConflictReview=PASS driveHashReceipt=PASS integrationRestartRecovery=PASS integrationClaimRace=PASS receiptEnvelope=PASS actorTransport=PASS edgeSchemaTransport=PASS signedHttp=PASS bodyHash=PASS camelCase=PASS retry=PASS reconcile=PASS conflict=PASS restartRecovery=PASS claimRace=PASS total=4 reconciled=3 conflict=1 integrationCompleted=4 integrationReview=1");
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
