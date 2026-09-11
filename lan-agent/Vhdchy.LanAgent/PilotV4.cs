using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Vhdchy.LanAgent;

internal static class V4Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            using var mutex = new Mutex(true, "VHDCHY_LAN_AGENT_BETA_SINGLE_INSTANCE", out var created);
            if (!created)
            {
                MessageBox.Show("VHDCHY LAN Agent BETA đang chạy.", "VHDCHY LAN Agent", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var controller = new V4AgentController();
            controller.StartAsync().GetAwaiter().GetResult();
            Application.Run(new V4TrayContext(controller));
            controller.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            try { V2DiagnosticLog.Write("FATAL", ex.GetType().Name + ": " + ex.Message); } catch { }
            MessageBox.Show(
                "LAN Agent không thể khởi động. Không dùng quyền Admin để vượt policy công ty.\r\n\r\n" + ex.Message,
                "VHDCHY LAN Agent BETA", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

internal sealed class V4AgentController : IAsyncDisposable
{
    private V4AgentHost? _host;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly V2SystemTelemetrySampler _telemetry = new();

    public string DataDirectory => DataPaths.GetDataDirectory();
    public int HttpPort => _host?.HttpPort ?? AgentController.DefaultHttpPort;
    public string DashboardUrl => $"http://127.0.0.1:{HttpPort}/";
    public V2SystemSnapshot SampleSystem() => _telemetry.Sample(DataDirectory);

    public async Task StartAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_host is not null) return;
            Directory.CreateDirectory(DataDirectory);
            V2DiagnosticLog.Initialize(DataDirectory);
            V2DiagnosticLog.Write("START", $"V4 Agent start | version={VersionInfo.Current} | pid={Environment.ProcessId} | data={DataDirectory}");
            _host = new V4AgentHost(DataDirectory, AgentController.DefaultHttpPort, AgentController.DefaultDiscoveryPort, _telemetry);
            await _host.StartAsync();
        }
        catch (Exception ex)
        {
            _host = null;
            V2DiagnosticLog.Write("START_FAIL", ex.GetType().Name + ": " + ex.Message);
            throw;
        }
        finally { _gate.Release(); }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_host is null) return;
            var old = _host;
            _host = null;
            await old.DisposeAsync();
            V2DiagnosticLog.Write("STOP", "V4 Agent stopped");
        }
        finally { _gate.Release(); }
    }

    public async Task RestartAsync() { await StopAsync(); await StartAsync(); }
    public void OpenDashboard() => Shell.Open(DashboardUrl);
    public void OpenDataFolder() => Shell.Open(DataDirectory);

    public async Task<string> ChangeDataDirectoryAsync(string selectedParent)
    {
        var target = Path.Combine(Path.GetFullPath(selectedParent), "VHDCHY-LAN-BETA-DATA");
        var source = DataDirectory;
        if (Path.GetFullPath(source).TrimEnd('\\').Equals(Path.GetFullPath(target).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) return target;
        Directory.CreateDirectory(target);
        var probe = Path.Combine(target, ".write-probe-" + Guid.NewGuid().ToString("N"));
        await File.WriteAllTextAsync(probe, "ok");
        File.Delete(probe);
        await StopAsync();
        try
        {
            FileTree.CopyDirectory(source, target);
            var db = Path.Combine(target, "pilot.db");
            if (File.Exists(db)) await SqliteStore.VerifyAsync(db);
            DataPaths.SetDataDirectory(target);
            await StartAsync();
            V2DiagnosticLog.Write("DATA_DIR", $"changed source={source} target={target}");
            return target;
        }
        catch
        {
            DataPaths.SetDataDirectory(source);
            await StartAsync();
            throw;
        }
    }

    public Task<V4ReleaseInfo?> CheckUpdateAsync() => V4ReleaseManager.GetLatestAsync();

    public async Task ExportDiagnosticsAsync(string destination)
    {
        var summary = _host is null
            ? JsonSerializer.Serialize(new { service = "VHDCHY_LAN_AGENT", environment = "BETA", running = false, version = VersionInfo.Current }, V2Json.Options)
            : await _host.BuildDiagnosticsSummaryJsonAsync();
        await V2DiagnosticExporter.ExportToFileAsync(DataDirectory, destination, summary);
        V2DiagnosticLog.Write("EXPORT", $"bundle={Path.GetFileName(destination)}");
    }

    public ValueTask DisposeAsync() => new(StopAsync());
}

internal sealed class V4AgentHost : IAsyncDisposable
{
    private const int MaxTransferBytes = 64 * 1024 * 1024;
    private readonly string _dataDirectory;
    private readonly int _discoveryPort;
    private readonly V4Metrics _metrics = new();
    private readonly V2SystemTelemetrySampler _telemetry;
    private readonly V4RealtimeHub _realtime = new(4096);
    private readonly CancellationTokenSource _cts = new();
    private readonly string _instanceId;
    private readonly string _streamEpoch = Guid.NewGuid().ToString("N");
    private WebApplication? _app;
    private Task? _udpTask;
    public int HttpPort { get; }

    public V4AgentHost(string dataDirectory, int httpPort, int discoveryPort, V2SystemTelemetrySampler telemetry)
    {
        _dataDirectory = dataDirectory;
        HttpPort = httpPort;
        _discoveryPort = discoveryPort;
        _telemetry = telemetry;
        _instanceId = IdentityStore.GetOrCreate(dataDirectory);
    }

