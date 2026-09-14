using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Vhdchy.LanService;

public sealed class CloudOperationalSnapshotHttpClient
{
    public const string SnapshotPath = "/api/v1/reconciliation/operational-snapshot";
    public const string SnapshotContractVersion = "VHDCHY_OPERATIONAL_SNAPSHOT_V1";
    private const int MaxResponseBytes = 8 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;
    private readonly string _environment;
    private readonly string _keyId;
    private readonly byte[] _keyMaterial;

    public CloudOperationalSnapshotHttpClient(
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
            throw new ArgumentException("Cloud operational snapshot endpoint must use HTTPS.", nameof(endpoint));
        if (!string.Equals(_endpoint.AbsolutePath, SnapshotPath, StringComparison.Ordinal) || !string.IsNullOrEmpty(_endpoint.Query))
            throw new ArgumentException($"Cloud operational snapshot endpoint path must be exactly {SnapshotPath} with no query string.", nameof(endpoint));
    }

    public async Task<CloudOperationalSnapshotHttpResult> FetchAsync(
        CloudOperationalSnapshotRequest snapshotRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshotRequest);
        if (!string.Equals(snapshotRequest.Environment, _environment, StringComparison.Ordinal))
            return CloudOperationalSnapshotHttpResult.Rejected("ENVIRONMENT_MISMATCH", "Snapshot request environment does not match client environment.");

        var requestedIds = snapshotRequest.RequestedCanonicalEventIds ?? Array.Empty<string>();
        if (requestedIds.Count > 10_000 || requestedIds.Distinct(StringComparer.Ordinal).Count() != requestedIds.Count)
            return CloudOperationalSnapshotHttpResult.Rejected("CANONICAL_COVERAGE_REQUEST_INVALID", "Canonical coverage request is invalid.");

