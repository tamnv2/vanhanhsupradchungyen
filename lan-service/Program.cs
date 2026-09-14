using System.Reflection;
using Vhdchy.LanService;

const string ServiceName = "VHDCHY_LAN_SERVICE";
const string DomainContractVersion = "VHDCHY_DOMAIN_V1";
const int DefaultPort = 17901;

var environment = (Environment.GetEnvironmentVariable("VHDCHY_ENV") ?? "BETA").Trim().ToUpperInvariant();
if (environment is not ("BETA" or "STABLE"))
{
    throw new InvalidOperationException("VHDCHY_ENV must be BETA or STABLE");
}

var clusterId = (Environment.GetEnvironmentVariable("VHDCHY_CLUSTER_ID") ?? "PICK_PACK_1291").Trim();
if (string.IsNullOrWhiteSpace(clusterId)) throw new InvalidOperationException("VHDCHY_CLUSTER_ID is required");

var portText = Environment.GetEnvironmentVariable("VHDCHY_LAN_PORT");
var port = int.TryParse(portText, out var configuredPort) && configuredPort is > 1024 and < 65536
    ? configuredPort
    : DefaultPort;

var canonicalLanHost = environment == "BETA" ? "lan-beta.supra.cc.cd" : "lan.supra.cc.cd";
var tlsLoadResult = LanTlsCertificateLoader.LoadFromEnvironment(environment, canonicalLanHost);
var tlsCertificate = tlsLoadResult?.Certificate;
var secureHttpEnabled = tlsCertificate is not null;

var cloudReconciliationUrlText = Environment.GetEnvironmentVariable("VHDCHY_CLOUD_RECONCILIATION_URL")?.Trim();
var cloudReconciliationKeyId = Environment.GetEnvironmentVariable("VHDCHY_CLOUD_RECONCILIATION_KEY_ID")?.Trim();
var cloudReconciliationKeyMaterial = Environment.GetEnvironmentVariable("VHDCHY_CLOUD_RECONCILIATION_KEY")?.Trim();
var cloudConfigValues = new[] { cloudReconciliationUrlText, cloudReconciliationKeyId, cloudReconciliationKeyMaterial };
var cloudConfigCount = cloudConfigValues.Count(value => !string.IsNullOrWhiteSpace(value));
if (cloudConfigCount is > 0 and < 3)
{
    throw new InvalidOperationException("Cloud reconciliation transport configuration is incomplete.");
}
var cloudReconciliationConfigured = cloudConfigCount == 3;

var rootOverride = Environment.GetEnvironmentVariable("VHDCHY_LAN_DATA_ROOT")?.Trim();
var root = string.IsNullOrWhiteSpace(rootOverride)
    ? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VHDCHY",
        "LanService",
        environment)
    : Path.GetFullPath(rootOverride);
Directory.CreateDirectory(root);

var instancePath = Path.Combine(root, "instance-id.txt");
var instanceId = File.Exists(instancePath)
    ? (await File.ReadAllTextAsync(instancePath)).Trim()
    : Guid.NewGuid().ToString("N");
if (!File.Exists(instancePath)) await File.WriteAllTextAsync(instancePath, instanceId);

var edgeEpoch = Guid.NewGuid().ToString("N");
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "dev";
var edgeStore = new EdgeStore(Path.Combine(root, "edge.db"));
await edgeStore.InitializeAsync(environment, clusterId, instanceId, edgeEpoch, DomainContractVersion);
await EmployeeCodeUniqueClaimStore.EnsureAsync(edgeStore.DatabasePath);
var stagedMediaRoot = Path.Combine(root, "staged-media");
await LanStagedMediaStore.EnsureAsync(edgeStore.DatabasePath, stagedMediaRoot);
var cloudSyncQueue = new CloudSyncQueueStore(edgeStore.DatabasePath);
var recoveredInterruptedCloudSync = await cloudSyncQueue.RecoverInterruptedClaimsAsync();
var startupIntegrity = await edgeStore.CheckIntegrityAsync();
if (!startupIntegrity.Ok)
{
    throw new InvalidOperationException("EDGE_STORE_INTEGRITY_FAILED");
}

var readinessEvaluator = new LanReadinessEvaluator(
    edgeStore.DatabasePath,
    environment,
    clusterId,
    DomainContractVersion,
    new[] { Slice1BusinessAdapter.ModuleId },
    securePublicRouteWiringEnabled: secureHttpEnabled);

