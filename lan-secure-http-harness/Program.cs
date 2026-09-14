using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Vhdchy.LanService;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: VHDCHY.LanSecureHttp.Harness <lan-service-dll> <data-root> <port>");
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
const string userId = "U_SECURE_HTTP";
const string username = "secure.operator";
const string password = "Secure-HTTP-Test-Password-1291!";
const string deviceId = "PDA-SECURE-HTTP-001";
const string authorityScope = "{\"clusterId\":\"PICK_PACK_1291\",\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}";

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static string Base64Url(byte[] value) =>
    Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

static (string Algorithm, string SecretHash) PasswordRecord(string value)
{
    var salt = Enumerable.Range(0, 16).Select(index => unchecked((byte)(71 + index))).ToArray();
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
    var nonce = $"vhdchy-secure-http-{Guid.NewGuid():N}";
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
    request.Headers.TryAddWithoutValidation("X-Request-Id", $"REQ-{Guid.NewGuid():N}");
    if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new("Bearer", token);
    return request;
}

static async Task<(HttpStatusCode Status, string Body)> SendAsync(HttpClient client, HttpRequestMessage request)
{
    using (request)
    using (var response = await client.SendAsync(request))
    {
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }
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

static string BusinessBody(string requestId, string idempotencyKey, string employeeId, long sequence) =>
    JsonSerializer.Serialize(new
    {
        requestId,
        idempotencyKey,
        commandCode = "EMPLOYEE_CREATE",
        entityId = employeeId,
        expectedEntityVersion = (long?)null,
        payload = new { employeeId, fullName = $"Secure HTTP {employeeId}", status = "ACTIVE" },
        deviceSeq = sequence
    });

static async Task WaitHealthyAsync(HttpClient client, RunningService service)
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
                var body = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(body);
                Assert(document.RootElement.GetProperty("transport").GetString() == "HTTPS", "HEALTH_NOT_HTTPS");
                Assert(document.RootElement.GetProperty("secureMutationTransportEnabled").GetBoolean(), "SECURE_TRANSPORT_NOT_ENABLED");
                Assert(document.RootElement.GetProperty("businessMutationEnabled").GetBoolean(), "BUSINESS_MUTATION_NOT_READY");
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

static RunningService StartService(string dll, string root, int listenPort, string pfx, string pfxPassword)
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
    start.Environment["VHDCHY_LAN_TLS_PFX_PATH"] = pfx;
    start.Environment["VHDCHY_LAN_TLS_PFX_PASSWORD"] = pfxPassword;
    var process = Process.Start(start) ?? throw new InvalidOperationException("LAN_SERVICE_START_FAILED");
    return new RunningService(process, process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync());
}

var dbPath = Path.Combine(dataRoot, "edge.db");
var edge = new EdgeStore(dbPath);
await edge.InitializeAsync(environment, clusterId, "SECURE-HTTP-HARNESS", Guid.NewGuid().ToString("N"), compatibility);
await EmployeeCodeUniqueClaimStore.EnsureAsync(dbPath);

var passwordRecord = PasswordRecord(password);
var authorityPayload = JsonSerializer.Serialize(new
{
    schemaVersion = LanPrimaryCredentialVerifier.LoginAuthoritySchemaVersion,
    permissionCatalogVersion = LanAuthorizationEvaluator.PermissionCatalogVersion,
    users = new object[]
    {
        new { userId, username, employeeId = "E_SECURE_HTTP", displayName = "Secure HTTP Operator", email = "secure-http@example.invalid", status = "ACTIVE", securityLevel = "NORMAL" }
    },
    credentials = new object[]
    {
        new { credentialId = "C_SECURE_HTTP", userId, credentialType = "PASSWORD", secretHash = passwordRecord.SecretHash, hashAlgorithm = passwordRecord.Algorithm, mustChange = false, status = "ACTIVE" }
    },
    roles = Array.Empty<object>(),
    permissions = new object[]
    {
        new { permissionId = "P_CREATE", resource = "employee", action = "create", status = "ACTIVE" }
    },
    rolePermissionGrants = Array.Empty<object>(),
    userRoleGrants = Array.Empty<object>(),
    userPermissionGrants = new object[]
    {
        new { grantId = "G_SECURE_CREATE", userId, permissionId = "P_CREATE", clusterId, moduleId, effect = "ALLOW", status = "ACTIVE" }
    }
});

