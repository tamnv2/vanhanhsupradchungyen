using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const int HttpPort = 17891;
const int DiscoveryPort = 17892;
const string DiscoveryMessage = "VHDCHY_DISCOVER_CURRENT_BETA_V1";
const string ServiceName = "VHDCHY_LAN_AGENT";
const string EnvironmentName = "BETA";
const string ProtocolName = "VHDCHY_LAN_TRANSPORT_TEST_V1";

var dataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "VHDCHY",
    "LanAgentBeta");
Directory.CreateDirectory(dataDirectory);

var instanceIdFile = Path.Combine(dataDirectory, "instance-id.txt");
var instanceId = File.Exists(instanceIdFile)
    ? (await File.ReadAllTextAsync(instanceIdFile)).Trim()
    : Guid.NewGuid().ToString("N");
if (!File.Exists(instanceIdFile)) await File.WriteAllTextAsync(instanceIdFile, instanceId);

var streamEpoch = Guid.NewGuid().ToString("N");
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "dev";
var store = new TestEventStore(Path.Combine(dataDirectory, "transport-test-events.jsonl"));
await store.InitializeAsync();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(HttpPort));
var app = builder.Build();

app.MapGet("/health", () => Results.Json(new
{
    ok = true,
    service = ServiceName,
    environment = EnvironmentName,
    protocol = ProtocolName,
    instanceId,
    streamEpoch,
    version,
    httpPort = HttpPort,
    discoveryPort = DiscoveryPort,
    mode = "TRANSPORT_TEST_ONLY",
    canonicalBusinessAuthority = false,
    capabilities = new[] { "UDP_DISCOVERY", "ECHO", "DURABLE_TEST_EVENT", "IDEMPOTENCY_COLLISION_GUARD", "DEVICE_SEQ_COLLISION_GUARD" }
}));

app.MapPost("/api/transport/echo", (EchoRequest request) => Results.Json(new
{
    ok = true,
    streamEpoch,
    request.DeviceId,
    request.Payload,
    serverUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
}));

app.MapPost("/api/transport/test-event", async (TransportTestEvent request) =>
{
    if (!request.TestOnly)
        return Results.BadRequest(new { ok = false, code = "TEST_ONLY_REQUIRED" });
    if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || string.IsNullOrWhiteSpace(request.DeviceId) || request.DeviceSeq <= 0)
        return Results.BadRequest(new { ok = false, code = "INVALID_TEST_EVENT" });
    if (request.Payload?.Length > 4096)
        return Results.BadRequest(new { ok = false, code = "PAYLOAD_TOO_LARGE" });

    var result = await store.AcceptAsync(request);
    return result.Status switch
    {
        TestAcceptStatus.Accepted => Results.Json(new
        {
            ok = true,
            ackType = "TEST_ACCEPTED_AGENT_ONLY",
            duplicate = false,
            request.IdempotencyKey,
            request.DeviceId,
            request.DeviceSeq,
            streamEpoch
        }),
        TestAcceptStatus.Duplicate => Results.Json(new
        {
            ok = true,
            ackType = "TEST_ACCEPTED_AGENT_ONLY",
            duplicate = true,
            request.IdempotencyKey,
            request.DeviceId,
            request.DeviceSeq,
            streamEpoch
        }),
        TestAcceptStatus.IdempotencyConflict => Results.Conflict(new { ok = false, code = "IDEMPOTENCY_PAYLOAD_CONFLICT" }),
        TestAcceptStatus.DeviceSequenceConflict => Results.Conflict(new { ok = false, code = "DEVICE_SEQUENCE_COLLISION" }),
        _ => Results.StatusCode(500)
    };
});

app.MapGet("/api/transport/metrics", () => Results.Json(new
{
    ok = true,
    accepted = store.AcceptedCount,
    duplicate = store.DuplicateCount,
    idempotencyConflict = store.IdempotencyConflictCount,
    deviceSequenceConflict = store.DeviceSequenceConflictCount,
    persistedRecords = store.PersistedCount,
    dataDirectory,
    streamEpoch
}));

var stop = new CancellationTokenSource();
app.Lifetime.ApplicationStopping.Register(stop.Cancel);
var discoveryTask = RunDiscoveryAsync(stop.Token);

Console.WriteLine($"VHDCHY LAN Agent BETA {version}");
Console.WriteLine($"HTTP :{HttpPort} | UDP discovery :{DiscoveryPort}");
Console.WriteLine("Mode: TRANSPORT_TEST_ONLY. Do not send business credentials/PII to pilot endpoints.");
Console.WriteLine($"Data: {dataDirectory}");

try
{
    await app.RunAsync();
}
finally
{
    stop.Cancel();
    try { await discoveryTask; } catch (OperationCanceledException) { }
    stop.Dispose();
}

