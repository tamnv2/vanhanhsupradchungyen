using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Certes;
using Certes.Acme;
using Certes.Pkcs;
using Vhdchy.LanService;

namespace Vhdchy.LanCertificateManager;

internal static class Program
{
    private const string ZoneName = "supra.cc.cd";
    private static readonly TimeSpan RenewalWindow = TimeSpan.FromDays(30);

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Contains("--self-test", StringComparer.Ordinal))
            {
                return await RunSelfTestAsync();
            }

            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("LAN certificate manager requires Windows DPAPI CurrentUser");
            }

            var environment = RequireEnvironment("VHDCHY_ENV").ToUpperInvariant();
            if (environment is not ("BETA" or "STABLE"))
            {
                throw new InvalidOperationException("VHDCHY_ENV must be BETA or STABLE");
            }

            var canonicalHost = environment == "BETA" ? "lan-beta.supra.cc.cd" : "lan.supra.cc.cd";
            var expectedAccountId = RequireEnvironment("VHDCHY_CLOUDFLARE_ACCOUNT_ID");
            var zoneId = RequireEnvironment("VHDCHY_CLOUDFLARE_ZONE_ID");
            var dnsToken = RequireEnvironment("VHDCHY_CLOUDFLARE_DNS_TOKEN");
            var email = RequireEnvironment("VHDCHY_ACME_EMAIL");
            var dataRoot = Path.GetFullPath(RequireEnvironment("VHDCHY_LAN_CERT_DATA_ROOT"));
            Directory.CreateDirectory(dataRoot);

            var protectedPfxPath = Path.Combine(dataRoot, "lan-tls.pfx.dpapi");
            if (!args.Contains("--force", StringComparer.Ordinal) && File.Exists(protectedPfxPath))
            {
                var metadata = LanTlsCertificateLoader.InspectProtectedPfx(protectedPfxPath, environment, canonicalHost);
                if (metadata.NotAfterUtc - DateTimeOffset.UtcNow > RenewalWindow)
                {
                    Console.WriteLine($"CERTIFICATE_RENEWAL_NOT_DUE notAfterUtc={metadata.NotAfterUtc:O}");
                    return 0;
                }
            }

            var production = args.Contains("--production", StringComparer.Ordinal);
            if (production && !string.Equals(
                    Environment.GetEnvironmentVariable("VHDCHY_ACME_ALLOW_PRODUCTION"),
                    "YES",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Production ACME requires VHDCHY_ACME_ALLOW_PRODUCTION=YES");
            }

            var directory = production
                ? WellKnownServers.LetsEncryptV2
                : WellKnownServers.LetsEncryptStagingV2;

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var dns = new CloudflareDnsProvider(http, expectedAccountId, zoneId, ZoneName, dnsToken);
            await dns.VerifyScopeAsync();

            var accountKeyPath = Path.Combine(dataRoot, production ? "acme-account-prod.dpapi" : "acme-account-staging.dpapi");
            var acme = await CreateAcmeContextAsync(directory, email, accountKeyPath, environment, canonicalHost);
            var order = await acme.NewOrder(new[] { canonicalHost });
            var authorization = (await order.Authorizations()).Single();
            var challenge = await authorization.Dns();
            var txtValue = acme.AccountKey.DnsTxt(challenge.Token);
            var challengeName = $"_acme-challenge.{canonicalHost}";

            string? recordId = null;
            byte[]? rawPfx = null;
            byte[]? protectedPfx = null;
            try
            {
                recordId = await dns.CreateTxtAsync(challengeName, txtValue);
                await dns.VerifyTxtAsync(recordId, challengeName, txtValue);
                Console.WriteLine($"DNS_CHALLENGE_PRESENT name={challengeName}");

                await Task.Delay(TimeSpan.FromSeconds(15));
                await challenge.Validate();
                await WaitForAuthorizationAsync(authorization, TimeSpan.FromMinutes(3));

                var certificateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var certificate = await order.Generate(
                    new CsrInfo
                    {
                        CommonName = canonicalHost,
                        Organization = "VHDCHY"
                    },
                    certificateKey,
                    retryCount: 8);

                rawPfx = certificate.ToPfx(certificateKey).Build(canonicalHost, string.Empty);
                ValidateIssuedPfx(rawPfx, canonicalHost);
                protectedPfx = LanTlsCertificateLoader.ProtectPfxForCurrentWindowsUser(rawPfx, environment, canonicalHost);
                await AtomicWriteAsync(protectedPfxPath, protectedPfx);

                var installed = LanTlsCertificateLoader.InspectProtectedPfx(protectedPfxPath, environment, canonicalHost);
                Console.WriteLine($"CERTIFICATE_UPDATED host={canonicalHost} notAfterUtc={installed.NotAfterUtc:O}");
                Console.WriteLine("LAN_SERVICE_RESTART_REQUIRED=YES");
            }
            finally
            {
                if (rawPfx is not null) CryptographicOperations.ZeroMemory(rawPfx);
                if (protectedPfx is not null) CryptographicOperations.ZeroMemory(protectedPfx);
                if (recordId is not null)
                {
                    await dns.DeleteTxtAsync(recordId, challengeName);
                    Console.WriteLine($"DNS_CHALLENGE_CLEANED name={challengeName}");
                }
            }

            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"LAN_CERTIFICATE_MANAGER_FAIL: {Sanitize(error.Message)}");
            return 1;
        }
    }

    private static async Task<AcmeContext> CreateAcmeContextAsync(
        Uri directory,
        string email,
        string accountKeyPath,
        string environment,
        string canonicalHost)
    {
        if (File.Exists(accountKeyPath))
        {
            var pem = DpapiTextStore.Read(accountKeyPath, environment, canonicalHost, "ACME_ACCOUNT");
            try
            {
                var key = KeyFactory.FromPem(pem);
                var context = new AcmeContext(directory, key);
                await context.Account();
                return context;
            }
            finally
            {
                pem = string.Empty;
            }
        }

        var created = new AcmeContext(directory);
        await created.NewAccount(email, true);
        var accountPem = created.AccountKey.ToPem();
        DpapiTextStore.Write(accountKeyPath, accountPem, environment, canonicalHost, "ACME_ACCOUNT");
        return created;
    }

    private static async Task WaitForAuthorizationAsync(IAuthorizationContext authorization, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var state = await authorization.Resource();
            var status = state.Status.ToString();
            if (string.Equals(status, "Valid", StringComparison.OrdinalIgnoreCase)) return;
            if (string.Equals(status, "Invalid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Deactivated", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Expired", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Revoked", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"ACME authorization ended in {status}");
            }
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
        throw new TimeoutException("ACME authorization validation timed out");
    }

    private static void ValidateIssuedPfx(byte[] pfx, string canonicalHost)
    {
#pragma warning disable SYSLIB0057
        using var certificate = new X509Certificate2(pfx, string.Empty, X509KeyStorageFlags.EphemeralKeySet);
#pragma warning restore SYSLIB0057
        LanTlsCertificateLoader.ValidateCertificate(certificate, canonicalHost, DateTimeOffset.UtcNow);
        if (certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow.AddDays(30))
        {
            throw new InvalidOperationException("Issued certificate validity is unexpectedly short");
        }
    }

    private static async Task AtomicWriteAsync(string targetPath, byte[] bytes)
    {
        var directory = Path.GetDirectoryName(targetPath) ?? throw new InvalidOperationException("Invalid certificate output path");
        Directory.CreateDirectory(directory);
        var temp = Path.Combine(directory, $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(temp, bytes);
            File.Move(temp, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static string RequireEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name)?.Trim();
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"Missing required environment value: {name}");
        return value;
    }

    private static string Sanitize(string message) =>
        message.Replace(Environment.GetEnvironmentVariable("VHDCHY_CLOUDFLARE_DNS_TOKEN") ?? "__NO_TOKEN__", "[REDACTED]", StringComparison.Ordinal);

    private static async Task<int> RunSelfTestAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("LAN_CERTIFICATE_MANAGER_SELF_TEST_SKIPPED_NON_WINDOWS");
            return 0;
        }

        const string environment = "BETA";
        const string host = "lan-beta.supra.cc.cd";
        var root = Path.Combine(Path.GetTempPath(), "vhdchy-cert-manager-self-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={host}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName(host);
            request.CertificateExtensions.Add(san.Build());
            var eku = new OidCollection { new("1.3.6.1.5.5.7.3.1") };
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, false));
            using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(60));
            var raw = cert.Export(X509ContentType.Pfx, string.Empty);
            var protectedBytes = LanTlsCertificateLoader.ProtectPfxForCurrentWindowsUser(raw, environment, host);
            CryptographicOperations.ZeroMemory(raw);
            var path = Path.Combine(root, "lan-tls.pfx.dpapi");
            await AtomicWriteAsync(path, protectedBytes);
            CryptographicOperations.ZeroMemory(protectedBytes);
            var metadata = LanTlsCertificateLoader.InspectProtectedPfx(path, environment, host);
            if (metadata.NotAfterUtc <= DateTimeOffset.UtcNow.AddDays(30)) throw new InvalidOperationException("SELF_TEST_EXPIRY");
            if (Directory.EnumerateFiles(root, "*.pfx").Any()) throw new InvalidOperationException("SELF_TEST_RAW_PFX_LEAK");

            DpapiTextStore.Write(Path.Combine(root, "account.dpapi"), "PRIVATE-ACCOUNT-KEY-TEST", environment, host, "ACME_ACCOUNT");
            var restored = DpapiTextStore.Read(Path.Combine(root, "account.dpapi"), environment, host, "ACME_ACCOUNT");
            if (restored != "PRIVATE-ACCOUNT-KEY-TEST") throw new InvalidOperationException("SELF_TEST_ACCOUNT_SECRET");

            Console.WriteLine("LAN_CERTIFICATE_MANAGER_SELF_TEST_PASS");
            Console.WriteLine("dpapiPfxAtomicWrite=PASS");
            Console.WriteLine("dpapiAccountSecret=PASS");
            Console.WriteLine("renewalMetadata=PASS");
            Console.WriteLine("rawPfxDiskLeak=PASS");
            return 0;
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }
}

