using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanService.Harness <edge.db>");
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
const string scope = "{\"clusterId\":\"PICK_PACK_1291\",\"modules\":[\"PICK_PACK\"]}";
const string payload1 = "{\"users\":[],\"permissionCatalogVersion\":\"VHDCHY_PERMISSION_CATALOG_V1\"}";
const string payload2 = "{\"users\":[],\"permissionCatalogVersion\":\"VHDCHY_PERMISSION_CATALOG_V1\",\"generation\":2}";

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static async Task ExpectLocalCommandError(string code, Func<Task> action)
{
    try
    {
        await action();
        throw new InvalidOperationException($"{code}_NOT_REJECTED");
    }
    catch (LanLocalCommandException error) when (error.Code == code)
    {
    }
}

var authorityStore = new AuthoritySnapshotStore(databasePath);

var a1 = new AuthoritySnapshotEnvelope(
    "AUTH-TEST-1",
    environment,
    clusterId,
    "cloud-checkpoint-1",
    compatibility,
    scope,
    payload1);

var first = await authorityStore.ImportAsync(a1, environment, clusterId, compatibility);
Assert(first.Activated && !first.AlreadyKnown && first.Status == "ACTIVE", "A1_FIRST_ACTIVATION_FAILED");
Assert(await authorityStore.ReadActiveVersionAsync() == "AUTH-TEST-1", "A1_NOT_ACTIVE");

var authorityReplay = await authorityStore.ImportAsync(a1, environment, clusterId, compatibility);
Assert(authorityReplay.AlreadyKnown && authorityReplay.Activated && authorityReplay.Status == "ACTIVE", "A1_REPLAY_NOT_IDEMPOTENT");

try
{
    await authorityStore.ImportAsync(a1 with { PayloadJson = payload2 }, environment, clusterId, compatibility);
    throw new InvalidOperationException("A1_CONFLICT_NOT_REJECTED");
}
catch (AuthoritySnapshotException error) when (error.Code == "AUTHORITY_VERSION_PAYLOAD_CONFLICT")
{
}
Assert(await authorityStore.ReadActiveVersionAsync() == "AUTH-TEST-1", "A1_LOST_AFTER_CONFLICT");

try
{
    await authorityStore.ImportAsync(
        new AuthoritySnapshotEnvelope(
            "AUTH-TEST-BAD",
            environment,
            clusterId,
            "cloud-checkpoint-bad",
            compatibility,
            scope,
            "{not-json"),
        environment,
        clusterId,
        compatibility);
    throw new InvalidOperationException("INVALID_JSON_NOT_REJECTED");
}
catch (AuthoritySnapshotException error) when (error.Code == "AUTHORITY_PAYLOAD_INVALID")
{
}
Assert(await authorityStore.ReadActiveVersionAsync() == "AUTH-TEST-1", "A1_LOST_AFTER_INVALID_IMPORT");
Assert(await authorityStore.ReadStatusAsync("AUTH-TEST-BAD") is null, "INVALID_IMPORT_PERSISTED");

var a2 = new AuthoritySnapshotEnvelope(
    "AUTH-TEST-2",
    environment,
    clusterId,
    "cloud-checkpoint-2",
    compatibility,
    scope,
    payload2);
var second = await authorityStore.ImportAsync(a2, environment, clusterId, compatibility);
Assert(second.Activated && second.Status == "ACTIVE", "A2_ACTIVATION_FAILED");
Assert(await authorityStore.ReadActiveVersionAsync() == "AUTH-TEST-2", "A2_NOT_ACTIVE");
Assert(await authorityStore.ReadStatusAsync("AUTH-TEST-1") == "REPLACED", "A1_NOT_REPLACED");
Assert(await authorityStore.ReadStatusAsync("AUTH-TEST-2") == "ACTIVE", "A2_STATUS_WRONG");

try
{
    await authorityStore.ImportAsync(
        new AuthoritySnapshotEnvelope(
            "AUTH-TEST-3",
            environment,
            clusterId,
            "cloud-checkpoint-3",
            "VHDCHY_DOMAIN_INCOMPATIBLE",
            scope,
            payload2),
        environment,
        clusterId,
        compatibility);
    throw new InvalidOperationException("INCOMPATIBLE_SNAPSHOT_NOT_REJECTED");
}
catch (AuthoritySnapshotException error) when (error.Code == "AUTHORITY_INCOMPATIBLE")
{
}
Assert(await authorityStore.ReadActiveVersionAsync() == "AUTH-TEST-2", "A2_LOST_AFTER_INCOMPATIBLE_IMPORT");
Console.WriteLine("LAN_AUTHORITY_SNAPSHOT_HARNESS_PASS active=AUTH-TEST-2 replay=PASS conflict=PASS rollback=PASS compatibility=PASS");

var commandStore = new LocalCommandStore(databasePath, environment, clusterId, compatibility);
const string moduleId = "IDENTITY_EMPLOYEE_ATTENDANCE";
const string employeeId = "EMP-HARNESS-001";
const string employeeStateKey = "employee:EMP-HARNESS-001";

