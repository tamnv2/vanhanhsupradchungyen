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
Assert(!report.Ready, "READINESS_MUST_REMAIN_FALSE_WITHOUT_REVIEWED_SLICE_ADAPTER");
Assert(report.Readiness == "EDGE_NOT_READY", "READINESS_STATE_WRONG");
Assert(report.SnapshotPrerequisitesReady, "SNAPSHOT_PREREQUISITES_NOT_READY");
Assert(report.AuthoritySnapshotVersion == "AUTH-TEST-2", "READINESS_AUTHORITY_VERSION_WRONG");
Assert(report.OperationalSnapshotVersion == "OP-TEST-2", "READINESS_OPERATIONAL_VERSION_WRONG");
Assert(report.OperationalAuthoritySnapshotVersion == "AUTH-TEST-2", "READINESS_OPERATIONAL_AUTHORITY_LINK_WRONG");
Assert(report.RequiredModules.SequenceEqual(new[] { moduleId }), "READINESS_REQUIRED_MODULES_WRONG");
Assert(report.Blockers.Count == 1, "READINESS_HAS_UNEXPECTED_BLOCKERS");
Assert(report.Blockers[0].Code == "SLICE_ADAPTER_REQUIRED", "READINESS_ADAPTER_GATE_MISSING");

Console.WriteLine("LAN_READINESS_HARNESS_PASS snapshots=PASS authorityLink=PASS requiredModules=PASS adapterGate=PASS ready=false");
return 0;
