using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanRouteWiring.Harness <edge.db>");
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
const string scope = "{\"clusterId\":\"PICK_PACK_1291\",\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}";
const string operatorUser = "U_ROUTE_OPERATOR";
const string denyUser = "U_ROUTE_DENY";
const string changeUser = "U_ROUTE_CHANGE";
const string device1 = "PDA-ROUTE-001";
const string device2 = "PDA-ROUTE-002";

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static string Base64Url(byte[] value) =>
    Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

static (string Algorithm, string SecretHash) PasswordRecord(string password, byte seed)
{
    var salt = Enumerable.Range(0, 16).Select(index => unchecked((byte)(seed + index))).ToArray();
    const int iterations = 100_000;
    var digest = Rfc2898DeriveBytes.Pbkdf2(
        Encoding.UTF8.GetBytes(password),
        salt,
        iterations,
        HashAlgorithmName.SHA256,
        32);
    return ($"PBKDF2-SHA256${iterations}${Base64Url(salt)}", Base64Url(digest));
}

static string BuildAuthorityPayload()
{
    var op = PasswordRecord("Route-Operator-Password-1291!", 11);
    var deny = PasswordRecord("Route-Deny-Password-1291!", 41);
    var change = PasswordRecord("Route-Change-Password-1291!", 71);
    return JsonSerializer.Serialize(new
    {
        schemaVersion = LanPrimaryCredentialVerifier.LoginAuthoritySchemaVersion,
        permissionCatalogVersion = LanAuthorizationEvaluator.PermissionCatalogVersion,
        users = new object[]
        {
            new { userId = operatorUser, username = "route.operator", employeeId = "E_ROUTE_OPERATOR", displayName = "Route Operator", email = "route.operator@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" },
            new { userId = denyUser, username = "route.deny", employeeId = "E_ROUTE_DENY", displayName = "Route Deny", email = "route.deny@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" },
            new { userId = changeUser, username = "route.change", employeeId = "E_ROUTE_CHANGE", displayName = "Route Change", email = "route.change@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" }
        },
        credentials = new object[]
        {
            new { credentialId = "C_ROUTE_OPERATOR", userId = operatorUser, credentialType = "PASSWORD", secretHash = op.SecretHash, hashAlgorithm = op.Algorithm, mustChange = false, status = "ACTIVE" },
            new { credentialId = "C_ROUTE_DENY", userId = denyUser, credentialType = "PASSWORD", secretHash = deny.SecretHash, hashAlgorithm = deny.Algorithm, mustChange = false, status = "ACTIVE" },
            new { credentialId = "C_ROUTE_CHANGE", userId = changeUser, credentialType = "PASSWORD", secretHash = change.SecretHash, hashAlgorithm = change.Algorithm, mustChange = true, status = "ACTIVE" }
        },
        roles = Array.Empty<object>(),
        permissions = new object[]
        {
            new { permissionId = "P_EMPLOYEE_CREATE", resource = "employee", action = "create", status = "ACTIVE" },
            new { permissionId = "P_EMPLOYEE_PORTRAIT", resource = "employee", action = "portrait", status = "ACTIVE" }
        },
        rolePermissionGrants = Array.Empty<object>(),
        userRoleGrants = Array.Empty<object>(),
        userPermissionGrants = new object[]
        {
            new { grantId = "G_OP_CREATE", userId = operatorUser, permissionId = "P_EMPLOYEE_CREATE", clusterId, moduleId, effect = "ALLOW", status = "ACTIVE" },
            new { grantId = "G_OP_PORTRAIT", userId = operatorUser, permissionId = "P_EMPLOYEE_PORTRAIT", clusterId, moduleId, effect = "ALLOW", status = "ACTIVE" },
            new { grantId = "G_DENY_ALLOW", userId = denyUser, permissionId = "P_EMPLOYEE_CREATE", clusterId, moduleId, effect = "ALLOW", status = "ACTIVE" },
            new { grantId = "G_DENY_BLOCK", userId = denyUser, permissionId = "P_EMPLOYEE_CREATE", clusterId, moduleId, effect = "DENY", status = "ACTIVE" },
            new { grantId = "G_CHANGE_CREATE", userId = changeUser, permissionId = "P_EMPLOYEE_CREATE", clusterId, moduleId, effect = "ALLOW", status = "ACTIVE" }
        }
    });
}

