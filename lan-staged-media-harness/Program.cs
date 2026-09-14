using System.Text;
using Vhdchy.LanService;

if (args.Length != 2 || string.IsNullOrWhiteSpace(args[0]) || string.IsNullOrWhiteSpace(args[1]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanStagedMedia.Harness <stage|link> <edge.db>");
    return 2;
}

var mode = args[0].Trim().ToLowerInvariant();
var databasePath = Path.GetFullPath(args[1]);
if (!File.Exists(databasePath))
{
    Console.Error.WriteLine($"Edge database not found: {databasePath}");
    return 2;
}

var dataRoot = Path.GetDirectoryName(databasePath)!;
var stagingRoot = Path.Combine(dataRoot, "staged-media");
var store = new LanStagedMediaStore(databasePath, stagingRoot);

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static MemoryStream Media(string value) => new(Encoding.UTF8.GetBytes(value), writable: false);

static async Task ExpectStageError(string code, Func<Task> action)
{
    try
    {
        await action();
        throw new InvalidOperationException($"{code}_NOT_REJECTED");
    }
    catch (LanStagedMediaException error) when (error.Code == code)
    {
    }
}

if (mode == "stage")
{
    var first = await store.StageAsync("MEDIA-RESTART-1", Media("vhdchy-staged-media-restart-proof"), "image/jpeg");
    Assert(!first.AlreadyStaged && first.State == "STAGED", "FIRST_STAGE_FAILED");
    Assert(first.SizeBytes > 0 && File.Exists(first.LocalPath), "FIRST_STAGE_FILE_MISSING");

    var replay = await store.StageAsync("MEDIA-RESTART-1", Media("vhdchy-staged-media-restart-proof"), "image/jpeg");
    Assert(replay.AlreadyStaged && replay.ContentHash == first.ContentHash, "STAGE_REPLAY_NOT_IDEMPOTENT");

    await ExpectStageError("STAGED_MEDIA_IDENTITY_CONFLICT", () =>
        store.StageAsync("MEDIA-RESTART-1", Media("different-bytes"), "image/jpeg"));

    var link = await store.StageAsync("MEDIA-LINK-1", Media("vhdchy-staged-media-link-proof"), "image/png");
    var mismatch = await store.StageAsync("MEDIA-MISMATCH-1", Media("vhdchy-staged-media-mismatch-proof"), "image/webp");
    Assert(link.State == "STAGED" && mismatch.State == "STAGED", "LINK_FIXTURE_STAGE_FAILED");

    var raceStoreA = new LanStagedMediaStore(databasePath, stagingRoot);
    var raceStoreB = new LanStagedMediaStore(databasePath, stagingRoot);
    var raceA = raceStoreA.StageAsync("MEDIA-RACE-1", Media("same-race-bytes"), "image/jpeg");
    var raceB = raceStoreB.StageAsync("MEDIA-RACE-1", Media("same-race-bytes"), "image/jpeg");
    var raceResults = await Task.WhenAll(raceA, raceB);
    Assert(raceResults.Count(value => value.AlreadyStaged) == 1, "STAGE_RACE_DID_NOT_SERIALIZE");
    Assert(raceResults.Select(value => value.ContentHash).Distinct(StringComparer.Ordinal).Count() == 1, "STAGE_RACE_HASH_DIVERGED");

    var inspection = await store.InspectAsync();
    Assert(inspection.Total == 4 && inspection.Staged == 4 && inspection.Queued == 0, "STAGE_INSPECTION_WRONG");
    Console.WriteLine("LAN_STAGED_MEDIA_STAGE_PASS durable=PASS replay=PASS identityConflict=PASS race=PASS count=4");
    return 0;
}

if (mode != "link")
{
    Console.Error.WriteLine($"Unsupported mode: {mode}");
    return 2;
}

var restartProof = await store.ReadVerifiedAsync("MEDIA-RESTART-1");
var linkProof = await store.ReadVerifiedAsync("MEDIA-LINK-1");
Assert(restartProof.State == "STAGED" && linkProof.State == "STAGED", "RESTART_DURABILITY_FAILED");

const string environment = "BETA";
const string clusterId = "PICK_PACK_1291";
const string compatibility = "VHDCHY_DOMAIN_V1";
var authorityStore = new AuthoritySnapshotStore(databasePath);
var authority = await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-STAGED-MEDIA-1",
        environment,
        clusterId,
        "staged-media-harness",
        compatibility,
        "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
        "{}"),
    environment,
    clusterId,
    compatibility);
Assert(authority.Activated, "STAGED_MEDIA_AUTHORITY_NOT_ACTIVE");

