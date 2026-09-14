using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Vhdchy.LanService;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("LAN DPAPI TLS harness requires Windows");
    return 2;
}

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: VHDCHY.LanDpapiTls.Harness <lan-service-dll> <data-root> <port>");
    return 2;
}

var serviceDll = Path.GetFullPath(args[0]);
var dataRoot = Path.GetFullPath(args[1]);
if (!File.Exists(serviceDll) || !int.TryParse(args[2], out var port) || port is <= 1024 or >= 65530)
{
    return 2;
}

const string environment = "BETA";
const string clusterId = "PICK_PACK_1291";
const string canonicalHost = "lan-beta.supra.cc.cd";

Directory.CreateDirectory(dataRoot);

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static byte[] CreatePfx(string dnsName, out string thumbprint)
{
    using var rsa = RSA.Create(2048);
    var request = new CertificateRequest(
        $"CN={dnsName}",
        rsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);

    var san = new SubjectAlternativeNameBuilder();
    san.AddDnsName(dnsName);
    san.AddIpAddress(IPAddress.Loopback);
    request.CertificateExtensions.Add(san.Build());
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
    request.CertificateExtensions.Add(new X509KeyUsageExtension(
        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
        critical: false));

    var usages = new OidCollection
    {
        new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication")
    };
    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(usages, critical: false));

    using var certificate = request.CreateSelfSigned(
        DateTimeOffset.UtcNow.AddMinutes(-5),
        DateTimeOffset.UtcNow.AddDays(2));
    thumbprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);
    return certificate.Export(X509ContentType.Pfx);
}

static async Task<string> ProtectToFileAsync(
    string directory,
    string fileName,
    byte[] pfx,
    string dnsName)
{
    var protectedBytes = LanTlsCertificateLoader.ProtectPfxForCurrentWindowsUser(
        pfx,
        environment,
        dnsName);
    try
    {
        var path = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(path, protectedBytes);
        return path;
    }
    finally
    {
        CryptographicOperations.ZeroMemory(protectedBytes);
        CryptographicOperations.ZeroMemory(pfx);
    }
}

static RunningService StartService(string dll, string root, int listenPort, string protectedPfxPath)
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
    start.Environment.Remove(LanTlsCertificateLoader.PlainPfxPathVariable);
    start.Environment.Remove(LanTlsCertificateLoader.PlainPfxPasswordVariable);
    start.Environment[LanTlsCertificateLoader.ProtectedPfxPathVariable] = protectedPfxPath;

    var process = Process.Start(start) ?? throw new InvalidOperationException("LAN_SERVICE_START_FAILED");
    return new RunningService(
        process,
        process.StandardOutput.ReadToEndAsync(),
        process.StandardError.ReadToEndAsync());
}

