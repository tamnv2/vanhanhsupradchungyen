using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Vhdchy.LanService;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: VHDCHY.LanAttendanceScanContext.Harness <lan-service-dll> <data-root> <port>");
    return 2;
}

var serviceDll = Path.GetFullPath(args[0]);
var dataRoot = Path.GetFullPath(args[1]);
if (!File.Exists(serviceDll) || !int.TryParse(args[2], out var port) || port is <= 1024 or >= 65536) return 2;
Directory.CreateDirectory(dataRoot);

const string environment = "BETA";
const string clusterId = "PICK_PACK_1291";
const string compatibility = "VHDCHY_DOMAIN_V1";
const string moduleId = "IDENTITY_EMPLOYEE_ATTENDANCE";
const string allowedUserId = "U_SCAN_ALLOWED";
const string deniedUserId = "U_SCAN_DENIED";
const string allowedUsername = "scan.allowed";
const string deniedUsername = "scan.denied";
const string password = "Attendance-Scan-Harness-1291!";
const string deviceId = "PDA-SCAN-CONTEXT-001";
const string authorityScope = "{\"clusterId\":\"PICK_PACK_1291\",\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}";

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static string Base64Url(byte[] value) =>
    Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

static (string Algorithm, string SecretHash) PasswordRecord(string value)
{
    var salt = Enumerable.Range(0, 16).Select(index => unchecked((byte)(31 + index))).ToArray();
    const int iterations = 100_000;
    var digest = Rfc2898DeriveBytes.Pbkdf2(
        Encoding.UTF8.GetBytes(value),
        salt,
        iterations,
        HashAlgorithmName.SHA256,
        32);
    return ($"PBKDF2-SHA256${iterations}${Base64Url(salt)}", Base64Url(digest));
}

static SignedEnvelope Sign(ECDsa key, string device, string epoch, string target, string rawBody)
{
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var nonce = $"vhdchy-scan-{Guid.NewGuid():N}";
    var unsigned = new LanClientSignedRequestProof(
        device,
        epoch,
        timestamp,
        nonce,
        "POST",
        target,
        LanClientSecurityStore.Sha256Hex(rawBody),
        "placeholder-signature");
    var canonical = LanClientSecurityStore.BuildCanonicalRequest(unsigned);
    var signature = key.SignData(
        Encoding.UTF8.GetBytes(canonical),
        HashAlgorithmName.SHA256,
        DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    return new SignedEnvelope(rawBody, timestamp, nonce, Convert.ToBase64String(signature));
}

static HttpRequestMessage RequestFor(
    string target,
    SignedEnvelope envelope,
    string device,
    string epoch,
    string? token = null,
    string? bodyOverride = null)
{
    var request = new HttpRequestMessage(HttpMethod.Post, target)
    {
        Content = new StringContent(bodyOverride ?? envelope.RawBody, Encoding.UTF8, "application/json")
    };
    request.Headers.TryAddWithoutValidation(LanSecureHttpRoutes.DeviceIdHeader, device);
    request.Headers.TryAddWithoutValidation(LanSecureHttpRoutes.SecurityEpochHeader, epoch);
    request.Headers.TryAddWithoutValidation(LanSecureHttpRoutes.TimestampHeader, envelope.TimestampUnixMs.ToString(System.Globalization.CultureInfo.InvariantCulture));
    request.Headers.TryAddWithoutValidation(LanSecureHttpRoutes.NonceHeader, envelope.Nonce);
    request.Headers.TryAddWithoutValidation(LanSecureHttpRoutes.SignatureHeader, envelope.SignatureBase64);
    request.Headers.TryAddWithoutValidation("X-Request-Id", $"REQ-SCAN-{Guid.NewGuid():N}");
    if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new("Bearer", token);
    return request;
}

static async Task<(HttpStatusCode Status, string Body)> SendAsync(HttpClient client, HttpRequestMessage request)
{
    using (request)
    using (var response = await client.SendAsync(request))
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
}

static string ErrorCode(string body)
{
    using var document = JsonDocument.Parse(body);
    return document.RootElement.GetProperty("error").GetProperty("code").GetString() ?? string.Empty;
}

static string SessionToken(string body)
{
    using var document = JsonDocument.Parse(body);
    return document.RootElement.GetProperty("session").GetProperty("token").GetString() ?? string.Empty;
}

static async Task<long> CountAsync(string databasePath, string table)
{
    await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadWrite,
        Cache = SqliteCacheMode.Shared
    }.ToString());
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = $"SELECT COUNT(*) FROM {table}";
    return Convert.ToInt64(await command.ExecuteScalarAsync() ?? 0L);
}