internal static class DpapiTextStore
{
    public static void Write(string path, string value, string environment, string host, string purpose)
    {
        var plain = Encoding.UTF8.GetBytes(value);
        byte[]? protectedBytes = null;
        try
        {
            protectedBytes = ProtectedData.Protect(plain, Entropy(environment, host, purpose), DataProtectionScope.CurrentUser);
            var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Invalid DPAPI secret path");
            Directory.CreateDirectory(directory);
            var temp = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllBytes(temp, protectedBytes);
                File.Move(temp, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
            if (protectedBytes is not null) CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    public static string Read(string path, string environment, string host, string purpose)
    {
        var encrypted = File.ReadAllBytes(path);
        byte[]? plain = null;
        try
        {
            plain = ProtectedData.Unprotect(encrypted, Entropy(environment, host, purpose), DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encrypted);
            if (plain is not null) CryptographicOperations.ZeroMemory(plain);
        }
    }

    private static byte[] Entropy(string environment, string host, string purpose) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"VHDCHY|{purpose}|{environment}|{host}|V1"));
}

internal sealed class CloudflareDnsProvider
{
    private readonly HttpClient _http;
    private readonly string _accountId;
    private readonly string _zoneId;
    private readonly string _zoneName;

    public CloudflareDnsProvider(HttpClient http, string accountId, string zoneId, string zoneName, string token)
    {
        _http = http;
        _accountId = accountId;
        _zoneId = zoneId;
        _zoneName = zoneName;
        _http.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task VerifyScopeAsync()
    {
        using var response = await _http.GetAsync($"zones/{Uri.EscapeDataString(_zoneId)}");
        var json = await ParseSuccessAsync(response);
        var result = json.RootElement.GetProperty("result");
        if (!string.Equals(result.GetProperty("name").GetString(), _zoneName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cloudflare zone name mismatch");
        var accountId = result.GetProperty("account").GetProperty("id").GetString();
        if (!string.Equals(accountId, _accountId, StringComparison.Ordinal))
            throw new InvalidOperationException("Cloudflare account/zone mismatch");
    }

    public async Task<string> CreateTxtAsync(string name, string value)
    {
        EnsureChallengeName(name);
        using var body = new StringContent(JsonSerializer.Serialize(new { type = "TXT", name, content = value, ttl = 60 }), Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync($"zones/{Uri.EscapeDataString(_zoneId)}/dns_records", body);
        var json = await ParseSuccessAsync(response);
        var result = json.RootElement.GetProperty("result");
        var id = result.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Cloudflare did not return DNS record id");
        return id;
    }

    public async Task VerifyTxtAsync(string recordId, string expectedName, string expectedValue)
    {
        EnsureChallengeName(expectedName);
        using var response = await _http.GetAsync($"zones/{Uri.EscapeDataString(_zoneId)}/dns_records/{Uri.EscapeDataString(recordId)}");
        var json = await ParseSuccessAsync(response);
        var result = json.RootElement.GetProperty("result");
        if (!string.Equals(result.GetProperty("type").GetString(), "TXT", StringComparison.Ordinal) ||
            !string.Equals(result.GetProperty("name").GetString(), expectedName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(result.GetProperty("content").GetString(), expectedValue, StringComparison.Ordinal))
            throw new InvalidOperationException("Cloudflare DNS challenge readback mismatch");
    }

    public async Task DeleteTxtAsync(string recordId, string expectedName)
    {
        EnsureChallengeName(expectedName);
        using var response = await _http.DeleteAsync($"zones/{Uri.EscapeDataString(_zoneId)}/dns_records/{Uri.EscapeDataString(recordId)}");
        await ParseSuccessAsync(response);
    }

    private void EnsureChallengeName(string name)
    {
        if (!name.StartsWith("_acme-challenge.", StringComparison.OrdinalIgnoreCase) ||
            !name.EndsWith("." + _zoneName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refusing DNS mutation outside the expected ACME challenge namespace");
    }

    private static async Task<JsonDocument> ParseSuccessAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        if (!response.IsSuccessStatusCode || !json.RootElement.TryGetProperty("success", out var success) || !success.GetBoolean())
        {
            var status = (int)response.StatusCode;
            json.Dispose();
            throw new InvalidOperationException($"Cloudflare API operation failed with HTTP {status}");
        }
        return json;
    }
}