var commandStore = new LocalCommandStore(databasePath, environment, clusterId, compatibility);
var driveWork = await store.CreateDriveUploadWorkAsync("MEDIA-LINK-1");
var linked = await commandStore.ExecuteAsync(new LanLocalCommandEnvelope(
    RequestId: "REQ-STAGED-MEDIA-LINK",
    IdempotencyKey: "IDEM-STAGED-MEDIA-LINK",
    ModuleId: "IDENTITY_EMPLOYEE_ATTENDANCE",
    CommandCode: "EMPLOYEE_CREATE",
    EventCode: "EMPLOYEE_CREATED",
    EntityType: "employee",
    EntityId: "EMP-STAGED-MEDIA-LINK",
    StateKey: "employee:EMP-STAGED-MEDIA-LINK",
    ExpectedBaseVersion: null,
    PayloadJson: "{\"employeeId\":\"EMP-STAGED-MEDIA-LINK\"}",
    NextStateJson: "{\"employeeId\":\"EMP-STAGED-MEDIA-LINK\",\"status\":\"ACTIVE\"}",
    DriveUploadWork: new[] { driveWork }));
Assert(linked.CommitStatus == "LAN_ACCEPTED_PENDING_SYNC" && linked.GoogleOutputStatus == "PENDING", "STAGED_MEDIA_LINK_COMMAND_FAILED");

var linkedMedia = await store.ReadVerifiedAsync("MEDIA-LINK-1");
Assert(linkedMedia.State == "QUEUED" && linkedMedia.LinkedEventId == linked.EventId, "STAGED_MEDIA_OUTBOX_LINK_FAILED");
await ExpectStageError("STAGED_MEDIA_STATE_INVALID", async () =>
{
    _ = await store.CreateDriveUploadWorkAsync("MEDIA-LINK-1");
});

var eventsBeforeMismatch = await commandStore.ReadAcceptedEventCountAsync();
var outboxBeforeMismatch = await commandStore.ReadCloudOutboxCountAsync();
var mismatchProof = await store.ReadVerifiedAsync("MEDIA-MISMATCH-1");
try
{
    await commandStore.ExecuteAsync(new LanLocalCommandEnvelope(
        RequestId: "REQ-STAGED-MEDIA-MISMATCH",
        IdempotencyKey: "IDEM-STAGED-MEDIA-MISMATCH",
        ModuleId: "IDENTITY_EMPLOYEE_ATTENDANCE",
        CommandCode: "EMPLOYEE_CREATE",
        EventCode: "EMPLOYEE_CREATED",
        EntityType: "employee",
        EntityId: "EMP-STAGED-MEDIA-MISMATCH",
        StateKey: "employee:EMP-STAGED-MEDIA-MISMATCH",
        ExpectedBaseVersion: null,
        PayloadJson: "{\"employeeId\":\"EMP-STAGED-MEDIA-MISMATCH\"}",
        NextStateJson: "{\"employeeId\":\"EMP-STAGED-MEDIA-MISMATCH\",\"status\":\"ACTIVE\"}",
        DriveUploadWork: new[]
        {
            new LanDriveUploadWork(
                mismatchProof.LogicalFileKey,
                mismatchProof.LocalPath,
                new string('0', 64),
                mismatchProof.ContentType,
                mismatchProof.SizeBytes)
        }));
    throw new InvalidOperationException("STAGED_MEDIA_EVIDENCE_MISMATCH_NOT_REJECTED");
}
catch (LanLocalCommandException error) when (
    error.Code == "LOCAL_COMMAND_COMMIT_FAILED" &&
    error.InnerException is Microsoft.Data.Sqlite.SqliteException sqlite &&
    sqlite.Message.Contains("VHDCHY_STAGED_MEDIA_EVIDENCE_MISMATCH", StringComparison.Ordinal))
{
}
Assert(await commandStore.ReadAcceptedEventCountAsync() == eventsBeforeMismatch, "STAGED_MEDIA_MISMATCH_APPENDED_EVENT");
Assert(await commandStore.ReadCloudOutboxCountAsync() == outboxBeforeMismatch, "STAGED_MEDIA_MISMATCH_APPENDED_CLOUD_OUTBOX");
Assert(await commandStore.ReadCurrentStateAsync("employee:EMP-STAGED-MEDIA-MISMATCH") is null, "STAGED_MEDIA_MISMATCH_MUTATED_STATE");
Assert((await store.ReadVerifiedAsync("MEDIA-MISMATCH-1")).State == "STAGED", "STAGED_MEDIA_MISMATCH_CHANGED_STAGE_STATE");

await File.AppendAllTextAsync(restartProof.LocalPath, "tamper");
await ExpectStageError("STAGED_MEDIA_FILE_MISMATCH", async () =>
{
    _ = await store.ReadVerifiedAsync("MEDIA-RESTART-1");
});

var finalInspection = await store.InspectAsync();
Assert(finalInspection.Total == 4 && finalInspection.Queued == 1 && finalInspection.Staged == 3, "FINAL_STAGED_MEDIA_INSPECTION_WRONG");
Console.WriteLine("LAN_STAGED_MEDIA_LINK_PASS restart=PASS verifiedHash=PASS outboxLink=PASS mismatchRollback=PASS tamperDetection=PASS providerReceiptFabricated=NO");
return 0;
