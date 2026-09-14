using Microsoft.Data.Sqlite;
using Vhdchy.LanService;
using Vhdchy.LanSlice1Business.Harness;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanSlice1Business.Harness <edge.db>");
    return 2;
}

var databasePath = Path.GetFullPath(args[0]);
if (!File.Exists(databasePath))
{
    Console.Error.WriteLine($"Edge database not found: {databasePath}");
    return 2;
}

await TestAuthority.SeedAsync(databasePath);
var adapter = new Slice1BusinessAdapter(
    databasePath,
    TestAuthority.Environment,
    TestAuthority.ClusterId,
    TestAuthority.Compatibility);

var inspection = adapter.Inspect();
HarnessAssert.That(!inspection.Ready, "ADAPTER_PREMATURELY_READY");
HarnessAssert.That(inspection.CatalogCommandCount == 8, "ADAPTER_CATALOG_COUNT_WRONG");
HarnessAssert.That(
    inspection.Blockers.Any(value => value.Code == "PORTRAIT_MEDIA_LIFECYCLE_REQUIRED"),
    "PORTRAIT_BLOCKER_MISSING");

await EmployeeVectors.RunAsync(adapter, databasePath);
await AttendanceVectors.RunAsync(adapter, databasePath);

await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadWrite
}.ToString());
await connection.OpenAsync();

static async Task<long> CountAsync(SqliteConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    return Convert.ToInt64(await command.ExecuteScalarAsync() ?? 0L);
}

HarnessAssert.That(await CountAsync(connection, "SELECT COUNT(*) FROM edge_events") == 12, "BUSINESS_EVENT_COUNT_WRONG");
HarnessAssert.That(await CountAsync(connection, "SELECT COUNT(*) FROM cloud_sync_outbox") == 12, "BUSINESS_OUTBOX_COUNT_WRONG");
HarnessAssert.That(await CountAsync(connection, "SELECT COUNT(*) FROM edge_event_actor_evidence") == 12, "BUSINESS_ACTOR_EVIDENCE_COUNT_WRONG");
HarnessAssert.That(await CountAsync(connection, "SELECT COUNT(*) FROM edge_actor_context") == 0, "BUSINESS_ACTOR_CONTEXT_NOT_CLEANED");

Console.WriteLine("LAN_SLICE1_BUSINESS_HARNESS_PASS authz=PASS create=PASS replay=PASS actorEvidence=PASS update=PASS status=PASS mnvReuse=PASS repeatedIn=PASS outGuard=PASS correction=PASS portraitFailClosed=PASS events=12");
return 0;
