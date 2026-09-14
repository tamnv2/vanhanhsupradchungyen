using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Vhdchy.LanService;

public static class LanSecureHttpRoutes
{
    public const string LoginRouteTarget = "/api/v1/auth/login";
    public const string BusinessRouteTarget = LanBusinessRouteCoordinator.BusinessCommandRouteTarget;
    public const string DeviceIdHeader = "X-VHDCHY-Device-Id";
    public const string SecurityEpochHeader = "X-VHDCHY-Security-Epoch";
    public const string TimestampHeader = "X-VHDCHY-Timestamp-Ms";
    public const string NonceHeader = "X-VHDCHY-Nonce";
    public const string SignatureHeader = "X-VHDCHY-Signature";
    public const int MaxSignedBodyBytes = 128 * 1024;

    public static async Task MapAsync(
        WebApplication app,
        string databasePath,
        string expectedEnvironment,
        string expectedClusterId,
        string expectedDomainContractVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        var security = new LanClientSecurityStore(databasePath);
        await security.EnsureAsync(cancellationToken);
        var sessions = new LanUserSessionStore(databasePath);
        await sessions.EnsureAsync(cancellationToken);
        var credentials = new LanPrimaryCredentialVerifier(databasePath, expectedDomainContractVersion);
        var business = new LanBusinessRouteCoordinator(
            databasePath,
            expectedEnvironment,
            expectedClusterId,
            expectedDomainContractVersion);
        var readiness = new LanReadinessEvaluator(
            databasePath,
            expectedEnvironment,
            expectedClusterId,
            expectedDomainContractVersion,
            new[] { Slice1BusinessAdapter.ModuleId },
            securePublicRouteWiringEnabled: true);

        app.MapPost(LoginRouteTarget, async (HttpRequest request, CancellationToken ct) =>
        {
            var requestId = RequestId(request);
            if (!request.IsHttps)
                return Error("SECURE_TRANSPORT_REQUIRED", StatusCodes.Status503ServiceUnavailable, requestId);

            try
            {
                var signedBody = await ReadSignedBodyAsync(request, ct);
                var proof = BuildProof(request, LoginRouteTarget, signedBody.BodySha256);
                var channel = await security.VerifySignedRequestAsync(proof, cancellationToken: ct);
                if (!channel.Authorized)
                    return Error(channel.Code, StatusCodes.Status401Unauthorized, requestId);

                var loginBody = ParseLoginBody(signedBody.RawBody);
                var decision = await credentials.VerifyAsync(loginBody.Username, loginBody.Password, ct);
                if (!decision.Authenticated || decision.User is null)
                {
                    var status = IsAuthorityDependencyError(decision.Code)
                        ? StatusCodes.Status503ServiceUnavailable
                        : StatusCodes.Status401Unauthorized;
                    return Error(decision.Code, status, requestId);
                }

                var issued = await sessions.IssueFromAuthenticatedEvidenceAsync(
                    decision.User.UserId,
                    proof.DeviceId,
                    proof.SecurityEpoch,
                    decision.User.AuthoritySnapshotVersion,
                    decision.User.MustChangePassword,
                    cancellationToken: ct);

                return Results.Json(new
                {
                    ok = true,
                    runtime = "LAN",
                    requestId,
                    session = new
                    {
                        token = issued.Token,
                        expiresAt = issued.ExpiresAt,
                        deviceId = issued.DeviceId,
                        securityEpoch = issued.SecurityEpoch,
                        authoritySnapshotVersion = issued.AuthoritySnapshotVersion,
                        mustChangePassword = issued.MustChangePassword
                    },
                    user = new
                    {
                        userId = decision.User.UserId,
                        username = decision.User.Username,
                        employeeId = decision.User.EmployeeId,
                        displayName = decision.User.DisplayName,
                        securityLevel = decision.User.SecurityLevel
                    }
                });
            }
            catch (LanSecureHttpException error)
            {
                return Error(error.Code, error.StatusCode, requestId);
            }
            catch (LanClientSecurityException error)
            {
                return Error(error.Code, StatusCodes.Status401Unauthorized, requestId);
            }
            catch (LanUserSessionException error)
            {
                var status = IsAuthorityDependencyError(error.Code)
                    ? StatusCodes.Status503ServiceUnavailable
                    : StatusCodes.Status401Unauthorized;
                return Error(error.Code, status, requestId);
            }
        });

        app.MapPost(BusinessRouteTarget, async (HttpRequest request, CancellationToken ct) =>
        {
            var requestId = RequestId(request);
            if (!request.IsHttps)
                return Error("SECURE_TRANSPORT_REQUIRED", StatusCodes.Status503ServiceUnavailable, requestId);

            try
            {
                var report = await readiness.EvaluateAsync(ct);
                if (!report.Ready)
                {
                    return Results.Json(new
                    {
                        ok = false,
                        runtime = "LAN",
                        requestId,
                        error = new
                        {
                            code = "RUNTIME_DEPENDENCY_UNAVAILABLE",
                            blockers = report.Blockers.Select(item => item.Code).Distinct(StringComparer.Ordinal).ToArray()
                        }
                    }, statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                var signedBody = await ReadSignedBodyAsync(request, ct);
                var proof = BuildProof(request, BusinessRouteTarget, signedBody.BodySha256);
                var token = BearerToken(request);
                var result = await business.ExecuteAsync(
                    new LanBusinessRouteRequest(token, proof, signedBody.RawBody),
                    cancellationToken: ct);

                return Results.Json(new
                {
                    ok = true,
                    runtime = "LAN",
                    requestId,
                    result
                });
            }
            catch (LanSecureHttpException error)
            {
                return Error(error.Code, error.StatusCode, requestId);
            }
            catch (LanClientSecurityException error)
            {
                return Error(error.Code, StatusCodes.Status401Unauthorized, requestId);
            }
            catch (LanBusinessRouteException error)
            {
                return Error(error.Code, BusinessStatus(error.Code), requestId);
            }
        });
    }

    private static async Task<SignedBody> ReadSignedBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is > MaxSignedBodyBytes)
            throw new LanSecureHttpException("REQUEST_TOO_LARGE", StatusCodes.Status413PayloadTooLarge);

        await using var buffer = new MemoryStream();
        await request.Body.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0)
            throw new LanSecureHttpException("INVALID_INPUT", StatusCodes.Status422UnprocessableEntity);
        if (buffer.Length > MaxSignedBodyBytes)
            throw new LanSecureHttpException("REQUEST_TOO_LARGE", StatusCodes.Status413PayloadTooLarge);