    public async Task StartAsync()
    {
        Directory.CreateDirectory(_dataDirectory);
        await SqliteStore.InitializeAsync(Path.Combine(_dataDirectory, "pilot.db"));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>() });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(HttpPort));
        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            var sw = Stopwatch.StartNew();
            _metrics.AddRequest();
            try
            {
                await next();
                if (context.Response.StatusCode >= 400 && context.Request.Path != "/favicon.ico")
                {
                    Interlocked.Increment(ref _metrics.ErrorCount);
                    V2DiagnosticLog.Write("HTTP", $"{context.Request.Method} {context.Request.Path} -> {context.Response.StatusCode}; remote={V4Safe.Ip(context.Connection.RemoteIpAddress)}");
                }
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                Interlocked.Increment(ref _metrics.ClientCancelledCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _metrics.ErrorCount);
                V2DiagnosticLog.Write("HTTP_ERROR", $"{context.Request.Method} {context.Request.Path} | {ex.GetType().Name}: {ex.Message}; remote={V4Safe.Ip(context.Connection.RemoteIpAddress)}");
                throw;
            }
            finally
            {
                sw.Stop();
                _metrics.AddLatency(sw.Elapsed.TotalMilliseconds);
            }
        });

        app.MapGet("/favicon.ico", () => Results.NoContent());
        app.MapGet("/health", () => Results.Json(HealthPayload()));
        app.MapGet("/api/pilot/time", () => Results.Json(new
        {
            ok = true,
            serverUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            streamEpoch = _streamEpoch,
            version = VersionInfo.Current
        }));

        app.MapPost("/api/pilot/echo", (HttpContext context, EchoRequest request) =>
        {
            _metrics.TouchDevice(request.DeviceId, context.Connection.RemoteIpAddress, V4DeviceActivity.Echo);
            var received = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return Results.Json(new
            {
                ok = true,
                service = "VHDCHY_LAN_AGENT",
                environment = "BETA",
                protocol = AgentController.Protocol,
                streamEpoch = _streamEpoch,
                serverReceiveAtUnixMs = received,
                serverSendAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                request.DeviceId,
                request.Payload
            });
        });

        app.MapPost("/api/pilot/event", async (HttpContext context, PilotEvent request) =>
        {
            if (string.IsNullOrWhiteSpace(request.EventId) || string.IsNullOrWhiteSpace(request.DeviceId) || request.DeviceSeq <= 0)
                return Results.BadRequest(new { ok = false, code = "INVALID_EVENT" });
            _metrics.TouchDevice(request.DeviceId, context.Connection.RemoteIpAddress, V4DeviceActivity.Event);
            var sw = Stopwatch.StartNew();
            var inserted = await SqliteStore.InsertEventAsync(Path.Combine(_dataDirectory, "pilot.db"), request);
            sw.Stop();
            if (!inserted) Interlocked.Increment(ref _metrics.DuplicateCount);
            V2DiagnosticLog.Write(inserted ? "EVENT" : "DUPLICATE",
                $"device={V4Safe.Device(request.DeviceId)}; seq={request.DeviceSeq}; accepted={inserted}; dbMs={sw.Elapsed.TotalMilliseconds:0.0}; remote={V4Safe.Ip(context.Connection.RemoteIpAddress)}");
            return Results.Json(new
            {
                ok = true,
                accepted = inserted,
                duplicate = !inserted,
                request.EventId,
                request.DeviceId,
                request.DeviceSeq,
                streamEpoch = _streamEpoch,
                ackAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        });

        app.MapPost("/api/pilot/realtime/publish", (HttpContext context, V4RealtimePublish request) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.MessageId))
                return Results.BadRequest(new { ok = false, code = "INVALID_REALTIME" });
            var payload = request.Payload ?? "";
            if (Encoding.UTF8.GetByteCount(payload) > 16 * 1024) return Results.BadRequest(new { ok = false, code = "PAYLOAD_TOO_LARGE" });
            _metrics.TouchDevice(request.DeviceId, context.Connection.RemoteIpAddress, V4DeviceActivity.RealtimePublish);
            var evt = _realtime.Publish(request.MessageId, request.DeviceId, payload);
            _metrics.AddRealtimePublish(evt.PayloadBytes);
            if (evt.Sequence == 1 || evt.Sequence % 500 == 0)
                V2DiagnosticLog.Write("REALTIME_PROGRESS", $"epoch={_streamEpoch[..8]}; seq={evt.Sequence}; buffered={_realtime.BufferedCount}; dropped={_realtime.DroppedCount}");
            return Results.Json(new
            {
                ok = true,
                streamEpoch = _streamEpoch,
                sequence = evt.Sequence,
                publishedAtUnixMs = evt.PublishedAtUnixMs,
                payloadBytes = evt.PayloadBytes
            });
        });

        app.MapGet("/api/pilot/realtime/poll", async (HttpContext context) =>
        {
            var after = long.TryParse(context.Request.Query["after"], out var a) ? Math.Max(0, a) : 0;
            var timeoutMs = int.TryParse(context.Request.Query["timeoutMs"], out var t) ? Math.Clamp(t, 0, 15000) : 8000;
            var deviceId = context.Request.Query["deviceId"].ToString();
            var clientEpoch = context.Request.Query["epoch"].ToString();
            _metrics.TouchDevice(deviceId, context.Connection.RemoteIpAddress, V4DeviceActivity.RealtimePoll);
            Interlocked.Increment(ref _metrics.ActiveRealtimePollers);
            try
            {
                if (!string.IsNullOrEmpty(clientEpoch) && !string.Equals(clientEpoch, _streamEpoch, StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref _metrics.EpochResetCount);
                    V2DiagnosticLog.Write("REALTIME_EPOCH_RESET", $"device={V4Safe.Device(deviceId)}; old={V4Safe.Epoch(clientEpoch)}; new={V4Safe.Epoch(_streamEpoch)}");
                    return Results.Json(new
                    {
                        ok = true,
                        streamEpoch = _streamEpoch,
                        epochChanged = true,
                        requiresResync = true,
                        events = Array.Empty<V4RealtimeEvent>(),
                        latestSequence = _realtime.LatestSequence,
                        oldestSequence = _realtime.OldestSequence,
                        serverNowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                }

                var poll = await _realtime.PollAsync(after, timeoutMs, context.RequestAborted);
                if (poll.RequiresResync)
                {
                    Interlocked.Increment(ref _metrics.ResyncRequiredCount);
                    V2DiagnosticLog.Write("REALTIME_RESYNC_REQUIRED", $"device={V4Safe.Device(deviceId)}; after={after}; oldest={poll.OldestSequence}; latest={poll.LatestSequence}; dropped={_realtime.DroppedCount}");
                }
                return Results.Json(new
                {
                    ok = true,
                    streamEpoch = _streamEpoch,
                    epochChanged = false,
                    requiresResync = poll.RequiresResync,
                    events = poll.Events,
                    latestSequence = poll.LatestSequence,
                    oldestSequence = poll.OldestSequence,
                    serverPollCompletedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                Interlocked.Increment(ref _metrics.ClientCancelledCount);
                return Results.StatusCode(499);
            }
            finally { Interlocked.Decrement(ref _metrics.ActiveRealtimePollers); }
        });

        app.MapGet("/api/pilot/realtime/snapshot", () => Results.Json(new
        {
            ok = true,
            streamEpoch = _streamEpoch,
            latestSequence = _realtime.LatestSequence,
            oldestSequence = _realtime.OldestSequence,
            buffered = _realtime.BufferedCount,
            dropped = _realtime.DroppedCount,
            serverUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            note = "Pilot snapshot carries stream position only. Production business snapshot must come from canonical state."
        }));

        app.MapPost("/api/pilot/transfer/upload", async (HttpContext context) =>
        {
            var deviceId = context.Request.Query["deviceId"].ToString();
            _metrics.TouchDevice(deviceId, context.Connection.RemoteIpAddress, V4DeviceActivity.Upload);
            var sw = Stopwatch.StartNew();
            long total = 0;
            var buffer = new byte[64 * 1024];
            while (true)
            {
                var n = await context.Request.Body.ReadAsync(buffer, context.RequestAborted);
                if (n <= 0) break;
                total += n;
                if (total > MaxTransferBytes)
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    await context.Response.WriteAsJsonAsync(new { ok = false, code = "TRANSFER_TOO_LARGE", maxBytes = MaxTransferBytes }, context.RequestAborted);
                    return;
                }
            }
            sw.Stop();
            _metrics.AddUpload(deviceId, total);
            V2DiagnosticLog.Write("TRANSFER_UP", $"device={V4Safe.Device(deviceId)}; bytes={total}; ms={sw.Elapsed.TotalMilliseconds:0.0}; mbps={V4Math.Mbps(total, sw.Elapsed.TotalMilliseconds):0.0}");
            await context.Response.WriteAsJsonAsync(new { ok = true, bytes = total, serverElapsedMs = sw.Elapsed.TotalMilliseconds }, context.RequestAborted);
        });

        app.MapGet("/api/pilot/transfer/download", async (HttpContext context) =>
        {
            var deviceId = context.Request.Query["deviceId"].ToString();
            _metrics.TouchDevice(deviceId, context.Connection.RemoteIpAddress, V4DeviceActivity.Download);
            var bytes = int.TryParse(context.Request.Query["bytes"], out var b) ? Math.Clamp(b, 1024, MaxTransferBytes) : 1024 * 1024;
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/octet-stream";
            context.Response.ContentLength = bytes;
            var block = new byte[64 * 1024];
            Array.Fill(block, (byte)0x5A);
            var remaining = bytes;
            var sw = Stopwatch.StartNew();
            while (remaining > 0)
            {
                var n = Math.Min(block.Length, remaining);
                await context.Response.Body.WriteAsync(block.AsMemory(0, n), context.RequestAborted);
                remaining -= n;
            }
            sw.Stop();
            _metrics.AddDownload(deviceId, bytes);
            V2DiagnosticLog.Write("TRANSFER_DOWN", $"device={V4Safe.Device(deviceId)}; bytes={bytes}; ms={sw.Elapsed.TotalMilliseconds:0.0}; mbps={V4Math.Mbps(bytes, sw.Elapsed.TotalMilliseconds):0.0}");
        });

        app.MapGet("/api/pilot/metrics", async () => Results.Json(await BuildMetricsAsync()));
        app.MapGet("/api/pilot/network", () => Results.Json(new
        {
            ok = true,
            addresses = NetworkInfo.GetLanIpv4Addresses(),
            interfaces = V4NetworkSnapshot.Capture(),
            httpPort = HttpPort,
            discoveryPort = _discoveryPort,
            note = "No-admin pilot; LAN IP/discovery only."
        }));
        app.MapGet("/api/pilot/update/check", async () =>
        {
            var latest = await V4ReleaseManager.GetLatestAsync();
            return Results.Json(new
            {
                ok = true,
                currentVersion = VersionInfo.Current,
                latest,
                updateAvailable = latest is not null && V4Version.IsNewer(latest.Version, VersionInfo.Current)
            });
        });
        app.MapGet("/api/pilot/diagnostics/export", async (HttpContext context) =>
        {
            if (context.Connection.RemoteIpAddress is not { } remote || !IPAddress.IsLoopback(remote)) return Results.StatusCode(StatusCodes.Status403Forbidden);
            var bytes = await V2DiagnosticExporter.CreateZipBytesAsync(_dataDirectory, await BuildDiagnosticsSummaryJsonAsync());
            V2DiagnosticLog.Write("EXPORT", "dashboard diagnostic bundle");
            return Results.File(bytes, "application/zip", $"VHDCHY-LAN-Agent-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
        });
        app.MapPost("/api/pilot/selftest/load", async (HttpContext context) =>
        {
            if (context.Connection.RemoteIpAddress is not { } remote || !IPAddress.IsLoopback(remote)) return Results.StatusCode(StatusCodes.Status403Forbidden);
            var clients = int.TryParse(context.Request.Query["clients"], out var c) ? Math.Clamp(c, 1, 200) : 10;
            var requests = int.TryParse(context.Request.Query["requests"], out var r) ? Math.Clamp(r, 1, 1000) : 100;
            var result = await RunLoadGenAsync(clients, requests, context.RequestAborted);
            V2DiagnosticLog.Write("LOADGEN", $"clients={clients}; requests={requests}; result={result.Replace('\r',' ').Replace('\n',' ')}");
            return Results.Text(result, "text/plain; charset=utf-8");
        });
        app.MapGet("/", () => Results.Content(V4Dashboard.Html, "text/html; charset=utf-8"));

        await app.StartAsync(_cts.Token);
        _app = app;
        _udpTask = Task.Run(() => DiscoveryLoopAsync(_cts.Token));
        var net = V4NetworkSnapshot.Capture();
        V2DiagnosticLog.Write("LISTEN", $"V4 HTTP={HttpPort}; UDP={_discoveryPort}; epoch={_streamEpoch[..8]}; IPs={string.Join(',', NetworkInfo.GetLanIpv4Addresses())}; nics={net.Length}");
    }

    private object HealthPayload() => new
    {
        ok = true,
        service = "VHDCHY_LAN_AGENT",
        environment = "BETA",
        protocol = AgentController.Protocol,
        instanceId = _instanceId,
        streamEpoch = _streamEpoch,
        version = VersionInfo.Current,
        httpPort = HttpPort,
        discoveryPort = _discoveryPort,
        addresses = NetworkInfo.GetLanIpv4Addresses(),
        startedAt = _metrics.StartedAt,
        startedAtUnixMs = _metrics.StartedAt.ToUnixTimeMilliseconds(),
        serverUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        lanPriorityPolicy = "PREFER_LAN_WHEN_AGENT_HEALTHY",
        backgroundPolicy = "REALTIME_FOREGROUND_ONLY__FINISH_INFLIGHT_THEN_IDLE",
        capabilities = new[] { "AUTO_LAN", "REALTIME_EPOCH", "REALTIME_RESYNC", "ASYNC_LONG_POLL", "TRANSFER_TEST", "LOAD_TEST", "RICH_DIAGNOSTICS", "VERIFIED_UPDATE" }
    };

    private async Task<object> BuildMetricsAsync()
    {
        var dbPath = Path.Combine(_dataDirectory, "pilot.db");
        var stored = await SqliteStore.CountEventsAsync(dbPath);
        var p = Process.GetCurrentProcess();
        var system = _telemetry.Sample(_dataDirectory);
        return _metrics.Snapshot(
            stored,
            p.WorkingSet64,
            GC.GetTotalMemory(false),
            p.Threads.Count,
            p.HandleCount,
            File.Exists(dbPath) ? new FileInfo(dbPath).Length : 0,
            system,
            _streamEpoch,
            _realtime);
    }

    public async Task<string> BuildDiagnosticsSummaryJsonAsync()
    {
        var p = Process.GetCurrentProcess();
        var summary = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            health = HealthPayload(),
            metrics = await BuildMetricsAsync(),
            network = new
            {
                addresses = NetworkInfo.GetLanIpv4Addresses(),
                interfaces = V4NetworkSnapshot.Capture(),
                httpPort = HttpPort,
                discoveryPort = _discoveryPort
            },
            runtime = new
            {
                os = Environment.OSVersion.ToString(),
                machine = Environment.MachineName,
                processorCount = Environment.ProcessorCount,
                is64BitProcess = Environment.Is64BitProcess,
                processId = Environment.ProcessId,
                startedAt = p.StartTime.ToUniversalTime(),
                workingSetBytes = p.WorkingSet64,
                privateMemoryBytes = p.PrivateMemorySize64,
                gcHeapBytes = GC.GetTotalMemory(false),
                threads = p.Threads.Count,
                handles = p.HandleCount,
                power = V4PowerSnapshot.Capture()
            },
            realtime = new
            {
                streamEpoch = _streamEpoch,
                latestSequence = _realtime.LatestSequence,
                oldestSequence = _realtime.OldestSequence,
                buffered = _realtime.BufferedCount,
                droppedFromBuffer = _realtime.DroppedCount,
                maxBuffer = _realtime.MaxBuffer
            },
            note = "Pilot diagnostics contain runtime/network counters and safe device identifiers. No pilot.db or business payload is included."
        };
        return JsonSerializer.Serialize(summary, V2Json.Options);
    }

    private async Task DiscoveryLoopAsync(CancellationToken ct)
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, _discoveryPort));
        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult packet;
            try { packet = await udp.ReceiveAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (SocketException ex)
            {
                Interlocked.Increment(ref _metrics.UdpErrorCount);
                V2DiagnosticLog.Write("UDP_ERROR", ex.SocketErrorCode + ": " + ex.Message);
                try { await Task.Delay(750, ct); } catch { break; }
                continue;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _metrics.UdpErrorCount);
                V2DiagnosticLog.Write("UDP_ERROR", ex.GetType().Name + ": " + ex.Message);
                try { await Task.Delay(750, ct); } catch { break; }
                continue;
            }

            var request = Encoding.UTF8.GetString(packet.Buffer).Trim();
            if (!string.Equals(request, "VHDCHY_DISCOVER_BETA_V1", StringComparison.Ordinal)) continue;
            Interlocked.Increment(ref _metrics.DiscoveryRequestCount);
            V2DiagnosticLog.Write("DISCOVERY", $"request remote={V4Safe.Ip(packet.RemoteEndPoint.Address)}; epoch={_streamEpoch[..8]}");
            var payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                ok = true,
                service = "VHDCHY_LAN_AGENT",
                environment = "BETA",
                protocol = AgentController.Protocol,
                instanceId = _instanceId,
                streamEpoch = _streamEpoch,
                httpPort = HttpPort,
                version = VersionInfo.Current,
                serverUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            try { await udp.SendAsync(payload, packet.RemoteEndPoint, ct); }
            catch (Exception ex) { V2DiagnosticLog.Write("DISCOVERY_REPLY_FAIL", ex.GetType().Name + ": " + ex.Message); }
        }
    }

    private async Task<string> RunLoadGenAsync(int clients, int requests, CancellationToken requestCt)
    {
        var exe = Path.Combine(AppContext.BaseDirectory, "tools", "VHDCHY.LanLoadGen.exe");
        if (!File.Exists(exe)) return "LoadGen executable not found in tools folder.";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestCt);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"http://127.0.0.1:{HttpPort} {clients} {requests}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi);
        if (process is null) return "Cannot start LoadGen.";
        var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        await process.WaitForExitAsync(timeout.Token);
        return (await stdout) + (await stderr);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_app is not null)
        {
            try
            {
                using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await _app.StopAsync(stop.Token);
            }
            catch { }
            await _app.DisposeAsync();
        }
        if (_udpTask is not null) { try { await _udpTask; } catch { } }
        _cts.Dispose();
    }
}

