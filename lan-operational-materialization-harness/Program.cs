using System.Text.Json;
using Microsoft.Data.Sqlite;
using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanOperationalMaterialization.Harness <edge.db>");
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

static string StringProperty(string json, string property)
{
    using var document = JsonDocument.Parse(json);
    return document.RootElement.GetProperty(property).GetString()
        ?? throw new InvalidOperationException($"MISSING_{property}");
}

static async Task ExpectOperationalError(string code, Func<Task> action)
{
    try
    {
        await action();
        throw new InvalidOperationException($"{code}_NOT_REJECTED");
    }
    catch (OperationalSnapshotException error) when (error.Code == code)
    {
    }
}

var authorityStore = new AuthoritySnapshotStore(databasePath);
var operationalStore = new OperationalSnapshotStore(databasePath);
var commandStore = new LocalCommandStore(databasePath, environment, clusterId, compatibility);

var authority = await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-MAT-1",
        environment,
        clusterId,
        "cloud-auth-materialization-1",
        compatibility,
        "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
        "{}"),
    environment,
    clusterId,
    compatibility);
Assert(authority.Activated && authority.Status == "ACTIVE", "AUTHORITY_NOT_ACTIVE");

const string state1 = """
{
  "employees": [
    {
      "employeeId": "EMP-MAT-1",
      "entityVersion": 7,
      "status": "ACTIVE",
      "fullName": "Materialized Employee"
    }
  ],
  "employeeCodes": [
    {
      "employeeCodeId": "CODE-MAT-1",
      "employeeId": "EMP-MAT-1",
      "employeeCode": "MNV-MAT-1",
      "entityVersion": 2,
      "status": "ACTIVE"
    }
  ],
  "presence": [
    {
      "employeeId": "EMP-MAT-1",
      "entityVersion": 4,
      "currentState": "OUT",
      "clusterId": "PICK_PACK_1291",
      "businessDate": "2026-09-14"
    }
  ]
}
""";

var op1 = new OperationalSnapshotEnvelope(
    "OP-MAT-1",
    environment,
    clusterId,
    "cloud-operational-materialization-1",
    compatibility,
    "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
    state1);
var imported = await operationalStore.ImportAsync(
    op1,
    environment,
    clusterId,
    compatibility,
    new[] { moduleId });
Assert(imported.Activated && imported.Status == "ACTIVE", "OP1_NOT_ACTIVE");
Assert(imported.AuthoritySnapshotVersion == "AUTH-MAT-1", "OP1_AUTHORITY_LINK_WRONG");

var employee = await commandStore.ReadCurrentStateAsync("employee:EMP-MAT-1");
var code = await commandStore.ReadCurrentStateAsync("employee_code:CODE-MAT-1");
var presence = await commandStore.ReadCurrentStateAsync("attendance:EMP-MAT-1");
Assert(employee is not null && employee.EntityVersion == 7 && employee.EntityType == "employee", "EMPLOYEE_NOT_MATERIALIZED");
Assert(code is not null && code.EntityVersion == 2 && code.EntityType == "employee_code", "EMPLOYEE_CODE_NOT_MATERIALIZED");
Assert(presence is not null && presence.EntityVersion == 4 && presence.EntityType == "attendance", "PRESENCE_NOT_MATERIALIZED");
Assert(StringProperty(employee.StateJson, "status") == "ACTIVE", "EMPLOYEE_STATE_WRONG");
Assert(StringProperty(code.StateJson, "employeeCode") == "MNV-MAT-1", "EMPLOYEE_CODE_STATE_WRONG");
Assert(StringProperty(presence.StateJson, "currentState") == "OUT", "PRESENCE_STATE_WRONG");

// Force a database-level collision after the materializer has deleted Slice-1 rows.
// The outer operational-snapshot transaction must roll everything back, preserving OP-MAT-1 state.
var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadWrite,
    Cache = SqliteCacheMode.Shared
}.ToString();
await using (var connection = new SqliteConnection(connectionString))
{
    await connection.OpenAsync();
    await using var insert = connection.CreateCommand();
    insert.CommandText = """
        INSERT INTO module_current_state(
          state_key, module_id, entity_type, entity_id,
          entity_version, state_json, updated_at
        ) VALUES (
          'employee:EMP-MAT-COLLISION', 'OTHER_MODULE', 'other', 'OTHER-1',
          1, '{}', $now
        )
        """;
    insert.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
    await insert.ExecuteNonQueryAsync();
}

var collisionSnapshot = new OperationalSnapshotEnvelope(
    "OP-MAT-COLLISION",
    environment,
    clusterId,
    "cloud-operational-materialization-collision",
    compatibility,
    "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
    """
    {
      "employees": [
        {"employeeId":"EMP-MAT-COLLISION","entityVersion":1,"status":"ACTIVE","fullName":"Collision"}
      ],
      "employeeCodes": [],
      "presence": [
        {"employeeId":"EMP-MAT-COLLISION","entityVersion":1,"currentState":"OUT"}
      ]
    }
    """);