static async Task WaitForHealthAsync(HttpClient client, RunningService service, string expectedTransport)
{
    Exception? last = null;
    for (var attempt = 0; attempt < 60; attempt++)
    {
        if (service.Process.HasExited)
            throw new InvalidOperationException($"LAN_SERVICE_EXITED:{service.Process.ExitCode}\n{await service.ReadDiagnosticsAsync()}");
        try
        {
            using var response = await client.GetAsync("/health");
            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert(document.RootElement.GetProperty("transport").GetString() == expectedTransport, "HEALTH_TRANSPORT_MISMATCH");
                return;
            }
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            last = error;
        }
        await Task.Delay(250);
    }
    throw new InvalidOperationException("LAN_SERVICE_HEALTH_TIMEOUT", last);
}

static RunningService StartService(string dll, string root, int listenPort, string? pfx = null, string? pfxPassword = null)
{
    var start = new ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    start.ArgumentList.Add(dll);
    start.Environment["VHDCHY_ENV"] = environment;
    start.Environment["VHDCHY_CLUSTER_ID"] = clusterId;
    start.Environment["VHDCHY_LAN_PORT"] = listenPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
    start.Environment["VHDCHY_LAN_DATA_ROOT"] = root;
    if (!string.IsNullOrWhiteSpace(pfx))
    {
        start.Environment["VHDCHY_LAN_TLS_PFX_PATH"] = pfx;
        start.Environment["VHDCHY_LAN_TLS_PFX_PASSWORD"] = pfxPassword ?? string.Empty;
    }
    var process = Process.Start(start) ?? throw new InvalidOperationException("LAN_SERVICE_START_FAILED");
    return new RunningService(process, process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync());
}

static async Task<string> LoginAsync(
    HttpClient client,
    ECDsa key,
    string epoch,
    string username)
{
    var rawBody = JsonSerializer.Serialize(new { username, password });
    var envelope = Sign(key, deviceId, epoch, LanSecureHttpRoutes.LoginRouteTarget, rawBody);
    var login = await SendAsync(client, RequestFor(LanSecureHttpRoutes.LoginRouteTarget, envelope, deviceId, epoch));
    Assert(login.Status == HttpStatusCode.OK, $"LOGIN_FAILED:{username}:{login.Status}:{login.Body}");
    var token = SessionToken(login.Body);
    Assert(token.Length >= 32, $"SESSION_TOKEN_MISSING:{username}");
    return token;
}

var dbPath = Path.Combine(dataRoot, "edge.db");
var diagnostics = new StringBuilder();

var bootstrap = StartService(serviceDll, dataRoot, port);
try
{
    using var bootstrapClient = new HttpClient
    {
        BaseAddress = new Uri($"http://127.0.0.1:{port}"),
        Timeout = TimeSpan.FromSeconds(5)
    };
    await WaitForHealthAsync(bootstrapClient, bootstrap, "HTTP_READ_ONLY");
}
finally
{
    diagnostics.Append(await bootstrap.StopAsync());
}
Assert(File.Exists(dbPath), "EDGE_DB_NOT_BOOTSTRAPPED");