var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = webRoot
});
builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port, listen =>
    {
        if (tlsCertificate is not null) listen.UseHttps(tlsCertificate);
    });
});
var app = builder.Build();

HttpClient? cloudReconciliationHttpClient = null;
Task? cloudReconciliationPumpTask = null;
if (cloudReconciliationConfigured)
{
    cloudReconciliationHttpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(15)
    };
    var cloudEndpoint = new Uri(cloudReconciliationUrlText!, UriKind.Absolute);
    var cloudSender = new CloudReconciliationHttpSender(
        cloudReconciliationHttpClient,
        cloudEndpoint,
        environment,
        cloudReconciliationKeyId!,
        cloudReconciliationKeyMaterial!);
    var cloudEnvelopeBuilder = new CloudSyncTransportEnvelopeBuilder(edgeStore.DatabasePath);
    var cloudPump = new CloudReconciliationPump(cloudSyncQueue, cloudEnvelopeBuilder, cloudSender);
    cloudReconciliationPumpTask = Task.Run(() =>
        cloudPump.RunAsync(TimeSpan.FromSeconds(5), app.Lifetime.ApplicationStopping));
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", async (CancellationToken cancellationToken) =>
{
    var state = await edgeStore.ReadStatusAsync(cancellationToken);
    var publicReadiness = await readinessEvaluator.EvaluateAsync(cancellationToken);
    return Results.Json(new
    {
        ok = true,
        service = ServiceName,
        environment,
        clusterId,
        version,
        instanceId,
        edgeEpoch,
        port,
        canonicalLanHost,
        transport = secureHttpEnabled ? "HTTPS" : "HTTP_READ_ONLY",
        secureMutationTransportEnabled = secureHttpEnabled,
        tlsCertificateStorageMode = tlsLoadResult?.StorageMode,
        tlsCertificateNotAfterUtc = tlsLoadResult?.NotAfterUtc,
        domainContractVersion = DomainContractVersion,
        edgeSchemaVersion = EdgeStore.SchemaVersion,
        edgeStoreReady = true,
        edgeStoreIntegrity = "PASS",
        readiness = state.Readiness,
        publicReadiness = publicReadiness.Readiness,
        businessMutationEnabled = secureHttpEnabled && publicReadiness.Ready,
        cloudReconciliationTransportConfigured = cloudReconciliationConfigured,
        recoveredInterruptedCloudSync,
        localDataRoot = root
    });
});

app.MapGet("/api/v1/meta", async (CancellationToken cancellationToken) =>
{
    var state = await edgeStore.ReadStatusAsync(cancellationToken);
    return Results.Json(new
    {
        ok = true,
        service = ServiceName,
        environment,
        clusterId,
        runtime = "LAN",
        version,
        instanceId,
        edgeEpoch,
        canonicalLanHost,
        transport = secureHttpEnabled ? "HTTPS" : "HTTP_READ_ONLY",
        secureMutationTransportEnabled = secureHttpEnabled,
        tlsCertificateStorageMode = tlsLoadResult?.StorageMode,
        tlsCertificateNotAfterUtc = tlsLoadResult?.NotAfterUtc,
        domainContractVersion = DomainContractVersion,
        edgeSchemaVersion = EdgeStore.SchemaVersion,
        cloudReconciliationTransportConfigured = cloudReconciliationConfigured,
        readiness = state.Readiness
    });
});