var create = new LanLocalCommandEnvelope(
    RequestId: "REQ-HARNESS-CREATE-1",
    IdempotencyKey: "IDEM-HARNESS-CREATE-1",
    ModuleId: moduleId,
    CommandCode: "EMPLOYEE_CREATE",
    EventCode: "EMPLOYEE_CREATED",
    EntityType: "employee",
    EntityId: employeeId,
    StateKey: employeeStateKey,
    ExpectedBaseVersion: null,
    PayloadJson: "{\"status\":\"ACTIVE\",\"employeeId\":\"EMP-HARNESS-001\"}",
    NextStateJson: "{\"employeeId\":\"EMP-HARNESS-001\",\"status\":\"ACTIVE\"}",
    DeviceId: "PDA-HARNESS-1",
    DeviceSeq: 1,
    GoogleProjectionWork: new[]
    {
        new LanGoogleProjectionWork(
            ProjectionKey: "HARNESS-PROJECTION-EMP-001-V1",
            TargetKey: "HARNESS-TARGET",
            PayloadJson: "{\"employeeId\":\"EMP-HARNESS-001\",\"version\":1}")
    });

var created = await commandStore.ExecuteAsync(create);
Assert(created.CommitStatus == "LAN_ACCEPTED_PENDING_SYNC", "CREATE_COMMIT_STATUS_WRONG");
Assert(created.GoogleOutputStatus == "PENDING", "CREATE_GOOGLE_STATUS_WRONG");
Assert(created.ResultingVersion == 1, "CREATE_VERSION_WRONG");
Assert(created.AuthoritySnapshotVersion == "AUTH-TEST-2", "CREATE_AUTHORITY_EVIDENCE_WRONG");
Assert(!created.AlreadyAccepted, "CREATE_MARKED_REPLAY");
Assert(await commandStore.ReadAcceptedEventCountAsync() == 1, "CREATE_EVENT_COUNT_WRONG");
Assert(await commandStore.ReadCloudOutboxCountAsync() == 1, "CREATE_CLOUD_OUTBOX_COUNT_WRONG");
var createdState = await commandStore.ReadCurrentStateAsync(employeeStateKey);
Assert(createdState is not null && createdState.EntityVersion == 1, "CREATE_STATE_NOT_COMMITTED");

var createReplay = await commandStore.ExecuteAsync(create with
{
    PayloadJson = "{ \"employeeId\": \"EMP-HARNESS-001\", \"status\": \"ACTIVE\" }",
    NextStateJson = "{\"status\":\"ACTIVE\",\"employeeId\":\"EMP-HARNESS-001\"}"
});
Assert(createReplay.AlreadyAccepted, "CREATE_REPLAY_NOT_RECOGNIZED");
Assert(createReplay.EventId == created.EventId, "CREATE_REPLAY_EVENT_CHANGED");
Assert(createReplay.ResultingVersion == 1, "CREATE_REPLAY_VERSION_CHANGED");
Assert(await commandStore.ReadAcceptedEventCountAsync() == 1, "CREATE_REPLAY_DUPLICATED_EVENT");
Assert(await commandStore.ReadCloudOutboxCountAsync() == 1, "CREATE_REPLAY_DUPLICATED_OUTBOX");

await ExpectLocalCommandError(
    "IDEMPOTENCY_PAYLOAD_CONFLICT",
    () => commandStore.ExecuteAsync(create with
    {
        PayloadJson = "{\"employeeId\":\"EMP-HARNESS-001\",\"status\":\"INACTIVE\"}",
        NextStateJson = "{\"employeeId\":\"EMP-HARNESS-001\",\"status\":\"INACTIVE\"}"
    }));
Assert(await commandStore.ReadAcceptedEventCountAsync() == 1, "IDEMPOTENCY_CONFLICT_MUTATED_EVENT_LOG");

await ExpectLocalCommandError(
    "DEVICE_SEQUENCE_CONFLICT",
    () => commandStore.ExecuteAsync(create with
    {
        RequestId = "REQ-HARNESS-SEQ-COLLISION",
        IdempotencyKey = "IDEM-HARNESS-SEQ-COLLISION",
        CommandCode = "EMPLOYEE_UPDATE",
        EventCode = "EMPLOYEE_UPDATED",
        ExpectedBaseVersion = 1,
        PayloadJson = "{\"employeeId\":\"EMP-HARNESS-001\",\"status\":\"ACTIVE\",\"note\":\"seq-collision\"}",
        NextStateJson = "{\"employeeId\":\"EMP-HARNESS-001\",\"status\":\"ACTIVE\",\"note\":\"seq-collision\"}",
        GoogleProjectionWork = null
    }));
Assert(await commandStore.ReadAcceptedEventCountAsync() == 1, "DEVICE_SEQUENCE_CONFLICT_MUTATED_EVENT_LOG");

