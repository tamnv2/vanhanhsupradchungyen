using System.Text.Json;

namespace Vhdchy.LanService;

public sealed class LanBusinessRouteCoordinator
{
    public const string BusinessCommandRouteTarget = "/api/v1/data/commands";
    public const string BusinessCommandMethod = "POST";

    private static readonly HashSet<string> AllowedTopLevelFields = new(StringComparer.Ordinal)
    {
        "requestId",
        "idempotencyKey",
        "commandCode",
        "entityId",
        "expectedEntityVersion",
        "payload",
        "deviceSeq"
    };

    private readonly LanClientSecurityStore _clientSecurity;
    private readonly LanUserSessionStore _sessions;
    private readonly Slice1BusinessAdapter _business;

    public LanBusinessRouteCoordinator(
        string databasePath,
        string expectedEnvironment,
        string expectedClusterId,
        string expectedDomainContractVersion)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        _clientSecurity = new LanClientSecurityStore(databasePath);
        _sessions = new LanUserSessionStore(databasePath);
        _business = new Slice1BusinessAdapter(
            databasePath,
            expectedEnvironment,
            expectedClusterId,
            expectedDomainContractVersion);
    }

    public async Task<LanLocalCommandResult> ExecuteAsync(
        LanBusinessRouteRequest request,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var proof = request.SignedProof;
        if (!string.Equals(proof.Method.Trim(), BusinessCommandMethod, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(proof.RequestTarget.Trim(), BusinessCommandRouteTarget, StringComparison.Ordinal))
        {
            throw new LanBusinessRouteException(
                "SIGNED_REQUEST_TARGET_INVALID",
                "Signed request method/target does not match the reviewed LAN Slice-1 business route.");
        }

        var actualBodyHash = LanClientSecurityStore.Sha256Hex(request.RawRequestBody);
        if (!string.Equals(proof.BodySha256, actualBodyHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new LanBusinessRouteException(
                "SIGNED_REQUEST_BODY_MISMATCH",
                "Signed request body digest does not match the received request body.");
        }

        var parsed = ParseSignedBody(request.RawRequestBody);

        var channel = await _clientSecurity.VerifySignedRequestAsync(proof, now, cancellationToken);
        if (!channel.Authorized)
            throw new LanBusinessRouteException(channel.Code, "Paired-device request authentication failed.");

        var session = await _sessions.AuthenticateAsync(
            request.SessionToken,
            proof.DeviceId,
            proof.SecurityEpoch,
            now,
            cancellationToken);
        if (!session.Authenticated || session.Principal is null)
            throw new LanBusinessRouteException(session.Code, "LAN user session authentication failed.");
        if (session.Principal.MustChangePassword)
            throw new LanBusinessRouteException(
                "PASSWORD_CHANGE_REQUIRED",
                "The authenticated account must establish a new permanent password before ordinary business mutations are allowed.");

        try
        {
            return await _business.ExecuteAsync(
                new Slice1BusinessCommandRequest(
                    AuthenticatedUserId: session.Principal.UserId,
                    RequestId: parsed.RequestId,
                    IdempotencyKey: parsed.IdempotencyKey,
                    CommandCode: parsed.CommandCode,
                    EntityId: parsed.EntityId,
                    ExpectedEntityVersion: parsed.ExpectedEntityVersion,
                    PayloadJson: parsed.PayloadJson,
                    DeviceId: proof.DeviceId,
                    DeviceSeq: parsed.DeviceSeq),
                cancellationToken);
        }
        catch (Slice1BusinessException error)
        {
            throw new LanBusinessRouteException(error.Code, error.Message, error);
        }
    }

    private static ParsedBusinessCommand ParseSignedBody(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw InvalidBody("Business command body must be a JSON object.");

            foreach (var property in root.EnumerateObject())
            {
                if (!AllowedTopLevelFields.Contains(property.Name))
                    throw InvalidBody($"Unsupported business command field: {property.Name}.");
            }

            var requestId = RequiredString(root, "requestId");
            var idempotencyKey = RequiredString(root, "idempotencyKey");
            var commandCode = RequiredString(root, "commandCode");
            var entityId = RequiredString(root, "entityId");
            var expectedVersion = OptionalPositiveInt64(root, "expectedEntityVersion");
            var deviceSeq = OptionalNonNegativeInt64(root, "deviceSeq");
            if (!root.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object)
                throw InvalidBody("Business command payload must be a JSON object.");

            return new ParsedBusinessCommand(
                requestId,
                idempotencyKey,
                commandCode,
                entityId,
                expectedVersion,
                payload.GetRawText(),
                deviceSeq);
        }
        catch (LanBusinessRouteException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new LanBusinessRouteException("INVALID_INPUT", "Business command body is invalid JSON.", error);
        }
    }

    private static string RequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw InvalidBody($"Required business command field is missing or invalid: {propertyName}.");
        }
        return value.GetString()!.Trim();
    }

    private static long? OptionalPositiveInt64(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var parsed) || parsed < 1)
            throw InvalidBody($"Business command field must be a positive integer when supplied: {propertyName}.");
        return parsed;
    }

    private static long? OptionalNonNegativeInt64(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var parsed) || parsed < 0)
            throw InvalidBody($"Business command field must be a non-negative integer when supplied: {propertyName}.");
        return parsed;
    }

    private static LanBusinessRouteException InvalidBody(string message) => new("INVALID_INPUT", message);

    private static void ValidateRequest(LanBusinessRouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SignedProof);
        if (string.IsNullOrWhiteSpace(request.SessionToken))
            throw new ArgumentException("Session token is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.RawRequestBody))
            throw new ArgumentException("Raw request body is required", nameof(request));
    }

    private sealed record ParsedBusinessCommand(
        string RequestId,
        string IdempotencyKey,
        string CommandCode,
        string EntityId,
        long? ExpectedEntityVersion,
        string PayloadJson,
        long? DeviceSeq);
}

public sealed record LanBusinessRouteRequest(
    string SessionToken,
    LanClientSignedRequestProof SignedProof,
    string RawRequestBody);

public sealed class LanBusinessRouteException : InvalidOperationException
{
    public LanBusinessRouteException(string code, string message) : base(message) => Code = code;
    public LanBusinessRouteException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