var authority = new AuthoritySnapshotStore(dbPath);
await authority.ImportAsync(
    new("AUTH-SECURE-HTTP-V2", environment, clusterId, "secure-http-authority", compatibility, authorityScope, authorityPayload),
    environment,
    clusterId,
    compatibility);
await new OperationalSnapshotStore(dbPath).ImportAsync(
    new(
        "OP-SECURE-HTTP-V1",
        environment,
        clusterId,
        "secure-http-operational",
        compatibility,
        "{\"generation\":1,\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
        "{\"employees\":[],\"employeeCodes\":[],\"presence\":[],\"generation\":1}"),
    environment,
    clusterId,
    compatibility,
    new[] { moduleId });

var security = new LanClientSecurityStore(dbPath);
await security.EnsureAsync();
using var deviceKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
await security.RegisterPairedDeviceAsync(
    deviceId,
    Convert.ToBase64String(deviceKey.ExportSubjectPublicKeyInfo()),
    "ROOT-SECURE-HTTP-HARNESS");
var epoch = (await security.InspectAsync()).SecurityEpoch ?? throw new InvalidOperationException("SECURITY_EPOCH_MISSING");

var preflight = await new LanReadinessEvaluator(
    dbPath,
    environment,
    clusterId,
    compatibility,
    new[] { moduleId },
    securePublicRouteWiringEnabled: true).EvaluateAsync();
Assert(preflight.Ready, $"SECURE_HTTP_PREFLIGHT_NOT_READY:{string.Join(',', preflight.Blockers.Select(item => item.Code))}");

const string pfxPassword = "ci-only-pfx-password";
var pfxPath = Path.Combine(dataRoot, "secure-http-test.pfx");
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

