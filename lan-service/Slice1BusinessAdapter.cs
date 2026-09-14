using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class Slice1BusinessAdapter
{
    public const string ModuleId = Slice1CommandGate.SliceId;

    private readonly string _clusterId;
    private readonly string _connectionString;
    private readonly Slice1CommandGate _gate;
    private readonly LocalCommandStore _commandStore;
    private readonly LocalCommandReplayResolver _replayResolver;
    private readonly EdgeActorEvidenceStore _actorEvidence;

    public Slice1BusinessAdapter(
        string databasePath,
        string expectedEnvironment,
        string expectedClusterId,
        string expectedDomainContractVersion)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        if (string.IsNullOrWhiteSpace(expectedEnvironment)) throw new ArgumentException("Environment is required", nameof(expectedEnvironment));
        if (string.IsNullOrWhiteSpace(expectedClusterId)) throw new ArgumentException("Cluster ID is required", nameof(expectedClusterId));

        _clusterId = expectedClusterId;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
        _gate = new Slice1CommandGate(databasePath, expectedDomainContractVersion);
        _commandStore = new LocalCommandStore(databasePath, expectedEnvironment, expectedClusterId, expectedDomainContractVersion);
        _replayResolver = new LocalCommandReplayResolver(databasePath, expectedEnvironment, expectedClusterId);
        _actorEvidence = new EdgeActorEvidenceStore(databasePath);
    }

    public Slice1BusinessAdapterInspection Inspect()
    {
        var catalog = _gate.InspectCatalog();
        var supported = catalog.CommandCodes
            .Where(code => !string.Equals(code, "EMPLOYEE_PORTRAIT_REPLACE", StringComparison.Ordinal))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        return new Slice1BusinessAdapterInspection(
            Ready: false,
            CatalogCommandCount: catalog.CommandCount,
            SupportedCommandCodes: supported,
            Blockers: new[]
            {
                new Slice1BusinessAdapterBlocker(
                    "PORTRAIT_MEDIA_LIFECYCLE_REQUIRED",
                    "Employee portrait replacement remains fail-closed until staged media upload, durable readback, and prior-portrait deletion are linked atomically to the reviewed command flow.")
            });
    }

    public async Task<LanLocalCommandResult> ExecuteAsync(
        Slice1BusinessCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var gate = await _gate.AuthorizeAsync(
            new Slice1CommandGateRequest(
                request.AuthenticatedUserId,
                request.CommandCode,
                _clusterId,
                ModuleId),
            cancellationToken);
        if (!gate.Authorized)
            throw new Slice1BusinessException(gate.Code, gate.Message);

        var entityType = gate.EntityType
            ?? throw new Slice1BusinessException("RUNTIME_DEPENDENCY_UNAVAILABLE", "Trusted command mapping did not provide entity type.");
        var eventCode = gate.EventIntent
            ?? throw new Slice1BusinessException("RUNTIME_DEPENDENCY_UNAVAILABLE", "Trusted command mapping did not provide event intent.");

        var payload = ParsePayload(request.PayloadJson);
        RejectClientAuthorityFields(payload);

        var replay = await _replayResolver.ResolveAsync(
            new LanLocalCommandReplayProbe(
                request.IdempotencyKey,
                request.CommandCode,
                eventCode,
                entityType,
                request.EntityId,
                request.ExpectedEntityVersion,
                request.PayloadJson,
                request.DeviceId,
                request.DeviceSeq),
            cancellationToken);
        if (replay is not null)
        {
            var replayActor = await _actorEvidence.ReadActorAsync(replay.EventId, cancellationToken);
            if (!string.Equals(replayActor, request.AuthenticatedUserId, StringComparison.Ordinal))
            {
                throw new Slice1BusinessException(
                    "IDEMPOTENCY_PAYLOAD_CONFLICT",
                    "The previously accepted logical command does not carry matching authenticated-actor evidence.");
            }
            return replay;
        }

        var prepared = request.CommandCode switch
        {
            "EMPLOYEE_CREATE" => await PrepareEmployeeCreateAsync(request, payload, cancellationToken),
            "EMPLOYEE_UPDATE" => await PrepareEmployeeUpdateAsync(request, payload, cancellationToken),
            "EMPLOYEE_STATUS_CHANGE" => await PrepareEmployeeStatusChangeAsync(request, payload, cancellationToken),
            "EMPLOYEE_CODE_ASSIGN" => await PrepareEmployeeCodeAssignAsync(request, payload, cancellationToken),
            "EMPLOYEE_PORTRAIT_REPLACE" => throw new Slice1BusinessException(
                "RUNTIME_DEPENDENCY_UNAVAILABLE",
                "Employee portrait replacement remains closed until the reviewed staged-media and prior-file deletion lifecycle is implemented."),
            "ATTENDANCE_IN" => await PrepareAttendanceAsync(request, payload, "IN", cancellationToken),
            "ATTENDANCE_OUT" => await PrepareAttendanceAsync(request, payload, "OUT", cancellationToken),
            "ATTENDANCE_CORRECT" => await PrepareAttendanceCorrectionAsync(request, payload, cancellationToken),
            _ => throw new Slice1BusinessException("INVALID_INPUT", "Unsupported Slice-1 business command.")
        };

        await _actorEvidence.StageAsync(
            request.RequestId,
            request.IdempotencyKey,
            request.AuthenticatedUserId,
            cancellationToken);
        try
        {
            LanLocalCommandResult result;
            try
            {
                result = await _commandStore.ExecuteAsync(
                    new LanLocalCommandEnvelope(
                        RequestId: request.RequestId,
                        IdempotencyKey: request.IdempotencyKey,
                        ModuleId: ModuleId,
                        CommandCode: request.CommandCode,
                        EventCode: eventCode,
                        EntityType: entityType,
                        EntityId: request.EntityId,
                        StateKey: prepared.StateKey,
                        ExpectedBaseVersion: request.ExpectedEntityVersion,
                        PayloadJson: request.PayloadJson,
                        NextStateJson: prepared.NextStateJson,
                        DeviceId: request.DeviceId,
                        DeviceSeq: request.DeviceSeq),
                    cancellationToken);
            }
            catch (LanLocalCommandException error) when (IsEmployeeCodeUniqueClaimConflict(error))
            {
                throw new Slice1BusinessException(
                    "RESOURCE_NOT_AVAILABLE",
                    "The requested active employee-code identity is no longer available because another accepted mutation owns the required uniqueness claim.",
                    error);
            }

            var actor = await _actorEvidence.ReadActorAsync(result.EventId, cancellationToken);
            if (!string.Equals(actor, request.AuthenticatedUserId, StringComparison.Ordinal))
            {
                throw new Slice1BusinessException(
                    "RUNTIME_DEPENDENCY_UNAVAILABLE",
                    "Accepted edge event is missing the required immutable authenticated-actor evidence.");
            }
            return result;
        }
        finally
        {
            await _actorEvidence.CleanupContextAsync(request.RequestId, request.IdempotencyKey, cancellationToken);
        }
    }

    private async Task<PreparedCommand> PrepareEmployeeCreateAsync(
        Slice1BusinessCommandRequest request,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        RejectUnknown(payload, EmployeeCreateFields);
        RequirePayloadEntity(payload, "employeeId", request.EntityId);
        RequireString(payload, "fullName");
        if (request.ExpectedEntityVersion is not null)
            throw VersionConflict("EMPLOYEE_CREATE requires no expected entity version.");

        var stateKey = EmployeeStateKey(request.EntityId);
        if (await _commandStore.ReadCurrentStateAsync(stateKey, cancellationToken) is not null)
            throw VersionConflict("Employee already exists.");

        var status = OptionalString(payload, "status") ?? "ACTIVE";
        ValidateEmployeeStatus(status);
        var next = (JsonObject)payload.DeepClone();
        next["employeeId"] = request.EntityId;
        next["status"] = status;
        next["entityVersion"] = 1;
        return new PreparedCommand(stateKey, next.ToJsonString());
    }

    private async Task<PreparedCommand> PrepareEmployeeUpdateAsync(
        Slice1BusinessCommandRequest request,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        RejectUnknown(payload, EmployeeUpdateFields);
        RequirePayloadEntity(payload, "employeeId", request.EntityId);
        if (payload.Count <= 1)
            throw Invalid("EMPLOYEE_UPDATE requires at least one profile field to update.");

        var stateKey = EmployeeStateKey(request.EntityId);
        var current = await RequireStateAsync(stateKey, "employee", cancellationToken);
        RequireExpectedVersion(request.ExpectedEntityVersion, current);
        var next = ParseState(current.StateJson);
        foreach (var property in payload)
        {
            if (property.Key == "employeeId") continue;
            next[property.Key] = property.Value?.DeepClone();
        }
        next["employeeId"] = request.EntityId;
        next["entityVersion"] = checked(current.EntityVersion + 1);
        return new PreparedCommand(stateKey, next.ToJsonString());
    }

    private async Task<PreparedCommand> PrepareEmployeeStatusChangeAsync(
        Slice1BusinessCommandRequest request,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        RejectUnknown(payload, EmployeeStatusFields);
        RequirePayloadEntity(payload, "employeeId", request.EntityId);
        var status = RequireString(payload, "status");
        ValidateEmployeeStatus(status);

        var stateKey = EmployeeStateKey(request.EntityId);
        var current = await RequireStateAsync(stateKey, "employee", cancellationToken);
        RequireExpectedVersion(request.ExpectedEntityVersion, current);
        var next = ParseState(current.StateJson);
        var currentStatus = OptionalString(next, "status");
        if (string.Equals(currentStatus, status, StringComparison.Ordinal))
            throw Invalid("Employee already has the requested status.");
        next["status"] = status;
        if (payload.TryGetPropertyValue("permanentLeaveDate", out var leaveDate))
            next["permanentLeaveDate"] = leaveDate?.DeepClone();
        next["entityVersion"] = checked(current.EntityVersion + 1);
        return new PreparedCommand(stateKey, next.ToJsonString());
    }

    private async Task<PreparedCommand> PrepareEmployeeCodeAssignAsync(
        Slice1BusinessCommandRequest request,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        RejectUnknown(payload, EmployeeCodeFields);
        RequirePayloadEntity(payload, "employeeCodeId", request.EntityId);
        var employeeId = RequireString(payload, "employeeId");
        var employeeCode = RequireString(payload, "employeeCode");

        var employee = await RequireStateAsync(EmployeeStateKey(employeeId), "employee", cancellationToken);
        var employeeState = ParseState(employee.StateJson);
        if (!string.Equals(OptionalString(employeeState, "status"), "ACTIVE", StringComparison.Ordinal))
            throw Invalid("An ACTIVE employee is required before assigning an active employee code.");

        var stateKey = EmployeeCodeStateKey(request.EntityId);
        var current = await _commandStore.ReadCurrentStateAsync(stateKey, cancellationToken);
        if (current is null)
        {
            if (request.ExpectedEntityVersion is not null)
                throw VersionConflict("New employee-code identity requires no expected entity version.");
        }
        else
        {
            if (!string.Equals(current.ModuleId, ModuleId, StringComparison.Ordinal) ||
                !string.Equals(current.EntityType, "employee_code", StringComparison.Ordinal))
            {
                throw new Slice1BusinessException(
                    "RUNTIME_DEPENDENCY_UNAVAILABLE",
                    "Employee-code state identity is incompatible with the Slice-1 adapter.");
            }
            RequireExpectedVersion(request.ExpectedEntityVersion, current);

            var currentCodeState = ParseState(current.StateJson);
            var priorEmployeeId = RequireString(currentCodeState, "employeeId");
            if (!string.Equals(priorEmployeeId, employeeId, StringComparison.Ordinal))
            {
                var priorEmployee = await RequireStateAsync(EmployeeStateKey(priorEmployeeId), "employee", cancellationToken);
                var priorEmployeeState = ParseState(priorEmployee.StateJson);
                var priorStatus = RequireString(priorEmployeeState, "status");
                if (priorStatus is not ("INACTIVE" or "LEFT"))
                {
                    throw Invalid("An employee code cannot be reassigned while its previous holder is still ACTIVE or otherwise not explicitly inactive/left.");
                }
            }
        }

        var activeCodes = await ReadActiveEmployeeCodesAsync(cancellationToken);
        foreach (var active in activeCodes)
        {
            if (string.Equals(active.EntityId, request.EntityId, StringComparison.Ordinal)) continue;
            if (string.Equals(active.EmployeeCode, employeeCode, StringComparison.Ordinal))
                throw Invalid("The requested employee code is already active for another employee-code identity.");
            if (string.Equals(active.EmployeeId, employeeId, StringComparison.Ordinal))
                throw Invalid("The employee already has another active employee code. Explicit release/reassignment evidence is required before replacement.");
        }

        var nextVersion = current is null ? 1 : checked(current.EntityVersion + 1);
        var next = new JsonObject
        {
            ["employeeCodeId"] = request.EntityId,
            ["employeeId"] = employeeId,
            ["employeeCode"] = employeeCode,
            ["status"] = "ACTIVE",
            ["entityVersion"] = nextVersion
        };
        return new PreparedCommand(stateKey, next.ToJsonString());
    }

    private async Task<PreparedCommand> PrepareAttendanceAsync(
        Slice1BusinessCommandRequest request,
        JsonObject payload,
        string targetState,
        CancellationToken cancellationToken)
    {
        RejectUnknown(payload, AttendanceScanFields);
        RequirePayloadEntity(payload, "employeeId", request.EntityId);
        var businessDate = RequireString(payload, "businessDate");

        var employee = await RequireStateAsync(EmployeeStateKey(request.EntityId), "employee", cancellationToken);
        var employeeState = ParseState(employee.StateJson);
        if (!string.Equals(OptionalString(employeeState, "status"), "ACTIVE", StringComparison.Ordinal))
            throw Invalid("Attendance scan requires an ACTIVE employee.");

        var stateKey = AttendanceStateKey(request.EntityId);
        var current = await _commandStore.ReadCurrentStateAsync(stateKey, cancellationToken);
        if (current is null)
        {
            if (targetState == "OUT")
                throw Invalid("ATTENDANCE_OUT requires a valid preceding IN presence state.");
            if (request.ExpectedEntityVersion is not null)
                throw VersionConflict("No presence state exists for the supplied expected version.");
            return new PreparedCommand(
                stateKey,
                BuildPresenceState(payload, request.EntityId, targetState, businessDate, 1).ToJsonString());
        }

        if (!string.Equals(current.ModuleId, ModuleId, StringComparison.Ordinal) ||
            !string.Equals(current.EntityType, "attendance", StringComparison.Ordinal))
        {
            throw new Slice1BusinessException(
                "RUNTIME_DEPENDENCY_UNAVAILABLE",
                "Attendance state identity is incompatible with the Slice-1 adapter.");
        }

        RequireExpectedVersion(request.ExpectedEntityVersion, current);
        var currentState = ParseState(current.StateJson);
        var presence = RequireString(currentState, "currentState");
        if (targetState == "OUT" && !string.Equals(presence, "IN", StringComparison.Ordinal))
            throw Invalid("ATTENDANCE_OUT requires a valid preceding IN presence state.");

        var next = BuildPresenceState(
            payload,
            request.EntityId,
            targetState,
            businessDate,
            checked(current.EntityVersion + 1),
            currentState);
        return new PreparedCommand(stateKey, next.ToJsonString());
    }

    private async Task<PreparedCommand> PrepareAttendanceCorrectionAsync(
        Slice1BusinessCommandRequest request,
        JsonObject payload,
        CancellationToken cancellationToken)
    {
        RejectUnknown(payload, AttendanceCorrectionFields);
        RequirePayloadEntity(payload, "employeeId", request.EntityId);
        var targetState = RequireString(payload, "currentState");
        if (targetState is not ("IN" or "OUT"))
            throw Invalid("Attendance correction currentState must be IN or OUT.");
        var reason = RequireString(payload, "reason");
        var businessDate = RequireString(payload, "businessDate");
        _ = await RequireStateAsync(EmployeeStateKey(request.EntityId), "employee", cancellationToken);

        var stateKey = AttendanceStateKey(request.EntityId);
        var current = await RequireStateAsync(stateKey, "attendance", cancellationToken);
        RequireExpectedVersion(request.ExpectedEntityVersion, current);
        var currentState = ParseState(current.StateJson);
        var next = BuildPresenceState(
            payload,
            request.EntityId,
            targetState,
            businessDate,
            checked(current.EntityVersion + 1),
            currentState);
        next["lastCorrectionReason"] = reason;
        return new PreparedCommand(stateKey, next.ToJsonString());
    }

    private async Task<LanCurrentState> RequireStateAsync(
        string stateKey,
        string expectedEntityType,
        CancellationToken cancellationToken)
    {
        var current = await _commandStore.ReadCurrentStateAsync(stateKey, cancellationToken)
            ?? throw new Slice1BusinessException("NOT_FOUND", $"Required current state is missing: {stateKey}.");
        if (!string.Equals(current.ModuleId, ModuleId, StringComparison.Ordinal) ||
            !string.Equals(current.EntityType, expectedEntityType, StringComparison.Ordinal))
        {
            throw new Slice1BusinessException("RUNTIME_DEPENDENCY_UNAVAILABLE", "Current state identity is incompatible with the Slice-1 adapter.");
        }
        return current;
    }

    private async Task<IReadOnlyList<ActiveEmployeeCode>> ReadActiveEmployeeCodesAsync(CancellationToken cancellationToken)
    {
        var result = new List<ActiveEmployeeCode>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT entity_id, state_json
            FROM module_current_state
            WHERE module_id=$module AND entity_type='employee_code'
            """;
        command.Parameters.AddWithValue("$module", ModuleId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var entityId = reader.GetString(0);
            var state = ParseState(reader.GetString(1));
            if (!string.Equals(OptionalString(state, "status"), "ACTIVE", StringComparison.Ordinal)) continue;
            result.Add(new ActiveEmployeeCode(
                entityId,
                RequireString(state, "employeeId"),
                RequireString(state, "employeeCode")));
        }
        return result;
    }

    private static JsonObject BuildPresenceState(
        JsonObject payload,
        string employeeId,
        string targetState,
        string businessDate,
        long version,
        JsonObject? existing = null)
    {
        var next = existing is null ? new JsonObject() : (JsonObject)existing.DeepClone();
        next["employeeId"] = employeeId;
        next["currentState"] = targetState;
        next["businessDate"] = businessDate;
        next["entityVersion"] = version;
        if (payload.TryGetPropertyValue("occurredAt", out var occurredAt))
            next["lastOccurredAt"] = occurredAt?.DeepClone();
        return next;
    }

    private static JsonObject ParsePayload(string json)
    {
        ValidateNoDuplicateProperties(json, "INVALID_INPUT");
        try
        {
            var node = JsonNode.Parse(json);
            if (node is not JsonObject value)
                throw Invalid("Command payload must be a JSON object.");
            return value;
        }
        catch (Slice1BusinessException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new Slice1BusinessException("INVALID_INPUT", "Command payload JSON is invalid.", error);
        }
    }

    private static JsonObject ParseState(string json)
    {
        ValidateNoDuplicateProperties(json, "RUNTIME_DEPENDENCY_UNAVAILABLE");
        try
        {
            var node = JsonNode.Parse(json);
            if (node is not JsonObject value)
                throw new Slice1BusinessException("RUNTIME_DEPENDENCY_UNAVAILABLE", "Current state JSON is not an object.");
            return value;
        }
        catch (Slice1BusinessException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new Slice1BusinessException("RUNTIME_DEPENDENCY_UNAVAILABLE", "Current state JSON is invalid.", error);
        }
    }

    private static void ValidateNoDuplicateProperties(string json, string code)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new Slice1BusinessException(code, "JSON root must be an object.");
            var properties = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
            if (properties.Distinct(StringComparer.Ordinal).Count() != properties.Length)
                throw new Slice1BusinessException(code, "JSON object contains duplicate property names.");
        }
        catch (Slice1BusinessException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new Slice1BusinessException(code, "JSON is invalid.", error);
        }
    }

    private static void ValidateRequest(Slice1BusinessCommandRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        RequireBounded(request.AuthenticatedUserId, "AUTH_REQUIRED", 1, 240);
        RequireBounded(request.RequestId, "INVALID_INPUT", 1, 200);
        RequireBounded(request.IdempotencyKey, "INVALID_INPUT", 1, 240);
        RequireBounded(request.CommandCode, "INVALID_INPUT", 1, 120);
        RequireBounded(request.EntityId, "INVALID_INPUT", 1, 240);
        if (request.ExpectedEntityVersion is < 1)
            throw Invalid("Expected entity version must be at least 1 when supplied.");
        if ((request.DeviceId is null) != (request.DeviceSeq is null))
            throw Invalid("Device ID and device sequence must be supplied together.");
        if (request.DeviceSeq is < 0) throw Invalid("Device sequence must be non-negative.");
    }

    private static void RejectClientAuthorityFields(JsonObject payload)
    {
        foreach (var field in ClientAuthorityFields)
        {
            if (payload.ContainsKey(field))
                throw Invalid($"Client payload must not supply trusted authority field: {field}.");
        }
    }

    private static void RejectUnknown(JsonObject payload, IReadOnlySet<string> allowed)
    {
        foreach (var property in payload)
        {
            if (!allowed.Contains(property.Key))
                throw Invalid($"Unsupported payload field for this command: {property.Key}.");
        }
    }

    private static void RequirePayloadEntity(JsonObject payload, string propertyName, string expected)
    {
        var value = RequireString(payload, propertyName);
        if (!string.Equals(value, expected, StringComparison.Ordinal))
            throw Invalid($"Payload {propertyName} does not match the trusted command target identity.");
    }

    private static string RequireString(JsonObject value, string propertyName)
    {
        if (!value.TryGetPropertyValue(propertyName, out var node) || node is null)
            throw Invalid($"Missing required field: {propertyName}.");
        try
        {
            var text = node.GetValue<string>();
            if (string.IsNullOrWhiteSpace(text)) throw Invalid($"Field must be a non-empty string: {propertyName}.");
            return text.Trim();
        }
        catch (InvalidOperationException)
        {
            throw Invalid($"Field must be a string: {propertyName}.");
        }
    }

    private static string? OptionalString(JsonObject value, string propertyName)
    {
        if (!value.TryGetPropertyValue(propertyName, out var node) || node is null) return null;
        try
        {
            var text = node.GetValue<string>();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch (InvalidOperationException)
        {
            throw Invalid($"Field must be a string when supplied: {propertyName}.");
        }
    }

    private static void RequireExpectedVersion(long? expected, LanCurrentState current)
    {
        if (expected is null || expected.Value != current.EntityVersion)
            throw VersionConflict($"Expected entity version {expected?.ToString() ?? "<none>"}, current version is {current.EntityVersion}.");
    }

    private static void ValidateEmployeeStatus(string status)
    {
        if (status is not ("ACTIVE" or "INACTIVE" or "LEFT" or "ARCHIVED"))
            throw Invalid("Employee status must be ACTIVE, INACTIVE, LEFT, or ARCHIVED.");
    }

    private static bool IsEmployeeCodeUniqueClaimConflict(LanLocalCommandException error) =>
        string.Equals(error.Code, "LOCAL_COMMAND_COMMIT_FAILED", StringComparison.Ordinal) &&
        error.InnerException is SqliteException sqlite &&
        sqlite.Message.Contains("VHDCHY_EMPLOYEE_CODE_", StringComparison.Ordinal);

    private static void RequireBounded(string? value, string code, int min, int max)
    {
        var length = value?.Length ?? 0;
        if (length < min || length > max)
            throw new Slice1BusinessException(code, code == "AUTH_REQUIRED" ? "Authenticated user is required." : "Required request identity is missing or out of bounds.");
    }

    private static string EmployeeStateKey(string employeeId) => $"employee:{employeeId}";
    private static string EmployeeCodeStateKey(string employeeCodeId) => $"employee_code:{employeeCodeId}";
    private static string AttendanceStateKey(string employeeId) => $"attendance:{employeeId}";

    private static Slice1BusinessException Invalid(string message) => new("INVALID_INPUT", message);
    private static Slice1BusinessException VersionConflict(string message) => new("ENTITY_VERSION_CONFLICT", message);

    private static readonly IReadOnlySet<string> ClientAuthorityFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "actor", "actorUserId", "authenticatedUserId", "permission", "permissionResource",
        "permissionAction", "eventCode", "eventIntent", "entityType", "stateKey", "authoritySnapshotVersion"
    };

    private static readonly IReadOnlySet<string> EmployeeCreateFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "employeeId", "fullName", "phone", "status", "mainPosition", "vendor", "department",
        "site", "warehouse", "startDate", "permanentLeaveDate", "note"
    };

    private static readonly IReadOnlySet<string> EmployeeUpdateFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "employeeId", "fullName", "phone", "mainPosition", "vendor", "department",
        "site", "warehouse", "startDate", "permanentLeaveDate", "note"
    };

    private static readonly IReadOnlySet<string> EmployeeStatusFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "employeeId", "status", "permanentLeaveDate", "reason"
    };

    private static readonly IReadOnlySet<string> EmployeeCodeFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "employeeCodeId", "employeeId", "employeeCode"
    };

    private static readonly IReadOnlySet<string> AttendanceScanFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "employeeId", "businessDate", "occurredAt", "source"
    };

    private static readonly IReadOnlySet<string> AttendanceCorrectionFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "employeeId", "businessDate", "occurredAt", "currentState", "reason", "source"
    };

    private sealed record PreparedCommand(string StateKey, string NextStateJson);
    private sealed record ActiveEmployeeCode(string EntityId, string EmployeeId, string EmployeeCode);
}

public sealed record Slice1BusinessCommandRequest(
    string AuthenticatedUserId,
    string RequestId,
    string IdempotencyKey,
    string CommandCode,
    string EntityId,
    long? ExpectedEntityVersion,
    string PayloadJson,
    string? DeviceId = null,
    long? DeviceSeq = null);

public sealed record Slice1BusinessAdapterInspection(
    bool Ready,
    int CatalogCommandCount,
    IReadOnlyList<string> SupportedCommandCodes,
    IReadOnlyList<Slice1BusinessAdapterBlocker> Blockers);

public sealed record Slice1BusinessAdapterBlocker(string Code, string Message);

public sealed class Slice1BusinessException : InvalidOperationException
{
    public Slice1BusinessException(string code, string message) : base(message) => Code = code;
    public Slice1BusinessException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
