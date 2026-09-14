using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Vhdchy.LanService;

public sealed class CloudReconciliationHttpSender
{
    public const string AuthVersion = "VHDCHY_RECONCILIATION_HMAC_V1";
    public static readonly TimeSpan ReceivedRecheckDelay = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;
    private readonly string _environment;
    private readonly string _keyId;
    private readonly byte[] _keyMaterial;

    public CloudReconciliationHttpSender(
        HttpClient httpClient,
        Uri endpoint,
        string environment,
        string keyId,
        string keyMaterial)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _environment = RequireText(environment, nameof(environment), 32).ToUpperInvariant();
        _keyId = RequireText(keyId, nameof(keyId), 128);
        var material = RequireText(keyMaterial, nameof(keyMaterial), 4096);
        if (material.Length < 32) throw new ArgumentException("Machine authentication key material must be at least 32 characters.", nameof(keyMaterial));
        _keyMaterial = Encoding.UTF8.GetBytes(material);

        if (!_endpoint.IsAbsoluteUri || !string.Equals(_endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Cloud reconciliation endpoint must use HTTPS.", nameof(endpoint));
        if (!string.Equals(_endpoint.AbsolutePath, "/api/v1/reconciliation/events", StringComparison.Ordinal) || !string.IsNullOrEmpty(_endpoint.Query))
            throw new ArgumentException("Cloud reconciliation endpoint path must be exactly /api/v1/reconciliation/events with no query string.", nameof(endpoint));
    }

    public async Task<CloudReconciliationHttpResult> SendAsync(
        LanCloudReconciliationTransportEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (!string.Equals(envelope.Environment, _environment, StringComparison.Ordinal))
            return CloudReconciliationHttpResult.Rejected("ENVIRONMENT_MISMATCH", "Envelope environment does not match sender environment.");

        var rawBody = JsonSerializer.Serialize(envelope, JsonOptions);
        var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        var canonical = BuildCanonicalRequest(timestampMs, nonce, bodyHash);
        var signature = Convert.ToHexString(HMACSHA256.HashData(_keyMaterial, Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(rawBody, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-VHDCHY-Machine-Key-Id", _keyId);
        request.Headers.TryAddWithoutValidation("X-VHDCHY-Machine-Timestamp", timestampMs.ToString());
        request.Headers.TryAddWithoutValidation("X-VHDCHY-Machine-Nonce", nonce);
        request.Headers.TryAddWithoutValidation("X-VHDCHY-Content-SHA256", bodyHash);
        request.Headers.TryAddWithoutValidation("X-VHDCHY-Machine-Signature", signature);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CloudReconciliationHttpResult.Retry("CLOUD_TIMEOUT", "Cloud reconciliation request timed out.");
        }
        catch (HttpRequestException)
        {
            return CloudReconciliationHttpResult.Retry("CLOUD_NETWORK_ERROR", "Cloud reconciliation endpoint is unavailable.");
        }

        using (response)
        {
            string payload;
            try
            {
                payload = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch
            {
                return CloudReconciliationHttpResult.Retry("CLOUD_RESPONSE_READ_FAILED", "Cloud reconciliation response could not be read.");
            }

            if (payload.Length > 64 * 1024)
                return CloudReconciliationHttpResult.Retry("CLOUD_RESPONSE_TOO_LARGE", "Cloud reconciliation response exceeded the accepted limit.");

            JsonElement root;
            try
            {
                using var document = JsonDocument.Parse(payload);
                root = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return CloudReconciliationHttpResult.Retry("CLOUD_RESPONSE_INVALID", "Cloud reconciliation response was not valid JSON.");
            }

            var ok = root.TryGetProperty("ok", out var okElement) && okElement.ValueKind == JsonValueKind.True;
            if (response.IsSuccessStatusCode && ok)
            {
                var status = ReadString(root, "reconciliationStatus") ?? "RECEIVED";
                var edgeEventId = ReadString(root, "edgeEventId");
                var duplicate = root.TryGetProperty("duplicate", out var duplicateElement) && duplicateElement.ValueKind == JsonValueKind.True;
                if (status is "LAN_RECONCILED_CLOUD_COMMITTED" or "RECONCILED")
                {
                    var canonicalEventId = ReadString(root, "canonicalEventId");
                    var committedAtText = ReadString(root, "canonicalCommittedAt");
                    if (!string.IsNullOrWhiteSpace(canonicalEventId) && DateTimeOffset.TryParse(committedAtText, out var committedAt))
                        return CloudReconciliationHttpResult.Reconciled(canonicalEventId, committedAt, duplicate);
                }
                return CloudReconciliationHttpResult.Received(edgeEventId, duplicate);
            }

            var error = root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.Object
                ? errorElement
                : default;
            var code = error.ValueKind == JsonValueKind.Object ? ReadString(error, "code") : null;
            var reason = error.ValueKind == JsonValueKind.Object ? ReadString(error, "reason") : null;
            code ??= $"CLOUD_HTTP_{(int)response.StatusCode}";
            reason ??= "Cloud reconciliation rejected the request.";

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return CloudReconciliationHttpResult.Retry("CLOUD_MACHINE_AUTH_REJECTED", reason);
            if (response.StatusCode == HttpStatusCode.Conflict || response.StatusCode == HttpStatusCode.UnprocessableEntity)
                return CloudReconciliationHttpResult.Conflict(code, reason);
            if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
                return CloudReconciliationHttpResult.Retry(code, reason);
            return CloudReconciliationHttpResult.Rejected(code, reason);
        }
    }

    private string BuildCanonicalRequest(long timestampMs, string nonce, string bodyHash) =>
        string.Join('\n',
            AuthVersion,
            "POST",
            _endpoint.AbsolutePath,
            timestampMs.ToString(),
            nonce,
            bodyHash,
            _environment,
            _keyId);

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string RequireText(string value, string name, int max)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0 || normalized.Length > max) throw new ArgumentException($"{name} is required and must be <= {max} characters.", name);
        return normalized;
    }
}

public sealed class CloudReconciliationPump
{
    private readonly CloudSyncQueueStore _queueStore;
    private readonly CloudSyncTransportEnvelopeBuilder _envelopeBuilder;
    private readonly CloudReconciliationHttpSender _sender;

    public CloudReconciliationPump(
        CloudSyncQueueStore queueStore,
        CloudSyncTransportEnvelopeBuilder envelopeBuilder,
        CloudReconciliationHttpSender sender)
    {
        _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));
        _envelopeBuilder = envelopeBuilder ?? throw new ArgumentNullException(nameof(envelopeBuilder));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public async Task<int> RunOnceAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var claims = await _queueStore.ClaimDueAsync(limit, cancellationToken);
        foreach (var claim in claims)
        {
            CloudReconciliationHttpResult result;
            try
            {
                var envelope = await _envelopeBuilder.BuildAsync(claim, cancellationToken);
                result = await _sender.SendAsync(envelope, cancellationToken);
            }
            catch (LanCloudSyncException error)
            {
                await _queueStore.MarkConflictAsync(
                    claim.OutboxId,
                    error.Code,
                    JsonSerializer.Serialize(new { source = "LAN_SEND_PRECONDITION", error = error.Code }, JsonOptions),
                    cancellationToken);
                continue;
            }

            switch (result.Outcome)
            {
                case CloudReconciliationHttpOutcome.Reconciled:
                    await _queueStore.MarkReconciledAsync(
                        claim.OutboxId,
                        result.CanonicalEventId!,
                        result.CanonicalCommittedAt!.Value,
                        cancellationToken);
                    break;
                case CloudReconciliationHttpOutcome.Received:
                    await _queueStore.MarkRetryAsync(
                        claim.OutboxId,
                        "CLOUD_RECEIVED_PENDING_CANONICAL",
                        DateTimeOffset.UtcNow.Add(CloudReconciliationHttpSender.ReceivedRecheckDelay),
                        cancellationToken);
                    break;
                case CloudReconciliationHttpOutcome.Conflict:
                case CloudReconciliationHttpOutcome.Rejected:
                    await _queueStore.MarkConflictAsync(
                        claim.OutboxId,
                        result.ErrorCode ?? "CLOUD_RECONCILIATION_REJECTED",
                        JsonSerializer.Serialize(new
                        {
                            source = "CLOUD_RECONCILIATION",
                            code = result.ErrorCode,
                            reason = result.Reason
                        }, JsonOptions),
                        cancellationToken);
                    break;
                default:
                    var delaySeconds = Math.Min(300, Math.Max(5, 5 * Math.Pow(2, Math.Min(6, claim.AttemptCount - 1))));
                    await _queueStore.MarkRetryAsync(
                        claim.OutboxId,
                        result.ErrorCode ?? "CLOUD_RECONCILIATION_RETRY",
                        DateTimeOffset.UtcNow.AddSeconds(delaySeconds),
                        cancellationToken);
                    break;
            }
        }
        return claims.Count;
    }