await ExpectLocalCommandError(
    "ENTITY_VERSION_CONFLICT",
    () => commandStore.ExecuteAsync(create with
    {
        RequestId = "REQ-HARNESS-BAD-VERSION",
        IdempotencyKey = "IDEM-HARNESS-BAD-VERSION",
        CommandCode = "EMPLOYEE_UPDATE",
        EventCode = "EMPLOYEE_UPDATED",
        ExpectedBaseVersion = 9,
        DeviceSeq = 2,
        PayloadJson = "{\"employeeId\":\"EMP-HARNESS-001\",\"status\":\"ACTIVE\",\"versionAttempt\":9}",
        NextStateJson = "{\"employeeId\":\"EMP-HARNESS-001\",\"status\":\"ACTIVE\",\"versionAttempt\":9}",
        GoogleProjectionWork = null
    }));
Assert((await commandStore.ReadCurrentStateAsync(employeeStateKey))?.EntityVersion == 1, "VERSION_CONFLICT_MUTATED_STATE");
Assert(await commandStore.ReadAcceptedEventCountAsync() == 1, "VERSION_CONFLICT_MUTATED_EVENT_LOG");

var update = create with
{
    RequestId = "REQ-HARNESS-UPDATE-2",
    IdempotencyKey = "IDEM-HARNESS-UPDATE-2",
    CommandCode = "EMPLOYEE_UPDATE",
    EventCode = "EMPLOYEE_UPDATED",
    ExpectedBaseVersion = 1,
    DeviceSeq = 2,
    PayloadJson = "{\"status\":\"ACTIVE\",\"employeeId\":\"EMP-HARNESS-001\",\"generation\":2}",
    NextStateJson = "{\"employeeId\":\"EMP-HARNESS-001\",\"generation\":2,\"status\":\"ACTIVE\"}",
    GoogleProjectionWork = null
};
var updated = await commandStore.ExecuteAsync(update);
Assert(updated.ResultingVersion == 2, "UPDATE_VERSION_WRONG");
Assert(updated.GoogleOutputStatus == "NOT_REQUIRED", "UPDATE_GOOGLE_STATUS_WRONG");
Assert(await commandStore.ReadAcceptedEventCountAsync() == 2, "UPDATE_EVENT_COUNT_WRONG");
Assert(await commandStore.ReadCloudOutboxCountAsync() == 2, "UPDATE_CLOUD_OUTBOX_COUNT_WRONG");
Assert((await commandStore.ReadCurrentStateAsync(employeeStateKey))?.EntityVersion == 2, "UPDATE_STATE_VERSION_WRONG");

var eventsBeforeRollbackVector = await commandStore.ReadAcceptedEventCountAsync();
var outboxBeforeRollbackVector = await commandStore.ReadCloudOutboxCountAsync();
const string rollbackEntityId = "EMP-HARNESS-ROLLBACK";
const string rollbackStateKey = "employee:EMP-HARNESS-ROLLBACK";
var rollbackVector = new LanLocalCommandEnvelope(
    RequestId: "REQ-HARNESS-ROLLBACK",
    IdempotencyKey: "IDEM-HARNESS-ROLLBACK",
    ModuleId: moduleId,
    CommandCode: "EMPLOYEE_CREATE",
    EventCode: "EMPLOYEE_CREATED",
    EntityType: "employee",
    EntityId: rollbackEntityId,
    StateKey: rollbackStateKey,
    ExpectedBaseVersion: null,
    PayloadJson: "{\"employeeId\":\"EMP-HARNESS-ROLLBACK\",\"status\":\"ACTIVE\"}",
    NextStateJson: "{\"employeeId\":\"EMP-HARNESS-ROLLBACK\",\"status\":\"ACTIVE\"}",
    DeviceId: "PDA-HARNESS-1",
    DeviceSeq: 3,
    GoogleProjectionWork: new[]
    {
        new LanGoogleProjectionWork(
            ProjectionKey: "HARNESS-PROJECTION-EMP-001-V1",
            TargetKey: "HARNESS-TARGET",
            PayloadJson: "{\"employeeId\":\"EMP-HARNESS-ROLLBACK\"}")
    });

await ExpectLocalCommandError("LOCAL_COMMAND_COMMIT_FAILED", () => commandStore.ExecuteAsync(rollbackVector));
Assert(await commandStore.ReadCurrentStateAsync(rollbackStateKey) is null, "ROLLBACK_VECTOR_LEFT_CURRENT_STATE");
Assert(await commandStore.ReadAcceptedEventCountAsync() == eventsBeforeRollbackVector, "ROLLBACK_VECTOR_LEFT_EVENT");
Assert(await commandStore.ReadCloudOutboxCountAsync() == outboxBeforeRollbackVector, "ROLLBACK_VECTOR_LEFT_CLOUD_OUTBOX");

Console.WriteLine("LAN_LOCAL_COMMAND_TRANSACTION_PASS create=PASS replay=PASS idempotencyConflict=PASS deviceSequenceConflict=PASS versionConflict=PASS update=PASS atomicRollback=PASS authorityEvidence=PASS");
return 0;