static LanClientSignedRequestProof Sign(
    ECDsa key,
    string deviceId,
    string epoch,
    string nonce,
    string body,
    DateTimeOffset at,
    string target = LanBusinessRouteCoordinator.BusinessCommandRouteTarget)
{
    var unsigned = new LanClientSignedRequestProof(
        DeviceId: deviceId,
        SecurityEpoch: epoch,
        TimestampUnixMs: at.ToUnixTimeMilliseconds(),
        Nonce: nonce,
        Method: LanBusinessRouteCoordinator.BusinessCommandMethod,
        RequestTarget: target,
        BodySha256: LanClientSecurityStore.Sha256Hex(body),
        SignatureBase64: "placeholder-signature");
    var canonical = LanClientSecurityStore.BuildCanonicalRequest(unsigned);
    var signature = key.SignData(
        Encoding.UTF8.GetBytes(canonical),
        HashAlgorithmName.SHA256,
        DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    return unsigned with { SignatureBase64 = Convert.ToBase64String(signature) };
}

static string CommandBody(
    string requestId,
    string idempotencyKey,
    string commandCode,
    string entityId,
    object payload,
    long deviceSeq,
    long? expectedEntityVersion = null) =>
    JsonSerializer.Serialize(new
    {
        requestId,
        idempotencyKey,
        commandCode,
        entityId,
        expectedEntityVersion,
        payload,
        deviceSeq
    });

static async Task ExpectCodeAsync(string expectedCode, Func<Task> action)
{
    try
    {
        await action();
        throw new InvalidOperationException($"EXPECTED_ROUTE_ERROR_NOT_THROWN:{expectedCode}");
    }
    catch (LanBusinessRouteException error)
    {
        Assert(error.Code == expectedCode, $"ROUTE_ERROR_WRONG:{expectedCode}:{error.Code}");
    }
}

var authorityStore = new AuthoritySnapshotStore(databasePath);
var payload = BuildAuthorityPayload();
await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-ROUTE-V2-1",
        environment,
        clusterId,
        "route-wiring-authority-1",
        compatibility,
        scope,
        payload),
    environment,
    clusterId,
    compatibility);

var operationalStore = new OperationalSnapshotStore(databasePath);
await operationalStore.ImportAsync(
    new OperationalSnapshotEnvelope(
        "OP-ROUTE-1",
        environment,
        clusterId,
        "route-wiring-operational-1",
        compatibility,
        "{\"generation\":1,\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
        "{\"employees\":[],\"employeeCodes\":[],\"presence\":[],\"generation\":1}"),
    environment,
    clusterId,
    compatibility,
    new[] { moduleId });

var securityStore = new LanClientSecurityStore(databasePath);
var security = await securityStore.EnsureAsync();
using var key1 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
using var key2 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
await securityStore.RegisterPairedDeviceAsync(device1, Convert.ToBase64String(key1.ExportSubjectPublicKeyInfo()), "ROOT-ROUTE-HARNESS");
await securityStore.RegisterPairedDeviceAsync(device2, Convert.ToBase64String(key2.ExportSubjectPublicKeyInfo()), "ROOT-ROUTE-HARNESS");
var epoch = (await securityStore.InspectAsync()).SecurityEpoch!;
Assert(epoch == security.SecurityEpoch, "ROUTE_SECURITY_EPOCH_CHANGED_UNEXPECTEDLY");

var sessionStore = new LanUserSessionStore(databasePath);
await sessionStore.EnsureAsync();
var operatorSession = await sessionStore.IssueFromAuthenticatedEvidenceAsync(operatorUser, device1, epoch, "AUTH-ROUTE-V2-1", false);
var denySession = await sessionStore.IssueFromAuthenticatedEvidenceAsync(denyUser, device1, epoch, "AUTH-ROUTE-V2-1", false);
var changeSession = await sessionStore.IssueFromAuthenticatedEvidenceAsync(changeUser, device1, epoch, "AUTH-ROUTE-V2-1", true);
var coordinator = new LanBusinessRouteCoordinator(databasePath, environment, clusterId, compatibility);
var now = DateTimeOffset.UtcNow;

var createBody = CommandBody(
    "REQ-ROUTE-0001",
    "IDEM-ROUTE-0001",
    "EMPLOYEE_CREATE",
    "EMP-ROUTE-0001",
    new { employeeId = "EMP-ROUTE-0001", fullName = "Nhân sự Route Test", status = "ACTIVE" },
    1);
var createProof = Sign(key1, device1, epoch, "nonce-route-0001", createBody, now);
var created = await coordinator.ExecuteAsync(new LanBusinessRouteRequest(operatorSession.Token, createProof, createBody), now);
Assert(!string.IsNullOrWhiteSpace(created.EventId), "ROUTE_BUSINESS_EVENT_MISSING");

await ExpectCodeAsync("REQUEST_REPLAY", async () =>
    _ = await coordinator.ExecuteAsync(new LanBusinessRouteRequest(operatorSession.Token, createProof, createBody), now));

var tamperBody = createBody.Replace("Nhân sự Route Test", "Tampered", StringComparison.Ordinal);
var tamperProof = Sign(key1, device1, epoch, "nonce-route-0002", createBody, now);
await ExpectCodeAsync("SIGNED_REQUEST_BODY_MISMATCH", async () =>
    _ = await coordinator.ExecuteAsync(new LanBusinessRouteRequest(operatorSession.Token, tamperProof, tamperBody), now));

