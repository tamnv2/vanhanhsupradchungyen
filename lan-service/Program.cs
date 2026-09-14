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
var startupIntegrity = await edgeStore.CheckIntegrityAsync();
if (!startupIntegrity.Ok)
{
    throw new InvalidOperationException("EDGE_STORE_INTEGRITY_FAILED");
}

var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = webRoot
});
builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(port));
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", async (CancellationToken cancellationToken) =>
{
    var state = await edgeStore.ReadStatusAsync(cancellationToken);
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
        domainContractVersion = DomainContractVersion,
        edgeSchemaVersion = EdgeStore.SchemaVersion,
        edgeStoreReady = true,
        edgeStoreIntegrity = "PASS",
        readiness = state.Readiness,
        businessMutationEnabled = false,
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
        domainContractVersion = DomainContractVersion,
        edgeSchemaVersion = EdgeStore.SchemaVersion,
        readiness = state.Readiness
    });
});

app.MapGet("/api/v1/capabilities", () => Results.Json(new
{
    ok = true,
    runtime = "LAN",
    environment,
    clusterId,
    authorityModel = "LOCAL_EDGE_ACCEPTANCE_THEN_CLOUD_RECONCILIATION",
    domainContractVersion = DomainContractVersion,
    edgeSchemaVersion = EdgeStore.SchemaVersion,
    businessMutationEnabled = false,
    anonymousMutationAllowed = false,
    supports = new[]
    {
        "LOCAL_WEB_HOSTING",
        "EDGE_STATE_LOCAL_DURABLE",
        "CLOUD_SYNC_OUTBOX_STORAGE_LOCAL_DURABLE",
        "GOOGLE_OUTBOX_RECEIPT_STORAGE_LOCAL_DURABLE",
        "CONFLICT_STORAGE_LOCAL_DURABLE",
        "AUTHORITY_SNAPSHOT_STORAGE_LOCAL_DURABLE",
        "CLOUD_SYNC_ENGINE_PLANNED",
        "DIRECT_GOOGLE_SENDER_PLANNED"
    }
}));

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
        conflictCount = state.ConflictCount
    });
});

app.MapMethods("/api/v1/{**path}", new[] { "POST", "PUT", "PATCH", "DELETE" }, (HttpRequest request) =>
    Results.Json(new
    {
        ok = false,
        runtime = "LAN",
        error = new
        {
            code = "RUNTIME_DEPENDENCY_UNAVAILABLE",
            message = "LAN business mutations remain fail-closed until synchronized authority and shared domain adapters are active."
        },
        requestId = request.Headers["X-Request-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString()
    }, statusCode: StatusCodes.Status503ServiceUnavailable));

Console.WriteLine($"{ServiceName} {version} environment={environment} cluster={clusterId}");
Console.WriteLine($"Listening on :{port}; data={root}; readiness=EDGE_EMPTY; webRoot={webRoot}; edgeSchema={EdgeStore.SchemaVersion}");
Console.WriteLine("Edge persistence is durable. Business mutation remains FAIL_CLOSED until synchronized authority and shared domain adapters are active.");

await app.RunAsync();