internal sealed class V4RealtimeHub
{
    private readonly object _gate = new();
    private readonly Queue<V4RealtimeEvent> _events = new();
    private TaskCompletionSource<bool> _signal = NewSignal();
    private long _sequence;
    private long _dropped;
    public int MaxBuffer { get; }
    public long LatestSequence => Interlocked.Read(ref _sequence);
    public long DroppedCount => Interlocked.Read(ref _dropped);
    public int BufferedCount { get { lock (_gate) return _events.Count; } }
    public long OldestSequence { get { lock (_gate) return _events.Count == 0 ? LatestSequence + 1 : _events.Peek().Sequence; } }

    public V4RealtimeHub(int maxBuffer) => MaxBuffer = Math.Max(256, maxBuffer);

    public V4RealtimeEvent Publish(string messageId, string deviceId, string payload)
    {
        var seq = Interlocked.Increment(ref _sequence);
        var evt = new V4RealtimeEvent(seq, messageId, V4Safe.Device(deviceId), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Encoding.UTF8.GetByteCount(payload), payload);
        TaskCompletionSource<bool> wake;
        lock (_gate)
        {
            _events.Enqueue(evt);
            while (_events.Count > MaxBuffer)
            {
                _events.Dequeue();
                Interlocked.Increment(ref _dropped);
            }
            wake = _signal;
            _signal = NewSignal();
        }
        wake.TrySetResult(true);
        return evt;
    }

