using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Vhdchy.LanService;

public sealed record LanTlsCertificateLoadResult(
    X509Certificate2 Certificate,
    string StorageMode,
    DateTimeOffset NotAfterUtc);

public sealed record LanTlsCertificateMetadata(
    string ThumbprintSha256,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset NotAfterUtc,
    string CanonicalHost);

public static class LanTlsCertificateLoader
{
    public const string PlainPfxPathVariable = "VHDCHY_LAN_TLS_PFX_PATH";
    public const string PlainPfxPasswordVariable = "VHDCHY_LAN_TLS_PFX_PASSWORD";
    public const string ProtectedPfxPathVariable = "VHDCHY_LAN_TLS_PROTECTED_PFX_PATH";

    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

    public static LanTlsCertificateLoadResult? LoadFromEnvironment(string environment, string canonicalHost)
    {
        var plainPath = Environment.GetEnvironmentVariable(PlainPfxPathVariable)?.Trim();
        var protectedPath = Environment.GetEnvironmentVariable(ProtectedPfxPathVariable)?.Trim();

        if (!string.IsNullOrWhiteSpace(plainPath) && !string.IsNullOrWhiteSpace(protectedPath))
        {
            throw new InvalidOperationException("Configure only one LAN TLS PFX source");
        }

        if (string.IsNullOrWhiteSpace(plainPath) && string.IsNullOrWhiteSpace(protectedPath))
        {
            return null;
        }

        X509Certificate2 certificate;
        string storageMode;

        if (!string.IsNullOrWhiteSpace(protectedPath))
        {
            certificate = LoadProtectedCertificate(protectedPath, environment, canonicalHost);
            storageMode = "WINDOWS_DPAPI_CURRENT_USER";
        }
        else
        {
            var fullPath = Path.GetFullPath(plainPath!);
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException("VHDCHY_LAN_TLS_PFX_PATH does not exist");
            }

            var password = Environment.GetEnvironmentVariable(PlainPfxPasswordVariable) ?? string.Empty;
#pragma warning disable SYSLIB0057
            certificate = new X509Certificate2(
                fullPath,
                password,
                RuntimeTlsKeyStorageFlags());
#pragma warning restore SYSLIB0057
        
            storageMode = "PLAIN_PFX_COMPATIBILITY";
        }

        try
        {
            ValidateCertificate(certificate, canonicalHost, DateTimeOffset.UtcNow);
            return new LanTlsCertificateLoadResult(
                certificate,
                storageMode,
                new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero));
        }
        catch
        {
            certificate.Dispose();
            throw;
        }
    }

    public static LanTlsCertificateMetadata InspectProtectedPfx(
        string protectedPfxPath,
        string environment,
        string canonicalHost)
    {
        using var certificate = LoadProtectedCertificate(protectedPfxPath, environment, canonicalHost);
        ValidateCertificate(certificate, canonicalHost, DateTimeOffset.UtcNow);
        return new LanTlsCertificateMetadata(
            certificate.GetCertHashString(HashAlgorithmName.SHA256),
            new DateTimeOffset(certificate.NotBefore.ToUniversalTime(), TimeSpan.Zero),
            new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero),
            canonicalHost);
    }

    public static byte[] ProtectPfxForCurrentWindowsUser(
        ReadOnlySpan<byte> pfxBytes,
        string environment,
        string canonicalHost)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DPAPI PFX protection is supported only on Windows");
        }

        var copy = pfxBytes.ToArray();
        try
        {
            return ProtectedData.Protect(
                copy,
                BuildEntropy(environment, canonicalHost),
                DataProtectionScope.CurrentUser);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(copy);
        }
    }

    public static void ValidateCertificate(
        X509Certificate2 certificate,
        string canonicalHost,
        DateTimeOffset nowUtc)
    {
        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException("LAN TLS certificate must include its private key");
        }

        var notBeforeUtc = new DateTimeOffset(certificate.NotBefore.ToUniversalTime(), TimeSpan.Zero);
        var notAfterUtc = new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero);
        if (nowUtc < notBeforeUtc || nowUtc >= notAfterUtc)
        {
            throw new InvalidOperationException("LAN TLS certificate is outside its validity window");
        }

        if (!certificate.MatchesHostname(canonicalHost, allowWildcards: false, allowCommonName: false))
        {
            throw new InvalidOperationException("LAN TLS certificate SAN does not match the canonical LAN hostname");
        }

        var ekuExtensions = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().ToArray();
        if (ekuExtensions.Length > 0 && !ekuExtensions.Any(extension =>
                extension.EnhancedKeyUsages.Cast<Oid>().Any(oid =>
                    string.Equals(oid.Value, ServerAuthenticationOid, StringComparison.Ordinal))))
        {
            throw new InvalidOperationException("LAN TLS certificate is not valid for TLS server authentication");
        }
    }

    private static X509Certificate2 LoadProtectedCertificate(
        string protectedPfxPath,
        string environment,
        string canonicalHost)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("DPAPI-protected LAN TLS PFX is supported only on Windows");
        }

        var fullPath = Path.GetFullPath(protectedPfxPath);
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException("VHDCHY_LAN_TLS_PROTECTED_PFX_PATH does not exist");
        }

        var protectedBytes = File.ReadAllBytes(fullPath);
        byte[]? pfxBytes = null;
        try
        {
            pfxBytes = ProtectedData.Unprotect(
                protectedBytes,
                BuildEntropy(environment, canonicalHost),
                DataProtectionScope.CurrentUser);
#pragma warning disable SYSLIB0057
            return new X509Certificate2(
                pfxBytes,
                (string?)null,
                RuntimeTlsKeyStorageFlags());
#pragma warning restore SYSLIB0057
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException("LAN TLS protected PFX could not be decrypted or loaded for the current Windows user", exception);
        }
        finally
        {
            if (pfxBytes is not null) CryptographicOperations.ZeroMemory(pfxBytes);
            CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    private static X509KeyStorageFlags RuntimeTlsKeyStorageFlags()
    {
        return OperatingSystem.IsWindows()
            ? X509KeyStorageFlags.UserKeySet
            : X509KeyStorageFlags.EphemeralKeySet;
    }

    private static byte[] BuildEntropy(string environment, string canonicalHost) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(
            $"VHDCHY|LAN_TLS|{environment.Trim().ToUpperInvariant()}|{canonicalHost.Trim().ToLowerInvariant()}|V1"));
}