static async Task<JsonDocument> WaitForHealthAsync(HttpClient client, RunningService service)
{
    Exception? last = null;
    for (var attempt = 0; attempt < 80; attempt++)
    {
        if (service.Process.HasExited)
        {
            throw new InvalidOperationException(
                $"LAN_SERVICE_EXITED:{service.Process.ExitCode}\n{await service.ReadDiagnosticsAsync()}");
        }

        try
        {
            using var response = await client.GetAsync("/health");
            if (response.IsSuccessStatusCode)
            {
                return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
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

static async Task<string> ExpectStartupFailureAsync(
    string serviceDll,
    string root,
    int listenPort,
    string protectedPfxPath,
    string expectedDiagnostic)
{
    var service = StartService(serviceDll, root, listenPort, protectedPfxPath);
    try
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await service.Process.WaitForExitAsync(timeout.Token);
        var diagnostics = await service.ReadDiagnosticsAsync();
        Assert(service.Process.ExitCode != 0, "INVALID_CERT_SERVICE_EXITED_SUCCESS");
        Assert(diagnostics.Contains(expectedDiagnostic, StringComparison.Ordinal), "EXPECTED_TLS_FAILURE_DIAGNOSTIC_MISSING");
        return diagnostics;
    }
    finally
    {
        await service.StopAsync();
    }
}

var validPfx = CreatePfx(canonicalHost, out var expectedThumbprint);
var protectedPath = await ProtectToFileAsync(dataRoot, "lan-tls.pfx.dpapi", validPfx, canonicalHost);
Assert(File.Exists(protectedPath), "PROTECTED_PFX_NOT_WRITTEN");
Assert(!Directory.EnumerateFiles(dataRoot, "*.pfx", SearchOption.TopDirectoryOnly).Any(), "RAW_PFX_WRITTEN_TO_DISK");

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

var positiveRoot = Path.Combine(dataRoot, "positive");
Directory.CreateDirectory(positiveRoot);
var service = StartService(serviceDll, positiveRoot, port, protectedPath);
string positiveDiagnostics;
try
{
    using var health = await WaitForHealthAsync(client, service);
    var root = health.RootElement;
    Assert(root.GetProperty("transport").GetString() == "HTTPS", "DPAPI_TLS_NOT_HTTPS");
    Assert(root.GetProperty("secureMutationTransportEnabled").GetBoolean(), "DPAPI_TLS_NOT_ENABLED");
    Assert(root.GetProperty("canonicalLanHost").GetString() == canonicalHost, "CANONICAL_HOST_MISMATCH");
    Assert(root.GetProperty("tlsCertificateStorageMode").GetString() == "WINDOWS_DPAPI_CURRENT_USER", "DPAPI_STORAGE_MODE_MISMATCH");
    Assert(root.TryGetProperty("tlsCertificateNotAfterUtc", out var expiry) && expiry.ValueKind == JsonValueKind.String, "TLS_EXPIRY_NOT_EXPOSED");
}
finally
{
    positiveDiagnostics = await service.StopAsync();
}
Assert(positiveDiagnostics.Contains("WINDOWS_DPAPI_CURRENT_USER", StringComparison.Ordinal), "DPAPI_STARTUP_DIAGNOSTIC_MISSING");
Assert(!positiveDiagnostics.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase), "PRIVATE_KEY_DIAGNOSTIC_LEAK");

var wrongPfx = CreatePfx("wrong-host.example.invalid", out _);
var wrongProtectedPath = await ProtectToFileAsync(dataRoot, "wrong-host.pfx.dpapi", wrongPfx, canonicalHost);
await ExpectStartupFailureAsync(
    serviceDll,
    Path.Combine(dataRoot, "wrong-host-root"),
    port + 1,
    wrongProtectedPath,
    "LAN TLS certificate SAN does not match the canonical LAN hostname");

var corruptPath = Path.Combine(dataRoot, "corrupt.pfx.dpapi");
await File.WriteAllBytesAsync(corruptPath, RandomNumberGenerator.GetBytes(256));
await ExpectStartupFailureAsync(
    serviceDll,
    Path.Combine(dataRoot, "corrupt-root"),
    port + 2,
    corruptPath,
    "LAN TLS protected PFX could not be decrypted or loaded for the current Windows user");

Console.WriteLine("LAN_DPAPI_TLS_HARNESS_PASS");
Console.WriteLine("dpapiCurrentUser=PASS");
Console.WriteLine("userSpacePfx=PASS");
Console.WriteLine("canonicalSan=PASS");
Console.WriteLine("wrongHostRejected=PASS");
Console.WriteLine("corruptBlobRejected=PASS");
Console.WriteLine("rawPfxDiskLeak=PASS");
return 0;

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

    public async Task<string> ReadDiagnosticsAsync()
    {
        if (!Process.HasExited) return string.Empty;
        return (await _stdout) + Environment.NewLine + (await _stderr);
    }

    public async Task<string> StopAsync()
    {
        if (!Process.HasExited)
        {
            Process.Kill(entireProcessTree: true);
            await Process.WaitForExitAsync();
        }
        var diagnostics = (await _stdout) + Environment.NewLine + (await _stderr);
        Process.Dispose();
        return diagnostics;
    }
}
