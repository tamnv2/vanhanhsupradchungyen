using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class LanAttendanceScanContextCoordinator
{
    public const string RouteTarget = "/api/v1/attendance/scan-context";
    public const string Method = "POST";
    public const string PermissionResource = "attendance";
    public const string PermissionAction = "scan";
    public const int MaxEmployeeCodeLength = 120;

    private readonly string _clusterId;
    private readonly LanClientSecurityStore _clientSecurity;
    private readonly LanUserSessionStore _sessions;
    private readonly LanAuthorizationEvaluator _authorization;
    private readonly LanAttendanceScanContextStore _store;

    public LanAttendanceScanContextCoordinator(
        string databasePath,
        string expectedClusterId,
        string expectedDomainContractVersion)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        if (string.IsNullOrWhiteSpace(expectedClusterId)) throw new ArgumentException("Cluster ID is required", nameof(expectedClusterId));
        if (string.IsNullOrWhiteSpace(expectedDomainContractVersion)) throw new ArgumentException("Domain contract version is required", nameof(expectedDomainContractVersion));

        _clusterId = expectedClusterId.Trim();
        _clientSecurity = new LanClientSecurityStore(databasePath);
        _sessions = new LanUserSessionStore(databasePath);
        _authorization = new LanAuthorizationEvaluator(databasePath, expectedDomainContractVersion);
        _store = new LanAttendanceScanContextStore(databasePath);
    }

    public async Task<LanAttendanceScanContextResult> ExecuteAsync(
        LanAttendanceScanContextRequest request,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var proof = request.SignedProof;
        if (!string.Equals(proof.Method.Trim(), Method, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(proof.RequestTarget.Trim(), RouteTarget, StringComparison.Ordinal))
        {
            throw Failure("SIGNED_REQUEST_TARGET_INVALID", "Signed request method/target does not match attendance scan-context route.");
        }

        var actualBodyHash = LanClientSecurityStore.Sha256Hex(request.RawRequestBody);
        if (!string.Equals(proof.BodySha256, actualBodyHash, StringComparison.OrdinalIgnoreCase))
            throw Failure("SIGNED_REQUEST_BODY_MISMATCH", "Signed request body digest does not match the received request body.");

        var employeeCode = ParseEmployeeCode(request.RawRequestBody);

        LanClientSecurityDecision channel;
        try
        {
            channel = await _clientSecurity.VerifySignedRequestAsync(proof, now, cancellationToken);
        }
        catch (LanClientSecurityException error)
        {
            throw Failure(error.Code, "Paired-device request authentication failed.", error);
        }
        if (!channel.Authorized)
            throw Failure(channel.Code, "Paired-device request authentication failed.");

        LanUserSessionDecision session;
        try
        {
            session = await _sessions.AuthenticateAsync(
                request.SessionToken,
                proof.DeviceId,
                proof.SecurityEpoch,
                now,
                cancellationToken);
        }
        catch (LanUserSessionException error)
        {
            throw Failure(error.Code, "LAN user session authentication failed.", error);
        }
        if (!session.Authenticated || session.Principal is null)
            throw Failure(session.Code, "LAN user session authentication failed.");
        if (session.Principal.MustChangePassword)
            throw Failure(
                "PASSWORD_CHANGE_REQUIRED",
                "The authenticated account must establish a new permanent password before attendance scan lookup.");

        var authorization = await _authorization.AuthorizeAsync(
            new LanAuthorizationRequest(
                session.Principal.UserId,
                PermissionResource,
                PermissionAction,
                _clusterId,
                Slice1BusinessAdapter.ModuleId,
                MinimumSecurityLevel: null,
                AsOf: now),
            cancellationToken);
        if (!authorization.Allowed)
            throw Failure("PERMISSION_DENIED", "Attendance scan permission is required.");

        try
        {
            return await _store.ResolveAsync(employeeCode, cancellationToken);
        }
        catch (LanAttendanceScanContextException)
        {
            throw;
        }
        catch (Exception error) when (error is SqliteException or JsonException or InvalidOperationException)
        {
            throw Failure("RUNTIME_DEPENDENCY_UNAVAILABLE", "Attendance scan context state could not be read.", error);
        }
    }

    public static string ParseEmployeeCode(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody)) throw Failure("INVALID_INPUT", "Scan context body is required.");
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Failure("INVALID_INPUT", "Scan context body must be a JSON object.");

            var fields = root.EnumerateObject().ToArray();
            if (fields.Length != 1 || !string.Equals(fields[0].Name, "employeeCode", StringComparison.Ordinal))
                throw Failure("INVALID_INPUT", "Scan context accepts employeeCode only.");
            if (fields[0].Value.ValueKind != JsonValueKind.String)
                throw Failure("INVALID_INPUT", "employeeCode is required.");

            var employeeCode = fields[0].Value.GetString()?.Trim() ?? string.Empty;
            if (employeeCode.Length is < 1 or > MaxEmployeeCodeLength)
                throw Failure("INVALID_INPUT", "employeeCode is missing or out of bounds.");
            foreach (var character in employeeCode)
            {
                if (character < 0x20 || character == 0x7f)
                    throw Failure("INVALID_INPUT", "employeeCode contains control characters.");
            }
            return employeeCode;
        }
        catch (LanAttendanceScanContextException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw Failure("INVALID_INPUT", "Scan context body is invalid JSON.", error);
        }
    }

    private static void ValidateRequest(LanAttendanceScanContextRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SignedProof);
        if (string.IsNullOrWhiteSpace(request.SessionToken)) throw new ArgumentException("Session token is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.RawRequestBody)) throw new ArgumentException("Raw request body is required", nameof(request));
    }

    private static LanAttendanceScanContextException Failure(string code, string message, Exception? inner = null) =>
        inner is null
            ? new LanAttendanceScanContextException(code, message)
            : new LanAttendanceScanContextException(code, message, inner);
}