var passwordRecord = PasswordRecord(password);
var authorityPayload = JsonSerializer.Serialize(new
{
    schemaVersion = LanPrimaryCredentialVerifier.LoginAuthoritySchemaVersion,
    permissionCatalogVersion = LanAuthorizationEvaluator.PermissionCatalogVersion,
    users = new object[]
    {
        new { userId = allowedUserId, username = allowedUsername, employeeId = "EMP-ACTOR-1", displayName = "Scan Allowed", email = "allowed@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" },
        new { userId = deniedUserId, username = deniedUsername, employeeId = "EMP-ACTOR-2", displayName = "Scan Denied", email = "denied@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" }
    },
    credentials = new object[]
    {
        new { credentialId = "C_SCAN_ALLOWED", userId = allowedUserId, credentialType = "PASSWORD", secretHash = passwordRecord.SecretHash, hashAlgorithm = passwordRecord.Algorithm, mustChange = false, status = "ACTIVE" },
        new { credentialId = "C_SCAN_DENIED", userId = deniedUserId, credentialType = "PASSWORD", secretHash = passwordRecord.SecretHash, hashAlgorithm = passwordRecord.Algorithm, mustChange = false, status = "ACTIVE" }
    },
    roles = Array.Empty<object>(),
    permissions = new object[]
    {
        new { permissionId = "PERM:attendance:scan", resource = "attendance", action = "scan", status = "ACTIVE" }
    },
    rolePermissionGrants = Array.Empty<object>(),
    userRoleGrants = Array.Empty<object>(),
    userPermissionGrants = new object[]
    {
        new { grantId = "G_SCAN_ALLOWED", userId = allowedUserId, permissionId = "PERM:attendance:scan", clusterId, moduleId, effect = "ALLOW", status = "ACTIVE" }
    }
});

var authority = new AuthoritySnapshotStore(dbPath);
var authorityImport = await authority.ImportAsync(
    new("AUTH-SCAN-CONTEXT-1", environment, clusterId, "scan-context-authority", compatibility, authorityScope, authorityPayload),
    environment,
    clusterId,
    compatibility);
Assert(authorityImport.Activated, "AUTHORITY_NOT_ACTIVE");

var operationalState = JsonSerializer.Serialize(new
{
    employees = new object[]
    {
        new
        {
            employeeId = "EMP-001",
            fullName = "Nguyễn Văn A",
            status = "ACTIVE",
            currentPortraitMediaId = "MEDIA-001",
            entityVersion = 3
        }
    },
    employeeCodes = new object[]
    {
        new
        {
            employeeCodeId = "EC-001",
            employeeId = "EMP-001",
            employeeCode = "MNV001",
            status = "ACTIVE",
            entityVersion = 2
        }
    },
    presence = new object[]
    {
        new
        {
            employeeId = "EMP-001",
            currentState = "IN",
            businessDate = "2026-09-15",
            entityVersion = 4
        }
    }
});
var operational = await new OperationalSnapshotStore(dbPath).ImportAsync(
    new(
        "OP-SCAN-CONTEXT-1",
        environment,
        clusterId,
        "scan-context-operational",
        compatibility,
        "{\"generation\":1,\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
        operationalState),
    environment,
    clusterId,
    compatibility,
    new[] { moduleId });
Assert(operational.Activated, "OPERATIONAL_NOT_ACTIVE");

var security = new LanClientSecurityStore(dbPath);
await security.EnsureAsync();
using var deviceKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
await security.RegisterPairedDeviceAsync(
    deviceId,
    Convert.ToBase64String(deviceKey.ExportSubjectPublicKeyInfo()),
    "ROOT-SCAN-HARNESS");
var epoch = (await security.InspectAsync()).SecurityEpoch ?? throw new InvalidOperationException("SECURITY_EPOCH_MISSING");

const string pfxPassword = "ci-only-scan-pfx-password";
var pfxPath = Path.Combine(dataRoot, "scan-context-test.pfx");
using var rsa = RSA.Create(2048);
var certificateRequest = new CertificateRequest(
    "CN=lan-beta.supra.cc.cd",
    rsa,
    HashAlgorithmName.SHA256,
    RSASignaturePadding.Pkcs1);
var san = new SubjectAlternativeNameBuilder();
san.AddDnsName("lan-beta.supra.cc.cd");
san.AddIpAddress(IPAddress.Loopback);
certificateRequest.CertificateExtensions.Add(san.Build());
certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
certificateRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
    false));
using var certificate = certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
await File.WriteAllBytesAsync(pfxPath, certificate.Export(X509ContentType.Pfx, pfxPassword));
var expectedThumbprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);

