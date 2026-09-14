using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanCommandGate.Harness <edge.db>");
    return 2;
}

var databasePath = Path.GetFullPath(args[0]);
if (!File.Exists(databasePath))
{
    Console.Error.WriteLine($"Edge database not found: {databasePath}");
    return 2;
}

const string clusterId = "PICK_PACK_1291";
const string moduleId = "IDENTITY_EMPLOYEE_ATTENDANCE";
const string compatibility = "VHDCHY_DOMAIN_V1";

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

var gate = new Slice1CommandGate(databasePath, compatibility);
var catalog = gate.InspectCatalog();
var expectedCommands = new[]
{
    "ATTENDANCE_CORRECT",
    "ATTENDANCE_IN",
    "ATTENDANCE_OUT",
    "EMPLOYEE_CODE_ASSIGN",
    "EMPLOYEE_CREATE",
    "EMPLOYEE_PORTRAIT_REPLACE",
    "EMPLOYEE_STATUS_CHANGE",
    "EMPLOYEE_UPDATE"
};
Assert(catalog.Ready, "CATALOG_NOT_READY");
Assert(catalog.SchemaVersion == "VHDCHY_COMMAND_SPEC_V1", "CATALOG_SCHEMA_WRONG");
Assert(catalog.DomainContractVersion == compatibility, "CATALOG_DOMAIN_WRONG");
Assert(catalog.Slice == moduleId, "CATALOG_SLICE_WRONG");
Assert(catalog.CommandCount == 8, "CATALOG_COMMAND_COUNT_WRONG");
Assert(catalog.CommandCodes.SequenceEqual(expectedCommands), "CATALOG_COMMAND_SET_WRONG");

var directAllow = await gate.AuthorizeAsync(new Slice1CommandGateRequest(
    "U_ALLOW", "EMPLOYEE_CREATE", clusterId, moduleId));
Assert(directAllow.Authorized && directAllow.Code == "AUTHORIZED", "DIRECT_ALLOW_FAILED");
Assert(directAllow.PermissionResource == "employee" && directAllow.PermissionAction == "create", "CREATE_PERMISSION_MAPPING_WRONG");
Assert(directAllow.EventIntent == "EMPLOYEE_CREATED" && directAllow.EntityType == "employee", "CREATE_EVENT_MAPPING_WRONG");
Assert(directAllow.AuthoritySnapshotVersion == "AUTH-TEST-2", "CREATE_AUTHORITY_EVIDENCE_WRONG");

var roleAllow = await gate.AuthorizeAsync(new Slice1CommandGateRequest(
    "U_ROLE", "EMPLOYEE_CREATE", clusterId, moduleId));
Assert(roleAllow.Authorized && roleAllow.Code == "AUTHORIZED", "ROLE_ALLOW_FAILED");

var deny = await gate.AuthorizeAsync(new Slice1CommandGateRequest(
    "U_DENY", "EMPLOYEE_CREATE", clusterId, moduleId));
Assert(!deny.Authorized && deny.Code == "PERMISSION_DENIED", "DENY_PRECEDENCE_FAILED");

var wrongCluster = await gate.AuthorizeAsync(new Slice1CommandGateRequest(
    "U_SCOPE", "EMPLOYEE_CREATE", clusterId, moduleId));
Assert(!wrongCluster.Authorized && wrongCluster.Code == "PERMISSION_DENIED", "WRONG_CLUSTER_ALLOWED");

var wrongModule = await gate.AuthorizeAsync(new Slice1CommandGateRequest(
    "U_ALLOW", "EMPLOYEE_CREATE", clusterId, "OTHER_MODULE"));
Assert(!wrongModule.Authorized && wrongModule.Code == "COMMAND_MODULE_SCOPE_INVALID", "WRONG_MODULE_ALLOWED");

var unsupported = await gate.AuthorizeAsync(new Slice1CommandGateRequest(
    "U_ALLOW", "CLIENT_SUPPLIED_FAKE_COMMAND", clusterId, moduleId));
Assert(!unsupported.Authorized && unsupported.Code == "UNSUPPORTED_COMMAND", "UNSUPPORTED_COMMAND_ALLOWED");
Assert(unsupported.PermissionResource is null && unsupported.EventIntent is null, "UNSUPPORTED_COMMAND_TRUSTED_MAPPING_LEAKED");

var updateMapping = await gate.AuthorizeAsync(new Slice1CommandGateRequest(
    "U_ALLOW", "EMPLOYEE_UPDATE", clusterId, moduleId));
Assert(updateMapping.PermissionResource == "employee" && updateMapping.PermissionAction == "edit", "UPDATE_PERMISSION_MAPPING_WRONG");
Assert(updateMapping.EventIntent == "EMPLOYEE_UPDATED" && updateMapping.EntityType == "employee", "UPDATE_EVENT_MAPPING_WRONG");

var attendanceCorrectMapping = await gate.AuthorizeAsync(new Slice1CommandGateRequest(
    "U_ALLOW", "ATTENDANCE_CORRECT", clusterId, moduleId));
Assert(attendanceCorrectMapping.PermissionResource == "attendance" && attendanceCorrectMapping.PermissionAction == "correct", "ATTENDANCE_PERMISSION_MAPPING_WRONG");
Assert(attendanceCorrectMapping.EventIntent == "ATTENDANCE_CORRECTED" && attendanceCorrectMapping.EntityType == "attendance", "ATTENDANCE_EVENT_MAPPING_WRONG");

Console.WriteLine("LAN_COMMAND_GATE_HARNESS_PASS catalog=8 directAllow=PASS roleAllow=PASS denyPrecedence=PASS clusterScope=PASS moduleScope=PASS unsupported=PASS trustedMappings=PASS embeddedContract=PASS");
return 0;
