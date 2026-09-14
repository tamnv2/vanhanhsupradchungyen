namespace Vhdchy.LanService;

public sealed class LanBusinessRouteCoordinator
{
    public const string BusinessCommandRouteTarget = "/api/v1/data/commands";
    public const string BusinessCommandMethod = "POST";

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
                    RequestId: request.RequestId,
                    IdempotencyKey: request.IdempotencyKey,
                    CommandCode: request.CommandCode,
                    EntityId: request.EntityId,
                    ExpectedEntityVersion: request.ExpectedEntityVersion,
                    PayloadJson: request.PayloadJson,
                    DeviceId: proof.DeviceId,
                    DeviceSeq: request.DeviceSeq),
                cancellationToken);
        }
        catch (Slice1BusinessException error)
        {
            throw new LanBusinessRouteException(error.Code, error.Message, error);
        }
    }

    private static void ValidateRequest(LanBusinessRouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SignedProof);
        Require(request.SessionToken, nameof(request.SessionToken));
        Require(request.RawRequestBody, nameof(request.RawRequestBody), allowEmpty: true);
        Require(request.RequestId, nameof(request.RequestId));
        Require(request.IdempotencyKey, nameof(request.IdempotencyKey));
        Require(request.CommandCode, nameof(request.CommandCode));
        Require(request.EntityId, nameof(request.EntityId));
        Require(request.PayloadJson, nameof(request.PayloadJson), allowEmpty: true);
        if (request.DeviceSeq is < 0) throw new ArgumentOutOfRangeException(nameof(request.DeviceSeq));
    }

    private static void Require(string? value, string name, bool allowEmpty = false)
    {
        if (value is null || (!allowEmpty && string.IsNullOrWhiteSpace(value)))
            throw new ArgumentException($"{name} is required", name);
    }
}

public sealed record LanBusinessRouteRequest(
    string SessionToken,
    LanClientSignedRequestProof SignedProof,
    string RawRequestBody,
    string RequestId,
    string IdempotencyKey,
    string CommandCode,
    string EntityId,
    long? ExpectedEntityVersion,
    string PayloadJson,
    long? DeviceSeq = null);

public sealed class LanBusinessRouteException : InvalidOperationException
{
    public LanBusinessRouteException(string code, string message) : base(message) => Code = code;
    public LanBusinessRouteException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
