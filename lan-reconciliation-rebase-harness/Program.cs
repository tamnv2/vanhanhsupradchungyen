using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanReconciliationRebase.Harness <edge.db>");
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
const string moduleId = "IDENTITY_EMPLOYEE_ATTENDANCE";

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static async Task ExpectRebaseError(string code, Func<Task> action)
{
    try
    {
        await action();
        throw new InvalidOperationException($"{code}_NOT_REJECTED");
    }
    catch (PostReconciliationRebaseException error) when (error.Code == code)
    {
    }
}

static string SnapshotState(params string[] employeeIds)
{
    var employees = string.Join(",", employeeIds.Select((id, index) =>
        $"{{\"employeeId\":\"{id}\",\"entityVersion\":1,\"status\":\"ACTIVE\",\"fullName\":\"Rebase Employee {index + 1}\"}}"));
    return $"{{\"employees\":[{employees}],\"employeeCodes\":[],\"presence\":[]}}";
}

var authorityStore = new AuthoritySnapshotStore(databasePath);
var authority = await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-REBASE-1",
        environment,
        clusterId,
        "cloud-authority-rebase-1",
        compatibility,
        "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
        "{}"),
    environment,
    clusterId,
    compatibility);
Assert(authority.Activated, "AUTHORITY_NOT_ACTIVE");

var operationalStore = new OperationalSnapshotStore(databasePath);
var oldSnapshot = new OperationalSnapshotEnvelope(
    "OP-REBASE-OLD",
    environment,
    clusterId,
    "cloud-operational-rebase-old",
    compatibility,
    "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
    SnapshotState());
var oldImported = await operationalStore.ImportAsync(
    oldSnapshot,
    environment,
    clusterId,
    compatibility,
    new[] { moduleId });
Assert(oldImported.Activated, "OLD_SNAPSHOT_NOT_ACTIVE");

var commandStore = new LocalCommandStore(databasePath, environment, clusterId, compatibility);
var syncStore = new CloudSyncQueueStore(databasePath);
var tracker = new PostReconciliationRebaseTracker(databasePath);

async Task<LanLocalCommandResult> Accept(string suffix)
{
    var employeeId = $"EMP-REBASE-{suffix}";
    return await commandStore.ExecuteAsync(new LanLocalCommandEnvelope(
        RequestId: $"REQ-REBASE-{suffix}",
        IdempotencyKey: $"IDEM-REBASE-{suffix}",
        ModuleId: moduleId,
        CommandCode: "EMPLOYEE_CREATE",
        EventCode: "EMPLOYEE_CREATED",
        EntityType: "employee",
        EntityId: employeeId,
        StateKey: $"employee:{employeeId}",
        ExpectedBaseVersion: null,
        PayloadJson: $"{{\"employeeId\":\"{employeeId}\",\"status\":\"ACTIVE\",\"fullName\":\"Rebase {suffix}\"}}",
        NextStateJson: $"{{\"employeeId\":\"{employeeId}\",\"status\":\"ACTIVE\",\"fullName\":\"Rebase {suffix}\",\"entityVersion\":1}}"));
}

var first = await Accept("1");
var second = await Accept("2");
var canonicalByEvent = new Dictionary<string, string>(StringComparer.Ordinal)
{
    [first.EventId] = "CANONICAL-REBASE-1",
    [second.EventId] = "CANONICAL-REBASE-2"
};

var initialClaims = await syncStore.ClaimDueAsync(10);
Assert(initialClaims.Count == 2, "INITIAL_CLAIM_COUNT_WRONG");
foreach (var claim in initialClaims)
{
    await syncStore.MarkReconciledAsync(
        claim.OutboxId,
        canonicalByEvent[claim.Envelope.EventId],
        DateTimeOffset.UtcNow);
}

var pending = await tracker.InspectAsync();
Assert(pending.Required, "REBASE_NOT_REQUIRED_AFTER_RECONCILIATION");
Assert(pending.PendingCanonicalEventCount == 2, "REBASE_PENDING_COUNT_WRONG");
Assert(pending.CanonicalEventIds.ToHashSet(StringComparer.Ordinal).SetEquals(canonicalByEvent.Values), "REBASE_CANONICAL_SET_WRONG");

