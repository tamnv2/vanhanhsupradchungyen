using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanPrimaryAuth.Harness <edge.db>");
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
const string moduleId = "IDENTITY_EMPLOYEE_ATTENDANCE";
const string compatibility = "VHDCHY_DOMAIN_V1";
const string authorityScope = "{\"clusterId\":\"PICK_PACK_1291\",\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}";
const string deviceId = "PDA-PRIMARY-AUTH-001";
const string normalUserId = "U_NORMAL_AUTH";
const string superadminUserId = "U_SUPER_AUTH";
const string rootUserId = "U_ROOT_AUTH";
const string disabledUserId = "U_DISABLED_AUTH";
const string normalPassword = "Normal-Test-Password-1291!";
const string superPassword = "Super-Test-Password-1291!";

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

static string BuildAuthorityPayload(
    IReadOnlyList<object> users,
    IReadOnlyList<object> credentials,
    bool includePermissionGrant)
{
    var permissions = includePermissionGrant
        ? new object[]
        {
            new { permissionId = "P_EMPLOYEE_READ", resource = "employee", action = "read", status = "ACTIVE" }
        }
        : Array.Empty<object>();
    var grants = includePermissionGrant
        ? new object[]
        {
            new
            {
                grantId = "G_NORMAL_EMPLOYEE_READ",
                userId = normalUserId,
                permissionId = "P_EMPLOYEE_READ",
                clusterId,
                moduleId,
                effect = "ALLOW",
                status = "ACTIVE",
                effectiveFrom = (string?)null,
                effectiveTo = (string?)null
            }
        }
        : Array.Empty<object>();

    return JsonSerializer.Serialize(new
    {
        schemaVersion = LanPrimaryCredentialVerifier.LoginAuthoritySchemaVersion,
        permissionCatalogVersion = LanAuthorizationEvaluator.PermissionCatalogVersion,
        users,
        credentials,
        roles = Array.Empty<object>(),
        permissions,
        rolePermissionGrants = Array.Empty<object>(),
        userRoleGrants = Array.Empty<object>(),
        userPermissionGrants = grants
    });
}

var normalRecord = PasswordRecord(normalPassword, 11);
var superRecord = PasswordRecord(superPassword, 41);

object[] validUsers =
{
    new { userId = normalUserId, username = "normal.operator", employeeId = "E-NORMAL", displayName = "Normal Operator", email = "normal@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" },
    new { userId = superadminUserId, username = "super.admin", employeeId = "E-SUPER", displayName = "Super Admin", email = "super@example.invalid", status = "ACTIVE", securityLevel = "SUPERADMIN" },
    new { userId = rootUserId, username = "root.owner", employeeId = (string?)null, displayName = "Root Owner", email = "root@example.invalid", status = "ACTIVE", securityLevel = "ROOT" },
    new { userId = disabledUserId, username = "disabled.user", employeeId = "E-DISABLED", displayName = "Disabled User", email = "disabled@example.invalid", status = "DISABLED", securityLevel = "NORMAL" }
};
object[] validCredentials =
{
    new { credentialId = "C_NORMAL", userId = normalUserId, credentialType = "PASSWORD", secretHash = normalRecord.SecretHash, hashAlgorithm = normalRecord.Algorithm, mustChange = true, status = "ACTIVE" },
    new { credentialId = "C_SUPER", userId = superadminUserId, credentialType = "PASSWORD", secretHash = superRecord.SecretHash, hashAlgorithm = superRecord.Algorithm, mustChange = false, status = "ACTIVE" }
};

var authorityStore = new AuthoritySnapshotStore(databasePath);
await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-LOGIN-V2-VALID",
        environment,
        clusterId,
        "primary-auth-harness-valid",
        compatibility,
        authorityScope,
        BuildAuthorityPayload(validUsers, validCredentials, includePermissionGrant: true)),
    environment,
    clusterId,
    compatibility);