var allDiagnostics = new StringBuilder();
var service = StartService(serviceDll, dataRoot, port, pfxPath, pfxPassword);
try
{
    await WaitHealthyAsync(client, service);

    using (var plain = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
    {
        try
        {
            using var probe = await plain.PostAsync(
                $"http://127.0.0.1:{port}{LanSecureHttpRoutes.LoginRouteTarget}",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert(!probe.IsSuccessStatusCode, "PLAINTEXT_HTTP_ROUTE_ACCEPTED");
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            // Expected for an HTTPS-only Kestrel listener.
        }
    }

    var wrongBody = JsonSerializer.Serialize(new { username, password = "wrong-password" });
    var wrongEnvelope = Sign(deviceKey, deviceId, epoch, LanSecureHttpRoutes.LoginRouteTarget, wrongBody);
    var wrong = await SendAsync(client, RequestFor(LanSecureHttpRoutes.LoginRouteTarget, wrongEnvelope, deviceId, epoch));
    Assert(wrong.Status == HttpStatusCode.Unauthorized && ErrorCode(wrong.Body) == "INVALID_CREDENTIALS", "WRONG_PASSWORD_NOT_REJECTED");

    var loginBody = JsonSerializer.Serialize(new { username, password });
    var loginEnvelope = Sign(deviceKey, deviceId, epoch, LanSecureHttpRoutes.LoginRouteTarget, loginBody);
    var tampered = JsonSerializer.Serialize(new { username, password = "tampered-after-sign" });
    var tamperResult = await SendAsync(client, RequestFor(LanSecureHttpRoutes.LoginRouteTarget, loginEnvelope, deviceId, epoch, bodyOverride: tampered));
    Assert(tamperResult.Status == HttpStatusCode.Unauthorized && ErrorCode(tamperResult.Body) == "REQUEST_SIGNATURE_INVALID", "SIGNED_LOGIN_BODY_TAMPER_NOT_REJECTED");

    loginEnvelope = Sign(deviceKey, deviceId, epoch, LanSecureHttpRoutes.LoginRouteTarget, loginBody);
    var login = await SendAsync(client, RequestFor(LanSecureHttpRoutes.LoginRouteTarget, loginEnvelope, deviceId, epoch));
    Assert(login.Status == HttpStatusCode.OK, $"LOGIN_FAILED:{login.Status}:{login.Body}");
    Assert(!login.Body.Contains(password, StringComparison.Ordinal), "PASSWORD_ECHOED_IN_LOGIN_RESPONSE");
    var token = SessionToken(login.Body);
    Assert(token.Length >= 32, "SESSION_TOKEN_MISSING");

    var loginReplay = await SendAsync(client, RequestFor(LanSecureHttpRoutes.LoginRouteTarget, loginEnvelope, deviceId, epoch));
    Assert(loginReplay.Status == HttpStatusCode.Unauthorized && ErrorCode(loginReplay.Body) == "REQUEST_REPLAY", "LOGIN_REPLAY_NOT_REJECTED");

    var command1 = BusinessBody("REQ-SECURE-1", "IDEM-SECURE-1", "EMP-SECURE-1", 1);
    var commandEnvelope1 = Sign(deviceKey, deviceId, epoch, LanSecureHttpRoutes.BusinessRouteTarget, command1);
    var business1 = await SendAsync(client, RequestFor(LanSecureHttpRoutes.BusinessRouteTarget, commandEnvelope1, deviceId, epoch, token));
    Assert(business1.Status == HttpStatusCode.OK, $"BUSINESS_COMMAND_FAILED:{business1.Status}:{business1.Body}");

    var businessReplay = await SendAsync(client, RequestFor(LanSecureHttpRoutes.BusinessRouteTarget, commandEnvelope1, deviceId, epoch, token));
    Assert(businessReplay.Status == HttpStatusCode.Unauthorized && ErrorCode(businessReplay.Body) == "REQUEST_REPLAY", "BUSINESS_REPLAY_NOT_REJECTED");

    allDiagnostics.Append(await service.StopAsync());

    service = StartService(serviceDll, dataRoot, port, pfxPath, pfxPassword);
    await WaitHealthyAsync(client, service);
    var command2 = BusinessBody("REQ-SECURE-2", "IDEM-SECURE-2", "EMP-SECURE-2", 2);
    var commandEnvelope2 = Sign(deviceKey, deviceId, epoch, LanSecureHttpRoutes.BusinessRouteTarget, command2);
    var afterRestart = await SendAsync(client, RequestFor(LanSecureHttpRoutes.BusinessRouteTarget, commandEnvelope2, deviceId, epoch, token));
    Assert(afterRestart.Status == HttpStatusCode.OK, $"SESSION_RESTART_CONTINUITY_FAILED:{afterRestart.Status}:{afterRestart.Body}");
}
finally
{
    allDiagnostics.Append(await service.StopAsync());
}

Assert(!allDiagnostics.ToString().Contains(password, StringComparison.Ordinal), "PASSWORD_LEAKED_TO_SERVICE_DIAGNOSTICS");
Console.WriteLine("LAN_SECURE_HTTP_HARNESS_PASS tlsOnly=PASS pairedLogin=PASS wrongPassword=PASS signedBody=PASS loginReplay=PASS sessionIssued=PASS businessExecute=PASS businessReplay=PASS restartSession=PASS passwordDiagnostics=PASS");
return 0;

sealed record SignedEnvelope(string RawBody, long TimestampUnixMs, string Nonce, string SignatureBase64);

sealed class RunningService
{
    private readonly Task<string> _stdout;
    private readonly Task<string> _stderr;
    private bool _stopped;

    public RunningService(Process process, Task<string> stdout, Task<string> stderr)
    {
        Process = process;
        _stdout = stdout;
        _stderr = stderr;
    }

    public Process Process { get; }

    public async Task<string> ReadDiagnosticsAsync()
    {
        if (!Process.HasExited) return string.Empty;
        return (await _stdout) + Environment.NewLine + (await _stderr);
    }

    public async Task<string> StopAsync()
    {
        if (_stopped) return string.Empty;
        _stopped = true;
        if (!Process.HasExited)
        {
            Process.Kill(entireProcessTree: true);
            await Process.WaitForExitAsync();
        }
        var output = await _stdout;
        var error = await _stderr;
        Process.Dispose();
        return output + Environment.NewLine + error;
    }
}
