using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Vhdchy.LanService;

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: VHDCHY.LanClientSecurity.Harness <edge.db>");
    return 2;
}

var databasePath = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
if (!File.Exists(databasePath))
{
    var edge = new EdgeStore(databasePath);
    await edge.InitializeAsync(
        "BETA",
        "PICK_PACK_1291",
        "security-harness-edge",
        "security-harness-edge-epoch",
        "VHDCHY_DOMAIN_V1");
}

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static LanClientSignedRequestProof Sign(
    ECDsa key,
    string deviceId,
    string securityEpoch,
    string nonce,
    string method,
    string target,
    string body,
    DateTimeOffset at)
{
    var unsigned = new LanClientSignedRequestProof(
        DeviceId: deviceId,
        SecurityEpoch: securityEpoch,
        TimestampUnixMs: at.ToUnixTimeMilliseconds(),
        Nonce: nonce,
        Method: method,
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

const string deviceId = "PDA-SECURITY-001";
const string actor = "ROOT-SECURITY-HARNESS";
var store = new LanClientSecurityStore(databasePath);
var initial = await store.EnsureAsync();
Assert(initial.Ready, "SECURITY_STORE_NOT_READY");
Assert(!string.IsNullOrWhiteSpace(initial.SecurityEpoch), "SECURITY_EPOCH_MISSING");
Assert(initial.ActiveDeviceCount == 0, "SECURITY_STORE_NOT_EMPTY");

using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
var publicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
var paired = await store.RegisterPairedDeviceAsync(deviceId, publicKey, actor);
Assert(paired.SecurityEpoch == initial.SecurityEpoch, "PAIRING_EPOCH_WRONG");
Assert(paired.Status == "ACTIVE", "PAIRING_STATUS_WRONG");

var now = DateTimeOffset.UtcNow;
var valid = Sign(
    key,
    deviceId,
    paired.SecurityEpoch,
    "nonce-security-0001",
    "POST",
    "/api/v1/data/attendance",
    "{\"employeeId\":\"EMP-1\"}",
    now);
var validDecision = await store.VerifySignedRequestAsync(valid, now);
Assert(validDecision.Authorized && validDecision.Code == "CHANNEL_AUTHORIZED", "VALID_SIGNATURE_REJECTED");

var replayDecision = await store.VerifySignedRequestAsync(valid, now);
Assert(!replayDecision.Authorized && replayDecision.Code == "REQUEST_REPLAY", "REPLAY_NOT_REJECTED");

var signedBeforeTamper = Sign(
    key,
    deviceId,
    paired.SecurityEpoch,
    "nonce-security-0002",
    "POST",
    "/api/v1/data/attendance",
    "{\"employeeId\":\"EMP-1\"}",
    now);
var tampered = signedBeforeTamper with { BodySha256 = LanClientSecurityStore.Sha256Hex("{\"employeeId\":\"EMP-2\"}") };
var tamperedDecision = await store.VerifySignedRequestAsync(tampered, now);
Assert(!tamperedDecision.Authorized && tamperedDecision.Code == "REQUEST_SIGNATURE_INVALID", "TAMPERED_REQUEST_NOT_REJECTED");

var stale = Sign(
    key,
    deviceId,
    paired.SecurityEpoch,
    "nonce-security-0003",
    "GET",
    "/api/v1/sync/status",
    string.Empty,
    now.AddMinutes(-10));
var staleDecision = await store.VerifySignedRequestAsync(stale, now);
Assert(!staleDecision.Authorized && staleDecision.Code == "REQUEST_TIMESTAMP_OUT_OF_WINDOW", "STALE_REQUEST_NOT_REJECTED");

var nextEpoch = await store.RotateSecurityEpochAsync(actor, "security-harness-rotation");
Assert(nextEpoch != paired.SecurityEpoch, "SECURITY_EPOCH_NOT_ROTATED");
var afterRotation = await store.InspectAsync();
Assert(afterRotation.RepairRequiredDeviceCount == 1 && afterRotation.ActiveDeviceCount == 0, "ROTATION_DID_NOT_FENCE_DEVICE");

var oldEpochRequest = Sign(
    key,
    deviceId,
    paired.SecurityEpoch,
    "nonce-security-0004",
    "GET",
    "/api/v1/sync/status",
    string.Empty,
    now);
var oldEpochDecision = await store.VerifySignedRequestAsync(oldEpochRequest, now);
Assert(!oldEpochDecision.Authorized && oldEpochDecision.Code == "SECURITY_EPOCH_MISMATCH", "OLD_EPOCH_NOT_REJECTED");

var repaired = await store.RegisterPairedDeviceAsync(deviceId, publicKey, actor);
Assert(repaired.SecurityEpoch == nextEpoch && repaired.Status == "ACTIVE", "REPAIR_PAIRING_FAILED");
var repairedRequest = Sign(
    key,
    deviceId,
    repaired.SecurityEpoch,
    "nonce-security-0005",
    "GET",
    "/api/v1/sync/status",
    string.Empty,
    now);
var repairedDecision = await store.VerifySignedRequestAsync(repairedRequest, now);
Assert(repairedDecision.Authorized, "REPAIRED_DEVICE_REJECTED");

await store.RevokeDeviceAsync(deviceId, actor, "security-harness-revoke");
var revokedRequest = Sign(
    key,
    deviceId,
    repaired.SecurityEpoch,
    "nonce-security-0006",
    "GET",
    "/api/v1/sync/status",
    string.Empty,
    now);
var revokedDecision = await store.VerifySignedRequestAsync(revokedRequest, now);
Assert(!revokedDecision.Authorized && revokedDecision.Code == "DEVICE_NOT_ACTIVE", "REVOKED_DEVICE_NOT_REJECTED");

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
Assert(await CountAsync(connection, "SELECT COUNT(*) FROM lan_security_audit") == 4, "SECURITY_AUDIT_COUNT_WRONG");
Assert(await CountAsync(connection, "SELECT COUNT(*) FROM lan_request_nonces") == 2, "SECURITY_NONCE_COUNT_WRONG");
Assert(await CountAsync(connection, "SELECT COUNT(*) FROM lan_security_audit WHERE event_type='SECURITY_EPOCH_ROTATED'") == 1, "SECURITY_ROTATION_AUDIT_MISSING");

Console.WriteLine("LAN_CLIENT_SECURITY_HARNESS_PASS pairing=PASS signature=PASS tamper=PASS replay=PASS timestamp=PASS epochRotation=PASS repair=PASS revoke=PASS immutableAudit=PASS");
return 0;
