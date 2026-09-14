using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanUserSession.Harness <edge.db>");
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
const string deviceId = "PDA-SESSION-001";
const string secondDeviceId = "PDA-SESSION-002";
const string activeUserId = "U_SESSION";
const string disabledUserId = "U_DISABLED_SESSION";
const string authorityScope = "{\"clusterId\":\"PICK_PACK_1291\",\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}";
const string payload1 = """
{
  "schemaVersion":"VHDCHY_AUTHORITY_SNAPSHOT_V1",
  "permissionCatalogVersion":"VHDCHY_PERMISSION_CATALOG_V1",
  "users":[
    {"userId":"U_SESSION","status":"ACTIVE","securityLevel":"NORMAL"},
    {"userId":"U_DISABLED_SESSION","status":"DISABLED","securityLevel":"NORMAL"}
  ],
  "roles":[],
  "permissions":[],
  "rolePermissionGrants":[],
  "userRoleGrants":[],
  "userPermissionGrants":[]
}
""";
const string payload2 = """
{
  "schemaVersion":"VHDCHY_AUTHORITY_SNAPSHOT_V1",
  "permissionCatalogVersion":"VHDCHY_PERMISSION_CATALOG_V1",
  "users":[
    {"userId":"U_SESSION","status":"ACTIVE","securityLevel":"NORMAL"},
    {"userId":"U_DISABLED_SESSION","status":"DISABLED","securityLevel":"NORMAL"},
    {"userId":"U_SESSION_EXTRA","status":"ACTIVE","securityLevel":"NORMAL"}
  ],
  "roles":[],
  "permissions":[],
  "rolePermissionGrants":[],
  "userRoleGrants":[],
  "userPermissionGrants":[]
}
""";

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static async Task ExpectSessionError(string code, Func<Task> action)
{
    try
    {
        await action();
        throw new InvalidOperationException($"{code}_NOT_REJECTED");
    }
    catch (LanUserSessionException error) when (error.Code == code)
    {
    }
}

var authorityStore = new AuthoritySnapshotStore(databasePath);
await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-SESSION-1",
        environment,
        clusterId,
        "session-harness-checkpoint-1",
        compatibility,
        authorityScope,
        payload1),
    environment,
    clusterId,
    compatibility);

var securityStore = new LanClientSecurityStore(databasePath);
var security = await securityStore.EnsureAsync();
Assert(security.Ready && !string.IsNullOrWhiteSpace(security.SecurityEpoch), "SECURITY_STATE_NOT_READY");

using var primaryKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
using var secondKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
await securityStore.RegisterPairedDeviceAsync(
    deviceId,
    Convert.ToBase64String(primaryKey.ExportSubjectPublicKeyInfo()),
    "ROOT-SESSION-HARNESS");
await securityStore.RegisterPairedDeviceAsync(
    secondDeviceId,
    Convert.ToBase64String(secondKey.ExportSubjectPublicKeyInfo()),
    "ROOT-SESSION-HARNESS");

var epoch = (await securityStore.InspectAsync()).SecurityEpoch!;
var sessionStore = new LanUserSessionStore(databasePath);
await sessionStore.EnsureAsync();

var now = DateTimeOffset.UtcNow;
var first = await sessionStore.IssueFromAuthenticatedEvidenceAsync(
    activeUserId,
    deviceId,
    epoch,
    "AUTH-SESSION-1",
    mustChangePassword: false,
    now: now);
Assert(first.Token.Length >= 32, "SESSION_TOKEN_TOO_SHORT");
Assert(first.AuthoritySnapshotVersion == "AUTH-SESSION-1", "SESSION_AUTHORITY_EVIDENCE_WRONG");

var firstAuth = await sessionStore.AuthenticateAsync(first.Token, deviceId, epoch, now.AddMinutes(1));
Assert(firstAuth.Authenticated && firstAuth.Code == "SESSION_AUTHENTICATED", "VALID_SESSION_REJECTED");
Assert(firstAuth.Principal?.UserId == activeUserId, "SESSION_ACTOR_WRONG");
Assert(firstAuth.Principal?.DeviceId == deviceId, "SESSION_DEVICE_BINDING_WRONG");
Assert(firstAuth.Principal?.AuthoritySnapshotVersion == "AUTH-SESSION-1", "SESSION_AUTHORITY_BINDING_WRONG");

await ExpectSessionError(
    "ACCOUNT_NOT_FOUND",
    () => sessionStore.IssueFromAuthenticatedEvidenceAsync(
        "U_UNKNOWN",
        deviceId,
        epoch,
        "AUTH-SESSION-1",
        false));
await ExpectSessionError(
    "ACCOUNT_NOT_ACTIVE",
    () => sessionStore.IssueFromAuthenticatedEvidenceAsync(
        disabledUserId,
        deviceId,
        epoch,
        "AUTH-SESSION-1",
        false));

var wrongDevice = await sessionStore.AuthenticateAsync(first.Token, secondDeviceId, epoch, now.AddMinutes(1));
Assert(!wrongDevice.Authenticated && wrongDevice.Code == "SESSION_DEVICE_MISMATCH", "SESSION_REUSED_ON_OTHER_PAIRED_DEVICE");

