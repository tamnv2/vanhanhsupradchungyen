using System.Reflection;
using System.Text.Json;

namespace Vhdchy.LanService;

public sealed class Slice1CommandGate
{
    public const string CommandSpecSchemaVersion = "VHDCHY_COMMAND_SPEC_V1";
    public const string DomainContractVersion = "VHDCHY_DOMAIN_V1";
    public const string SliceId = "IDENTITY_EMPLOYEE_ATTENDANCE";
    private const string ResourceName = "VHDCHY.Contracts.commands.slice1.v1.json";

    private readonly LanAuthorizationEvaluator _authorizationEvaluator;
    private readonly Lazy<Slice1CommandCatalog> _catalog;

    public Slice1CommandGate(string databasePath, string expectedDomainContractVersion)
    {
        if (!string.Equals(expectedDomainContractVersion, DomainContractVersion, StringComparison.Ordinal))
        {
            throw new Slice1CommandGateException(
                "COMMAND_DOMAIN_INCOMPATIBLE",
                $"Slice-1 command gate only supports {DomainContractVersion}.");
        }

        _authorizationEvaluator = new LanAuthorizationEvaluator(databasePath, expectedDomainContractVersion);
        _catalog = new Lazy<Slice1CommandCatalog>(LoadCatalog, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Slice1CommandCatalogInspection InspectCatalog()
    {
        var catalog = _catalog.Value;
        return new Slice1CommandCatalogInspection(
            Ready: true,
            SchemaVersion: catalog.SchemaVersion,
            DomainContractVersion: catalog.DomainContractVersion,
            Slice: catalog.Slice,
            CommandCount: catalog.Commands.Count,
            CommandCodes: catalog.Commands.Keys.OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public async Task<Slice1CommandGateDecision> AuthorizeAsync(
        Slice1CommandGateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var catalog = _catalog.Value;

        if (!string.Equals(request.ModuleId, catalog.Slice, StringComparison.Ordinal))
        {
            return Deny(
                "COMMAND_MODULE_SCOPE_INVALID",
                null,
                null,
                null,
                null,
                "Command is not valid for the requested module.");
        }

        if (!catalog.Commands.TryGetValue(request.CommandCode, out var command))
        {
            return Deny(
                "UNSUPPORTED_COMMAND",
                null,
                null,
                null,
                null,
                "Command code is not part of the reviewed Slice-1 contract.");
        }

        var authorization = await _authorizationEvaluator.AuthorizeAsync(
            new LanAuthorizationRequest(
                UserId: request.AuthenticatedUserId,
                Resource: command.PermissionResource,
                Action: command.PermissionAction,
                ClusterId: request.ClusterId,
                ModuleId: request.ModuleId,
                MinimumSecurityLevel: null,
                AsOf: request.AsOf),
            cancellationToken);

        if (!authorization.Allowed)
        {
            return new Slice1CommandGateDecision(
                Authorized: false,
                Code: authorization.Code,
                CommandCode: command.Code,
                EntityType: command.EntityType,
                PermissionResource: command.PermissionResource,
                PermissionAction: command.PermissionAction,
                EventIntent: command.EventIntent,
                ExpectedEntityVersionSemantics: command.ExpectedEntityVersionSemantics,
                LanAutonomousSemantics: command.LanAutonomousSemantics,
                AuthoritySnapshotVersion: authorization.AuthoritySnapshotVersion,
                PermissionId: authorization.PermissionId,
                Message: authorization.Message);
        }

        return new Slice1CommandGateDecision(
            Authorized: true,
            Code: "AUTHORIZED",
            CommandCode: command.Code,
            EntityType: command.EntityType,
            PermissionResource: command.PermissionResource,
            PermissionAction: command.PermissionAction,
            EventIntent: command.EventIntent,
            ExpectedEntityVersionSemantics: command.ExpectedEntityVersionSemantics,
            LanAutonomousSemantics: command.LanAutonomousSemantics,
            AuthoritySnapshotVersion: authorization.AuthoritySnapshotVersion,
            PermissionId: authorization.PermissionId,
            Message: "Command vocabulary and synchronized permission grant are authorized.");
    }

    private static Slice1CommandCatalog LoadCatalog()
    {
        var assembly = typeof(Slice1CommandGate).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new Slice1CommandGateException(
                "COMMAND_CONTRACT_UNAVAILABLE",
                $"Embedded command contract resource is missing: {ResourceName}.");

        try
        {
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw InvalidContract("Command contract root must be an object.");

            var schemaVersion = RequiredString(root, "schemaVersion");
            var domainVersion = RequiredString(root, "domainContractVersion");
            var slice = RequiredString(root, "slice");
            if (!string.Equals(schemaVersion, CommandSpecSchemaVersion, StringComparison.Ordinal))
                throw InvalidContract($"Unsupported command spec schema: {schemaVersion}.");
            if (!string.Equals(domainVersion, DomainContractVersion, StringComparison.Ordinal))
                throw InvalidContract($"Unsupported domain contract version: {domainVersion}.");
            if (!string.Equals(slice, SliceId, StringComparison.Ordinal))
                throw InvalidContract($"Unsupported Slice-1 identifier: {slice}.");

            if (!root.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Object)
                throw InvalidContract("Command contract rules are missing.");
            if (!RequiredBoolean(rules, "actorFromAuthenticatedContextOnly") ||
                RequiredBoolean(rules, "clientActorFieldsAuthoritative") ||
                !RequiredBoolean(rules, "sameLogicalCommandKeepsIdentityAcrossRuntimeRoute") ||
                !RequiredBoolean(rules, "correctionsAppendEvidence") ||
                RequiredBoolean(rules, "silentLastWriteWinsAllowed"))
            {
                throw InvalidContract("Command contract authority/idempotency rules are incompatible with LAN execution.");
            }

            if (!root.TryGetProperty("commands", out var commandArray) || commandArray.ValueKind != JsonValueKind.Array)
                throw InvalidContract("Command contract commands array is missing.");

            var commands = new Dictionary<string, Slice1TrustedCommand>(StringComparer.Ordinal);
            foreach (var item in commandArray.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) throw InvalidContract("Command entry must be an object.");
                var code = RequiredString(item, "code");
                var entityType = RequiredString(item, "entityType");
                var eventIntent = RequiredString(item, "eventIntent");
                var expectedVersion = RequiredString(item, "expectedEntityVersion");
                var lanSemantics = RequiredString(item, "lanAutonomousSemantics");
                if (!item.TryGetProperty("permission", out var permission) || permission.ValueKind != JsonValueKind.Object)
                    throw InvalidContract($"Permission mapping is missing for command: {code}.");
                var resource = RequiredString(permission, "resource");
                var action = RequiredString(permission, "action");

                var trusted = new Slice1TrustedCommand(
                    Code: code,
                    EntityType: entityType,
                    PermissionResource: resource,
                    PermissionAction: action,
                    EventIntent: eventIntent,
                    ExpectedEntityVersionSemantics: expectedVersion,
                    LanAutonomousSemantics: lanSemantics);
                if (!commands.TryAdd(code, trusted))
                    throw InvalidContract($"Duplicate command code: {code}.");
            }

            if (commands.Count == 0) throw InvalidContract("Command contract is empty.");
            return new Slice1CommandCatalog(schemaVersion, domainVersion, slice, commands);
        }
        catch (Slice1CommandGateException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new Slice1CommandGateException(
                "COMMAND_CONTRACT_INVALID",
                "Embedded command contract JSON is invalid.",
                error);
        }
    }

    private static void ValidateRequest(Slice1CommandGateRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.AuthenticatedUserId))
            throw new ArgumentException("Authenticated user ID is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.CommandCode))
            throw new ArgumentException("Command code is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ClusterId))
            throw new ArgumentException("Cluster ID is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ModuleId))
            throw new ArgumentException("Module ID is required", nameof(request));
    }

    private static string RequiredString(JsonElement value, string propertyName)
    {
        if (!value.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw InvalidContract($"Missing or invalid string: {propertyName}.");
        }
        return property.GetString()!;
    }

    private static bool RequiredBoolean(JsonElement value, string propertyName)
    {
        if (!value.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw InvalidContract($"Missing or invalid boolean: {propertyName}.");
        }
        return property.GetBoolean();
    }

    private static Slice1CommandGateDecision Deny(
        string code,
        Slice1TrustedCommand? command,
        string? authorityVersion,
        string? permissionId,
        string? messagePrefix,
        string message) =>
        new(
            Authorized: false,
            Code: code,
            CommandCode: command?.Code,
            EntityType: command?.EntityType,
            PermissionResource: command?.PermissionResource,
            PermissionAction: command?.PermissionAction,
            EventIntent: command?.EventIntent,
            ExpectedEntityVersionSemantics: command?.ExpectedEntityVersionSemantics,
            LanAutonomousSemantics: command?.LanAutonomousSemantics,
            AuthoritySnapshotVersion: authorityVersion,
            PermissionId: permissionId,
            Message: messagePrefix is null ? message : $"{messagePrefix}: {message}");

    private static Slice1CommandGateException InvalidContract(string message) =>
        new("COMMAND_CONTRACT_INVALID", message);

    private sealed record Slice1CommandCatalog(
        string SchemaVersion,
        string DomainContractVersion,
        string Slice,
        IReadOnlyDictionary<string, Slice1TrustedCommand> Commands);
}

public sealed record Slice1CommandGateRequest(
    string AuthenticatedUserId,
    string CommandCode,
    string ClusterId,
    string ModuleId,
    DateTimeOffset? AsOf = null);

public sealed record Slice1TrustedCommand(
    string Code,
    string EntityType,
    string PermissionResource,
    string PermissionAction,
    string EventIntent,
    string ExpectedEntityVersionSemantics,
    string LanAutonomousSemantics);

public sealed record Slice1CommandGateDecision(
    bool Authorized,
    string Code,
    string? CommandCode,
    string? EntityType,
    string? PermissionResource,
    string? PermissionAction,
    string? EventIntent,
    string? ExpectedEntityVersionSemantics,
    string? LanAutonomousSemantics,
    string? AuthoritySnapshotVersion,
    string? PermissionId,
    string Message);

public sealed record Slice1CommandCatalogInspection(
    bool Ready,
    string SchemaVersion,
    string DomainContractVersion,
    string Slice,
    int CommandCount,
    IReadOnlyList<string> CommandCodes);

public sealed class Slice1CommandGateException : InvalidOperationException
{
    public Slice1CommandGateException(string code, string message) : base(message) => Code = code;
    public Slice1CommandGateException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
