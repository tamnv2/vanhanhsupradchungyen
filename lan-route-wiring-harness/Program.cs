using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0])) return 2;
var db = Path.GetFullPath(args[0]);
if (!File.Exists(db)) return 2;

const string env = "BETA";
const string cluster = "PICK_PACK_1291";
const string domain = "VHDCHY_DOMAIN_V1";
const string module = "IDENTITY_EMPLOYEE_ATTENDANCE";
const string scope = "{\"clusterId\":\"PICK_PACK_1291\",\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}";
const string opUser = "U_ROUTE_OPERATOR";
const string denyUser = "U_ROUTE_DENY";
const string changeUser = "U_ROUTE_CHANGE";
const string device1 = "PDA-ROUTE-001";
const string device2 = "PDA-ROUTE-002";

static void A(bool value, string code)
{
    if (!value) throw new InvalidOperationException(code);
}

static string B64(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
static object Credential(string id, string userId, string password, byte seed, bool mustChange)
{
    var salt = Enumerable.Range(0, 16).Select(i => unchecked((byte)(seed + i))).ToArray();
    const int iterations = 100_000;
    var digest = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, 32);
    return new
    {
        credentialId = id,
        userId,
        credentialType = "PASSWORD",
        secretHash = B64(digest),
        hashAlgorithm = $"PBKDF2-SHA256${iterations}${B64(salt)}",
        mustChange,
        status = "ACTIVE"
    };
}

static string AuthorityPayload() => JsonSerializer.Serialize(new
{
    schemaVersion = LanPrimaryCredentialVerifier.LoginAuthoritySchemaVersion,
    permissionCatalogVersion = LanAuthorizationEvaluator.PermissionCatalogVersion,
    users = new object[]
    {
        new { userId = opUser, username = "route.operator", employeeId = "E_ROUTE_OPERATOR", displayName = "Route Operator", email = "route.operator@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" },
        new { userId = denyUser, username = "route.deny", employeeId = "E_ROUTE_DENY", displayName = "Route Deny", email = "route.deny@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" },
        new { userId = changeUser, username = "route.change", employeeId = "E_ROUTE_CHANGE", displayName = "Route Change", email = "route.change@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" }
    },
    credentials = new object[]
    {
        Credential("C_OP", opUser, "Route-Operator-Password-1291!", 11, false),
        Credential("C_DENY", denyUser, "Route-Deny-Password-1291!", 41, false),
        Credential("C_CHANGE", changeUser, "Route-Change-Password-1291!", 71, true)
    },
    roles = Array.Empty<object>(),
    permissions = new object[]
    {
        new { permissionId = "P_CREATE", resource = "employee", action = "create", status = "ACTIVE" },
        new { permissionId = "P_PORTRAIT", resource = "employee", action = "portrait", status = "ACTIVE" }
    },
    rolePermissionGrants = Array.Empty<object>(),
    userRoleGrants = Array.Empty<object>(),
    userPermissionGrants = new object[]
    {
        new { grantId = "G_OP_CREATE", userId = opUser, permissionId = "P_CREATE", clusterId = cluster, moduleId = module, effect = "ALLOW", status = "ACTIVE" },
        new { grantId = "G_OP_PORTRAIT", userId = opUser, permissionId = "P_PORTRAIT", clusterId = cluster, moduleId = module, effect = "ALLOW", status = "ACTIVE" },
        new { grantId = "G_DENY_ALLOW", userId = denyUser, permissionId = "P_CREATE", clusterId = cluster, moduleId = module, effect = "ALLOW", status = "ACTIVE" },
        new { grantId = "G_DENY_BLOCK", userId = denyUser, permissionId = "P_CREATE", clusterId = cluster, moduleId = module, effect = "DENY", status = "ACTIVE" },
        new { grantId = "G_CHANGE_CREATE", userId = changeUser, permissionId = "P_CREATE", clusterId = cluster, moduleId = module, effect = "ALLOW", status = "ACTIVE" }
    }
});

