using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class LanPrimaryCredentialVerifier
{
    public const string LoginAuthoritySchemaVersion = "VHDCHY_AUTHORITY_SNAPSHOT_V2";

    private readonly string _connectionString;
    private readonly string _expectedDomainContractVersion;

    public LanPrimaryCredentialVerifier(string databasePath, string expectedDomainContractVersion)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        if (string.IsNullOrWhiteSpace(expectedDomainContractVersion)) throw new ArgumentException("Domain contract version is required", nameof(expectedDomainContractVersion));
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
        _expectedDomainContractVersion = expectedDomainContractVersion;
    }

    public async Task<LanPrimaryCredentialInspection> InspectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var authority = await LoadAuthorityAsync(cancellationToken);
            var loginCapableUsers = authority.Users.Values.Count(user =>
                user.Status == "ACTIVE" &&
                user.SecurityLevel != "ROOT" &&
                authority.ActivePasswords.ContainsKey(user.UserId));
            return new LanPrimaryCredentialInspection(
                true,
                "READY",
                authority.AuthorityVersion,
                loginCapableUsers);
        }
        catch (LanPrimaryCredentialException error)
        {
            return new LanPrimaryCredentialInspection(false, error.Code, null, 0);
        }
    }

    public async Task<LanPrimaryLoginDecision> VerifyAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username?.Trim() ?? string.Empty;
        if (normalizedUsername.Length == 0 || password is null)
            return Deny("INVALID_CREDENTIALS");

        AuthorityModel authority;
        try
        {
            authority = await LoadAuthorityAsync(cancellationToken);
        }
        catch (LanPrimaryCredentialException error)
        {
            return Deny(error.Code);
        }

        var user = authority.Users.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase));
        if (user is null || user.Status != "ACTIVE") return Deny("INVALID_CREDENTIALS");

        if (user.SecurityLevel == "ROOT")
        {
            return new LanPrimaryLoginDecision(
                false,
                "ROOT_EMAIL_OTP_REQUIRED",
                new LanAuthenticatedUserEvidence(
                    user.UserId,
                    user.Username,
                    user.EmployeeId,
                    user.DisplayName,
                    user.SecurityLevel,
                    false,
                    authority.AuthorityVersion));
        }

        if (!authority.ActivePasswords.TryGetValue(user.UserId, out var credential))
            return Deny("INVALID_CREDENTIALS");
        if (!VerifyPassword(password, credential.HashAlgorithm, credential.SecretHash))
            return Deny("INVALID_CREDENTIALS");

        return new LanPrimaryLoginDecision(
            true,
            "AUTHENTICATED",
            new LanAuthenticatedUserEvidence(
                user.UserId,
                user.Username,
                user.EmployeeId,
                user.DisplayName,
                user.SecurityLevel,
                credential.MustChange,
                authority.AuthorityVersion));
    }

    private async Task<AuthorityModel> LoadAuthorityAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT authority_version,compatibility_version,payload_json
            FROM authority_snapshots
            WHERE status='ACTIVE'
            ORDER BY imported_at DESC
            LIMIT 2
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new LanPrimaryCredentialException("AUTHORITY_SNAPSHOT_REQUIRED", "No active authority snapshot is available.");
        var authorityVersion = reader.GetString(0);
        var compatibilityVersion = reader.GetString(1);
        var payload = reader.GetString(2);
        if (await reader.ReadAsync(cancellationToken))
            throw new LanPrimaryCredentialException("AUTHORITY_ACTIVE_SET_INVALID", "More than one active authority snapshot exists.");
        if (!string.Equals(compatibilityVersion, _expectedDomainContractVersion, StringComparison.Ordinal))
            throw new LanPrimaryCredentialException("AUTHORITY_INCOMPATIBLE", "Active authority snapshot is incompatible with this LAN runtime.");
        return ParseAuthority(authorityVersion, payload);
    }

    private static AuthorityModel ParseAuthority(string authorityVersion, string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw Invalid("Authority payload must be an object.");
            if (RequiredString(root, "schemaVersion") != LoginAuthoritySchemaVersion)
                throw new LanPrimaryCredentialException("LOGIN_AUTHORITY_SCHEMA_REQUIRED", "LAN primary login requires authority snapshot V2.");

            var usersElement = RequiredArray(root, "users");
            var credentialsElement = RequiredArray(root, "credentials");
            var users = new Dictionary<string, AuthorityUser>(StringComparer.Ordinal);
            var usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in usersElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) throw Invalid("Authority user entry is invalid.");
                var user = new AuthorityUser(
                    RequiredString(item, "userId"),
                    RequiredString(item, "username"),
                    OptionalString(item, "employeeId"),
                    OptionalString(item, "displayName"),
                    RequiredEnum(item, "status", "ACTIVE", "DISABLED", "LOCKED", "CLOSED", "ARCHIVED"),
                    RequiredEnum(item, "securityLevel", "NORMAL", "SUPERADMIN", "ROOT"));
                if (!users.TryAdd(user.UserId, user) || !usernames.Add(user.Username))
                    throw Invalid("Authority users contain duplicate identity values.");
            }

            var activePasswords = new Dictionary<string, AuthorityCredential>(StringComparer.Ordinal);
            foreach (var item in credentialsElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) throw Invalid("Authority credential entry is invalid.");
                _ = RequiredString(item, "credentialId");
                var userId = RequiredString(item, "userId");
                if (!users.ContainsKey(userId)) throw Invalid("Authority credential references an unknown user.");
                var credentialType = RequiredEnum(item, "credentialType", "PASSWORD");
                var status = RequiredEnum(item, "status", "ACTIVE", "REPLACED", "REVOKED");
                var credential = new AuthorityCredential(
                    userId,
                    credentialType,
                    RequiredString(item, "secretHash"),
                    RequiredString(item, "hashAlgorithm"),
                    RequiredBoolean(item, "mustChange"),
                    status);
                if (credentialType == "PASSWORD" && status == "ACTIVE")
                {
                    if (!ValidatePasswordRecord(credential.HashAlgorithm, credential.SecretHash))
                        throw new LanPrimaryCredentialException("AUTHORITY_CREDENTIAL_INCOMPATIBLE", "An active synchronized password verifier is invalid or unsupported.");
                    if (!activePasswords.TryAdd(userId, credential))
                        throw Invalid("More than one active password credential exists for a user.");
                }
            }

            foreach (var user in users.Values)
            {
                if (user.Status == "ACTIVE" && user.SecurityLevel != "ROOT" && !activePasswords.ContainsKey(user.UserId))
                    throw new LanPrimaryCredentialException("LOGIN_AUTHORITY_CREDENTIALS_REQUIRED", "An active non-ROOT account is missing its synchronized password verifier.");
            }

            return new AuthorityModel(authorityVersion, users, activePasswords);
        }
        catch (LanPrimaryCredentialException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new LanPrimaryCredentialException("AUTHORITY_SNAPSHOT_INVALID", "Authority snapshot JSON is invalid.", error);
        }
    }

    private static bool ValidatePasswordRecord(string algorithm, string secretHash) =>
        TryParsePasswordRecord(algorithm, secretHash, out _, out _, out _);

    private static bool VerifyPassword(string password, string algorithm, string secretHash)
    {
        if (!TryParsePasswordRecord(algorithm, secretHash, out var iterations, out var salt, out var expected)) return false;
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool TryParsePasswordRecord(
        string algorithm,
        string secretHash,
        out int iterations,
        out byte[] salt,
        out byte[] expected)
    {
        iterations = 0;
        salt = Array.Empty<byte>();
        expected = Array.Empty<byte>();
        var parts = algorithm.Split('$', StringSplitOptions.None);
        if (parts.Length != 3 || parts[0] != "PBKDF2-SHA256" || !int.TryParse(parts[1], out iterations)) return false;
        if (iterations < 100_000 || iterations > 1_000_000) return false;
        try
        {
            salt = FromBase64Url(parts[2]);
            expected = FromBase64Url(secretHash);
        }
        catch (FormatException)
        {
            return false;
        }
        return salt.Length >= 16 && expected.Length == 32;
    }

    private static byte[] FromBase64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized += new string('=', (4 - normalized.Length % 4) % 4);
        return Convert.FromBase64String(normalized);
    }

    private static JsonElement RequiredArray(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            throw Invalid($"Required authority array is missing: {property}.");
        return value;
    }

    private static string RequiredString(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw Invalid($"Required authority field is missing: {property}.");
        return value.GetString()!.Trim();
    }

    private static string RequiredEnum(JsonElement item, string property, params string[] allowed)
    {
        var value = RequiredString(item, property).ToUpperInvariant();
        if (!allowed.Contains(value, StringComparer.Ordinal)) throw Invalid($"Authority field is invalid: {property}.");
        return value;
    }

    private static bool RequiredBoolean(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var value) || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw Invalid($"Authority boolean field is missing: {property}.");
        return value.GetBoolean();
    }

    private static string? OptionalString(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String) throw Invalid($"Authority field is invalid: {property}.");
        var text = value.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static LanPrimaryCredentialException Invalid(string message) =>
        new("AUTHORITY_SNAPSHOT_INVALID", message);

    private static LanPrimaryLoginDecision Deny(string code) => new(false, code, null);

    private sealed record AuthorityModel(
        string AuthorityVersion,
        IReadOnlyDictionary<string, AuthorityUser> Users,
        IReadOnlyDictionary<string, AuthorityCredential> ActivePasswords);

    private sealed record AuthorityUser(
        string UserId,
        string Username,
        string? EmployeeId,
        string? DisplayName,
        string Status,
        string SecurityLevel);

    private sealed record AuthorityCredential(
        string UserId,
        string CredentialType,
        string SecretHash,
        string HashAlgorithm,
        bool MustChange,
        string Status);
}

public sealed record LanPrimaryCredentialInspection(
    bool Ready,
    string Code,
    string? AuthoritySnapshotVersion,
    long LoginCapableUserCount);

public sealed record LanPrimaryLoginDecision(
    bool Authenticated,
    string Code,
    LanAuthenticatedUserEvidence? User);

public sealed record LanAuthenticatedUserEvidence(
    string UserId,
    string Username,
    string? EmployeeId,
    string? DisplayName,
    string SecurityLevel,
    bool MustChangePassword,
    string AuthoritySnapshotVersion);

public sealed class LanPrimaryCredentialException : InvalidOperationException
{
    public LanPrimaryCredentialException(string code, string message) : base(message) => Code = code;
    public LanPrimaryCredentialException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