app.MapGet("/api/v1/capabilities", async (CancellationToken cancellationToken) =>
{
    var publicReadiness = await readinessEvaluator.EvaluateAsync(cancellationToken);
    return Results.Json(new
    {
        ok = true,
        runtime = "LAN",
        environment,
        clusterId,
        authorityModel = "LOCAL_EDGE_ACCEPTANCE_THEN_CLOUD_RECONCILIATION",
        domainContractVersion = DomainContractVersion,
        edgeSchemaVersion = EdgeStore.SchemaVersion,
        secureMutationTransportEnabled = secureHttpEnabled,
        businessMutationEnabled = secureHttpEnabled && publicReadiness.Ready,
        anonymousMutationAllowed = false,
        supports = new[]
        {
            "LOCAL_WEB_HOSTING",
            "EDGE_STATE_LOCAL_DURABLE",
            "CLOUD_SYNC_OUTBOX_STORAGE_LOCAL_DURABLE",
            "CLOUD_SYNC_QUEUE_STATE_MACHINE_LOCAL_DURABLE",
            "CLOUD_SYNC_INTERRUPTED_CLAIM_RECOVERY",
            "GOOGLE_OUTBOX_RECEIPT_STORAGE_LOCAL_DURABLE",
            "CONFLICT_STORAGE_LOCAL_DURABLE",
            "AUTHORITY_SNAPSHOT_STORAGE_LOCAL_DURABLE",
            "STAGED_MEDIA_LOCAL_DURABLE",
            "SIGNED_PAIRED_CLIENT_REQUESTS",
            "TLS_GATED_PRIMARY_LOGIN",
            "TLS_GATED_SLICE1_BUSINESS_ROUTE",
            "WINDOWS_DPAPI_CURRENT_USER_TLS_PFX",
            "CLOUD_RECONCILIATION_HMAC_V1",
            cloudReconciliationConfigured ? "CLOUD_RECONCILIATION_NETWORK_SENDER_ACTIVE" : "CLOUD_RECONCILIATION_NETWORK_SENDER_NOT_CONFIGURED",
            "DIRECT_GOOGLE_SENDER_PLANNED"
        }
    });
});

app.MapGet("/api/v1/sync/status", async (CancellationToken cancellationToken) =>
{
    var state = await edgeStore.ReadStatusAsync(cancellationToken);
    return Results.Json(new
    {
        ok = true,
        runtime = "LAN",
        readiness = state.Readiness,
        authoritySnapshotVersion = state.AuthoritySnapshotVersion,
        operationalSnapshotVersion = state.OperationalSnapshotVersion,
        lastCloudSyncAt = state.LastCloudSyncAt,
        pendingCloudSync = state.PendingCloudSync,
        pendingGoogleWork = state.PendingGoogleWork,
        conflictCount = state.ConflictCount,
        cloudReconciliationTransportConfigured = cloudReconciliationConfigured
    });
});

if (secureHttpEnabled)
{
    await LanSecureHttpRoutes.MapAsync(
        app,
        edgeStore.DatabasePath,
        environment,
        clusterId,
        DomainContractVersion);
}

app.MapMethods("/api/v1/{**path}", new[] { "POST", "PUT", "PATCH", "DELETE" }, (HttpRequest request) =>
    Results.Json(new
    {
        ok = false,
        runtime = "LAN",
        error = new
        {
            code = "RUNTIME_DEPENDENCY_UNAVAILABLE",
            message = secureHttpEnabled
                ? "Requested LAN mutation is not part of the reviewed secure public subset."
                : "LAN business mutations remain fail-closed until a reviewed HTTPS transport is configured."
        },
        requestId = request.Headers["X-Request-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString()
    }, statusCode: StatusCodes.Status503ServiceUnavailable));

Console.WriteLine($"{ServiceName} {version} environment={environment} cluster={clusterId}");
Console.WriteLine($"Listening on {(secureHttpEnabled ? "https" : "http")}://0.0.0.0:{port}; canonicalHost={canonicalLanHost}; data={root}; webRoot={webRoot}; edgeSchema={EdgeStore.SchemaVersion}");
if (tlsLoadResult is not null)
{
    Console.WriteLine($"TLS certificate loaded from {tlsLoadResult.StorageMode}; notAfterUtc={tlsLoadResult.NotAfterUtc:O}.");
}
Console.WriteLine($"Cloud sync queue recovery active; interruptedClaimsRecovered={recoveredInterruptedCloudSync}; machineAuth={CloudReconciliationHttpSender.AuthVersion}; networkSenderConfigured={cloudReconciliationConfigured}.");
Console.WriteLine(secureHttpEnabled
    ? "Secure LAN login and reviewed Slice-1 business routes are TLS-gated; runtime readiness still fails closed on missing synchronized authority/operational/security prerequisites."
    : "LAN runtime is HTTP read-only. All business mutation remains FAIL_CLOSED until a reviewed TLS certificate is configured.");

try
{
    await app.RunAsync();
}
finally
{
    if (cloudReconciliationPumpTask is not null)
    {
        try
        {
            await cloudReconciliationPumpTask;
        }
        catch (OperationCanceledException)
        {
        }
    }
    cloudReconciliationHttpClient?.Dispose();
    tlsCertificate?.Dispose();
}