using var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
        cert is not null && string.Equals(
            cert.GetCertHashString(HashAlgorithmName.SHA256),
            expectedThumbprint,
            StringComparison.OrdinalIgnoreCase)
};
using var client = new HttpClient(handler)
{
    BaseAddress = new Uri($"https://127.0.0.1:{port}"),
    Timeout = TimeSpan.FromSeconds(5)
};

var eventsBefore = await CountAsync(dbPath, "edge_events");
var cloudOutboxBefore = await CountAsync(dbPath, "cloud_sync_outbox");
var googleOutboxBefore = await CountAsync(dbPath, "google_projection_outbox");

var service = StartService(serviceDll, dataRoot, port, pfxPath, pfxPassword);
try
{
    await WaitForHealthAsync(client, service, "HTTPS");
    Assert(LanSecureHttpRoutes.AttendanceScanRouteTarget == "/api/v1/attendance/scan-context", "SCAN_ROUTE_CONTRACT_MISMATCH");

    var allowedToken = await LoginAsync(client, deviceKey, epoch, allowedUsername);
    var rawScan = JsonSerializer.Serialize(new { employeeCode = "MNV001" });
    var scanEnvelope = Sign(deviceKey, deviceId, epoch, LanSecureHttpRoutes.AttendanceScanRouteTarget, rawScan);
    var scan = await SendAsync(client, RequestFor(LanSecureHttpRoutes.AttendanceScanRouteTarget, scanEnvelope, deviceId, epoch, allowedToken));
    Assert(scan.Status == HttpStatusCode.OK, $"SCAN_FAILED:{scan.Status}:{scan.Body}");
    using (var document = JsonDocument.Parse(scan.Body))
    {
        var root = document.RootElement;
        Assert(root.GetProperty("ok").GetBoolean(), "SCAN_OK_FALSE");
        Assert(root.GetProperty("runtime").GetString() == "LAN", "SCAN_RUNTIME_MISMATCH");
        var context = root.GetProperty("context");
        Assert(context.GetProperty("employeeCodeId").GetString() == "EC-001", "SCAN_CODE_ID_MISMATCH");
        Assert(context.GetProperty("employeeCode").GetString() == "MNV001", "SCAN_CODE_MISMATCH");
        Assert(context.GetProperty("employeeId").GetString() == "EMP-001", "SCAN_EMPLOYEE_ID_MISMATCH");
        Assert(context.GetProperty("fullName").GetString() == "Nguyễn Văn A", "SCAN_NAME_MISMATCH");
        Assert(context.GetProperty("currentPortraitMediaId").GetString() == "MEDIA-001", "SCAN_PORTRAIT_MISMATCH");
        var presence = context.GetProperty("presence");
        Assert(presence.GetProperty("currentState").GetString() == "IN", "SCAN_PRESENCE_STATE_MISMATCH");
        Assert(presence.GetProperty("businessDate").GetString() == "2026-09-15", "SCAN_BUSINESS_DATE_MISMATCH");
        Assert(presence.GetProperty("entityVersion").GetInt64() == 4, "SCAN_ENTITY_VERSION_MISMATCH");
        Assert(!context.TryGetProperty("actorUserId", out _), "SCAN_ACTOR_AUTHORITY_LEAKED");
    }

    var replay = await SendAsync(client, RequestFor(LanSecureHttpRoutes.AttendanceScanRouteTarget, scanEnvelope, deviceId, epoch, allowedToken));
    Assert(replay.Status == HttpStatusCode.Unauthorized && ErrorCode(replay.Body) == "REQUEST_REPLAY", "SCAN_REPLAY_NOT_REJECTED");

    var unknownBody = JsonSerializer.Serialize(new { employeeCode = "MNV404" });
    var unknownEnvelope = Sign(deviceKey, deviceId, epoch, LanSecureHttpRoutes.AttendanceScanRouteTarget, unknownBody);
    var unknown = await SendAsync(client, RequestFor(LanSecureHttpRoutes.AttendanceScanRouteTarget, unknownEnvelope, deviceId, epoch, allowedToken));
    Assert(unknown.Status == HttpStatusCode.NotFound && ErrorCode(unknown.Body) == "SCAN_CONTEXT_NOT_FOUND", "UNKNOWN_MNV_NOT_404");

    var strictBody = JsonSerializer.Serialize(new { employeeCode = "MNV001", employeeId = "SPOOF" });
    var strictEnvelope = Sign(deviceKey, deviceId, epoch, LanSecureHttpRoutes.AttendanceScanRouteTarget, strictBody);
    var strict = await SendAsync(client, RequestFor(LanSecureHttpRoutes.AttendanceScanRouteTarget, strictEnvelope, deviceId, epoch, allowedToken));
    Assert(strict.Status == HttpStatusCode.UnprocessableEntity && ErrorCode(strict.Body) == "INVALID_INPUT", "CLIENT_AUTHORITY_FIELD_NOT_REJECTED");

    var signedBody = JsonSerializer.Serialize(new { employeeCode = "MNV001" });
    var tamperEnvelope = Sign(deviceKey, deviceId, epoch, LanSecureHttpRoutes.AttendanceScanRouteTarget, signedBody);
    var tampered = JsonSerializer.Serialize(new { employeeCode = "MNV404" });
    var tamper = await SendAsync(client, RequestFor(LanSecureHttpRoutes.AttendanceScanRouteTarget, tamperEnvelope, deviceId, epoch, allowedToken, tampered));
    Assert(tamper.Status == HttpStatusCode.Unauthorized && ErrorCode(tamper.Body) == "REQUEST_SIGNATURE_INVALID", "SCAN_BODY_TAMPER_NOT_REJECTED");

    var deniedToken = await LoginAsync(client, deviceKey, epoch, deniedUsername);
    var deniedBody = JsonSerializer.Serialize(new { employeeCode = "MNV001" });
    var deniedEnvelope = Sign(deviceKey, deviceId, epoch, LanSecureHttpRoutes.AttendanceScanRouteTarget, deniedBody);
    var denied = await SendAsync(client, RequestFor(LanSecureHttpRoutes.AttendanceScanRouteTarget, deniedEnvelope, deviceId, epoch, deniedToken));
    Assert(denied.Status == HttpStatusCode.Forbidden && ErrorCode(denied.Body) == "PERMISSION_DENIED", "SCAN_PERMISSION_NOT_ENFORCED");
}
finally
{
    diagnostics.Append(await service.StopAsync());
}