    public async Task RunAsync(TimeSpan pollInterval, CancellationToken cancellationToken)
    {
        if (pollInterval < TimeSpan.FromSeconds(1)) throw new ArgumentOutOfRangeException(nameof(pollInterval));
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(10, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Keep the pump alive. Individual claim state transitions remain durable and restart recovery handles interrupted claims.
            }

            try
            {
                await Task.Delay(pollInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

public enum CloudReconciliationHttpOutcome
{
    Received,
    Reconciled,
    Retry,
    Conflict,
    Rejected
}

public sealed record CloudReconciliationHttpResult(
    CloudReconciliationHttpOutcome Outcome,
    string? ErrorCode,
    string? Reason,
    string? EdgeEventId,
    string? CanonicalEventId,
    DateTimeOffset? CanonicalCommittedAt,
    bool Duplicate)
{
    public static CloudReconciliationHttpResult Received(string? edgeEventId, bool duplicate) =>
        new(CloudReconciliationHttpOutcome.Received, null, null, edgeEventId, null, null, duplicate);

    public static CloudReconciliationHttpResult Reconciled(string canonicalEventId, DateTimeOffset committedAt, bool duplicate) =>
        new(CloudReconciliationHttpOutcome.Reconciled, null, null, null, canonicalEventId, committedAt, duplicate);

    public static CloudReconciliationHttpResult Retry(string code, string reason) =>
        new(CloudReconciliationHttpOutcome.Retry, code, reason, null, null, null, false);

    public static CloudReconciliationHttpResult Conflict(string code, string reason) =>
        new(CloudReconciliationHttpOutcome.Conflict, code, reason, null, null, null, false);

    public static CloudReconciliationHttpResult Rejected(string code, string reason) =>
        new(CloudReconciliationHttpOutcome.Rejected, code, reason, null, null, null, false);
}