    public async Task<V4PollResult> PollAsync(long after, int timeoutMs, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            Task wait;
            lock (_gate)
            {
                var oldest = _events.Count == 0 ? _sequence + 1 : _events.Peek().Sequence;
                var latest = _sequence;
                var resync = after > 0 && after < oldest - 1;
                var effectiveAfter = resync ? oldest - 1 : after;
                var found = _events.Where(x => x.Sequence > effectiveAfter).Take(256).ToArray();
                if (found.Length > 0 || timeoutMs <= 0 || resync)
                    return new V4PollResult(found, latest, oldest, resync);
                wait = _signal.Task;
            }

            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                lock (_gate)
                {
                    var oldest = _events.Count == 0 ? _sequence + 1 : _events.Peek().Sequence;
                    return new V4PollResult(Array.Empty<V4RealtimeEvent>(), _sequence, oldest, false);
                }
            }
            try { await wait.WaitAsync(remaining, ct); }
            catch (TimeoutException)
            {
                lock (_gate)
                {
                    var oldest = _events.Count == 0 ? _sequence + 1 : _events.Peek().Sequence;
                    return new V4PollResult(Array.Empty<V4RealtimeEvent>(), _sequence, oldest, false);
                }
            }
        }
    }

    private static TaskCompletionSource<bool> NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal record V4RealtimePublish(string MessageId, string DeviceId, string? Payload);
internal record V4RealtimeEvent(long Sequence, string MessageId, string DeviceId, long PublishedAtUnixMs, int PayloadBytes, string Payload);
internal record V4PollResult(V4RealtimeEvent[] Events, long LatestSequence, long OldestSequence, bool RequiresResync);

internal enum V4DeviceActivity { Echo, Event, RealtimePublish, RealtimePoll, Upload, Download }

internal sealed class V4Metrics
{
    public long RequestCount, ErrorCount, ClientCancelledCount, DuplicateCount, ActiveRealtimePollers, ResyncRequiredCount, EpochResetCount, UdpErrorCount, DiscoveryRequestCount;
    private long _realtimePublishes, _realtimePayloadBytes, _uploadBytes, _downloadBytes;
    private readonly ConcurrentQueue<double> _latencies = new();
    private readonly ConcurrentQueue<DateTimeOffset> _recentRequests = new();
    private readonly ConcurrentDictionary<string, V4DeviceStat> _devices = new(StringComparer.Ordinal);
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public void AddRequest()
    {
        Interlocked.Increment(ref RequestCount);
        _recentRequests.Enqueue(DateTimeOffset.UtcNow);
        TrimRecent();
    }

    public void AddLatency(double ms)
    {
        _latencies.Enqueue(ms);
        while (_latencies.Count > 4096) _latencies.TryDequeue(out _);
    }

    public void TouchDevice(string? deviceId, IPAddress? remote, V4DeviceActivity activity)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return;
        var stat = _devices.GetOrAdd(deviceId, id => new V4DeviceStat(id, remote));
        stat.Touch(remote, activity);
    }

    public void AddRealtimePublish(long bytes)
    {
        Interlocked.Increment(ref _realtimePublishes);
        Interlocked.Add(ref _realtimePayloadBytes, bytes);
    }

    public void AddUpload(string? deviceId, long bytes)
    {
        Interlocked.Add(ref _uploadBytes, bytes);
        if (!string.IsNullOrWhiteSpace(deviceId) && _devices.TryGetValue(deviceId, out var d)) Interlocked.Add(ref d.UploadBytes, bytes);
    }

    public void AddDownload(string? deviceId, long bytes)
    {
        Interlocked.Add(ref _downloadBytes, bytes);
        if (!string.IsNullOrWhiteSpace(deviceId) && _devices.TryGetValue(deviceId, out var d)) Interlocked.Add(ref d.DownloadBytes, bytes);
    }

    private void TrimRecent()
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1);
        while (_recentRequests.TryPeek(out var t) && t < cutoff) _recentRequests.TryDequeue(out _);
    }

    public object Snapshot(long storedEvents, long workingSetBytes, long gcHeapBytes, int threads, int handles, long dbBytes, V2SystemSnapshot system, string streamEpoch, V4RealtimeHub realtime)
    {
        TrimRecent();
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1);
        var physical = _devices.Values.Count(x => !x.IsSynthetic && x.LastSeen >= cutoff);
        var synthetic = _devices.Values.Count(x => x.IsSynthetic && x.LastSeen >= cutoff);
        var list = _latencies.ToArray();
        Array.Sort(list);
        double P(double p) => list.Length == 0 ? 0 : list[Math.Min(list.Length - 1, (int)Math.Ceiling((list.Length - 1) * p))];
        var devices = _devices.Values
            .Where(x => !x.IsSynthetic)
            .OrderByDescending(x => x.LastSeen)
            .Take(20)
            .Select(x => x.Snapshot())
            .ToArray();
        return new
        {
            ok = true,
            service = "VHDCHY_LAN_AGENT",
            environment = "BETA",
            protocol = AgentController.Protocol,
            uptimeSeconds = (long)(DateTimeOffset.UtcNow - StartedAt).TotalSeconds,
            requests = Interlocked.Read(ref RequestCount),
            requestsPerMinute = _recentRequests.Count,
            errors = Interlocked.Read(ref ErrorCount),
            clientCancelled = Interlocked.Read(ref ClientCancelledCount),
            duplicates = Interlocked.Read(ref DuplicateCount),
            activePhysicalPda60s = physical,
            activeSynthetic60s = synthetic,
            discoveryRequests = Interlocked.Read(ref DiscoveryRequestCount),
            udpErrors = Interlocked.Read(ref UdpErrorCount),
            storedEvents,
            latencyMs = new { p50 = P(.50), p95 = P(.95), p99 = P(.99), samples = list.Length },
            realtime = new
            {
                publishes = Interlocked.Read(ref _realtimePublishes),
                payloadBytes = Interlocked.Read(ref _realtimePayloadBytes),
                activePollers = Interlocked.Read(ref ActiveRealtimePollers),
                streamEpoch,
                latestSequence = realtime.LatestSequence,
                oldestSequence = realtime.OldestSequence,
                buffered = realtime.BufferedCount,
                droppedFromBuffer = realtime.DroppedCount,
                resyncRequired = Interlocked.Read(ref ResyncRequiredCount),
                epochResets = Interlocked.Read(ref EpochResetCount)
            },
            transfer = new { uploadBytes = Interlocked.Read(ref _uploadBytes), downloadBytes = Interlocked.Read(ref _downloadBytes) },
            process = new { workingSetBytes, gcHeapBytes, threads, handles },
            localDbBytes = dbBytes,
            system,
            devices,
            version = VersionInfo.Current,
            dataDirectory = DataPaths.GetDataDirectory()
        };
    }
}

