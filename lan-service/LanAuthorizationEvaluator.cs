using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class LanAuthorizationEvaluator
{
    public const string AuthoritySchemaVersion = "VHDCHY_AUTHORITY_SNAPSHOT_V1";
    public const string PermissionCatalogVersion = "VHDCHY_PERMISSION_CATALOG_V1";

    private readonly string _connectionString;
    private readonly string _expectedDomainContractVersion;

    public LanAuthorizationEvaluator(string databasePath, string expectedDomainContractVersion)
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

    public async Task<LanAuthorizationDecision> AuthorizeAsync(
        LanAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var asOf = request.AsOf ?? DateTimeOffset.UtcNow;

        await using var connection = await OpenAsync(cancellationToken);
        var active = await ReadActiveSnapshotAsync(connection, cancellationToken);
        if (active is null)
        {
            return Deny("AUTHORITY_SNAPSHOT_REQUIRED", null, null, "No active authority snapshot is available.");
        }
        if (!string.Equals(active.CompatibilityVersion, _expectedDomainContractVersion, StringComparison.Ordinal))
        {
            return Deny("AUTHORITY_INCOMPATIBLE", active.AuthorityVersion, null, "Active authority snapshot is incompatible with this runtime.");
        }

        AuthorityModel model;
        try
        {
            model = ParseModel(active.PayloadJson);
        }
        catch (LanAuthorityModelException error)
        {
            return Deny(error.Code, active.AuthorityVersion, null, error.Message);
        }

        if (!model.Users.TryGetValue(request.UserId, out var user))
        {
            return Deny("ACCOUNT_NOT_FOUND", active.AuthorityVersion, null, "User is absent from the synchronized authority snapshot.");
        }
        if (!string.Equals(user.Status, "ACTIVE", StringComparison.Ordinal))
        {
            return Deny("ACCOUNT_NOT_ACTIVE", active.AuthorityVersion, user.SecurityLevel, "User is not active in the synchronized authority snapshot.");
        }

        if (!MeetsMinimumSecurityLevel(user.SecurityLevel, request.MinimumSecurityLevel))
        {
            return Deny("SECURITY_LEVEL_REQUIRED", active.AuthorityVersion, user.SecurityLevel, "User security level does not satisfy the command minimum.");
        }

        var matchingPermissions = model.Permissions.Values
            .Where(permission =>
                string.Equals(permission.Status, "ACTIVE", StringComparison.Ordinal) &&
                string.Equals(permission.Resource, request.Resource, StringComparison.Ordinal) &&
                string.Equals(permission.Action, request.Action, StringComparison.Ordinal))
            .ToArray();

        if (matchingPermissions.Length != 1)
        {
            return Deny(
                matchingPermissions.Length == 0 ? "PERMISSION_NOT_CATALOGED" : "AUTHORITY_SNAPSHOT_INVALID",
                active.AuthorityVersion,
                user.SecurityLevel,
                matchingPermissions.Length == 0
                    ? "Requested resource/action is not active in the synchronized permission catalog."
                    : "Synchronized authority contains duplicate active resource/action permissions.");
        }

        var permission = matchingPermissions[0];
        var effects = new List<MatchedEffect>();

        foreach (var grant in model.UserPermissionGrants)
        {
            if (!string.Equals(grant.UserId, request.UserId, StringComparison.Ordinal) ||
                !string.Equals(grant.PermissionId, permission.PermissionId, StringComparison.Ordinal) ||
                !GrantApplies(grant.Status, grant.ClusterId, grant.ModuleId, grant.EffectiveFrom, grant.EffectiveTo, request.ClusterId, request.ModuleId, asOf))
            {
                continue;
            }

            effects.Add(new MatchedEffect(grant.Effect, "USER_PERMISSION", grant.GrantId));
        }

        foreach (var userRole in model.UserRoleGrants)
        {
            if (!string.Equals(userRole.UserId, request.UserId, StringComparison.Ordinal) ||
                !GrantApplies(userRole.Status, userRole.ClusterId, userRole.ModuleId, userRole.EffectiveFrom, userRole.EffectiveTo, request.ClusterId, request.ModuleId, asOf))
            {
                continue;
            }
            if (!model.Roles.TryGetValue(userRole.RoleId, out var role) || !string.Equals(role.Status, "ACTIVE", StringComparison.Ordinal))
            {
                continue;
            }

            if (model.RolePermissionGrants.TryGetValue((userRole.RoleId, permission.PermissionId), out var rolePermission))
            {
                effects.Add(new MatchedEffect(rolePermission.Effect, "ROLE_PERMISSION", userRole.GrantId));
            }
        }

        var deny = effects.FirstOrDefault(effect => string.Equals(effect.Effect, "DENY", StringComparison.Ordinal));
        if (deny is not null)
        {
            return new LanAuthorizationDecision(
                Allowed: false,
                Code: "PERMISSION_DENIED",
                AuthoritySnapshotVersion: active.AuthorityVersion,
                PermissionId: permission.PermissionId,
                SecurityLevel: user.SecurityLevel,
                MatchedEffect: "DENY",
                MatchedSource: deny.Source,
                MatchedGrantId: deny.GrantId,
                Message: "A matching DENY grant takes precedence over ALLOW grants.");
        }

        var allow = effects.FirstOrDefault(effect => string.Equals(effect.Effect, "ALLOW", StringComparison.Ordinal));
        if (allow is null)
        {
            return new LanAuthorizationDecision(
                Allowed: false,
                Code: "PERMISSION_DENIED",
                AuthoritySnapshotVersion: active.AuthorityVersion,
                PermissionId: permission.PermissionId,
                SecurityLevel: user.SecurityLevel,
                MatchedEffect: null,
                MatchedSource: null,
                MatchedGrantId: null,
                Message: "No matching active ALLOW grant exists for the requested cluster/module scope.");
        }

        return new LanAuthorizationDecision(
            Allowed: true,
            Code: "AUTHORIZED",
            AuthoritySnapshotVersion: active.AuthorityVersion,
            PermissionId: permission.PermissionId,
            SecurityLevel: user.SecurityLevel,
            MatchedEffect: "ALLOW",
            MatchedSource: allow.Source,
            MatchedGrantId: allow.GrantId,
            Message: "Authorized by synchronized authority snapshot.");
    }

    public async Task<LanAuthoritySnapshotInspection> InspectActiveSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var active = await ReadActiveSnapshotAsync(connection, cancellationToken);
        if (active is null)
        {
            return new LanAuthoritySnapshotInspection(false, null, null, null, "AUTHORITY_SNAPSHOT_REQUIRED");
        }
        if (!string.Equals(active.CompatibilityVersion, _expectedDomainContractVersion, StringComparison.Ordinal))
        {
            return new LanAuthoritySnapshotInspection(false, active.AuthorityVersion, active.CompatibilityVersion, null, "AUTHORITY_INCOMPATIBLE");
        }

        try
        {
            var model = ParseModel(active.PayloadJson);
            return new LanAuthoritySnapshotInspection(true, active.AuthorityVersion, active.CompatibilityVersion, model.PermissionCatalogVersion, "READY");
        }
        catch (LanAuthorityModelException error)
        {
            return new LanAuthoritySnapshotInspection(false, active.AuthorityVersion, active.CompatibilityVersion, null, error.Code);
        }
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000;";
        await pragma.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }

    private static async Task<ActiveSnapshot?> ReadActiveSnapshotAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT authority_version, compatibility_version, payload_json
            FROM authority_snapshots
            WHERE status='ACTIVE'
            ORDER BY imported_at DESC
            LIMIT 2
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var result = new ActiveSnapshot(reader.GetString(0), reader.GetString(1), reader.GetString(2));
        if (await reader.ReadAsync(cancellationToken))
            throw new LanAuthorityModelException("AUTHORITY_ACTIVE_SET_INVALID", "More than one active authority snapshot exists.");
        return result;
    }

    private static AuthorityModel ParseModel(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            RequireObject(root, "AUTHORITY_SNAPSHOT_INVALID");
            var schemaVersion = RequiredString(root, "schemaVersion");
            if (!string.Equals(schemaVersion, AuthoritySchemaVersion, StringComparison.Ordinal))
                throw new LanAuthorityModelException("AUTHORITY_SCHEMA_INCOMPATIBLE", "Authority snapshot schema version is not supported.");
            var catalogVersion = RequiredString(root, "permissionCatalogVersion");
            if (!string.Equals(catalogVersion, PermissionCatalogVersion, StringComparison.Ordinal))
                throw new LanAuthorityModelException("PERMISSION_CATALOG_INCOMPATIBLE", "Permission catalog version is not supported.");

            var users = ParseUsers(RequiredArray(root, "users"));
            var roles = ParseRoles(RequiredArray(root, "roles"));
            var permissions = ParsePermissions(RequiredArray(root, "permissions"));
            var rolePermissionGrants = ParseRolePermissionGrants(RequiredArray(root, "rolePermissionGrants"), roles, permissions);
            var userRoleGrants = ParseUserRoleGrants(RequiredArray(root, "userRoleGrants"), users, roles);
            var userPermissionGrants = ParseUserPermissionGrants(RequiredArray(root, "userPermissionGrants"), users, permissions);

            return new AuthorityModel(
                catalogVersion,
                users,
                roles,
                permissions,
                rolePermissionGrants,
                userRoleGrants,
                userPermissionGrants);
        }
        catch (LanAuthorityModelException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new LanAuthorityModelException("AUTHORITY_SNAPSHOT_INVALID", "Authority snapshot JSON is invalid.", error);
        }
    }

    private static Dictionary<string, AuthorityUser> ParseUsers(JsonElement array)
    {
        var result = new Dictionary<string, AuthorityUser>(StringComparer.Ordinal);
        foreach (var item in array.EnumerateArray())
        {
            RequireObject(item, "AUTHORITY_SNAPSHOT_INVALID");
            var user = new AuthorityUser(
                RequiredString(item, "userId"),
                RequiredEnum(item, "status", "ACTIVE", "DISABLED", "LOCKED", "CLOSED", "ARCHIVED"),
                RequiredEnum(item, "securityLevel", "NORMAL", "SUPERADMIN", "ROOT"));
            if (!result.TryAdd(user.UserId, user)) throw Duplicate("user", user.UserId);
        }
        return result;
    }

    private static Dictionary<string, AuthorityRole> ParseRoles(JsonElement array)
    {
        var result = new Dictionary<string, AuthorityRole>(StringComparer.Ordinal);
        foreach (var item in array.EnumerateArray())
        {
            RequireObject(item, "AUTHORITY_SNAPSHOT_INVALID");
            var role = new AuthorityRole(
                RequiredString(item, "roleId"),
                RequiredEnum(item, "status", "ACTIVE", "INACTIVE", "ARCHIVED"));
            if (!result.TryAdd(role.RoleId, role)) throw Duplicate("role", role.RoleId);
        }
        return result;
    }

    private static Dictionary<string, AuthorityPermission> ParsePermissions(JsonElement array)
    {
        var result = new Dictionary<string, AuthorityPermission>(StringComparer.Ordinal);
        foreach (var item in array.EnumerateArray())
        {
            RequireObject(item, "AUTHORITY_SNAPSHOT_INVALID");
            var permission = new AuthorityPermission(
                RequiredString(item, "permissionId"),
                RequiredString(item, "resource"),
                RequiredString(item, "action"),
                RequiredEnum(item, "status", "ACTIVE", "INACTIVE"));
            if (!result.TryAdd(permission.PermissionId, permission)) throw Duplicate("permission", permission.PermissionId);
        }
        return result;
    }

    private static Dictionary<(string RoleId, string PermissionId), AuthorityRolePermissionGrant> ParseRolePermissionGrants(
        JsonElement array,
        IReadOnlyDictionary<string, AuthorityRole> roles,
        IReadOnlyDictionary<string, AuthorityPermission> permissions)
    {
        var result = new Dictionary<(string, string), AuthorityRolePermissionGrant>();
        foreach (var item in array.EnumerateArray())
        {
            RequireObject(item, "AUTHORITY_SNAPSHOT_INVALID");
            var grant = new AuthorityRolePermissionGrant(
                RequiredString(item, "roleId"),
                RequiredString(item, "permissionId"),
                RequiredEnum(item, "effect", "ALLOW", "DENY"));
            if (!roles.ContainsKey(grant.RoleId) || !permissions.ContainsKey(grant.PermissionId)) throw Orphan("rolePermissionGrant");
            if (!result.TryAdd((grant.RoleId, grant.PermissionId), grant)) throw Duplicate("rolePermissionGrant", $"{grant.RoleId}/{grant.PermissionId}");
        }
        return result;
    }

    private static List<AuthorityUserRoleGrant> ParseUserRoleGrants(
        JsonElement array,
        IReadOnlyDictionary<string, AuthorityUser> users,
        IReadOnlyDictionary<string, AuthorityRole> roles)
    {
        var result = new List<AuthorityUserRoleGrant>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in array.EnumerateArray())
        {
            RequireObject(item, "AUTHORITY_SNAPSHOT_INVALID");
            var grant = new AuthorityUserRoleGrant(
                RequiredString(item, "grantId"),
                RequiredString(item, "userId"),
                RequiredString(item, "roleId"),
                OptionalString(item, "clusterId"),
                OptionalString(item, "moduleId"),
                RequiredEnum(item, "status", "ACTIVE", "REVOKED", "EXPIRED"),
                OptionalTime(item, "effectiveFrom"),
                OptionalTime(item, "effectiveTo"));
            if (!ids.Add(grant.GrantId)) throw Duplicate("userRoleGrant", grant.GrantId);
            if (!users.ContainsKey(grant.UserId) || !roles.ContainsKey(grant.RoleId)) throw Orphan("userRoleGrant");
            result.Add(grant);
        }
        return result;
    }

    private static List<AuthorityUserPermissionGrant> ParseUserPermissionGrants(
        JsonElement array,
        IReadOnlyDictionary<string, AuthorityUser> users,
        IReadOnlyDictionary<string, AuthorityPermission> permissions)
    {
        var result = new List<AuthorityUserPermissionGrant>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in array.EnumerateArray())
        {
            RequireObject(item, "AUTHORITY_SNAPSHOT_INVALID");
            var grant = new AuthorityUserPermissionGrant(
                RequiredString(item, "grantId"),
                RequiredString(item, "userId"),
                RequiredString(item, "permissionId"),
                OptionalString(item, "clusterId"),
                OptionalString(item, "moduleId"),
                RequiredEnum(item, "effect", "ALLOW", "DENY"),
                RequiredEnum(item, "status", "ACTIVE", "REVOKED", "EXPIRED"),
                OptionalTime(item, "effectiveFrom"),
                OptionalTime(item, "effectiveTo"));
            if (!ids.Add(grant.GrantId)) throw Duplicate("userPermissionGrant", grant.GrantId);
            if (!users.ContainsKey(grant.UserId) || !permissions.ContainsKey(grant.PermissionId)) throw Orphan("userPermissionGrant");
            result.Add(grant);
        }
        return result;
    }

    private static bool GrantApplies(
        string status,
        string? clusterId,
        string? moduleId,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        string requestedClusterId,
        string requestedModuleId,
        DateTimeOffset asOf)
    {
        if (!string.Equals(status, "ACTIVE", StringComparison.Ordinal)) return false;
        if (clusterId is not null && !string.Equals(clusterId, requestedClusterId, StringComparison.Ordinal)) return false;
        if (moduleId is not null && !string.Equals(moduleId, requestedModuleId, StringComparison.Ordinal)) return false;
        if (effectiveFrom is not null && asOf < effectiveFrom.Value) return false;
        if (effectiveTo is not null && asOf > effectiveTo.Value) return false;
        return true;
    }

    private static bool MeetsMinimumSecurityLevel(string actual, string? minimum)
    {
        if (minimum is null) return true;
        var actualRank = SecurityRank(actual);
        var minimumRank = SecurityRank(minimum);
        return actualRank >= minimumRank;
    }

    private static int SecurityRank(string value) => value switch
    {
        "NORMAL" => 0,
        "SUPERADMIN" => 1,
        "ROOT" => 2,
        _ => throw new LanAuthorityModelException("AUTHORITY_SNAPSHOT_INVALID", $"Unknown security level: {value}.")
    };

    private static void ValidateRequest(LanAuthorizationRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.UserId)) throw new ArgumentException("User ID is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Resource)) throw new ArgumentException("Resource is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Action)) throw new ArgumentException("Action is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ClusterId)) throw new ArgumentException("Cluster ID is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ModuleId)) throw new ArgumentException("Module ID is required", nameof(request));
        if (request.MinimumSecurityLevel is not null && request.MinimumSecurityLevel is not ("NORMAL" or "SUPERADMIN" or "ROOT"))
            throw new ArgumentException("Minimum security level is invalid", nameof(request));
    }

    private static LanAuthorizationDecision Deny(string code, string? authorityVersion, string? securityLevel, string message) =>
        new(false, code, authorityVersion, null, securityLevel, null, null, null, message);

    private static void RequireObject(JsonElement value, string code)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new LanAuthorityModelException(code, "Expected JSON object.");
    }

    private static JsonElement RequiredArray(JsonElement value, string propertyName)
    {
        if (!value.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            throw new LanAuthorityModelException("AUTHORITY_SNAPSHOT_INVALID", $"Missing or invalid array: {propertyName}.");
        return property;
    }

    private static string RequiredString(JsonElement value, string propertyName)
    {
        if (!value.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString()))
            throw new LanAuthorityModelException("AUTHORITY_SNAPSHOT_INVALID", $"Missing or invalid string: {propertyName}.");
        return property.GetString()!;
    }

    private static string RequiredEnum(JsonElement value, string propertyName, params string[] allowed)
    {
        var text = RequiredString(value, propertyName);
        if (!allowed.Contains(text, StringComparer.Ordinal))
            throw new LanAuthorityModelException("AUTHORITY_SNAPSHOT_INVALID", $"Invalid {propertyName}: {text}.");
        return text;
    }

    private static string? OptionalString(JsonElement value, string propertyName)
    {
        if (!value.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null) return null;
        if (property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString()))
            throw new LanAuthorityModelException("AUTHORITY_SNAPSHOT_INVALID", $"Invalid optional string: {propertyName}.");
        return property.GetString();
    }

    private static DateTimeOffset? OptionalTime(JsonElement value, string propertyName)
    {
        var text = OptionalString(value, propertyName);
        if (text is null) return null;
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            throw new LanAuthorityModelException("AUTHORITY_SNAPSHOT_INVALID", $"Invalid timestamp: {propertyName}.");
        return parsed;
    }

    private static LanAuthorityModelException Duplicate(string type, string id) =>
        new("AUTHORITY_SNAPSHOT_INVALID", $"Duplicate {type}: {id}.");

    private static LanAuthorityModelException Orphan(string type) =>
        new("AUTHORITY_SNAPSHOT_INVALID", $"Orphan {type} reference in authority snapshot.");

    private sealed record ActiveSnapshot(string AuthorityVersion, string CompatibilityVersion, string PayloadJson);
    private sealed record AuthorityUser(string UserId, string Status, string SecurityLevel);
    private sealed record AuthorityRole(string RoleId, string Status);
    private sealed record AuthorityPermission(string PermissionId, string Resource, string Action, string Status);
    private sealed record AuthorityRolePermissionGrant(string RoleId, string PermissionId, string Effect);
    private sealed record AuthorityUserRoleGrant(
        string GrantId,
        string UserId,
        string RoleId,
        string? ClusterId,
        string? ModuleId,
        string Status,
        DateTimeOffset? EffectiveFrom,
        DateTimeOffset? EffectiveTo);
    private sealed record AuthorityUserPermissionGrant(
        string GrantId,
        string UserId,
        string PermissionId,
        string? ClusterId,
        string? ModuleId,
        string Effect,
        string Status,
        DateTimeOffset? EffectiveFrom,
        DateTimeOffset? EffectiveTo);
    private sealed record AuthorityModel(
        string PermissionCatalogVersion,
        IReadOnlyDictionary<string, AuthorityUser> Users,
        IReadOnlyDictionary<string, AuthorityRole> Roles,
        IReadOnlyDictionary<string, AuthorityPermission> Permissions,
        IReadOnlyDictionary<(string RoleId, string PermissionId), AuthorityRolePermissionGrant> RolePermissionGrants,
        IReadOnlyList<AuthorityUserRoleGrant> UserRoleGrants,
        IReadOnlyList<AuthorityUserPermissionGrant> UserPermissionGrants);
    private sealed record MatchedEffect(string Effect, string Source, string GrantId);
}

public sealed record LanAuthorizationRequest(
    string UserId,
    string Resource,
    string Action,
    string ClusterId,
    string ModuleId,
    string? MinimumSecurityLevel = null,
    DateTimeOffset? AsOf = null);

public sealed record LanAuthorizationDecision(
    bool Allowed,
    string Code,
    string? AuthoritySnapshotVersion,
    string? PermissionId,
    string? SecurityLevel,
    string? MatchedEffect,
    string? MatchedSource,
    string? MatchedGrantId,
    string Message);

public sealed record LanAuthoritySnapshotInspection(
    bool Ready,
    string? AuthoritySnapshotVersion,
    string? CompatibilityVersion,
    string? PermissionCatalogVersion,
    string Code);

public sealed class LanAuthorityModelException : InvalidOperationException
{
    public LanAuthorityModelException(string code, string message) : base(message) => Code = code;
    public LanAuthorityModelException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