        var rawBody = JsonSerializer.Serialize(snapshotRequest, JsonOptions);
        var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        var canonical = string.Join('\n',
            CloudReconciliationHttpSender.AuthVersion,
            "POST",
            _endpoint.AbsolutePath,
            timestampMs.ToString(System.Globalization.CultureInfo.InvariantCulture),
            nonce,
            bodyHash,
            _environment,
            _keyId);
        var signature = Convert.ToHexString(HMACSHA256.HashData(_keyMaterial, Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(rawBody, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-VHDCHY-Machine-Key-Id", _keyId);
        request.Headers.TryAddWithoutValidation("X-VHDCHY-Machine-Timestamp", timestampMs.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
            return CloudOperationalSnapshotHttpResult.Retry("CLOUD_SNAPSHOT_TIMEOUT", "Cloud operational snapshot request timed out.");
        }
        catch (HttpRequestException)
        {
            return CloudOperationalSnapshotHttpResult.Retry("CLOUD_SNAPSHOT_NETWORK_ERROR", "Cloud operational snapshot endpoint is unavailable.");
        }

        using (response)
        {
            if (response.Content.Headers.ContentLength is long contentLength && contentLength > MaxResponseBytes)
                return CloudOperationalSnapshotHttpResult.Retry("CLOUD_SNAPSHOT_RESPONSE_TOO_LARGE", "Cloud operational snapshot response exceeded the accepted limit.");

            byte[] payloadBytes;
            try
            {
                payloadBytes = await ReadBoundedAsync(response.Content, MaxResponseBytes, cancellationToken);
            }
            catch (CloudOperationalSnapshotException error)
            {
                return CloudOperationalSnapshotHttpResult.Retry(error.Code, error.Message);
            }
            catch
            {
                return CloudOperationalSnapshotHttpResult.Retry("CLOUD_SNAPSHOT_RESPONSE_READ_FAILED", "Cloud operational snapshot response could not be read.");
            }

            JsonElement root;
            try
            {
                using var document = JsonDocument.Parse(payloadBytes);
                root = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return CloudOperationalSnapshotHttpResult.Retry("CLOUD_SNAPSHOT_RESPONSE_INVALID", "Cloud operational snapshot response was not valid JSON.");
            }

            var ok = root.TryGetProperty("ok", out var okElement) && okElement.ValueKind == JsonValueKind.True;
            if (response.IsSuccessStatusCode && ok)
            {
                try
                {
                    var snapshot = ParseSuccess(root, snapshotRequest);
                    return CloudOperationalSnapshotHttpResult.Success(snapshot);
                }
                catch (CloudOperationalSnapshotException error)
                {
                    return CloudOperationalSnapshotHttpResult.Conflict(error.Code, error.Message);
                }
            }

            var errorElement = root.TryGetProperty("error", out var cloudError) && cloudError.ValueKind == JsonValueKind.Object
                ? cloudError
                : default;
            var code = errorElement.ValueKind == JsonValueKind.Object ? ReadString(errorElement, "code") : null;
            var reason = errorElement.ValueKind == JsonValueKind.Object ? ReadString(errorElement, "reason") : null;
            code ??= $"CLOUD_HTTP_{(int)response.StatusCode}";
            reason ??= "Cloud operational snapshot request was rejected.";

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return CloudOperationalSnapshotHttpResult.Retry("CLOUD_MACHINE_AUTH_REJECTED", reason);
            if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
                return CloudOperationalSnapshotHttpResult.Conflict(code, reason);
            if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
                return CloudOperationalSnapshotHttpResult.Retry(code, reason);
            return CloudOperationalSnapshotHttpResult.Rejected(code, reason);
        }
    }

    private static CloudOperationalSnapshotResponse ParseSuccess(
        JsonElement root,
        CloudOperationalSnapshotRequest request)
    {
        var contractVersion = RequireResponseText(root, "snapshotContractVersion", 160);
        if (!string.Equals(contractVersion, SnapshotContractVersion, StringComparison.Ordinal))
            throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_CONTRACT_MISMATCH", "Cloud snapshot contract version is not supported.");

        var environment = RequireResponseText(root, "environment", 32);
        var clusterId = RequireResponseText(root, "clusterId", 160);
        var edgeInstanceId = RequireResponseText(root, "edgeInstanceId", 300);
        var edgeEpoch = RequireResponseText(root, "edgeEpoch", 300);
        var compatibilityVersion = RequireResponseText(root, "compatibilityVersion", 160);
        if (!string.Equals(environment, request.Environment, StringComparison.Ordinal) ||
            !string.Equals(clusterId, request.ClusterId, StringComparison.Ordinal) ||
            !string.Equals(edgeInstanceId, request.EdgeInstanceId, StringComparison.Ordinal) ||
            !string.Equals(edgeEpoch, request.EdgeEpoch, StringComparison.Ordinal) ||
            !string.Equals(compatibilityVersion, request.DomainContractVersion, StringComparison.Ordinal))
        {
            throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_IDENTITY_MISMATCH", "Cloud snapshot identity or compatibility evidence does not match the LAN request.");
        }

        var snapshotVersion = RequireResponseText(root, "snapshotVersion", 160);
        var sourceCheckpoint = RequireResponseText(root, "sourceCheckpoint", 300);
        var scopeJson = RequireResponseText(root, "scopeJson", 1024 * 1024);
        var stateJson = RequireResponseText(root, "stateJson", MaxResponseBytes);
        var machineAuthVersion = RequireResponseText(root, "machineAuthVersion", 160);
        if (!string.Equals(machineAuthVersion, CloudReconciliationHttpSender.AuthVersion, StringComparison.Ordinal))
            throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_AUTH_VERSION_MISMATCH", "Cloud snapshot machine-auth version does not match the LAN client.");

        var requested = ReadStringArray(root, "requestedCanonicalEventIds", 10_000);
        var covered = ReadStringArray(root, "coveredCanonicalEventIds", 10_000);
        var expectedRequested = request.RequestedCanonicalEventIds ?? Array.Empty<string>();
        if (!requested.SequenceEqual(expectedRequested, StringComparer.Ordinal))
            throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_REQUEST_ECHO_MISMATCH", "Cloud snapshot did not echo the canonical coverage request exactly.");
        if (covered.Distinct(StringComparer.Ordinal).Count() != covered.Count ||
            covered.Any(id => !expectedRequested.Contains(id, StringComparer.Ordinal)))
            throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_COVERAGE_INVALID", "Cloud snapshot returned invalid canonical coverage evidence.");
        if (expectedRequested.Count > 0 && !covered.SequenceEqual(expectedRequested, StringComparer.Ordinal))
            throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_COVERAGE_INCOMPLETE", "Cloud snapshot did not prove every requested canonical event.");

        return new CloudOperationalSnapshotResponse(
            new OperationalSnapshotEnvelope(
                snapshotVersion,
                environment,
                clusterId,
                sourceCheckpoint,
                compatibilityVersion,
                scopeJson,
                stateJson),
            edgeInstanceId,
            edgeEpoch,
            requested,
            covered,
            root.TryGetProperty("edgeSourceRegistered", out var registered) && registered.ValueKind == JsonValueKind.True);
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpContent content, int maxBytes, CancellationToken cancellationToken)
    {
        await using var input = await content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0) break;
            if (output.Length + read > maxBytes)
                throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_RESPONSE_TOO_LARGE", "Cloud operational snapshot response exceeded the accepted limit.");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName, int maxCount)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
            throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_RESPONSE_INVALID", $"Cloud snapshot response is missing {propertyName}.");
        var values = new List<string>();
        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(element.GetString()))
                throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_RESPONSE_INVALID", $"Cloud snapshot response contains an invalid {propertyName} value.");
            values.Add(element.GetString()!);
            if (values.Count > maxCount)
                throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_RESPONSE_INVALID", $"Cloud snapshot response contains too many {propertyName} values.");
        }
        return values;
    }

    private static string RequireResponseText(JsonElement root, string propertyName, int max)
    {
        var value = ReadString(root, propertyName);
        if (string.IsNullOrWhiteSpace(value) || value.Length > max)
            throw new CloudOperationalSnapshotException("CLOUD_SNAPSHOT_RESPONSE_INVALID", $"Cloud snapshot response is missing or invalid: {propertyName}.");
        return value;
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string RequireText(string? value, string name, int max)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0 || normalized.Length > max)
            throw new ArgumentException($"{name} is required and must be <= {max} characters.", name);
        return normalized;
    }
}