        var bytes = buffer.ToArray();
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            throw new LanSecureHttpException("INVALID_INPUT", StatusCodes.Status422UnprocessableEntity);

        string rawBody;
        try
        {
            rawBody = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new LanSecureHttpException("INVALID_INPUT", StatusCodes.Status422UnprocessableEntity);
        }

        var bodyHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return new SignedBody(rawBody, bodyHash);
    }

    private static LanClientSignedRequestProof BuildProof(HttpRequest request, string requiredTarget, string bodySha256)
    {
        if (request.QueryString.HasValue)
            throw new LanSecureHttpException("SIGNED_REQUEST_TARGET_INVALID", StatusCodes.Status401Unauthorized);
        if (!string.Equals(request.Path.Value, requiredTarget, StringComparison.Ordinal))
            throw new LanSecureHttpException("SIGNED_REQUEST_TARGET_INVALID", StatusCodes.Status401Unauthorized);

        var deviceId = RequiredHeader(request, DeviceIdHeader);
        var epoch = RequiredHeader(request, SecurityEpochHeader);
        var nonce = RequiredHeader(request, NonceHeader);
        var signature = RequiredHeader(request, SignatureHeader);
        var timestampText = RequiredHeader(request, TimestampHeader);
        if (!long.TryParse(timestampText, NumberStyles.None, CultureInfo.InvariantCulture, out var timestampUnixMs))
            throw new LanSecureHttpException("REQUEST_TIMESTAMP_INVALID", StatusCodes.Status401Unauthorized);

        return new LanClientSignedRequestProof(
            deviceId,
            epoch,
            timestampUnixMs,
            nonce,
            request.Method,
            requiredTarget,
            bodySha256,
            signature);
    }

    private static LoginBody ParseLoginBody(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw InvalidLogin();

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name is not ("username" or "password"))
                    throw InvalidLogin();
            }

            if (!root.TryGetProperty("username", out var usernameValue) ||
                usernameValue.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(usernameValue.GetString()))
                throw InvalidLogin();
            if (!root.TryGetProperty("password", out var passwordValue) ||
                passwordValue.ValueKind != JsonValueKind.String ||
                passwordValue.GetString() is null)
                throw InvalidLogin();

            var username = usernameValue.GetString()!.Trim();
            var password = passwordValue.GetString()!;
            if (username.Length > 240 || password.Length > 1024)
                throw InvalidLogin();
            return new LoginBody(username, password);
        }
        catch (LanSecureHttpException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw InvalidLogin();
        }
    }

    private static string BearerToken(HttpRequest request)
    {
        var value = request.Headers.Authorization.FirstOrDefault();
        const string prefix = "Bearer ";
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new LanSecureHttpException("AUTH_TOKEN_REQUIRED", StatusCodes.Status401Unauthorized);
        var token = value[prefix.Length..].Trim();
        if (token.Length is < 32 or > 256)
            throw new LanSecureHttpException("AUTH_TOKEN_REQUIRED", StatusCodes.Status401Unauthorized);
        return token;
    }

    private static string RequiredHeader(HttpRequest request, string name)
    {
        if (!request.Headers.TryGetValue(name, out var values) || values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
            throw new LanSecureHttpException("SIGNED_REQUEST_PROOF_REQUIRED", StatusCodes.Status401Unauthorized);
        return values[0]!.Trim();
    }

    private static string RequestId(HttpRequest request)
    {
        var value = request.Headers["X-Request-Id"].FirstOrDefault()?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 200 ? Guid.NewGuid().ToString("N") : value;
    }

    private static bool IsAuthorityDependencyError(string code) =>
        code.StartsWith("AUTHORITY_", StringComparison.Ordinal) ||
        code.StartsWith("LOGIN_AUTHORITY_", StringComparison.Ordinal);

    private static int BusinessStatus(string code) => code switch
    {
        "PERMISSION_DENIED" => StatusCodes.Status403Forbidden,
        "PASSWORD_CHANGE_REQUIRED" => StatusCodes.Status403Forbidden,
        "INVALID_INPUT" => StatusCodes.Status422UnprocessableEntity,
        "IDEMPOTENCY_CONFLICT" => StatusCodes.Status409Conflict,
        "DEVICE_SEQUENCE_COLLISION" => StatusCodes.Status409Conflict,
        "ENTITY_VERSION_CONFLICT" => StatusCodes.Status409Conflict,
        "RESOURCE_CONFLICT" => StatusCodes.Status409Conflict,
        "RUNTIME_DEPENDENCY_UNAVAILABLE" => StatusCodes.Status503ServiceUnavailable,
        "AUTHORITY_REFRESH_REAUTH_REQUIRED" => StatusCodes.Status401Unauthorized,
        "SESSION_NOT_FOUND" => StatusCodes.Status401Unauthorized,
        "SESSION_NOT_ACTIVE" => StatusCodes.Status401Unauthorized,
        "SESSION_DEVICE_MISMATCH" => StatusCodes.Status401Unauthorized,
        "SESSION_EXPIRED" => StatusCodes.Status401Unauthorized,
        "ACCOUNT_NOT_FOUND" => StatusCodes.Status401Unauthorized,
        "ACCOUNT_NOT_ACTIVE" => StatusCodes.Status401Unauthorized,
        "SECURITY_EPOCH_MISMATCH" => StatusCodes.Status401Unauthorized,
        "DEVICE_NOT_PAIRED" => StatusCodes.Status401Unauthorized,
        "DEVICE_REPAIR_REQUIRED" => StatusCodes.Status401Unauthorized,
        "DEVICE_NOT_ACTIVE" => StatusCodes.Status401Unauthorized,
        "REQUEST_REPLAY" => StatusCodes.Status401Unauthorized,
        "REQUEST_SIGNATURE_INVALID" => StatusCodes.Status401Unauthorized,
        "REQUEST_TIMESTAMP_INVALID" => StatusCodes.Status401Unauthorized,
        "REQUEST_TIMESTAMP_OUT_OF_WINDOW" => StatusCodes.Status401Unauthorized,
        "SIGNED_REQUEST_TARGET_INVALID" => StatusCodes.Status401Unauthorized,
        "SIGNED_REQUEST_BODY_MISMATCH" => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status409Conflict
    };

    private static IResult Error(string code, int statusCode, string requestId) =>
        Results.Json(new
        {
            ok = false,
            runtime = "LAN",
            requestId,
            error = new { code }
        }, statusCode: statusCode);

    private static LanSecureHttpException InvalidLogin() =>
        new("INVALID_INPUT", StatusCodes.Status422UnprocessableEntity);

    private sealed record SignedBody(string RawBody, string BodySha256);
    private sealed record LoginBody(string Username, string Password);
}

public sealed class LanSecureHttpException : InvalidOperationException
{
    public LanSecureHttpException(string code, int statusCode) : base(code)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }
}