var verifier = new LanPrimaryCredentialVerifier(databasePath, compatibility);
var inspection = await verifier.InspectAsync();
Assert(inspection.Ready && inspection.Code == "READY", "VALID_LOGIN_AUTHORITY_NOT_READY");
Assert(inspection.AuthoritySnapshotVersion == "AUTH-LOGIN-V2-VALID", "VALID_LOGIN_AUTHORITY_VERSION_WRONG");
Assert(inspection.LoginCapableUserCount == 2, "LOGIN_CAPABLE_USER_COUNT_WRONG");

var normal = await verifier.VerifyAsync("normal.operator", normalPassword);
Assert(normal.Authenticated && normal.Code == "AUTHENTICATED", "NORMAL_LOGIN_FAILED");
Assert(normal.User?.UserId == normalUserId, "NORMAL_LOGIN_ACTOR_WRONG");
Assert(normal.User?.MustChangePassword == true, "NORMAL_MUST_CHANGE_NOT_PRESERVED");
Assert(normal.User?.AuthoritySnapshotVersion == "AUTH-LOGIN-V2-VALID", "NORMAL_AUTHORITY_EVIDENCE_WRONG");

var normalCase = await verifier.VerifyAsync("NoRmAl.OpErAtOr", normalPassword);
Assert(normalCase.Authenticated && normalCase.User?.UserId == normalUserId, "USERNAME_CASE_INSENSITIVE_LOGIN_FAILED");

var wrongPassword = await verifier.VerifyAsync("normal.operator", "definitely-wrong");
Assert(!wrongPassword.Authenticated && wrongPassword.Code == "INVALID_CREDENTIALS", "WRONG_PASSWORD_NOT_REJECTED");
var missingUser = await verifier.VerifyAsync("missing.user", normalPassword);
Assert(!missingUser.Authenticated && missingUser.Code == "INVALID_CREDENTIALS", "MISSING_USER_NOT_REJECTED");
var disabled = await verifier.VerifyAsync("disabled.user", normalPassword);
Assert(!disabled.Authenticated && disabled.Code == "INVALID_CREDENTIALS", "DISABLED_USER_NOT_REJECTED");

var super = await verifier.VerifyAsync("super.admin", superPassword);
Assert(super.Authenticated && super.User?.SecurityLevel == "SUPERADMIN", "SUPERADMIN_PASSWORD_LOGIN_FAILED");
var root = await verifier.VerifyAsync("root.owner", "any-permanent-password-must-not-authenticate");
Assert(!root.Authenticated && root.Code == "ROOT_EMAIL_OTP_REQUIRED", "ROOT_PASSWORD_BYPASSED_OTP");
Assert(root.User?.UserId == rootUserId, "ROOT_OTP_USER_EVIDENCE_MISSING");

var authorization = new LanAuthorizationEvaluator(databasePath, compatibility);
var authzDecision = await authorization.AuthorizeAsync(new LanAuthorizationRequest(
    normalUserId,
    "employee",
    "read",
    clusterId,
    moduleId));
Assert(authzDecision.Allowed && authzDecision.Code == "AUTHORIZED", "AUTHORITY_V2_PERMISSION_EVALUATION_FAILED");
Assert(authzDecision.AuthoritySnapshotVersion == "AUTH-LOGIN-V2-VALID", "AUTHORITY_V2_PERMISSION_VERSION_WRONG");

var securityStore = new LanClientSecurityStore(databasePath);
var security = await securityStore.EnsureAsync();
using var deviceKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
await securityStore.RegisterPairedDeviceAsync(
    deviceId,
    Convert.ToBase64String(deviceKey.ExportSubjectPublicKeyInfo()),
    "ROOT-PRIMARY-AUTH-HARNESS");
var epoch = (await securityStore.InspectAsync()).SecurityEpoch!;

var sessionStore = new LanUserSessionStore(databasePath);
await sessionStore.EnsureAsync();
var authenticatedUser = normal.User ?? throw new InvalidOperationException("NORMAL_AUTH_EVIDENCE_MISSING");
var session = await sessionStore.IssueFromAuthenticatedEvidenceAsync(
    authenticatedUser.UserId,
    deviceId,
    epoch,
    authenticatedUser.AuthoritySnapshotVersion,
    authenticatedUser.MustChangePassword);
