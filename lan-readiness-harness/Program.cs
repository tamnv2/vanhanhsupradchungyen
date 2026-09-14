using System.Security.Cryptography;
using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanReadiness.Harness <edge.db>");
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

var evaluator = new LanReadinessEvaluator(
    databasePath,
    environment,
    clusterId,
    compatibility,
    new[] { moduleId });

var report = await evaluator.EvaluateAsync();
Assert(!report.Ready, "READINESS_MUST_REMAIN_FALSE_WITHOUT_CLIENT_SECURITY");
Assert(report.Readiness == "EDGE_NOT_READY", "READINESS_STATE_WRONG");
Assert(report.SnapshotPrerequisitesReady, "SNAPSHOT_PREREQUISITES_NOT_READY");
Assert(report.AuthoritySnapshotVersion == "AUTH-TEST-2", "READINESS_AUTHORITY_VERSION_WRONG");
Assert(report.OperationalSnapshotVersion == "OP-TEST-2", "READINESS_OPERATIONAL_VERSION_WRONG");
Assert(report.OperationalAuthoritySnapshotVersion == "AUTH-TEST-2", "READINESS_OPERATIONAL_AUTHORITY_LINK_WRONG");
Assert(report.RequiredModules.SequenceEqual(new[] { moduleId }), "READINESS_REQUIRED_MODULES_WRONG");
Assert(report.SupportedCommandCodes.Count == 7, "READINESS_SUPPORTED_COMMAND_COUNT_WRONG");
Assert(report.SupportedCommandCodes.Contains("EMPLOYEE_CREATE"), "READINESS_EMPLOYEE_CREATE_NOT_LINKED");
Assert(report.SupportedCommandCodes.Contains("ATTENDANCE_IN"), "READINESS_ATTENDANCE_IN_NOT_LINKED");
Assert(report.BlockedCommandCodes.SequenceEqual(new[] { Slice1BusinessAdapter.PortraitCommandCode }), "READINESS_PORTRAIT_BLOCK_NOT_SCOPED");
Assert(report.Blockers.Count == 1, "READINESS_HAS_UNEXPECTED_BLOCKERS");
Assert(report.Blockers[0].Code == "PUBLIC_CLIENT_SECURITY_REQUIRED", "READINESS_SECURITY_GATE_MISSING");

var clientSecurity = new LanClientSecurityStore(databasePath);
var security = await clientSecurity.EnsureAsync();
Assert(security.Ready, "CLIENT_SECURITY_STATE_NOT_READY");
using var deviceKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
await clientSecurity.RegisterPairedDeviceAsync(
    "PDA-READINESS-001",
    Convert.ToBase64String(deviceKey.ExportSubjectPublicKeyInfo()),
    "ROOT-READINESS-HARNESS");

var afterClientSecurity = await evaluator.EvaluateAsync();
Assert(!afterClientSecurity.Ready, "READINESS_MUST_REMAIN_FALSE_WITHOUT_USER_SESSION_AUTH");
Assert(afterClientSecurity.SnapshotPrerequisitesReady, "SNAPSHOT_PREREQUISITES_REGRESSED");
Assert(afterClientSecurity.SupportedCommandCodes.Count == 7, "SUPPORTED_SUBSET_REGRESSED");
Assert(afterClientSecurity.BlockedCommandCodes.SequenceEqual(new[] { Slice1BusinessAdapter.PortraitCommandCode }), "PORTRAIT_BLOCK_SCOPE_REGRESSED");
Assert(afterClientSecurity.Blockers.Count == 1, "READINESS_HAS_UNEXPECTED_POST_SECURITY_BLOCKERS");
Assert(afterClientSecurity.Blockers[0].Code == "LAN_USER_SESSION_AUTH_REQUIRED", "USER_SESSION_GATE_NOT_REACHED");

Console.WriteLine("LAN_READINESS_HARNESS_PASS snapshots=PASS authorityLink=PASS requiredModules=PASS supportedSubset=PASS portraitScopedBlock=PASS signedClientGate=PASS userSessionGate=PASS ready=false");
return 0;