internal sealed class V4DeviceStat
{
    private readonly object _gate = new();
    public string DeviceId { get; }
    public bool IsSynthetic { get; }
    public DateTimeOffset FirstSeen { get; }
    public DateTimeOffset LastSeen { get; private set; }
    public string RemoteIp { get; private set; }
    public long Echoes, Events, RealtimePublishes, RealtimePolls, Uploads, Downloads, UploadBytes, DownloadBytes;

    public V4DeviceStat(string id, IPAddress? remote)
    {
        DeviceId = id;
        IsSynthetic = id.StartsWith("synthetic-", StringComparison.OrdinalIgnoreCase);
        FirstSeen = LastSeen = DateTimeOffset.UtcNow;
        RemoteIp = V4Safe.Ip(remote);
    }

    public void Touch(IPAddress? remote, V4DeviceActivity activity)
    {
        lock (_gate)
        {
            LastSeen = DateTimeOffset.UtcNow;
            if (remote is not null) RemoteIp = V4Safe.Ip(remote);
        }
        switch (activity)
        {
            case V4DeviceActivity.Echo: Interlocked.Increment(ref Echoes); break;
            case V4DeviceActivity.Event: Interlocked.Increment(ref Events); break;
            case V4DeviceActivity.RealtimePublish: Interlocked.Increment(ref RealtimePublishes); break;
            case V4DeviceActivity.RealtimePoll: Interlocked.Increment(ref RealtimePolls); break;
            case V4DeviceActivity.Upload: Interlocked.Increment(ref Uploads); break;
            case V4DeviceActivity.Download: Interlocked.Increment(ref Downloads); break;
        }
    }

    public object Snapshot() => new
    {
        device = V4Safe.Device(DeviceId),
        remoteIp = RemoteIp,
        firstSeen = FirstSeen,
        lastSeen = LastSeen,
        echoes = Interlocked.Read(ref Echoes),
        events = Interlocked.Read(ref Events),
        realtimePublishes = Interlocked.Read(ref RealtimePublishes),
        realtimePolls = Interlocked.Read(ref RealtimePolls),
        uploads = Interlocked.Read(ref Uploads),
        downloads = Interlocked.Read(ref Downloads),
        uploadBytes = Interlocked.Read(ref UploadBytes),
        downloadBytes = Interlocked.Read(ref DownloadBytes)
    };
}

internal static class V4NetworkSnapshot
{
    public static object[] Capture()
    {
        var list = new List<object>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var props = nic.GetIPProperties();
                list.Add(new
                {
                    name = nic.Name,
                    type = nic.NetworkInterfaceType.ToString(),
                    status = nic.OperationalStatus.ToString(),
                    speedMbps = nic.Speed > 0 ? nic.Speed / 1_000_000d : 0,
                    ipv4 = props.UnicastAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork).Select(x => x.Address.ToString()).ToArray(),
                    gateways = props.GatewayAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork).Select(x => x.Address.ToString()).ToArray()
                });
            }
        }
        catch { }
        return list.ToArray();
    }
}

internal static class V4PowerSnapshot
{
    public static object Capture()
    {
        try
        {
            var p = SystemInformation.PowerStatus;
            return new
            {
                batteryPercent = p.BatteryLifePercent < 0 ? (double?)null : p.BatteryLifePercent * 100d,
                powerLine = p.PowerLineStatus.ToString(),
                batteryStatus = p.BatteryChargeStatus.ToString(),
                batteryLifeRemainingSeconds = p.BatteryLifeRemaining
            };
        }
        catch { return new { available = false }; }
    }
}

internal record V4ReleaseInfo(string Tag, string Version, string HtmlUrl, string AgentZipUrl, string AgentShaUrl, string ApkUrl, string ApkShaUrl);

internal static class V4ReleaseManager
{
    private static readonly HttpClient Http = Create();

    private static HttpClient Create()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("VHDCHY-LanAgent-Beta/0.4");
        return h;
    }

    public static async Task<V4ReleaseInfo?> GetLatestAsync()
    {
        using var response = await Http.GetAsync("https://api.github.com/repos/tamnv2/vanhanhdchungyen/releases?per_page=20");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("prerelease", out var pre) || !pre.GetBoolean()) continue;
            var tag = item.GetProperty("tag_name").GetString() ?? "";
            if (!tag.StartsWith("lan-pilot-beta-v", StringComparison.OrdinalIgnoreCase)) continue;
            var version = tag["lan-pilot-beta-v".Length..];
            if (!Version.TryParse(version, out _)) continue;
            var html = item.GetProperty("html_url").GetString() ?? "https://github.com/tamnv2/vanhanhdchungyen/releases";
            string agentZip = "", agentSha = "", apk = "", apkSha = "";
            if (item.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    var url = asset.GetProperty("browser_download_url").GetString() ?? "";
                    if (name.Equals("VHDCHY-LAN-Agent-BETA-win-x64.zip", StringComparison.OrdinalIgnoreCase)) agentZip = url;
                    else if (name.Equals("VHDCHY-LAN-Agent-BETA-win-x64.sha256", StringComparison.OrdinalIgnoreCase)) agentSha = url;
                    else if (name.Equals("VHDCHY-LAN-Pilot-BETA.apk", StringComparison.OrdinalIgnoreCase)) apk = url;
                    else if (name.Equals("VHDCHY-LAN-Pilot-BETA.apk.sha256", StringComparison.OrdinalIgnoreCase)) apkSha = url;
                }
            }
            return new V4ReleaseInfo(tag, version, html, agentZip, agentSha, apk, apkSha);
        }
        return null;
    }

    public static async Task<string> DownloadTextAsync(string url) => await Http.GetStringAsync(url);
    public static async Task<byte[]> DownloadBytesAsync(string url) => await Http.GetByteArrayAsync(url);
}

internal static class V4Version
{
    public static bool IsNewer(string candidate, string current)
    {
        if (!Version.TryParse(candidate, out var c) || !Version.TryParse(current, out var now)) return false;
        return c.CompareTo(now) > 0;
    }
}