await ExpectRebaseError(
    "POST_RECONCILIATION_REBASE_SNAPSHOT_STALE",
    () => tracker.ConfirmAsync(new PostReconciliationRebaseEvidence(
        oldSnapshot.SnapshotVersion,
        oldSnapshot.SourceCheckpoint,
        canonicalByEvent.Values.ToArray())));

var freshSnapshot = new OperationalSnapshotEnvelope(
    "OP-REBASE-FRESH-1",
    environment,
    clusterId,
    "cloud-operational-rebase-fresh-1",
    compatibility,
    "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
    SnapshotState("EMP-REBASE-1", "EMP-REBASE-2"));
var freshImported = await operationalStore.ImportAsync(
    freshSnapshot,
    environment,
    clusterId,
    compatibility,
    new[] { moduleId });
Assert(freshImported.Activated, "FRESH_SNAPSHOT_NOT_ACTIVE");

await ExpectRebaseError(
    "POST_RECONCILIATION_REBASE_COVERAGE_MISSING",
    () => tracker.ConfirmAsync(new PostReconciliationRebaseEvidence(
        freshSnapshot.SnapshotVersion,
        freshSnapshot.SourceCheckpoint,
        new[] { "CANONICAL-REBASE-1" })));

var firstConfirm = await tracker.ConfirmAsync(new PostReconciliationRebaseEvidence(
    freshSnapshot.SnapshotVersion,
    freshSnapshot.SourceCheckpoint,
    new[] { "CANONICAL-REBASE-1", "CANONICAL-REBASE-2" }));
Assert(firstConfirm.Cleared && !firstConfirm.AlreadyComplete, "FIRST_REBASE_NOT_CLEARED");
Assert(firstConfirm.CoveredPendingCanonicalEventCount == 2, "FIRST_REBASE_COVERED_COUNT_WRONG");
Assert(!(await tracker.InspectAsync()).Required, "REBASE_REMAINED_REQUIRED_AFTER_FULL_COVERAGE");

// New reconciliation after the cursor must become pending without reopening older covered events.
var third = await Accept("3");
var thirdClaims = await syncStore.ClaimDueAsync(10);
Assert(thirdClaims.Count == 1 && thirdClaims[0].Envelope.EventId == third.EventId, "THIRD_CLAIM_WRONG");
await syncStore.MarkReconciledAsync(thirdClaims[0].OutboxId, "CANONICAL-REBASE-3", DateTimeOffset.UtcNow);

var afterCursor = await tracker.InspectAsync();
Assert(afterCursor.Required, "NEW_RECONCILIATION_NOT_PENDING_AFTER_CURSOR");
Assert(afterCursor.PendingCanonicalEventCount == 1, "CURSOR_REOPENED_OLD_EVENTS");
Assert(afterCursor.CanonicalEventIds.Count == 1 && afterCursor.CanonicalEventIds[0] == "CANONICAL-REBASE-3", "CURSOR_PENDING_ID_WRONG");

var secondFreshSnapshot = new OperationalSnapshotEnvelope(
    "OP-REBASE-FRESH-2",
    environment,
    clusterId,
    "cloud-operational-rebase-fresh-2",
    compatibility,
    "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
    SnapshotState("EMP-REBASE-1", "EMP-REBASE-2", "EMP-REBASE-3"));
var secondFreshImported = await operationalStore.ImportAsync(
    secondFreshSnapshot,
    environment,
    clusterId,
    compatibility,
    new[] { moduleId });
Assert(secondFreshImported.Activated, "SECOND_FRESH_SNAPSHOT_NOT_ACTIVE");

var secondConfirm = await tracker.ConfirmAsync(new PostReconciliationRebaseEvidence(
    secondFreshSnapshot.SnapshotVersion,
    secondFreshSnapshot.SourceCheckpoint,
    new[] { "CANONICAL-REBASE-3" }));
Assert(secondConfirm.Cleared && secondConfirm.CoveredPendingCanonicalEventCount == 1, "SECOND_REBASE_NOT_CLEARED");
Assert(!(await tracker.InspectAsync()).Required, "FINAL_REBASE_STATE_NOT_CLEAR");

Console.WriteLine("LAN_RECONCILIATION_REBASE_PASS backlog=PASS staleSnapshot=PASS incompleteCoverage=PASS fullCoverage=PASS cursor=PASS newEventAfterCursor=PASS");
return 0;