public sealed class CloudOperationalRefreshCoordinator
{
    private readonly OperationalSnapshotStore _snapshotStore;
    private readonly PostReconciliationRebaseTracker _rebaseTracker;
    private readonly CloudOperationalSnapshotHttpClient _cloudClient;
    private readonly string _environment;
    private readonly string _clusterId;
    private readonly string _edgeInstanceId;
    private readonly string _edgeEpoch;
    private readonly string _domainContractVersion;
    private readonly string _edgeSchemaVersion;
    private readonly string[] _requiredModules;

    public CloudOperationalRefreshCoordinator(
        string databasePath,
        CloudOperationalSnapshotHttpClient cloudClient,
        string environment,
        string clusterId,
        string edgeInstanceId,
        string edgeEpoch,
        string domainContractVersion,
        string edgeSchemaVersion,
        IEnumerable<string> requiredModules)
    {
        _snapshotStore = new OperationalSnapshotStore(databasePath);
        _rebaseTracker = new PostReconciliationRebaseTracker(databasePath);
        _cloudClient = cloudClient ?? throw new ArgumentNullException(nameof(cloudClient));
        _environment = RequireText(environment, nameof(environment), 32);
        _clusterId = RequireText(clusterId, nameof(clusterId), 160);
        _edgeInstanceId = RequireText(edgeInstanceId, nameof(edgeInstanceId), 300);
        _edgeEpoch = RequireText(edgeEpoch, nameof(edgeEpoch), 300);
        _domainContractVersion = RequireText(domainContractVersion, nameof(domainContractVersion), 160);
        _edgeSchemaVersion = RequireText(edgeSchemaVersion, nameof(edgeSchemaVersion), 160);
        _requiredModules = requiredModules
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (_requiredModules.Length == 0) throw new ArgumentException("At least one operational module is required.", nameof(requiredModules));
    }

    public async Task<CloudOperationalRefreshResult> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        PostReconciliationRebaseInspection rebase;
        try
        {
            rebase = await _rebaseTracker.InspectAsync(cancellationToken);
        }
        catch (PostReconciliationRebaseException error)
        {
            return CloudOperationalRefreshResult.Failed(error.Code, error.Message);
        }

        var request = new CloudOperationalSnapshotRequest(
            _environment,
            _clusterId,
            _edgeInstanceId,
            _edgeEpoch,
            _domainContractVersion,
            _edgeSchemaVersion,
            "FULL",
            rebase.Required ? rebase.CanonicalEventIds : Array.Empty<string>());

        var cloud = await _cloudClient.FetchAsync(request, cancellationToken);
        if (cloud.Outcome != CloudOperationalSnapshotHttpOutcome.Success || cloud.Snapshot is null)
        {
            return cloud.Outcome switch
            {
                CloudOperationalSnapshotHttpOutcome.Conflict => CloudOperationalRefreshResult.Conflict(cloud.ErrorCode ?? "CLOUD_SNAPSHOT_CONFLICT", cloud.Reason ?? "Cloud snapshot conflict."),
                CloudOperationalSnapshotHttpOutcome.Rejected => CloudOperationalRefreshResult.Failed(cloud.ErrorCode ?? "CLOUD_SNAPSHOT_REJECTED", cloud.Reason ?? "Cloud snapshot rejected."),
                _ => CloudOperationalRefreshResult.Retry(cloud.ErrorCode ?? "CLOUD_SNAPSHOT_RETRY", cloud.Reason ?? "Cloud snapshot retry required.")
            };
        }

        OperationalSnapshotImportResult import;
        try
        {
            import = await _snapshotStore.ImportAsync(
                cloud.Snapshot.Envelope,
                _environment,
                _clusterId,
                _domainContractVersion,
                _requiredModules,
                cancellationToken);
        }
        catch (OperationalSnapshotException error)
        {
            return CloudOperationalRefreshResult.Retry(error.Code, error.Message);
        }