async Task RunDiscoveryAsync(CancellationToken cancellationToken)
{
    using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, DiscoveryPort));
    while (!cancellationToken.IsCancellationRequested)
    {
        UdpReceiveResult packet;
        try
        {
            packet = await udp.ReceiveAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (SocketException)
        {
            try { await Task.Delay(750, cancellationToken); } catch (OperationCanceledException) { break; }
            continue;
        }

        var message = Encoding.UTF8.GetString(packet.Buffer).Trim();
        if (!string.Equals(message, DiscoveryMessage, StringComparison.Ordinal)) continue;

        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            ok = true,
            service = ServiceName,
            environment = EnvironmentName,
            protocol = ProtocolName,
            instanceId,
            streamEpoch,
            version,
            httpPort = HttpPort,
            serverUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        try
        {
            await udp.SendAsync(payload, packet.RemoteEndPoint, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (SocketException)
        {
            // Physical network policy is evidence, not something the pilot bypasses.
        }
    }
}

sealed record EchoRequest(string? DeviceId, string? Payload);
sealed record TransportTestEvent(string IdempotencyKey, string DeviceId, long DeviceSeq, string? Payload, bool TestOnly);

enum TestAcceptStatus
{
    Accepted,
    Duplicate,
    IdempotencyConflict,
    DeviceSequenceConflict
}

sealed record TestAcceptResult(TestAcceptStatus Status);
sealed record StoredTestEvent(string IdempotencyKey, string DeviceId, long DeviceSeq, string PayloadHash, long AcceptedAtUnixMs);

sealed class TestEventStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly ConcurrentDictionary<string, StoredTestEvent> _byIdempotency = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _byDeviceSequence = new(StringComparer.Ordinal);

    private long _acceptedCount;
    private long _duplicateCount;
    private long _idempotencyConflictCount;
    private long _deviceSequenceConflictCount;

    public long AcceptedCount => Interlocked.Read(ref _acceptedCount);
    public long DuplicateCount => Interlocked.Read(ref _duplicateCount);
    public long IdempotencyConflictCount => Interlocked.Read(ref _idempotencyConflictCount);
    public long DeviceSequenceConflictCount => Interlocked.Read(ref _deviceSequenceConflictCount);
    public int PersistedCount => _byIdempotency.Count;

    public TestEventStore(string path) => _path = path;

    public async Task InitializeAsync()
    {
        if (!File.Exists(_path)) return;
        foreach (var line in await File.ReadAllLinesAsync(_path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var stored = JsonSerializer.Deserialize<StoredTestEvent>(line);
                if (stored is null) continue;
                _byIdempotency[stored.IdempotencyKey] = stored;
                _byDeviceSequence[SequenceKey(stored.DeviceId, stored.DeviceSeq)] = stored.IdempotencyKey;
            }
            catch (JsonException)
            {
                // Keep running; malformed historic test lines are diagnostic evidence, not business authority.
            }
        }
    }

    public async Task<TestAcceptResult> AcceptAsync(TransportTestEvent request)
    {
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Payload ?? string.Empty))).ToLowerInvariant();

        if (_byIdempotency.TryGetValue(request.IdempotencyKey, out var existing))
        {
            if (!string.Equals(existing.PayloadHash, payloadHash, StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _idempotencyConflictCount);
                return new(TestAcceptStatus.IdempotencyConflict);
            }
            Interlocked.Increment(ref _duplicateCount);
            return new(TestAcceptStatus.Duplicate);
        }

        var sequenceKey = SequenceKey(request.DeviceId, request.DeviceSeq);
        if (_byDeviceSequence.TryGetValue(sequenceKey, out var priorIdempotency) &&
            !string.Equals(priorIdempotency, request.IdempotencyKey, StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _deviceSequenceConflictCount);
            return new(TestAcceptStatus.DeviceSequenceConflict);
        }

        await _writeGate.WaitAsync();
        try
        {
            if (_byIdempotency.TryGetValue(request.IdempotencyKey, out existing))
            {
                if (!string.Equals(existing.PayloadHash, payloadHash, StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref _idempotencyConflictCount);
                    return new(TestAcceptStatus.IdempotencyConflict);
                }
                Interlocked.Increment(ref _duplicateCount);
                return new(TestAcceptStatus.Duplicate);
            }

            if (_byDeviceSequence.TryGetValue(sequenceKey, out priorIdempotency) &&
                !string.Equals(priorIdempotency, request.IdempotencyKey, StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _deviceSequenceConflictCount);
                return new(TestAcceptStatus.DeviceSequenceConflict);
            }

            var stored = new StoredTestEvent(
                request.IdempotencyKey,
                request.DeviceId,
                request.DeviceSeq,
                payloadHash,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            await File.AppendAllTextAsync(_path, JsonSerializer.Serialize(stored) + Environment.NewLine);
            _byIdempotency[stored.IdempotencyKey] = stored;
            _byDeviceSequence[sequenceKey] = stored.IdempotencyKey;
            Interlocked.Increment(ref _acceptedCount);
            return new(TestAcceptStatus.Accepted);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private static string SequenceKey(string deviceId, long deviceSeq) => deviceId + ":" + deviceSeq;
}
