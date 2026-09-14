using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
const string authorityScope = "{\"clusterId\":\"PICK_PACK_1291\",\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}";

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static string Base64Url(byte[] value) =>
    Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

static (string Algorithm, string SecretHash) PasswordRecord(string password)
{
    var salt = Enumerable.Range(0, 16).Select(index => unchecked((byte)(31 + index))).ToArray();
    const int iterations = 100_000;
    var digest = Rfc2898DeriveBytes.Pbkdf2(
        Encoding.UTF8.GetBytes(password),
        salt,
        iterations,
        HashAlgorithmName.SHA256,
        32);
    return ($"PBKDF2-SHA256${iterations}${Base64Url(salt)}", Base64Url(digest));
}

var authorityStore = new AuthoritySnapshotStore(databasePath);
var operationalStore = new OperationalSnapshotStore(databasePath);

// Seed the readiness harness independently. V1 is valid synchronized authority for
// ordinary authorization inspection, but intentionally cannot satisfy the V2 primary
// credential verifier. No local command vectors run on this database.
const string v1Payload = """
{
  "schemaVersion":"VHDCHY_AUTHORITY_SNAPSHOT_V1",
  "permissionCatalogVersion":"VHDCHY_PERMISSION_CATALOG_V1",
  "users":[
    {"userId":"U_READINESS_V1","status":"ACTIVE","securityLevel":"NORMAL"}
  ],
  "roles":[],
  "permissions":[],
  "rolePermissionGrants":[],
  "userRoleGrants":[],
  "userPermissionGrants":[]
}
""";

await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-READINESS-V1",
        environment,
        clusterId,
        "readiness-authority-v1",
        compatibility,
        authorityScope,
        v1Payload),
    environment,
    clusterId,
    compatibility);

await operationalStore.ImportAsync(
    new OperationalSnapshotEnvelope(
        "OP-READINESS-V1",
        environment,
        clusterId,
        "readiness-operational-v1",
        compatibility,
        "{\"generation\":1,\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
        "{\"employees\":[],\"presence\":[],\"generation\":1}"),
    environment,
    clusterId,
    compatibility,
    new[] { moduleId });

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
Assert(report.AuthoritySnapshotVersion == "AUTH-READINESS-V1", "READINESS_AUTHORITY_VERSION_WRONG");
Assert(report.OperationalSnapshotVersion == "OP-READINESS-V1", "READINESS_OPERATIONAL_VERSION_WRONG");
Assert(report.OperationalAuthoritySnapshotVersion == "AUTH-READINESS-V1", "READINESS_OPERATIONAL_AUTHORITY_LINK_WRONG");
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
Assert(!afterClientSecurity.Ready, "READINESS_MUST_REMAIN_FALSE_WITHOUT_PRIMARY_LOGIN_AUTHORITY");
Assert(afterClientSecurity.SnapshotPrerequisitesReady, "SNAPSHOT_PREREQUISITES_REGRESSED");
Assert(afterClientSecurity.SupportedCommandCodes.Count == 7, "SUPPORTED_SUBSET_REGRESSED");
Assert(afterClientSecurity.BlockedCommandCodes.SequenceEqual(new[] { Slice1BusinessAdapter.PortraitCommandCode }), "PORTRAIT_BLOCK_SCOPE_REGRESSED");
Assert(afterClientSecurity.Blockers.Count == 1, "READINESS_HAS_UNEXPECTED_POST_SECURITY_BLOCKERS");
Assert(afterClientSecurity.Blockers[0].Code == "LAN_PRIMARY_CREDENTIAL_AUTHORITY_REQUIRED", "PRIMARY_CREDENTIAL_AUTHORITY_GATE_NOT_REACHED");
Assert(afterClientSecurity.Blockers[0].Message.Contains("LOGIN_AUTHORITY_SCHEMA_REQUIRED", StringComparison.Ordinal), "PRIMARY_CREDENTIAL_GATE_REASON_NOT_EXPOSED");

// Upgrade synchronized authority to V2 with a current password verifier, then refresh
// the operational snapshot so its activation evidence is bound to that authority generation.
const string readinessPassword = "Readiness-Test-Password-1291!";
var passwordRecord = PasswordRecord(readinessPassword);
var v2Payload = JsonSerializer.Serialize(new
{
    schemaVersion = LanPrimaryCredentialVerifier.LoginAuthoritySchemaVersion,
    permissionCatalogVersion = LanAuthorizationEvaluator.PermissionCatalogVersion,
    users = new object[]
    {
        new
        {
            userId = "U_READINESS",
            username = "readiness.operator",
            employeeId = "E_READINESS",
            displayName = "Readiness Operator",
            email = "readiness@example.invalid",
            status = "ACTIVE",
            securityLevel = "NORMAL"
        }
    },
    credentials = new object[]
    {
        new
        {
            credentialId = "C_READINESS",
            userId = "U_READINESS",
            credentialType = "PASSWORD",
            secretHash = passwordRecord.SecretHash,
            hashAlgorithm = passwordRecord.Algorithm,
            mustChange = false,
            status = "ACTIVE"
        }
    },
    roles = Array.Empty<object>(),
    permissions = Array.Empty<object>(),
    rolePermissionGrants = Array.Empty<object>(),
    userRoleGrants = Array.Empty<object>(),
    userPermissionGrants = Array.Empty<object>()
});

await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-READINESS-V2",
        environment,
        clusterId,
        "readiness-authority-v2",
        compatibility,
        authorityScope,
        v2Payload),
    environment,
    clusterId,
    compatibility);

await operationalStore.ImportAsync(
    new OperationalSnapshotEnvelope(
        "OP-READINESS-V2",
        environment,
        clusterId,
        "readiness-operational-v2",
        compatibility,
        "{\"generation\":2,\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
        "{\"employees\":[],\"presence\":[],\"generation\":2}"),
    environment,
    clusterId,
    compatibility,
    new[] { moduleId });

var afterPrimaryAuthority = await evaluator.EvaluateAsync();
Assert(!afterPrimaryAuthority.Ready, "READINESS_MUST_REMAIN_FALSE_WITHOUT_ROUTE_WIRING");
Assert(afterPrimaryAuthority.SnapshotPrerequisitesReady, "V2_SNAPSHOT_PREREQUISITES_NOT_READY");
Assert(afterPrimaryAuthority.AuthoritySnapshotVersion == "AUTH-READINESS-V2", "V2_AUTHORITY_VERSION_WRONG");
Assert(afterPrimaryAuthority.OperationalSnapshotVersion == "OP-READINESS-V2", "V2_OPERATIONAL_VERSION_WRONG");
Assert(afterPrimaryAuthority.OperationalAuthoritySnapshotVersion == "AUTH-READINESS-V2", "V2_OPERATIONAL_AUTHORITY_LINK_WRONG");
Assert(afterPrimaryAuthority.Blockers.Count == 1, "READINESS_HAS_UNEXPECTED_POST_AUTH_BLOCKERS");
Assert(afterPrimaryAuthority.Blockers[0].Code == "LAN_USER_SESSION_ROUTE_WIRING_REQUIRED", "USER_SESSION_ROUTE_WIRING_GATE_NOT_REACHED");

Console.WriteLine("LAN_READINESS_HARNESS_PASS snapshots=PASS authorityLink=PASS requiredModules=PASS supportedSubset=PASS portraitScopedBlock=PASS signedClientGate=PASS primaryCredentialGate=PASS routeWiringGate=PASS ready=false");
return 0;