internal sealed record V4StagedUpdate(string ScriptPath, string Version, string StatusFile);

internal static class V4AgentUpdater
{
    public static async Task<V4StagedUpdate> StageAsync(V4ReleaseInfo release)
    {
        if (string.IsNullOrWhiteSpace(release.AgentZipUrl) || string.IsNullOrWhiteSpace(release.AgentShaUrl))
            throw new InvalidOperationException("Release thiếu asset Agent ZIP/SHA256.");
        if (!V4Version.IsNewer(release.Version, VersionInfo.Current))
            throw new InvalidOperationException("Không có phiên bản mới hơn phiên bản đang chạy.");

        var installDir = Path.GetFullPath(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        EnsureWritable(installDir);
        var updateRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VHDCHY", "LanAgentBeta", "updates", release.Version);
        var stage = Path.Combine(updateRoot, "stage");
        var backup = Path.Combine(updateRoot, "backup");
        var zipPath = Path.Combine(updateRoot, "package.zip");
        var statusFile = Path.Combine(updateRoot, "update-status.txt");
        if (Directory.Exists(updateRoot)) Directory.Delete(updateRoot, true);
        Directory.CreateDirectory(stage);
        Directory.CreateDirectory(backup);

        V2DiagnosticLog.Write("UPDATE_STAGE", $"start current={VersionInfo.Current}; target={release.Version}; install={installDir}");
        var zip = await V4ReleaseManager.DownloadBytesAsync(release.AgentZipUrl);
        var shaText = await V4ReleaseManager.DownloadTextAsync(release.AgentShaUrl);
        var expected = shaText.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var actual = Convert.ToHexString(SHA256.HashData(zip)).ToLowerInvariant();
        if (!actual.Equals(expected.Trim().ToLowerInvariant(), StringComparison.Ordinal))
            throw new InvalidDataException($"SHA256 mismatch. expected={expected} actual={actual}");
        await File.WriteAllBytesAsync(zipPath, zip);
        ZipFile.ExtractToDirectory(zipPath, stage, true);
        if (!File.Exists(Path.Combine(stage, "VHDCHY.LanAgent.exe"))) throw new InvalidDataException("Staged package thiếu VHDCHY.LanAgent.exe");

        V4FileCopy.CopyDirectory(installDir, backup, skipDirectory: updateRoot);
        var script = Path.Combine(updateRoot, "apply-update.cmd");
        var scriptText = BuildScript(Environment.ProcessId, installDir, stage, backup, statusFile);
        await File.WriteAllTextAsync(script, scriptText, Encoding.ASCII);
        V2DiagnosticLog.Write("UPDATE_STAGE", $"verified target={release.Version}; sha256={actual}; stage={stage}");
        return new V4StagedUpdate(script, release.Version, statusFile);
    }

    public static void Launch(V4StagedUpdate staged)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c start \"\" /min \"{staged.ScriptPath}\"",
            WorkingDirectory = Path.GetDirectoryName(staged.ScriptPath)!,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi);
        V2DiagnosticLog.Write("UPDATE_APPLY", $"helper launched target={staged.Version}");
    }

    private static void EnsureWritable(string dir)
    {
        var probe = Path.Combine(dir, ".vhdchy-write-probe-" + Guid.NewGuid().ToString("N"));
        try { File.WriteAllText(probe, "ok"); File.Delete(probe); }
        catch (Exception ex) { throw new UnauthorizedAccessException("Thư mục Agent hiện tại không cho user thường tự cập nhật: " + dir, ex); }
    }

    private static string BuildScript(int pid, string install, string stage, string backup, string status)
    {
        static string Q(string s) => s.Replace("%", "%%");
        return $"""
@echo off
setlocal EnableExtensions
set "PID={pid}"
set "INSTALL={Q(install)}"
set "STAGE={Q(stage)}"
set "BACKUP={Q(backup)}"
set "STATUS={Q(status)}"
echo WAITING>%STATUS%
:wait_loop
tasklist /FI "PID eq %PID%" /NH 2>nul | findstr /R /C:"[ ]%PID[ ]" >nul
if not errorlevel 1 (
  timeout /t 1 /nobreak >nul
  goto wait_loop
)
echo APPLYING>%STATUS%
robocopy "%STAGE%" "%INSTALL%" /E /COPY:DAT /R:2 /W:1 /NFL /NDL /NJH /NJS /NP >nul
if errorlevel 8 goto rollback
start "" "%INSTALL%\VHDCHY.LanAgent.exe"
timeout /t 5 /nobreak >nul
for /L %%i in (1,1,5) do (
  powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $r=Invoke-WebRequest -UseBasicParsing 'http://127.0.0.1:17891/health' -TimeoutSec 2; exit [int]($r.StatusCode -ne 200)" >nul 2>&1
  if not errorlevel 1 goto success
  timeout /t 2 /nobreak >nul
)
:rollback
echo ROLLBACK>%STATUS%
taskkill /IM VHDCHY.LanAgent.exe /F >nul 2>&1
robocopy "%BACKUP%" "%INSTALL%" /E /COPY:DAT /R:2 /W:1 /NFL /NDL /NJH /NJS /NP >nul
start "" "%INSTALL%\VHDCHY.LanAgent.exe"
echo ROLLED_BACK>%STATUS%
exit /b 2
:success
echo SUCCESS>%STATUS%
exit /b 0
""";
    }
}

internal static class V4FileCopy
{
    public static void CopyDirectory(string source, string destination, string? skipDirectory = null)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            var name = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destination, name), true);
        }
        foreach (var dir in Directory.GetDirectories(source))
        {
            if (skipDirectory is not null && Path.GetFullPath(dir).StartsWith(Path.GetFullPath(skipDirectory), StringComparison.OrdinalIgnoreCase)) continue;
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)), skipDirectory);
        }
    }
}

internal sealed class V4TrayContext : ApplicationContext
{
    private readonly V4AgentController _controller;
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private readonly System.Windows.Forms.Timer _autoUpdateTimer;
    private string? _lastNotifiedVersion;