        PostReconciliationRebaseConfirmResult? confirmed = null;
        if (rebase.Required)
        {
            try
            {
                confirmed = await _rebaseTracker.ConfirmAsync(
                    new PostReconciliationRebaseEvidence(
                        cloud.Snapshot.Envelope.SnapshotVersion,
                        cloud.Snapshot.Envelope.SourceCheckpoint,
                        cloud.Snapshot.CoveredCanonicalEventIds),
                    cancellationToken);
            }
            catch (PostReconciliationRebaseException error)
            {
                return CloudOperationalRefreshResult.Retry(error.Code, error.Message, import.SnapshotVersion);
            }
        }

        return CloudOperationalRefreshResult.Succeeded(
            import.SnapshotVersion,
            import.AlreadyKnown,
            rebase.PendingCanonicalEventCount,
            confirmed?.Cleared ?? false);
    }

    private static string RequireText(string? value, string name, int max)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0 || normalized.Length > max)
            throw new ArgumentException($"{name} is required and must be <= {max} characters.", name);
        return normalized;
    }
}

public sealed class CloudOperationalRefreshPump
{
    private readonly CloudOperationalRefreshCoordinator _coordinator;

    public CloudOperationalRefreshPump(CloudOperationalRefreshCoordinator coordinator) =>
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

    public async Task RunAsync(TimeSpan pollInterval, CancellationToken cancellationToken)
    {
        if (pollInterval < TimeSpan.FromSeconds(5)) throw new ArgumentOutOfRangeException(nameof(pollInterval));
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _coordinator.RunOnceAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Fail closed and retry on the next poll. Snapshot import and rebase cursor changes are independently atomic.
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

public sealed record CloudOperationalSnapshotRequest(
    string Environment,
    string ClusterId,
    string EdgeInstanceId,
    string EdgeEpoch,
    string DomainContractVersion,
    string EdgeSchemaVersion,
    string Mode,
    IReadOnlyList<string> RequestedCanonicalEventIds);

public sealed record CloudOperationalSnapshotResponse(
    OperationalSnapshotEnvelope Envelope,
    string EdgeInstanceId,
    string EdgeEpoch,
    IReadOnlyList<string> RequestedCanonicalEventIds,
    IReadOnlyList<string> CoveredCanonicalEventIds,
    bool EdgeSourceRegistered);

public enum CloudOperationalSnapshotHttpOutcome
{
    Success,
    Retry,
    Conflict,
    Rejected
}

public sealed record CloudOperationalSnapshotHttpResult(
    CloudOperationalSnapshotHttpOutcome Outcome,
    CloudOperationalSnapshotResponse? Snapshot,
    string? ErrorCode,
    string? Reason)
{
    public static CloudOperationalSnapshotHttpResult Success(CloudOperationalSnapshotResponse snapshot) =>
        new(CloudOperationalSnapshotHttpOutcome.Success, snapshot, null, null);
    public static CloudOperationalSnapshotHttpResult Retry(string code, string reason) =>
        new(CloudOperationalSnapshotHttpOutcome.Retry, null, code, reason);
    public static CloudOperationalSnapshotHttpResult Conflict(string code, string reason) =>
        new(CloudOperationalSnapshotHttpOutcome.Conflict, null, code, reason);
    public static CloudOperationalSnapshotHttpResult Rejected(string code, string reason) =>
        new(CloudOperationalSnapshotHttpOutcome.Rejected, null, code, reason);
}

public enum CloudOperationalRefreshOutcome
{
    Success,
    Retry,
    Conflict,
    Failed
}

public sealed record CloudOperationalRefreshResult(
    CloudOperationalRefreshOutcome Outcome,
    string? SnapshotVersion,
    bool SnapshotAlreadyKnown,
    int PendingCanonicalEventCount,
    bool RebaseCleared,
    string? ErrorCode,
    string? Reason)
{
    public static CloudOperationalRefreshResult Succeeded(string snapshotVersion, bool alreadyKnown, int pendingCount, bool rebaseCleared) =>
        new(CloudOperationalRefreshOutcome.Success, snapshotVersion, alreadyKnown, pendingCount, rebaseCleared, null, null);
    public static CloudOperationalRefreshResult Retry(string code, string reason, string? snapshotVersion = null) =>
        new(CloudOperationalRefreshOutcome.Retry, snapshotVersion, false, 0, false, code, reason);
    public static CloudOperationalRefreshResult Conflict(string code, string reason) =>
        new(CloudOperationalRefreshOutcome.Conflict, null, false, 0, false, code, reason);
    public static CloudOperationalRefreshResult Failed(string code, string reason) =>
        new(CloudOperationalRefreshOutcome.Failed, null, false, 0, false, code, reason);
}

public sealed class CloudOperationalSnapshotException : InvalidOperationException
{
    public CloudOperationalSnapshotException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}