static string Body(string id, string idem, string code, string entity, object payload, long seq, long? version = null) =>
    JsonSerializer.Serialize(new { requestId = id, idempotencyKey = idem, commandCode = code, entityId = entity, expectedEntityVersion = version, payload, deviceSeq = seq });

static LanClientSignedRequestProof Sign(ECDsa key, string device, string epoch, string nonce, string body, DateTimeOffset at, string? target = null)
{
    var unsigned = new LanClientSignedRequestProof(
        device,
        epoch,
        at.ToUnixTimeMilliseconds(),
        nonce,
        LanBusinessRouteCoordinator.BusinessCommandMethod,
        target ?? LanBusinessRouteCoordinator.BusinessCommandRouteTarget,
        LanClientSecurityStore.Sha256Hex(body),
        "placeholder");
    var canonical = LanClientSecurityStore.BuildCanonicalRequest(unsigned);
    var signature = key.SignData(Encoding.UTF8.GetBytes(canonical), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    return unsigned with { SignatureBase64 = Convert.ToBase64String(signature) };
}

static async Task Error(string code, Func<Task> action)
{
    try
    {
        await action();
        throw new InvalidOperationException($"EXPECTED:{code}");
    }
    catch (LanBusinessRouteException error)
    {
        A(error.Code == code, $"WRONG:{code}:{error.Code}");
    }
}

var authority = new AuthoritySnapshotStore(db);
var authorityPayload = AuthorityPayload();
await authority.ImportAsync(new("AUTH-ROUTE-V2-1", env, cluster, "route-authority-1", domain, scope, authorityPayload), env, cluster, domain);
await new OperationalSnapshotStore(db).ImportAsync(
    new("OP-ROUTE-1", env, cluster, "route-operational-1", domain,
        "{\"generation\":1,\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
        "{\"employees\":[],\"employeeCodes\":[],\"presence\":[],\"generation\":1}"),
    env, cluster, domain, new[] { module });

var security = new LanClientSecurityStore(db);
await security.EnsureAsync();
using var key1 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
using var key2 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
await security.RegisterPairedDeviceAsync(device1, Convert.ToBase64String(key1.ExportSubjectPublicKeyInfo()), "ROOT-ROUTE-HARNESS");
await security.RegisterPairedDeviceAsync(device2, Convert.ToBase64String(key2.ExportSubjectPublicKeyInfo()), "ROOT-ROUTE-HARNESS");
var epoch = (await security.InspectAsync()).SecurityEpoch!;

var sessions = new LanUserSessionStore(db);
await sessions.EnsureAsync();
var opSession = await sessions.IssueFromAuthenticatedEvidenceAsync(opUser, device1, epoch, "AUTH-ROUTE-V2-1", false);
var denySession = await sessions.IssueFromAuthenticatedEvidenceAsync(denyUser, device1, epoch, "AUTH-ROUTE-V2-1", false);
var changeSession = await sessions.IssueFromAuthenticatedEvidenceAsync(changeUser, device1, epoch, "AUTH-ROUTE-V2-1", true);
var route = new LanBusinessRouteCoordinator(db, env, cluster, domain);
var now = DateTimeOffset.UtcNow;

var create = Body("REQ-1", "IDEM-1", "EMPLOYEE_CREATE", "EMP-1", new { employeeId = "EMP-1", fullName = "Route Test", status = "ACTIVE" }, 1);
var createProof = Sign(key1, device1, epoch, "nonce-1", create, now);
var accepted = await route.ExecuteAsync(new(opSession.Token, createProof, create), now);
A(!string.IsNullOrWhiteSpace(accepted.EventId), "EVENT_MISSING");
await Error("REQUEST_REPLAY", async () => _ = await route.ExecuteAsync(new(opSession.Token, createProof, create), now));

var tampered = Body("REQ-1", "IDEM-1", "EMPLOYEE_CREATE", "EMP-1", new { employeeId = "EMP-1", fullName = "Tampered", status = "ACTIVE" }, 1);
var tamperProof = Sign(key1, device1, epoch, "nonce-2", create, now);
A(tampered != create, "TAMPER_VECTOR_NOT_DIFFERENT");
await Error("SIGNED_REQUEST_BODY_MISMATCH", async () => _ = await route.ExecuteAsync(new(opSession.Token, tamperProof, tampered), now));

var wrongTarget = Body("REQ-2", "IDEM-2", "EMPLOYEE_CREATE", "EMP-2", new { employeeId = "EMP-2", fullName = "Target" }, 2);
var wrongTargetProof = Sign(key1, device1, epoch, "nonce-3", wrongTarget, now, "/api/v1/data/not-reviewed");
await Error("SIGNED_REQUEST_TARGET_INVALID", async () => _ = await route.ExecuteAsync(new(opSession.Token, wrongTargetProof, wrongTarget), now));

var wrongDevice = Body("REQ-3", "IDEM-3", "EMPLOYEE_CREATE", "EMP-3", new { employeeId = "EMP-3", fullName = "Device" }, 3);
var wrongDeviceProof = Sign(key2, device2, epoch, "nonce-4", wrongDevice, now);
await Error("SESSION_DEVICE_MISMATCH", async () => _ = await route.ExecuteAsync(new(opSession.Token, wrongDeviceProof, wrongDevice), now));

var mustChange = Body("REQ-4", "IDEM-4", "EMPLOYEE_CREATE", "EMP-4", new { employeeId = "EMP-4", fullName = "Change" }, 4);
var mustChangeProof = Sign(key1, device1, epoch, "nonce-5", mustChange, now);
await Error("PASSWORD_CHANGE_REQUIRED", async () => _ = await route.ExecuteAsync(new(changeSession.Token, mustChangeProof, mustChange), now));

var denied = Body("REQ-5", "IDEM-5", "EMPLOYEE_CREATE", "EMP-5", new { employeeId = "EMP-5", fullName = "Denied" }, 5);
var deniedProof = Sign(key1, device1, epoch, "nonce-6", denied, now);
await Error("PERMISSION_DENIED", async () => _ = await route.ExecuteAsync(new(denySession.Token, deniedProof, denied), now));

var unsupported = Body("REQ-6", "IDEM-6", "CLIENT_FAKE_COMMAND", "EMP-6", new { employeeId = "EMP-6" }, 6);
var unsupportedProof = Sign(key1, device1, epoch, "nonce-7", unsupported, now);
await Error("UNSUPPORTED_COMMAND", async () => _ = await route.ExecuteAsync(new(opSession.Token, unsupportedProof, unsupported), now));

var portrait = Body("REQ-7", "IDEM-7", Slice1BusinessAdapter.PortraitCommandCode, "EMP-1", new { employeeId = "EMP-1", portraitRef = "staged:test" }, 7, 1);
var portraitProof = Sign(key1, device1, epoch, "nonce-8", portrait, now);
await Error("RUNTIME_DEPENDENCY_UNAVAILABLE", async () => _ = await route.ExecuteAsync(new(opSession.Token, portraitProof, portrait), now));

await authority.ImportAsync(new("AUTH-ROUTE-V2-2", env, cluster, "route-authority-2", domain, scope, authorityPayload), env, cluster, domain);
var stale = Body("REQ-8", "IDEM-8", "EMPLOYEE_CREATE", "EMP-8", new { employeeId = "EMP-8", fullName = "Stale" }, 8);
var staleProof = Sign(key1, device1, epoch, "nonce-9", stale, now);
await Error("AUTHORITY_REFRESH_REAUTH_REQUIRED", async () => _ = await route.ExecuteAsync(new(opSession.Token, staleProof, stale), now));

Console.WriteLine("LAN_ROUTE_WIRING_HARNESS_PASS signedBodyBinding=PASS pairedDevice=PASS sessionBinding=PASS currentAuthority=PASS authzDeny=PASS unsupported=PASS mustChange=PASS portraitClosed=PASS businessExecute=PASS replay=PASS");
return 0;