    public V4TrayContext(V4AgentController controller)
    {
        _controller = controller;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Mở LAN Dashboard / Test Center", null, (_, _) => _controller.OpenDashboard());
        menu.Items.Add("Xuất log chẩn đoán...", null, async (_, _) => await ExportDiagnosticsAsync());
        menu.Items.Add("Mở thư mục dữ liệu", null, (_, _) => _controller.OpenDataFolder());
        menu.Items.Add("Chọn thư mục dữ liệu...", null, async (_, _) => await SelectDataFolderAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Kiểm tra / cập nhật Agent", null, async (_, _) => await CheckUpdateAsync(true));
        menu.Items.Add("Bật/tắt tự khởi động theo user", null, (_, _) => ToggleAutoStart());
        menu.Items.Add("Khởi động lại Agent", null, async (_, _) => await RestartAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Thoát Agent", null, async (_, _) => await ExitAsync());

        _tray = new NotifyIcon { Icon = SystemIcons.Application, Text = "VHDCHY LAN Agent BETA", Visible = true, ContextMenuStrip = menu };
        _tray.DoubleClick += (_, _) => _controller.OpenDashboard();

        _statusTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _statusTimer.Tick += (_, _) => UpdateTrayTooltip();
        _statusTimer.Start();
        UpdateTrayTooltip();

        _autoUpdateTimer = new System.Windows.Forms.Timer { Interval = 6 * 60 * 60 * 1000 };
        _autoUpdateTimer.Tick += async (_, _) => await CheckUpdateAsync(false);
        _autoUpdateTimer.Start();
        _ = Task.Run(async () => { await Task.Delay(12000); await CheckUpdateAsync(false); });
    }

    private void UpdateTrayTooltip()
    {
        try
        {
            var s = _controller.SampleSystem();
            var t = $"CPU {s.CpuPercent:0}%\nRAM {s.MemoryUsedPercent:0}%\nDisk {s.DiskUsedPercent:0}%\nNetwork {V2Format.Rate(s.NetworkBytesPerSecond)}";
            _tray.Text = t.Length > 127 ? t[..127] : t;
        }
        catch { _tray.Text = "VHDCHY LAN Agent BETA"; }
    }

    private async Task CheckUpdateAsync(bool manual)
    {
        try
        {
            var release = await _controller.CheckUpdateAsync();
            if (release is null)
            {
                if (manual) MessageBox.Show("Không đọc được LAN Pilot BETA release hợp lệ.", "Cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var newer = V4Version.IsNewer(release.Version, VersionInfo.Current);
            V2DiagnosticLog.Write("UPDATE_CHECK", $"current={VersionInfo.Current}; latest={release.Version}; newer={newer}; manual={manual}");
            if (!newer)
            {
                if (manual) MessageBox.Show($"Agent đang ở bản mới nhất.\r\nHiện tại: {VersionInfo.Current}\r\nRelease: {release.Version}", "Cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!manual)
            {
                if (!string.Equals(_lastNotifiedVersion, release.Version, StringComparison.OrdinalIgnoreCase))
                {
                    _lastNotifiedVersion = release.Version;
                    Notify($"Có bản mới {release.Version}. Chuột phải → Kiểm tra / cập nhật Agent.", ToolTipIcon.Info);
                }
                return;
            }

            var go = MessageBox.Show(
                $"Phiên bản hiện tại: {VersionInfo.Current}\r\nPhiên bản mới: {release.Version}\r\n\r\nTải, kiểm tra SHA256 và cập nhật ngay?\r\nNếu updater bị policy công ty chặn, vẫn có thể dùng trang tải thủ công.",
                "Cập nhật VHDCHY LAN Agent BETA", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
            if (go == DialogResult.Cancel) return;
            if (go == DialogResult.No) { Shell.Open(release.HtmlUrl); return; }

            var staged = await V4AgentUpdater.StageAsync(release);
            V4AgentUpdater.Launch(staged);
            _tray.Visible = false;
            ExitThread();
        }
        catch (Exception ex)
        {
            V2DiagnosticLog.Write("UPDATE_FAIL", ex.GetType().Name + ": " + ex.Message);
            if (manual)
            {
                var open = MessageBox.Show("Cập nhật tự động thất bại. Không dùng quyền Admin để vượt policy.\r\n\r\n" + ex.Message + "\r\n\r\nMở GitHub Release để cập nhật thủ công?", "Cập nhật", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (open == DialogResult.Yes) Shell.Open("https://github.com/tamnv2/vanhanhdchungyen/releases");
            }
        }
    }

    private async Task ExportDiagnosticsAsync()
    {
        using var d = new SaveFileDialog { Title = "Xuất log chẩn đoán VHDCHY LAN Agent", Filter = "ZIP diagnostics (*.zip)|*.zip", FileName = $"VHDCHY-LAN-Agent-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip", AddExtension = true, DefaultExt = "zip" };
        if (d.ShowDialog() != DialogResult.OK) return;
        try { await _controller.ExportDiagnosticsAsync(d.FileName); MessageBox.Show("Đã xuất log ZIP.", "VHDCHY LAN Agent", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Xuất log thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task RestartAsync()
    {
        try { await _controller.RestartAsync(); Notify("Agent đã khởi động lại.", ToolTipIcon.Info); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Restart Agent thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task SelectDataFolderAsync()
    {
        using var d = new FolderBrowserDialog { Description = "Chọn thư mục cha cho local database VHDCHY LAN BETA" };
        if (d.ShowDialog() != DialogResult.OK) return;
        try
        {
            var target = await _controller.ChangeDataDirectoryAsync(d.SelectedPath);
            MessageBox.Show("Đã chuyển local data tới:\r\n" + target, "VHDCHY LAN Agent", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show("Không thể đổi data directory; đã rollback.\r\n\r\n" + ex.Message, "VHDCHY LAN Agent", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ToggleAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            const string name = "VHDCHY LAN Agent BETA";
            var current = key?.GetValue(name) as string;
            if (string.IsNullOrWhiteSpace(current))
            {
                key?.SetValue(name, '"' + Environment.ProcessPath! + '"');
                Notify("Đã bật auto-start theo user (HKCU).", ToolTipIcon.Info);
            }
            else
            {
                key?.DeleteValue(name, false);
                Notify("Đã tắt auto-start theo user.", ToolTipIcon.Info);
            }
        }
        catch (Exception ex) { MessageBox.Show("Corporate policy có thể chặn HKCU auto-start. Agent vẫn chạy thủ công được.\r\n\r\n" + ex.Message, "Auto-start", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void Notify(string text, ToolTipIcon icon)
    {
        _tray.BalloonTipTitle = "VHDCHY LAN Agent BETA";
        _tray.BalloonTipText = text;
        _tray.BalloonTipIcon = icon;
        _tray.ShowBalloonTip(5000);
    }

    private async Task ExitAsync()
    {
        _statusTimer.Stop();
        _autoUpdateTimer.Stop();
        _tray.Visible = false;
        await _controller.StopAsync();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _statusTimer.Dispose();
            _autoUpdateTimer.Dispose();
            _tray.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal static class V4Safe
{
    public static string Device(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "—";
        if (id.StartsWith("synthetic-", StringComparison.OrdinalIgnoreCase)) return id;
        return id.Length <= 10 ? id : id[..4] + "…" + id[^6..];
    }
    public static string Ip(IPAddress? ip) => ip?.ToString() ?? "—";
    public static string Epoch(string? epoch) => string.IsNullOrWhiteSpace(epoch) ? "—" : epoch.Length <= 8 ? epoch : epoch[..8];
}

internal static class V4Math
{
    public static double Mbps(long bytes, double ms) => ms <= 0 ? 0 : bytes * 8d / ms / 1000d;
}

internal static class V4Dashboard
{
    public const string Html = """
<!doctype html><html lang="vi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>VHDCHY LAN BETA</title><style>
body{font-family:Segoe UI,Arial;margin:20px;max-width:1200px;color:#202124}h1{font-size:22px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(185px,1fr));gap:9px}.c{border:1px solid #d5d8dc;border-radius:10px;padding:11px}.v{font-size:20px;font-weight:650;margin:4px 0}.d{font-size:12px;color:#5f6368;line-height:1.35}.ok{color:#137333}.warn{color:#b06000}.bad{color:#b3261e}button,.btn{padding:8px 11px;border:1px solid #aaa;border-radius:7px;background:#fff;color:#111;text-decoration:none;display:inline-block;cursor:pointer;margin:3px}pre{white-space:pre-wrap}.note{padding:10px;background:#f5f5f5;border-radius:8px}table{border-collapse:collapse;width:100%;font-size:12px}th,td{border-bottom:1px solid #ddd;padding:5px;text-align:left}</style></head>
<body><h1>VHDCHY LAN Agent — BETA V4</h1><p>Thiết kế cho laptop công ty user thường: không yêu cầu Admin/router/DNS/firewall. Realtime dùng async long-poll, không polling 50 ms trong Agent.</p>
<h2>Tài nguyên</h2><div class="grid">
<div class="c">CPU máy<div class="v" id="cpu">…</div><div class="d">Tải CPU toàn laptop.</div></div>
<div class="c">RAM máy<div class="v" id="mem">…</div><div class="d">RAM toàn laptop đang dùng.</div></div>
<div class="c">Agent CPU<div class="v" id="agentcpu">…</div><div class="d">CPU riêng Agent; mục tiêu idle gần 0.</div></div>
<div class="c">Agent RAM<div class="v" id="ram">…</div><div class="d">Working set riêng Agent.</div></div>
<div class="c">Threads<div class="v" id="threads">…</div><div class="d">Số thread Agent; theo dõi rò rỉ thread.</div></div>
<div class="c">Network máy<div class="v" id="network">…</div><div class="d">Tổng send+receive gần đây của laptop.</div></div></div>
<h2>Kết nối / request</h2><div class="grid">
<div class="c">Agent<div class="v" id="health">…</div><div class="d">ONLINE = local API đang hoạt động.</div></div>
<div class="c">PDA thật 60s<div class="v" id="physical">0</div><div class="d">Chỉ device thật; synthetic được tách riêng.</div></div>
<div class="c">Synthetic 60s<div class="v" id="synthetic">0</div><div class="d">Client LoadGen, không đại diện RF/Wi-Fi.</div></div>
<div class="c">Request/phút<div class="v" id="rpm">0</div><div class="d">Request 60 giây gần nhất.</div></div>
<div class="c">Errors<div class="v" id="errors">0</div><div class="d">Lỗi server thật. Client cancel long-poll được tách riêng.</div></div>
<div class="c">Client cancel<div class="v" id="cancelled">0</div><div class="d">Long-poll bị client đóng/restart; không tính lỗi server.</div></div>
<div class="c">p95<div class="v" id="p95">0 ms</div><div class="d">95% request nhanh hơn mức này.</div></div>
<div class="c">Durable events<div class="v" id="events">0</div><div class="d">Event đã ghi SQLite.</div></div></div>
<h2>Realtime</h2><div class="grid">
<div class="c">Stream epoch<div class="v" id="epoch">…</div><div class="d">Đổi khi Agent restart; client phải reset cursor/resync.</div></div>
<div class="c">Latest seq<div class="v" id="seq">0</div><div class="d">Sequence hiện tại trong epoch.</div></div>
<div class="c">Buffer<div class="v" id="buffer">0</div><div class="d">Notification đang giữ trong RAM.</div></div>
<div class="c">Dropped<div class="v" id="dropped">0</div><div class="d">Notification cũ rơi khỏi bounded buffer; nghiệp vụ phải resync canonical state.</div></div>
<div class="c">Resync required<div class="v" id="resync">0</div><div class="d">Client cursor cũ hơn buffer và được yêu cầu resync.</div></div>
<div class="c">Epoch reset<div class="v" id="epochreset">0</div><div class="d">Client phát hiện Agent restart/stream mới.</div></div>
<div class="c">Realtime last<div class="v" id="rtlast">—</div><div class="d">Tin gần nhất dashboard nhận.</div></div></div>
<h2>Load test Agent</h2><p>
<button onclick="load(10)">10 clients</button><button onclick="load(25)">25 clients</button><button onclick="load(50)">50 clients</button><button onclick="load(100)">100 clients</button></p><pre id="loadout">Chưa chạy.</pre>
<h2>PDA thật đã thấy</h2><div id="devices">—</div>
<h2>LAN / dữ liệu</h2><pre id="net">…</pre><code id="data">…</code>
<h2>Chẩn đoán & cập nhật</h2><p><a class="btn" href="/api/pilot/diagnostics/export">Xuất log Agent (.zip)</a> <button onclick="checkUpdate()">Kiểm tra cập nhật</button> <span id="update"></span></p>
<p class="note"><b>Power policy:</b> Agent idle dựa trên async wait/event, tray refresh 5 giây. PDA production target: realtime chỉ khi app foreground; app ra nền chỉ giữ tiến trình đang dở/pending queue trong cửa sổ hữu hạn rồi dừng.</p>
<script>
let rtAfter=0,rtEpoch='';
function rate(b){if(b>=1048576)return (b/1048576).toFixed(1)+' MB/s';if(b>=1024)return (b/1024).toFixed(1)+' KB/s';return b.toFixed(0)+' B/s'}
function safe(x){return x??'—'}
async function refresh(){try{let [h,m,n]=await Promise.all([fetch('/health').then(r=>r.json()),fetch('/api/pilot/metrics').then(r=>r.json()),fetch('/api/pilot/network').then(r=>r.json())]);health.textContent=h.ok?'ONLINE':'ERROR';health.className='v '+(h.ok?'ok':'bad');physical.textContent=m.activePhysicalPda60s;synthetic.textContent=m.activeSynthetic60s;rpm.textContent=m.requestsPerMinute;errors.textContent=m.errors;cancelled.textContent=m.clientCancelled;p95.textContent=m.latencyMs.p95.toFixed(1)+' ms';ram.textContent=(m.process.workingSetBytes/1048576).toFixed(1)+' MB';threads.textContent=m.process.threads;events.textContent=m.storedEvents;cpu.textContent=m.system.cpuPercent.toFixed(1)+'%';mem.textContent=m.system.memoryUsedPercent.toFixed(1)+'%';network.textContent=rate(m.system.networkBytesPerSecond);agentcpu.textContent=m.system.agentCpuPercent.toFixed(2)+'%';epoch.textContent=m.realtime.streamEpoch.slice(0,8);seq.textContent=m.realtime.latestSequence;buffer.textContent=m.realtime.buffered;dropped.textContent=m.realtime.droppedFromBuffer;resync.textContent=m.realtime.resyncRequired;epochreset.textContent=m.realtime.epochResets;net.textContent=n.interfaces.map(x=>x.name+' | '+x.type+' | '+x.status+' | '+x.ipv4.join(',')).join('\n');data.textContent=m.dataDirectory;devices.innerHTML='<table><tr><th>Device</th><th>IP</th><th>Last</th><th>Echo</th><th>Event</th><th>RT pub</th><th>RT poll</th></tr>'+m.devices.map(d=>'<tr><td>'+d.device+'</td><td>'+d.remoteIp+'</td><td>'+d.lastSeen+'</td><td>'+d.echoes+'</td><td>'+d.events+'</td><td>'+d.realtimePublishes+'</td><td>'+d.realtimePolls+'</td></tr>').join('')+'</table>'}catch(e){health.textContent='UNREACHABLE';health.className='v bad'}}
async function realtime(){while(true){try{let u='/api/pilot/realtime/poll?after='+rtAfter+'&timeoutMs=8000&deviceId=dashboard&epoch='+encodeURIComponent(rtEpoch);let r=await fetch(u).then(x=>x.json());if(r.epochChanged){rtEpoch=r.streamEpoch;rtAfter=0;continue}rtEpoch=r.streamEpoch;if(r.requiresResync&&r.oldestSequence>0)rtAfter=r.oldestSequence-1;for(let e of (r.events||[])){rtAfter=Math.max(rtAfter,e.sequence);rtlast.textContent=e.deviceId+' #'+e.sequence+' | '+Math.max(0,Date.now()-e.publishedAtUnixMs)+' ms'}}catch(e){await new Promise(r=>setTimeout(r,1000))}}}
async function load(c){loadout.textContent='Đang chạy '+c+' clients…';try{loadout.textContent=await fetch('/api/pilot/selftest/load?clients='+c+'&requests=100',{method:'POST'}).then(r=>r.text())}catch(e){loadout.textContent='FAIL '+e}}
async function checkUpdate(){update.textContent='đang kiểm tra…';try{let x=await fetch('/api/pilot/update/check').then(r=>r.json());update.textContent=x.latest?(x.updateAvailable?('Có bản mới '+x.latest.version):('Đã mới nhất '+x.currentVersion)):'Không đọc được release'}catch(e){update.textContent='Lỗi kiểm tra'}}
refresh();setInterval(refresh,3000);realtime();
</script></body></html>
""";
}
