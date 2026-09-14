using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vhdchy.LanService;

const string environment = "BETA";
const string clusterId = "PICK_PACK_1291";
const string compatibility = "VHDCHY_DOMAIN_V1";
const string moduleId = "IDENTITY_EMPLOYEE_ATTENDANCE";
const string edgeInstanceId = "edge-operational-refresh-harness";
const string oldEdgeEpoch = "old-epoch-before-restart";
const string currentEdgeEpoch = "current-epoch-after-restart";
const string keyId = "lan-beta-machine-harness";
const string keyMaterial = "harness-only-machine-auth-key-material-2026-09-14";

static void Assert(bool condition, string code)
{
    if (!condition) throw new InvalidOperationException(code);
}

static string StateJson(string employeeId) => JsonSerializer.Serialize(new
{
    employees = new[]
    {
        new
        {
            employeeId,
            fullName = "Operational Refresh Employee",
            status = "ACTIVE",
            entityVersion = 1
        }
    },
    employeeCodes = Array.Empty<object>(),
    presence = Array.Empty<object>()
});

var tempRoot = Path.Combine(Path.GetTempPath(), $"vhdchy-operational-refresh-{Guid.NewGuid():N}");
Directory.CreateDirectory(tempRoot);
var databasePath = Path.Combine(tempRoot, "edge.db");