var second = await sessionStore.IssueFromAuthenticatedEvidenceAsync(
    activeUserId,
    deviceId,
    epoch,
    "AUTH-SESSION-1",
    mustChangePassword: true,
    now: now.AddMinutes(2));
var superseded = await sessionStore.AuthenticateAsync(first.Token, deviceId, epoch, now.AddMinutes(3));
Assert(!superseded.Authenticated && superseded.Code == "SESSION_NOT_ACTIVE", "OLDER_SESSION_NOT_SUPERSEDED");
var secondAuth = await sessionStore.AuthenticateAsync(second.Token, deviceId, epoch, now.AddMinutes(3));
Assert(secondAuth.Authenticated && secondAuth.Principal?.MustChangePassword == true, "MUST_CHANGE_PASSWORD_STATE_NOT_PRESERVED");

var expiring = await sessionStore.IssueFromAuthenticatedEvidenceAsync(
    activeUserId,
    secondDeviceId,
    epoch,
    "AUTH-SESSION-1",
    false,
    now: now,
    ttl: TimeSpan.FromMinutes(15));
var expired = await sessionStore.AuthenticateAsync(expiring.Token, secondDeviceId, epoch, now.AddMinutes(16));
Assert(!expired.Authenticated && expired.Code == "SESSION_EXPIRED", "SESSION_EXPIRY_NOT_ENFORCED");

await authorityStore.ImportAsync(
    new AuthoritySnapshotEnvelope(
        "AUTH-SESSION-2",
        environment,
        clusterId,
        "session-harness-checkpoint-2",
        compatibility,
        authorityScope,
        payload2),
    environment,
    clusterId,
    compatibility);
var staleAuthority = await sessionStore.AuthenticateAsync(second.Token, deviceId, epoch, now.AddMinutes(4));
Assert(!staleAuthority.Authenticated && staleAuthority.Code == "AUTHORITY_REFRESH_REAUTH_REQUIRED", "AUTHORITY_REFRESH_DID_NOT_FENCE_SESSION");

var current = await sessionStore.IssueFromAuthenticatedEvidenceAsync(
    activeUserId,
    deviceId,
    epoch,
    "AUTH-SESSION-2",
    false,
    now: now.AddMinutes(5));
var currentAuth = await sessionStore.AuthenticateAsync(current.Token, deviceId, epoch, now.AddMinutes(6));
Assert(currentAuth.Authenticated, "CURRENT_AUTHORITY_SESSION_REJECTED");

var nextEpoch = await securityStore.RotateSecurityEpochAsync("ROOT-SESSION-HARNESS", "session-harness-rotation");
var oldEpoch = await sessionStore.AuthenticateAsync(current.Token, deviceId, epoch, now.AddMinutes(7));
Assert(!oldEpoch.Authenticated && oldEpoch.Code == "SECURITY_EPOCH_MISMATCH", "ROTATED_SECURITY_EPOCH_DID_NOT_FENCE_SESSION");

await securityStore.RegisterPairedDeviceAsync(
    deviceId,
    Convert.ToBase64String(primaryKey.ExportSubjectPublicKeyInfo()),
    "ROOT-SESSION-HARNESS");
var oldBoundEpoch = await sessionStore.AuthenticateAsync(current.Token, deviceId, nextEpoch, now.AddMinutes(8));
Assert(!oldBoundEpoch.Authenticated && oldBoundEpoch.Code == "SECURITY_EPOCH_MISMATCH", "SESSION_EPOCH_BINDING_NOT_ENFORCED_AFTER_REPAIR");

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

static async Task<string?> ScalarTextAsync(SqliteConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    var value = await command.ExecuteScalarAsync();
    return value is null or DBNull ? null : Convert.ToString(value);
}

Assert(await CountAsync(connection, "SELECT COUNT(*) FROM lan_auth_sessions") == 4, "SESSION_ROW_COUNT_WRONG");
Assert(await CountAsync(connection, "SELECT COUNT(*) FROM lan_auth_sessions WHERE status='REVOKED'") >= 1, "SUPERSEDED_SESSION_NOT_DURABLE");
var storedHash = await ScalarTextAsync(connection, $"SELECT token_hash FROM lan_auth_sessions WHERE auth_session_id='{current.SessionId}'");
Assert(!string.IsNullOrWhiteSpace(storedHash), "SESSION_TOKEN_HASH_NOT_STORED");
Assert(!string.Equals(storedHash, current.Token, StringComparison.Ordinal), "PLAINTEXT_SESSION_TOKEN_STORED");
Assert(await CountAsync(connection, "PRAGMA foreign_key_check") == 0, "SESSION_FOREIGN_KEY_CHECK_FAILED");

Console.WriteLine("LAN_USER_SESSION_BINDING_HARNESS_PASS activeUser=PASS inactiveUser=PASS pairedDevice=PASS tokenHash=PASS deviceBinding=PASS supersede=PASS expiry=PASS authorityRefresh=PASS securityEpoch=PASS mustChange=PASS");
return 0;