try
{
    await operationalStore.ImportAsync(
        collisionSnapshot,
        environment,
        clusterId,
        compatibility,
        new[] { moduleId });
    throw new InvalidOperationException("SQL_COLLISION_NOT_REJECTED");
}
catch (SqliteException)
{
}

Assert(await operationalStore.ReadActiveVersionAsync() == "OP-MAT-1", "OP1_LOST_AFTER_SQL_ROLLBACK");
Assert(await operationalStore.ReadStatusAsync("OP-MAT-COLLISION") is null, "COLLISION_SNAPSHOT_PERSISTED");
var employeeAfterRollback = await commandStore.ReadCurrentStateAsync("employee:EMP-MAT-1");
var codeAfterRollback = await commandStore.ReadCurrentStateAsync("employee_code:CODE-MAT-1");
var presenceAfterRollback = await commandStore.ReadCurrentStateAsync("attendance:EMP-MAT-1");
Assert(employeeAfterRollback?.EntityVersion == 7, "EMPLOYEE_LOST_AFTER_ROLLBACK");
Assert(codeAfterRollback?.EntityVersion == 2, "CODE_LOST_AFTER_ROLLBACK");
Assert(presenceAfterRollback?.EntityVersion == 4, "PRESENCE_LOST_AFTER_ROLLBACK");

await using (var connection = new SqliteConnection(connectionString))
{
    await connection.OpenAsync();
    await using var delete = connection.CreateCommand();
    delete.CommandText = "DELETE FROM module_current_state WHERE module_id='OTHER_MODULE' AND state_key='employee:EMP-MAT-COLLISION'";
    await delete.ExecuteNonQueryAsync();
}

// Accept one local command so Cloud outbox is pending. Snapshot refresh must now fail closed
// rather than overwrite locally accepted current state that has not reconciled.
var localCreate = new LanLocalCommandEnvelope(
    RequestId: "REQ-MAT-LOCAL-1",
    IdempotencyKey: "IDEM-MAT-LOCAL-1",
    ModuleId: moduleId,
    CommandCode: "EMPLOYEE_CREATE",
    EventCode: "EMPLOYEE_CREATED",
    EntityType: "employee",
    EntityId: "EMP-LOCAL-2",
    StateKey: "employee:EMP-LOCAL-2",
    ExpectedBaseVersion: null,
    PayloadJson: "{\"employeeId\":\"EMP-LOCAL-2\",\"status\":\"ACTIVE\",\"fullName\":\"Local Pending\"}",
    NextStateJson: "{\"employeeId\":\"EMP-LOCAL-2\",\"status\":\"ACTIVE\",\"fullName\":\"Local Pending\"}",
    DeviceId: "PDA-MAT-1",
    DeviceSeq: 1);
var accepted = await commandStore.ExecuteAsync(localCreate);
Assert(accepted.CommitStatus == "LAN_ACCEPTED_PENDING_SYNC", "LOCAL_ACCEPTANCE_FAILED");
Assert(await commandStore.ReadCloudOutboxCountAsync() == 1, "LOCAL_OUTBOX_NOT_PENDING");

var op2 = new OperationalSnapshotEnvelope(
    "OP-MAT-2",
    environment,
    clusterId,
    "cloud-operational-materialization-2",
    compatibility,
    "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
    state1.Replace("Materialized Employee", "Cloud Refresh", StringComparison.Ordinal));
await ExpectOperationalError(
    "OPERATIONAL_PENDING_LOCAL_WORK",
    () => operationalStore.ImportAsync(
        op2,
        environment,
        clusterId,
        compatibility,
        new[] { moduleId }));

Assert(await operationalStore.ReadActiveVersionAsync() == "OP-MAT-1", "OP1_LOST_AFTER_PENDING_GUARD");
Assert(await operationalStore.ReadStatusAsync("OP-MAT-2") is null, "OP2_PERSISTED_DESPITE_PENDING_GUARD");
var localState = await commandStore.ReadCurrentStateAsync("employee:EMP-LOCAL-2");
Assert(localState is not null && localState.EntityVersion == 1, "LOCAL_PENDING_STATE_OVERWRITTEN");
Assert(await commandStore.ReadAcceptedEventCountAsync() == 1, "LOCAL_EVENT_EVIDENCE_CHANGED");
Assert(await commandStore.ReadCloudOutboxCountAsync() == 1, "LOCAL_OUTBOX_EVIDENCE_CHANGED");

Console.WriteLine("LAN_OPERATIONAL_MATERIALIZATION_HARNESS_PASS materialized=3 rollback=PASS pendingLocalGuard=PASS atomic=PASS");
return 0;