try
{
    var edgeStore = new EdgeStore(databasePath);
    await edgeStore.InitializeAsync(environment, clusterId, edgeInstanceId, oldEdgeEpoch, compatibility);
    await EmployeeCodeUniqueClaimStore.EnsureAsync(databasePath);

    var authorityStore = new AuthoritySnapshotStore(databasePath);
    var authority = await authorityStore.ImportAsync(
        new AuthoritySnapshotEnvelope(
            "AUTH-OP-REFRESH-1",
            environment,
            clusterId,
            "cloud-authority-operational-refresh-1",
            compatibility,
            "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
            "{}"),
        environment,
        clusterId,
        compatibility);
    Assert(authority.Activated, "AUTHORITY_NOT_ACTIVE");

    var operationalStore = new OperationalSnapshotStore(databasePath);
    var oldSnapshot = await operationalStore.ImportAsync(
        new OperationalSnapshotEnvelope(
            "OP-OP-REFRESH-OLD",
            environment,
            clusterId,
            "cloud-operational-refresh-old",
            compatibility,
            "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
            "{\"employees\":[],\"employeeCodes\":[],\"presence\":[]}"),
        environment,
        clusterId,
        compatibility,
        new[] { moduleId });
    Assert(oldSnapshot.Activated, "OLD_OPERATIONAL_SNAPSHOT_NOT_ACTIVE");

    var commandStore = new LocalCommandStore(databasePath, environment, clusterId, compatibility);
    var syncStore = new CloudSyncQueueStore(databasePath);
    var tracker = new PostReconciliationRebaseTracker(databasePath);

    var local = await commandStore.ExecuteAsync(new LanLocalCommandEnvelope(
        RequestId: "REQ-OP-REFRESH-1",
        IdempotencyKey: "IDEM-OP-REFRESH-1",
        ModuleId: moduleId,
        CommandCode: "EMPLOYEE_CREATE",
        EventCode: "EMPLOYEE_CREATED",
        EntityType: "employee",
        EntityId: "EMP-OP-REFRESH-1",
        StateKey: "employee:EMP-OP-REFRESH-1",
        ExpectedBaseVersion: null,
        PayloadJson: "{\"employeeId\":\"EMP-OP-REFRESH-1\",\"status\":\"ACTIVE\",\"fullName\":\"Operational Refresh Employee\"}",
        NextStateJson: "{\"employeeId\":\"EMP-OP-REFRESH-1\",\"status\":\"ACTIVE\",\"fullName\":\"Operational Refresh Employee\",\"entityVersion\":1}"));

    var claims = await syncStore.ClaimDueAsync(10);
    Assert(claims.Count == 1 && claims[0].Envelope.EventId == local.EventId, "SYNC_CLAIM_WRONG");
    const string canonicalEventId = "CANONICAL-OP-REFRESH-1";
    await syncStore.MarkReconciledAsync(claims[0].OutboxId, canonicalEventId, DateTimeOffset.UtcNow);

    var pendingBefore = await tracker.InspectAsync();
    Assert(pendingBefore.Required && pendingBefore.PendingCanonicalEventCount == 1, "REBASE_NOT_PENDING");
    Assert(pendingBefore.CanonicalEventIds.SequenceEqual(new[] { canonicalEventId }, StringComparer.Ordinal), "PENDING_CANONICAL_ID_WRONG");

    var readinessBefore = await new LanReadinessEvaluator(
        databasePath,
        environment,
        clusterId,
        compatibility,
        new[] { moduleId }).EvaluateAsync();
    Assert(readinessBefore.Blockers.Any(blocker => blocker.Code == "POST_RECONCILIATION_REBASE_REQUIRED"), "READINESS_DID_NOT_FAIL_CLOSED");

    // Emulate a LAN process restart: edgeInstanceId stays persistent while edgeEpoch changes.
    await edgeStore.InitializeAsync(environment, clusterId, edgeInstanceId, currentEdgeEpoch, compatibility);

    var endpoint = new Uri("https://beta.example/api/v1/reconciliation/operational-snapshot");

    var incompleteHandler = new SnapshotHandler(
        environment,
        clusterId,
        edgeInstanceId,
        currentEdgeEpoch,
        keyId,
        keyMaterial,
        canonicalEventId,
        includeCoverage: false);
    using (var incompleteHttp = new HttpClient(incompleteHandler) { Timeout = TimeSpan.FromSeconds(5) })
    {
        var incompleteClient = new CloudOperationalSnapshotHttpClient(
            incompleteHttp,
            endpoint,
            environment,
            keyId,
            keyMaterial);
        var incompleteCoordinator = new CloudOperationalRefreshCoordinator(
            databasePath,
            incompleteClient,
            environment,
            clusterId,
            edgeInstanceId,
            currentEdgeEpoch,
            compatibility,
            EdgeStore.SchemaVersion,
            new[] { moduleId });
        var incomplete = await incompleteCoordinator.RunOnceAsync();
        Assert(incomplete.Outcome == CloudOperationalRefreshOutcome.Conflict, "INCOMPLETE_COVERAGE_NOT_REJECTED");
        Assert((await tracker.InspectAsync()).Required, "INCOMPLETE_COVERAGE_CLEARED_REBASE");
        Assert(await operationalStore.ReadActiveVersionAsync() == "OP-OP-REFRESH-OLD", "INCOMPLETE_COVERAGE_CHANGED_ACTIVE_SNAPSHOT");
        Assert(incompleteHandler.RequestVerified, "INCOMPLETE_REQUEST_AUTH_NOT_VERIFIED");
    }

    var successHandler = new SnapshotHandler(
        environment,
        clusterId,
        edgeInstanceId,
        currentEdgeEpoch,
        keyId,
        keyMaterial,
        canonicalEventId,
        includeCoverage: true);
    using (var successHttp = new HttpClient(successHandler) { Timeout = TimeSpan.FromSeconds(5) })
    {
        var successClient = new CloudOperationalSnapshotHttpClient(
            successHttp,
            endpoint,
            environment,
            keyId,
            keyMaterial);
        var successCoordinator = new CloudOperationalRefreshCoordinator(
            databasePath,
            successClient,
            environment,
            clusterId,
            edgeInstanceId,
            currentEdgeEpoch,
            compatibility,
            EdgeStore.SchemaVersion,
            new[] { moduleId });
        var success = await successCoordinator.RunOnceAsync();
        Assert(success.Outcome == CloudOperationalRefreshOutcome.Success, "SUCCESS_REFRESH_FAILED");
        Assert(success.RebaseCleared, "SUCCESS_REFRESH_DID_NOT_CLEAR_REBASE");
        Assert(success.PendingCanonicalEventCount == 1, "SUCCESS_PENDING_COUNT_WRONG");
        Assert(successHandler.RequestVerified, "SUCCESS_REQUEST_AUTH_NOT_VERIFIED");
        Assert(successHandler.SawCurrentRestartEpoch, "SUCCESS_REQUEST_EPOCH_WRONG");
    }

    var activeVersion = await operationalStore.ReadActiveVersionAsync();
    Assert(activeVersion == "OP-OP-REFRESH-FRESH", "FRESH_OPERATIONAL_SNAPSHOT_NOT_ACTIVE");
    Assert(!(await tracker.InspectAsync()).Required, "REBASE_REMAINED_REQUIRED_AFTER_REFRESH");

    var readinessAfter = await new LanReadinessEvaluator(
        databasePath,
        environment,
        clusterId,
        compatibility,
        new[] { moduleId }).EvaluateAsync();
    Assert(!readinessAfter.Blockers.Any(blocker => blocker.Code == "POST_RECONCILIATION_REBASE_REQUIRED"), "REBASE_BLOCKER_REMAINED_AFTER_REFRESH");

    Console.WriteLine("LAN_OPERATIONAL_REFRESH_PASS hmac=PASS exactRequest=PASS restartEpoch=PASS incompleteCoverage=PASS atomicImport=PASS rebaseClear=PASS readinessRecovery=PASS");
    return 0;
}
finally
{
    try { Directory.Delete(tempRoot, recursive: true); } catch { }
}

sealed class SnapshotHandler : HttpMessageHandler
{
    private readonly string _environment;
    private readonly string _clusterId;
    private readonly string _edgeInstanceId;
    private readonly string _edgeEpoch;
    private readonly string _keyId;
    private readonly byte[] _keyMaterial;
    private readonly string _canonicalEventId;
    private readonly bool _includeCoverage;