var wrongTargetBody = CommandBody(
    "REQ-ROUTE-0002", "IDEM-ROUTE-0002", "EMPLOYEE_CREATE", "EMP-ROUTE-0002",
    new { employeeId = "EMP-ROUTE-0002", fullName = "Wrong Target" }, 2);
var wrongTargetProof = Sign(key1, device1, epoch, "nonce-route-0003", wrongTargetBody, now, "/api/v1/data/not-reviewed");
await ExpectCodeAsync("SIGNED_REQUEST_TARGET_INVALID", async () =>
    _ = await coordinator.ExecuteAsync(new LanBusinessRouteRequest(operatorSession.Token, wrongTargetProof, wrongTargetBody), now));

var wrongDeviceBody = CommandBody(
    "REQ-ROUTE-0003", "IDEM-ROUTE-0003", "EMPLOYEE_CREATE", "EMP-ROUTE-0003",
    new { employeeId = "EMP-ROUTE-0003", fullName = "Wrong Device" }, 3);
var wrongDeviceProof = Sign(key2, device2, epoch, "nonce-route-0004", wrongDeviceBody, now);
await ExpectCodeAsync("SESSION_DEVICE_MISMATCH", async () =>
    _ = await coordinator.ExecuteAsync(new LanBusinessRouteRequest(operatorSession.Token, wrongDeviceProof, wrongDeviceBody), now));

var mustChangeBody = CommandBody(
    "REQ-ROUTE-0004", "IDEM-ROUTE-0004", "EMPLOYEE_CREATE", "EMP-ROUTE-0004",
    new { employeeId = "EMP-ROUTE-0004", fullName = "Must Change" }, 4);
var mustChangeProof = Sign(key1, device1, epoch, "nonce-route-0005", mustChangeBody, now);
await ExpectCodeAsync("PASSWORD_CHANGE_REQUIRED", async () =>
    _ = await coordinator.ExecuteAsync(new LanBusinessRouteRequest(changeSession.Token, mustChangeProof, mustChangeBody), now));

var denyBody = CommandBody(
    "REQ-ROUTE-0005", "IDEM-ROUTE-0005", "EMPLOYEE_CREATE", "EMP-ROUTE-0005",
    new { employeeId = "EMP-ROUTE-0005", fullName = "Denied" }, 5);
var denyProof = Sign(key1, device1, epoch, "nonce-route-0006", denyBody, now);
await ExpectCodeAsync("PERMISSION_DENIED", async () =>
    _ = await coordinator.ExecuteAsync(new LanBusinessRouteRequest(denySession.Token, denyProof, denyBody), now));

var unsupportedBody = CommandBody(
    "REQ-ROUTE-0006", "IDEM-ROUTE-0006", "CLIENT_FAKE_COMMAND", "EMP-ROUTE-0006",
    new { employeeId = "EMP-ROUTE-0006" }, 6);
var unsupportedProof = Sign(key1, device1, epoch, "nonce-route-0007", unsupportedBody, now);
await ExpectCodeAsync("UNSUPPORTED_COMMAND", async () =>
    _ = await coordinator.ExecuteAsync(new LanBusinessRouteRequest(operatorSession.Token, unsupportedProof, unsupportedBody), now));

var portraitBody = CommandBody(
    "REQ-ROUTE-0007", "IDEM-ROUTE-0007", Slice1BusinessAdapter.PortraitCommandCode, "EMP-ROUTE-0001",
    new { employeeId = "EMP-ROUTE-0001", portraitRef = "staged:test" }, 7, 1);
var portraitProof = Sign(key1, device1, epoch, "nonce-route-0008", portraitBody, now);
await ExpectCodeAsync("RUNTIME_DEPENDENCY_UNAVAILABLE", async () =>
    _ = await coordinator.ExecuteAsync(new LanBusinessRouteRequest(operatorSession.Token, portraitProof, portraitBody), now));

await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-ROUTE-V2-2",
        environment,
        clusterId,
        "route-wiring-authority-2",
        compatibility,
        scope,
        payload),
    environment,
    clusterId,
    compatibility);
var staleBody = CommandBody(
    "REQ-ROUTE-0008", "IDEM-ROUTE-0008", "EMPLOYEE_CREATE", "EMP-ROUTE-0008",
    new { employeeId = "EMP-ROUTE-0008", fullName = "Stale Session" }, 8);
var staleProof = Sign(key1, device1, epoch, "nonce-route-0009", staleBody, now);
await ExpectCodeAsync("AUTHORITY_REFRESH_REAUTH_REQUIRED", async () =>
    _ = await coordinator.ExecuteAsync(new LanBusinessRouteRequest(operatorSession.Token, staleProof, staleBody), now));

Console.WriteLine("LAN_ROUTE_WIRING_HARNESS_PASS signedBodyBinding=PASS pairedDevice=PASS sessionBinding=PASS currentAuthority=PASS authzDeny=PASS unsupported=PASS mustChange=PASS portraitClosed=PASS businessExecute=PASS replay=PASS");
return 0;
