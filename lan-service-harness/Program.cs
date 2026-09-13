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

var store = new AuthoritySnapshotStore(databasePath);
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

var a1 = new AuthoritySnapshotEnvelope(
    "AUTH-TEST-1",
    environment,
    clusterId,
    "cloud-checkpoint-1",
    compatibility,
    scope,
    payload1);

var first = await store.ImportAsync(a1, environment, clusterId, compatibility);
Assert(first.Activated && !first.AlreadyKnown && first.Status == "ACTIVE", "A1_FIRST_ACTIVATION_FAILED");
Assert(await store.ReadActiveVersionAsync() == "AUTH-TEST-1", "A1_NOT_ACTIVE");

var replay = await store.ImportAsync(a1, environment, clusterId, compatibility);
Assert(replay.AlreadyKnown && replay.Activated && replay.Status == "ACTIVE", "A1_REPLAY_NOT_IDEMPOTENT");

try
{
    await store.ImportAsync(a1 with { PayloadJson = payload2 }, environment, clusterId, compatibility);
    throw new InvalidOperationException("A1_CONFLICT_NOT_REJECTED");
}
catch (AuthoritySnapshotException error) when (error.Code == "AUTHORITY_VERSION_PAYLOAD_CONFLICT")
{
}
Assert(await store.ReadActiveVersionAsync() == "AUTH-TEST-1", "A1_LOST_AFTER_CONFLICT");

try
{
    await store.ImportAsync(
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
Assert(await store.ReadActiveVersionAsync() == "AUTH-TEST-1", "A1_LOST_AFTER_INVALID_IMPORT");
Assert(await store.ReadStatusAsync("AUTH-TEST-BAD") is null, "INVALID_IMPORT_PERSISTED");

var a2 = new AuthoritySnapshotEnvelope(
    "AUTH-TEST-2",
    environment,
    clusterId,
    "cloud-checkpoint-2",
    compatibility,
    scope,
    payload2);
var second = await store.ImportAsync(a2, environment, clusterId, compatibility);
Assert(second.Activated && second.Status == "ACTIVE", "A2_ACTIVATION_FAILED");
Assert(await store.ReadActiveVersionAsync() == "AUTH-TEST-2", "A2_NOT_ACTIVE");
Assert(await store.ReadStatusAsync("AUTH-TEST-1") == "REPLACED", "A1_NOT_REPLACED");
Assert(await store.ReadStatusAsync("AUTH-TEST-2") == "ACTIVE", "A2_STATUS_WRONG");

try
{
    await store.ImportAsync(
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
Assert(await store.ReadActiveVersionAsync() == "AUTH-TEST-2", "A2_LOST_AFTER_INCOMPATIBLE_IMPORT");

Console.WriteLine("LAN_AUTHORITY_SNAPSHOT_HARNESS_PASS active=AUTH-TEST-2 replay=PASS conflict=PASS rollback=PASS compatibility=PASS");
return 0;