    public SnapshotHandler(
        string environment,
        string clusterId,
        string edgeInstanceId,
        string edgeEpoch,
        string keyId,
        string keyMaterial,
        string canonicalEventId,
        bool includeCoverage)
    {
        _environment = environment;
        _clusterId = clusterId;
        _edgeInstanceId = edgeInstanceId;
        _edgeEpoch = edgeEpoch;
        _keyId = keyId;
        _keyMaterial = Encoding.UTF8.GetBytes(keyMaterial);
        _canonicalEventId = canonicalEventId;
        _includeCoverage = includeCoverage;
    }

    public bool RequestVerified { get; private set; }
    public bool SawCurrentRestartEpoch { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post || request.RequestUri is null || request.RequestUri.AbsolutePath != CloudOperationalSnapshotHttpClient.SnapshotPath)
            return Json(HttpStatusCode.BadRequest, new { ok = false, error = new { code = "HARNESS_BAD_PATH", reason = "bad path" } });

        var rawBody = await request.Content!.ReadAsStringAsync(cancellationToken);
        using var bodyDocument = JsonDocument.Parse(rawBody);
        var root = bodyDocument.RootElement;
        var requestedIds = root.GetProperty("requestedCanonicalEventIds").EnumerateArray().Select(value => value.GetString()).ToArray();
        if (requestedIds.Length != 1 || requestedIds[0] != _canonicalEventId)
            return Json(HttpStatusCode.UnprocessableEntity, new { ok = false, error = new { code = "HARNESS_BAD_COVERAGE_REQUEST", reason = "bad coverage request" } });
        if (root.GetProperty("edgeInstanceId").GetString() != _edgeInstanceId || root.GetProperty("edgeEpoch").GetString() != _edgeEpoch)
            return Json(HttpStatusCode.Conflict, new { ok = false, error = new { code = "HARNESS_BAD_EDGE_IDENTITY", reason = "bad edge identity" } });
        SawCurrentRestartEpoch = true;

        var timestamp = Header(request, "X-VHDCHY-Machine-Timestamp");
        var nonce = Header(request, "X-VHDCHY-Machine-Nonce");
        var keyId = Header(request, "X-VHDCHY-Machine-Key-Id");
        var bodyHash = Header(request, "X-VHDCHY-Content-SHA256");
        var signature = Header(request, "X-VHDCHY-Machine-Signature");
        var expectedBodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        var canonical = string.Join('\n',
            CloudReconciliationHttpSender.AuthVersion,
            "POST",
            request.RequestUri.AbsolutePath,
            timestamp,
            nonce,
            expectedBodyHash,
            _environment,
            _keyId);
        var expectedSignature = Convert.ToHexString(HMACSHA256.HashData(_keyMaterial, Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        if (keyId != _keyId || bodyHash != expectedBodyHash || signature != expectedSignature)
            return Json(HttpStatusCode.Unauthorized, new { ok = false, error = new { code = "HARNESS_BAD_SIGNATURE", reason = "bad signature" } });
        RequestVerified = true;

        var coverage = _includeCoverage ? new[] { _canonicalEventId } : Array.Empty<string>();
        var payload = new
        {
            ok = true,
            snapshotContractVersion = CloudOperationalSnapshotHttpClient.SnapshotContractVersion,
            snapshotVersion = _includeCoverage ? "OP-OP-REFRESH-FRESH" : "OP-OP-REFRESH-INCOMPLETE",
            environment = _environment,
            clusterId = _clusterId,
            edgeInstanceId = _edgeInstanceId,
            edgeEpoch = _edgeEpoch,
            edgeSourceId = $"{_environment}:{_clusterId}:{_edgeInstanceId}:{_edgeEpoch}",
            edgeSourceRegistered = false,
            sourceCheckpoint = _includeCoverage ? "D1-SLICE1-SHA256:fresh" : "D1-SLICE1-SHA256:incomplete",
            compatibilityVersion = "VHDCHY_DOMAIN_V1",
            scopeJson = "{\"modules\":[\"IDENTITY_EMPLOYEE_ATTENDANCE\"]}",
            stateJson = StateJsonForHandler(),
            requestedCanonicalEventIds = new[] { _canonicalEventId },
            coveredCanonicalEventIds = coverage,
            counts = new { employees = 1, employeeCodes = 0, presence = 0, canonicalCoverage = coverage.Length },
            machineAuthVersion = CloudReconciliationHttpSender.AuthVersion
        };
        return Json(HttpStatusCode.OK, payload);
    }

    private string StateJsonForHandler() => JsonSerializer.Serialize(new
    {
        employees = new[]
        {
            new
            {
                employeeId = "EMP-OP-REFRESH-1",
                fullName = "Operational Refresh Employee",
                status = "ACTIVE",
                entityVersion = 1
            }
        },
        employeeCodes = Array.Empty<object>(),
        presence = Array.Empty<object>()
    });

    private static string Header(HttpRequestMessage request, string name)
    {
        if (!request.Headers.TryGetValues(name, out var values)) return string.Empty;
        return values.Single();
    }

    private static HttpResponseMessage Json(HttpStatusCode status, object payload) => new(status)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    };
}
