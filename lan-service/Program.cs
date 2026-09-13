using System.Reflection;

const string ServiceName = "VHDCHY_LAN_SERVICE";
const string DomainContractVersion = "VHDCHY_DOMAIN_V1";
const string EdgeSchemaVersion = "VHDCHY_EDGE_V1";
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

var root = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "VHDCHY",
    "LanService",
    environment);
Directory.CreateDirectory(root);

var instancePath = Path.Combine(root, "instance-id.txt");
var instanceId = File.Exists(instancePath)
    ? (await File.ReadAllTextAsync(instancePath)).Trim()
    : Guid.NewGuid().ToString("N");
if (!File.Exists(instancePath)) await File.WriteAllTextAsync(instancePath, instanceId);

var edgeEpoch = Guid.NewGuid().ToString("N");
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "dev";
var state = new EdgeRuntimeState(
    Readiness: "EDGE_EMPTY",
    AuthoritySnapshotVersion: null,
    OperationalSnapshotVersion: null,
    LastCloudSyncAt: null,
    PendingCloudSync: 0,
    PendingGoogleWork: 0,
    ConflictCount: 0);

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

app.MapGet("/health", () => Results.Json(new
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
    edgeSchemaVersion = EdgeSchemaVersion,
    readiness = state.Readiness,
    businessMutationEnabled = false,
    localDataRoot = root
}));

app.MapGet("/api/v1/meta", () => Results.Json(new
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
    edgeSchemaVersion = EdgeSchemaVersion,
    readiness = state.Readiness
}));

app.MapGet("/api/v1/capabilities", () => Results.Json(new
{
    ok = true,
    runtime = "LAN",
    environment,
    clusterId,
    authorityModel = "LOCAL_EDGE_ACCEPTANCE_THEN_CLOUD_RECONCILIATION",
    domainContractVersion = DomainContractVersion,
    businessMutationEnabled = false,
    anonymousMutationAllowed = false,
    supports = new[]
    {
        "LOCAL_WEB_HOSTING",
        "EDGE_STATE_PLANNED",
        "AUTHORITY_SNAPSHOT_PLANNED",
        "CLOUD_SYNC_OUTBOX_PLANNED",
        "DIRECT_GOOGLE_RECEIPTS_PLANNED",
        "CONFLICT_QUEUE_PLANNED"
    }
}));

app.MapGet("/api/v1/sync/status", () => Results.Json(new
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
}));

app.MapMethods("/api/v1/{**path}", new[] { "POST", "PUT", "PATCH", "DELETE" }, (HttpRequest request) =>
    Results.Json(new
    {
        ok = false,
        runtime = "LAN",
        error = new
        {
            code = "RUNTIME_DEPENDENCY_UNAVAILABLE",
            message = "LAN business mutations remain fail-closed until edge persistence, synchronized authority and shared domain adapters are active."
        },
        requestId = request.Headers["X-Request-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString()
    }, statusCode: StatusCodes.Status503ServiceUnavailable));

Console.WriteLine($"{ServiceName} {version} environment={environment} cluster={clusterId}");
Console.WriteLine($"Listening on :{port}; data={root}; readiness={state.Readiness}; webRoot={webRoot}");
Console.WriteLine("Business mutation is FAIL_CLOSED until edge persistence and synchronized authority are implemented.");

await app.RunAsync();

sealed record EdgeRuntimeState(
    string Readiness,
    string? AuthoritySnapshotVersion,
    string? OperationalSnapshotVersion,
    DateTimeOffset? LastCloudSyncAt,
    long PendingCloudSync,
    long PendingGoogleWork,
    long ConflictCount);