public sealed class LanAttendanceScanContextStore
{
    private readonly string _connectionString;

    public LanAttendanceScanContextStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
    }

    public async Task<LanAttendanceScanContextResult> ResolveAsync(
        string employeeCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(employeeCode) || employeeCode.Length > LanAttendanceScanContextCoordinator.MaxEmployeeCodeLength)
            throw new LanAttendanceScanContextException("INVALID_INPUT", "employeeCode is invalid.");

        await using var connection = await OpenAsync(cancellationToken);
        var codes = await ReadCodeMatchesAsync(connection, employeeCode, cancellationToken);
        if (codes.Count == 0)
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_NOT_FOUND", "No active employee assignment exists for the supplied MNV.");
        if (codes.Count != 1)
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", "Active MNV resolution is ambiguous.");

        var code = codes[0];
        if (!string.Equals(code.Status, "ACTIVE", StringComparison.Ordinal) ||
            !string.Equals(code.EmployeeCode, employeeCode, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(code.EmployeeCodeId) ||
            string.IsNullOrWhiteSpace(code.EmployeeId))
        {
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", "Resolved employee-code state is inconsistent.");
        }

        var employee = await ReadEmployeeAsync(connection, code.EmployeeId, cancellationToken);
        if (employee is null)
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", "Active employee-code state references a missing employee.");
        if (!string.Equals(employee.Status, "ACTIVE", StringComparison.Ordinal))
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_NOT_FOUND", "No active employee assignment exists for the supplied MNV.");
        if (!string.Equals(employee.EmployeeId, code.EmployeeId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(employee.FullName))
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", "Resolved employee state is inconsistent.");
        if (employee.CurrentPortraitMediaId is not null && employee.CurrentPortraitMediaId.Length == 0)
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", "Resolved portrait reference is invalid.");

        var presence = await ReadPresenceAsync(connection, code.EmployeeId, cancellationToken);
        if (presence is not null)
        {
            if (!string.Equals(presence.EmployeeId, code.EmployeeId, StringComparison.Ordinal) ||
                presence.CurrentState is not ("IN" or "OUT") ||
                string.IsNullOrWhiteSpace(presence.BusinessDate) ||
                presence.EntityVersion < 1)
            {
                throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", "Resolved presence state is invalid.");
            }
        }

        return new LanAttendanceScanContextResult(
            code.EmployeeCodeId,
            code.EmployeeCode,
            code.EmployeeId,
            employee.FullName,
            employee.CurrentPortraitMediaId,
            presence is null
                ? null
                : new LanAttendancePresenceContext(presence.CurrentState, presence.BusinessDate, presence.EntityVersion));
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        await pragma.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }

    private static async Task<IReadOnlyList<EmployeeCodeState>> ReadCodeMatchesAsync(
        SqliteConnection connection,
        string employeeCode,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT state_json
            FROM module_current_state
            WHERE module_id=$module
              AND entity_type='employee_code'
              AND json_extract(state_json,'$.employeeCode')=$employeeCode
              AND json_extract(state_json,'$.status')='ACTIVE'
            ORDER BY state_key
            LIMIT 3
            """;
        command.Parameters.AddWithValue("$module", Slice1BusinessAdapter.ModuleId);
        command.Parameters.AddWithValue("$employeeCode", employeeCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<EmployeeCodeState>();
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(ParseEmployeeCode(reader.GetString(0)));
        return rows;
    }

    private static async Task<EmployeeState?> ReadEmployeeAsync(
        SqliteConnection connection,
        string employeeId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT state_json
            FROM module_current_state
            WHERE module_id=$module AND entity_type='employee' AND entity_id=$employeeId
            LIMIT 2
            """;
        command.Parameters.AddWithValue("$module", Slice1BusinessAdapter.ModuleId);
        command.Parameters.AddWithValue("$employeeId", employeeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var result = ParseEmployee(reader.GetString(0));
        if (await reader.ReadAsync(cancellationToken))
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", "Employee materialized state is duplicated.");
        return result;
    }

    private static async Task<PresenceState?> ReadPresenceAsync(
        SqliteConnection connection,
        string employeeId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT entity_version,state_json
            FROM module_current_state
            WHERE module_id=$module AND entity_type='attendance' AND entity_id=$employeeId
            LIMIT 2
            """;
        command.Parameters.AddWithValue("$module", Slice1BusinessAdapter.ModuleId);
        command.Parameters.AddWithValue("$employeeId", employeeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var version = reader.GetInt64(0);
        var result = ParsePresence(reader.GetString(1), version);
        if (await reader.ReadAsync(cancellationToken))
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", "Presence materialized state is duplicated.");
        return result;
    }

    private static EmployeeCodeState ParseEmployeeCode(string stateJson)
    {
        using var document = JsonDocument.Parse(stateJson);
        var root = document.RootElement;
        return new EmployeeCodeState(
            RequiredString(root, "employeeCodeId"),
            RequiredString(root, "employeeId"),
            RequiredString(root, "employeeCode"),
            RequiredString(root, "status"));
    }

    private static EmployeeState ParseEmployee(string stateJson)
    {
        using var document = JsonDocument.Parse(stateJson);
        var root = document.RootElement;
        return new EmployeeState(
            RequiredString(root, "employeeId"),
            RequiredString(root, "fullName"),
            RequiredString(root, "status"),
            OptionalString(root, "currentPortraitMediaId"));
    }

    private static PresenceState ParsePresence(string stateJson, long entityVersion)
    {
        using var document = JsonDocument.Parse(stateJson);
        var root = document.RootElement;
        if (root.TryGetProperty("entityVersion", out var jsonVersion) &&
            (jsonVersion.ValueKind != JsonValueKind.Number || !jsonVersion.TryGetInt64(out var parsed) || parsed != entityVersion))
        {
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", "Presence entity version evidence is inconsistent.");
        }
        return new PresenceState(
            RequiredString(root, "employeeId"),
            RequiredString(root, "currentState"),
            RequiredString(root, "businessDate"),
            entityVersion);
    }

    private static string RequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", $"Materialized scan-context state is missing {propertyName}.");
        return value.GetString()!.Trim();
    }

    private static string? OptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new LanAttendanceScanContextException("SCAN_CONTEXT_CONFLICT", $"Materialized scan-context state has invalid {propertyName}.");
        return value.GetString()!.Trim();
    }

    private sealed record EmployeeCodeState(string EmployeeCodeId, string EmployeeId, string EmployeeCode, string Status);
    private sealed record EmployeeState(string EmployeeId, string FullName, string Status, string? CurrentPortraitMediaId);
    private sealed record PresenceState(string EmployeeId, string CurrentState, string BusinessDate, long EntityVersion);
}

public sealed record LanAttendanceScanContextRequest(
    string SessionToken,
    LanClientSignedRequestProof SignedProof,
    string RawRequestBody);

public sealed record LanAttendanceScanContextResult(
    string EmployeeCodeId,
    string EmployeeCode,
    string EmployeeId,
    string FullName,
    string? CurrentPortraitMediaId,
    LanAttendancePresenceContext? Presence);

public sealed record LanAttendancePresenceContext(
    string CurrentState,
    string BusinessDate,
    long EntityVersion);

public sealed class LanAttendanceScanContextException : InvalidOperationException
{
    public LanAttendanceScanContextException(string code, string message) : base(message) => Code = code;
    public LanAttendanceScanContextException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