var eventsAfter = await CountAsync(dbPath, "edge_events");
var cloudOutboxAfter = await CountAsync(dbPath, "cloud_sync_outbox");
var googleOutboxAfter = await CountAsync(dbPath, "google_projection_outbox");
Assert(eventsAfter == eventsBefore, "SCAN_CREATED_EDGE_EVENT");
Assert(cloudOutboxAfter == cloudOutboxBefore, "SCAN_CREATED_CLOUD_OUTBOX");
Assert(googleOutboxAfter == googleOutboxBefore, "SCAN_CREATED_GOOGLE_OUTBOX");

Console.WriteLine(
    "LAN_ATTENDANCE_SCAN_CONTEXT_PASS secureRoute=PASS signedProof=PASS sessionDeviceEpoch=PASS permission=PASS strictMnv=PASS presenceVersion=PASS replay=PASS readOnly=PASS");
return 0;

sealed record SignedEnvelope(string RawBody, long TimestampUnixMs, string Nonce, string SignatureBase64);

sealed class RunningService
{
    private readonly Task<string> _stdout;
    private readonly Task<string> _stderr;

    public RunningService(Process process, Task<string> stdout, Task<string> stderr)
    {
        Process = process;
        _stdout = stdout;
        _stderr = stderr;
    }

    public Process Process { get; }

    public async Task<string> StopAsync()
    {
        if (!Process.HasExited)
        {
            try { Process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        }
        await Process.WaitForExitAsync();
        return await ReadDiagnosticsAsync();
    }

    public async Task<string> ReadDiagnosticsAsync() =>
        $"STDOUT:\n{await _stdout}\nSTDERR:\n{await _stderr}\n";
}