var sessionDecision = await sessionStore.AuthenticateAsync(session.Token, deviceId, epoch);
Assert(sessionDecision.Authenticated && sessionDecision.Principal?.UserId == normalUserId, "VERIFIER_TO_SESSION_CHAIN_FAILED");
Assert(sessionDecision.Principal?.MustChangePassword == true, "VERIFIER_TO_SESSION_MUST_CHANGE_LOST");

var invalidSalt = Base64Url(Enumerable.Range(0, 16).Select(i => (byte)(90 + i)).ToArray());
object[] malformedCredentials =
{
    new { credentialId = "C_BAD", userId = normalUserId, credentialType = "PASSWORD", secretHash = normalRecord.SecretHash, hashAlgorithm = $"PBKDF2-SHA256$99999${invalidSalt}", mustChange = false, status = "ACTIVE" },
    new { credentialId = "C_SUPER", userId = superadminUserId, credentialType = "PASSWORD", secretHash = superRecord.SecretHash, hashAlgorithm = superRecord.Algorithm, mustChange = false, status = "ACTIVE" }
};
await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-LOGIN-V2-BAD-CRED",
        environment,
        clusterId,
        "primary-auth-harness-bad-credential",
        compatibility,
        authorityScope,
        BuildAuthorityPayload(validUsers, malformedCredentials, includePermissionGrant: false)),
    environment,
    clusterId,
    compatibility);
var malformedInspection = await verifier.InspectAsync();
Assert(!malformedInspection.Ready && malformedInspection.Code == "AUTHORITY_CREDENTIAL_INCOMPATIBLE", "MALFORMED_CREDENTIAL_AUTHORITY_NOT_REJECTED");

object[] missingCredentialUsers =
{
    new { userId = normalUserId, username = "normal.operator", employeeId = "E-NORMAL", displayName = "Normal Operator", email = "normal@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" },
    new { userId = rootUserId, username = "root.owner", employeeId = (string?)null, displayName = "Root Owner", email = "root@example.invalid", status = "ACTIVE", securityLevel = "ROOT" }
};
await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-LOGIN-V2-MISSING-CRED",
        environment,
        clusterId,
        "primary-auth-harness-missing-credential",
        compatibility,
        authorityScope,
        BuildAuthorityPayload(missingCredentialUsers, Array.Empty<object>(), includePermissionGrant: false)),
    environment,
    clusterId,
    compatibility);
var missingCredentialInspection = await verifier.InspectAsync();
Assert(!missingCredentialInspection.Ready && missingCredentialInspection.Code == "LOGIN_AUTHORITY_CREDENTIALS_REQUIRED", "MISSING_ACTIVE_CREDENTIAL_AUTHORITY_NOT_REJECTED");

const string v1Payload = """
{
  "schemaVersion":"VHDCHY_AUTHORITY_SNAPSHOT_V1",
  "permissionCatalogVersion":"VHDCHY_PERMISSION_CATALOG_V1",
  "users":[{"userId":"U_V1","status":"ACTIVE","securityLevel":"NORMAL"}],
  "roles":[],
  "permissions":[],
  "rolePermissionGrants":[],
  "userRoleGrants":[],
  "userPermissionGrants":[]
}
""";
await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-LOGIN-V1-LEGACY",
        environment,
        clusterId,
        "primary-auth-harness-v1",
        compatibility,
        authorityScope,
        v1Payload),
    environment,
    clusterId,
    compatibility);
var v1Inspection = await verifier.InspectAsync();
Assert(!v1Inspection.Ready && v1Inspection.Code == "LOGIN_AUTHORITY_SCHEMA_REQUIRED", "V1_LOGIN_AUTHORITY_NOT_FAIL_CLOSED");

Console.WriteLine("LAN_PRIMARY_AUTH_HARNESS_PASS normal=PASS caseInsensitive=PASS wrongPassword=PASS disabled=PASS superadmin=PASS rootOtp=PASS v2Authorization=PASS sessionChain=PASS malformedCredential=PASS missingCredential=PASS v1FailClosed=PASS");
return 0;
