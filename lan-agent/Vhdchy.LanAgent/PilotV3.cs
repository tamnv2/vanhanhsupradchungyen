using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Vhdchy.LanAgent;

internal static class V3Program
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
            var controller = new V3AgentController();
            controller.StartAsync().GetAwaiter().GetResult();
            Application.Run(new V3TrayContext(controller));
            controller.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "LAN Agent không thể khởi động. Không dùng quyền Admin để vượt policy công ty.\r\n\r\n" + ex.Message,
                "VHDCHY LAN Agent BETA", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

internal sealed class V3AgentController : IAsyncDisposable
{
    private V3AgentHost? _host;
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
            V2DiagnosticLog.Write("START", $"V3 Agent start | version={VersionInfo.Current} | data={DataDirectory}");
            _host = new V3AgentHost(DataDirectory, AgentController.DefaultHttpPort, AgentController.DefaultDiscoveryPort, _telemetry);
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
            V2DiagnosticLog.Write("STOP", "V3 Agent stopped");
        }
        finally { _gate.Release(); }
    }

    public async Task RestartAsync() { await StopAsync(); await StartAsync(); }
    public void OpenDashboard() => Shell.Open(DashboardUrl);
    public void OpenDataFolder() => Shell.Open(DataDirectory);
    public Task<ReleaseInfo?> CheckUpdateAsync() => ReleaseChecker.GetLatestPilotReleaseAsync();

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
            return target;
        }
        catch
        {
            DataPaths.SetDataDirectory(source);
            await StartAsync();
            throw;
        }
    }

    public async Task ExportDiagnosticsAsync(string destination)
    {
        var summary = _host is null
            ? JsonSerializer.Serialize(new { service = "VHDCHY_LAN_AGENT", environment = "BETA", running = false, version = VersionInfo.Current }, V2Json.Options)
            : await _host.BuildDiagnosticsSummaryJsonAsync();
        await V2DiagnosticExporter.ExportToFileAsync(DataDirectory, destination, summary);
        V2DiagnosticLog.Write("EXPORT", "V3 diagnostic bundle exported by user");
    }

    public ValueTask DisposeAsync() => new(StopAsync());
}

internal sealed class V3AgentHost : IAsyncDisposable
{
    private const int MaxTransferBytes = 64 * 1024 * 1024;
    private readonly string _dataDirectory;
    private readonly int _discoveryPort;
    private readonly V3Metrics _metrics = new();
    private readonly V2SystemTelemetrySampler _telemetry;
    private readonly V3RealtimeHub _realtime = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly string _instanceId;
    private WebApplication? _app;
    private Task? _udpTask;
    public int HttpPort { get; }

    public V3AgentHost(string dataDirectory, int httpPort, int discoveryPort, V2SystemTelemetrySampler telemetry)
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
                    V2DiagnosticLog.Write("HTTP", $"{context.Request.Method} {context.Request.Path} -> {context.Response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _metrics.ErrorCount);
                V2DiagnosticLog.Write("HTTP_ERROR", $"{context.Request.Method} {context.Request.Path} | {ex.GetType().Name}: {ex.Message}");
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
        app.MapPost("/api/pilot/echo", (EchoRequest request) =>
        {
            _metrics.TouchDevice(request.DeviceId);
            return Results.Json(new { ok = true, service = "VHDCHY_LAN_AGENT", environment = "BETA", protocol = AgentController.Protocol, serverTime = DateTimeOffset.UtcNow, request.DeviceId, request.Payload });
        });
        app.MapPost("/api/pilot/event", async (PilotEvent request) =>
        {
            if (string.IsNullOrWhiteSpace(request.EventId) || string.IsNullOrWhiteSpace(request.DeviceId) || request.DeviceSeq <= 0)
                return Results.BadRequest(new { ok = false, code = "INVALID_EVENT" });
            _metrics.TouchDevice(request.DeviceId);
            var inserted = await SqliteStore.InsertEventAsync(Path.Combine(_dataDirectory, "pilot.db"), request);
            if (!inserted) Interlocked.Increment(ref _metrics.DuplicateCount);
            V2DiagnosticLog.Write(inserted ? "EVENT" : "DUPLICATE", $"device={V3Safe.Device(request.DeviceId)}; seq={request.DeviceSeq}; accepted={inserted}");
            return Results.Json(new { ok = true, accepted = inserted, duplicate = !inserted, request.EventId, request.DeviceId, request.DeviceSeq });
        });

        app.MapPost("/api/pilot/realtime/publish", (V3RealtimePublish request) =>
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.MessageId))
                return Results.BadRequest(new { ok = false, code = "INVALID_REALTIME" });
            var payload = request.Payload ?? "";
            if (Encoding.UTF8.GetByteCount(payload) > 16 * 1024) return Results.BadRequest(new { ok = false, code = "PAYLOAD_TOO_LARGE" });
            _metrics.TouchDevice(request.DeviceId);
            var evt = _realtime.Publish(request.MessageId, request.DeviceId, request.ClientSentAtUnixMs, payload);
            _metrics.AddRealtimePublish(evt.PayloadBytes);
            return Results.Json(new { ok = true, sequence = evt.Sequence, publishedAtUnixMs = evt.PublishedAtUnixMs, payloadBytes = evt.PayloadBytes });
        });

        app.MapGet("/api/pilot/realtime/poll", async (HttpContext context) =>
        {
            var after = long.TryParse(context.Request.Query["after"], out var a) ? Math.Max(0, a) : 0;
            var timeoutMs = int.TryParse(context.Request.Query["timeoutMs"], out var t) ? Math.Clamp(t, 0, 15000) : 10000;
            var deviceId = context.Request.Query["deviceId"].ToString();
            _metrics.TouchDevice(deviceId);
            Interlocked.Increment(ref _metrics.ActiveRealtimePollers);
            try
            {
                var events = await _realtime.PollAsync(after, timeoutMs, context.RequestAborted);
                return Results.Json(new { ok = true, events, latestSequence = _realtime.LatestSequence, serverNowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
            }
            finally { Interlocked.Decrement(ref _metrics.ActiveRealtimePollers); }
        });

        app.MapPost("/api/pilot/transfer/upload", async (HttpContext context) =>
        {
            var deviceId = context.Request.Query["deviceId"].ToString();
            _metrics.TouchDevice(deviceId);
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
            _metrics.AddUpload(total);
            V2DiagnosticLog.Write("TRANSFER_UP", $"device={V3Safe.Device(deviceId)}; bytes={total}; ms={sw.Elapsed.TotalMilliseconds:0.0}");
            await context.Response.WriteAsJsonAsync(new { ok = true, bytes = total, serverElapsedMs = sw.Elapsed.TotalMilliseconds }, context.RequestAborted);
        });

        app.MapGet("/api/pilot/transfer/download", async (HttpContext context) =>
        {
            var deviceId = context.Request.Query["deviceId"].ToString();
            _metrics.TouchDevice(deviceId);
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
            _metrics.AddDownload(bytes);
            V2DiagnosticLog.Write("TRANSFER_DOWN", $"device={V3Safe.Device(deviceId)}; bytes={bytes}; ms={sw.Elapsed.TotalMilliseconds:0.0}");
        });

        app.MapGet("/api/pilot/metrics", async () => Results.Json(await BuildMetricsAsync()));
        app.MapGet("/api/pilot/network", () => Results.Json(new { ok = true, addresses = NetworkInfo.GetLanIpv4Addresses(), httpPort = HttpPort, discoveryPort = _discoveryPort, note = "No-admin pilot; LAN IP/discovery only." }));
        app.MapGet("/api/pilot/update/check", async () => Results.Json(new { ok = true, currentVersion = VersionInfo.Current, latest = await ReleaseChecker.GetLatestPilotReleaseAsync() }));
        app.MapGet("/api/pilot/diagnostics/export", async (HttpContext context) =>
        {
            if (context.Connection.RemoteIpAddress is not { } remote || !IPAddress.IsLoopback(remote)) return Results.StatusCode(StatusCodes.Status403Forbidden);
            var bytes = await V2DiagnosticExporter.CreateZipBytesAsync(_dataDirectory, await BuildDiagnosticsSummaryJsonAsync());
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
        app.MapGet("/", () => Results.Content(V3Dashboard.Html, "text/html; charset=utf-8"));

        await app.StartAsync(_cts.Token);
        _app = app;
        _udpTask = Task.Run(() => DiscoveryLoopAsync(_cts.Token));
        V2DiagnosticLog.Write("LISTEN", $"V3 HTTP={HttpPort}; UDP={_discoveryPort}; IPs={string.Join(',', NetworkInfo.GetLanIpv4Addresses())}");
    }

    private object HealthPayload() => new
    {
        ok = true, service = "VHDCHY_LAN_AGENT", environment = "BETA", protocol = AgentController.Protocol,
        instanceId = _instanceId, version = VersionInfo.Current, httpPort = HttpPort, discoveryPort = _discoveryPort,
        addresses = NetworkInfo.GetLanIpv4Addresses(), startedAt = _metrics.StartedAt,
        startedAtUnixMs = _metrics.StartedAt.ToUnixTimeMilliseconds(), lanPriorityPolicy = "PREFER_LAN_WHEN_AGENT_HEALTHY",
        capabilities = new[] { "AUTO_LAN", "REALTIME_LONG_POLL", "TRANSFER_TEST", "LOAD_TEST", "DIAGNOSTICS" }
    };

    private async Task<object> BuildMetricsAsync()
    {
        var dbPath = Path.Combine(_dataDirectory, "pilot.db");
        var stored = await SqliteStore.CountEventsAsync(dbPath);
        var p = Process.GetCurrentProcess();
        var system = _telemetry.Sample(_dataDirectory);
        return _metrics.Snapshot(stored, p.WorkingSet64, File.Exists(dbPath) ? new FileInfo(dbPath).Length : 0, system, _realtime.LatestSequence, _realtime.BufferedCount);
    }

    public async Task<string> BuildDiagnosticsSummaryJsonAsync()
    {
        return JsonSerializer.Serialize(new
        {
            exportedAt = DateTimeOffset.UtcNow,
            health = HealthPayload(),
            metrics = await BuildMetricsAsync(),
            network = new { addresses = NetworkInfo.GetLanIpv4Addresses(), httpPort = HttpPort, discoveryPort = _discoveryPort },
            realtime = new { latestSequence = _realtime.LatestSequence, buffered = _realtime.BufferedCount },
            note = "Pilot diagnostics only; no business DB/payload is included."
        }, V2Json.Options);
    }

    private async Task DiscoveryLoopAsync(CancellationToken ct)
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, _discoveryPort));
        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult packet;
            try { packet = await udp.ReceiveAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                V2DiagnosticLog.Write("UDP_ERROR", ex.GetType().Name + ": " + ex.Message);
                try { await Task.Delay(500, ct); } catch { break; }
                continue;
            }
            var request = Encoding.UTF8.GetString(packet.Buffer).Trim();
            if (!string.Equals(request, "VHDCHY_DISCOVER_BETA_V1", StringComparison.Ordinal)) continue;
            V2DiagnosticLog.Write("DISCOVERY", $"request from {packet.RemoteEndPoint.Address}");
            var payload = JsonSerializer.SerializeToUtf8Bytes(new { ok = true, service = "VHDCHY_LAN_AGENT", environment = "BETA", protocol = AgentController.Protocol, instanceId = _instanceId, httpPort = HttpPort, version = VersionInfo.Current });
            try { await udp.SendAsync(payload, packet.RemoteEndPoint, ct); } catch { }
        }
    }

    private async Task<string> RunLoadGenAsync(int clients, int requests, CancellationToken requestCt)
    {
        var baseDir = AppContext.BaseDirectory;
        var exe = Path.Combine(baseDir, "tools", "VHDCHY.LanLoadGen.exe");
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
        using var p = Process.Start(psi);
        if (p is null) return "Cannot start LoadGen.";
        var stdout = p.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = p.StandardError.ReadToEndAsync(timeout.Token);
        await p.WaitForExitAsync(timeout.Token);
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

internal sealed class V3RealtimeHub
{
    private readonly object _gate = new();
    private readonly Queue<V3RealtimeEvent> _events = new();
    private long _sequence;
    public long LatestSequence => Interlocked.Read(ref _sequence);
    public int BufferedCount { get { lock (_gate) return _events.Count; } }

    public V3RealtimeEvent Publish(string messageId, string deviceId, long clientSentAtUnixMs, string payload)
    {
        var seq = Interlocked.Increment(ref _sequence);
        var evt = new V3RealtimeEvent(seq, messageId, V3Safe.Device(deviceId), clientSentAtUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Encoding.UTF8.GetByteCount(payload), payload);
        lock (_gate)
        {
            _events.Enqueue(evt);
            while (_events.Count > 4096) _events.Dequeue();
        }
        return evt;
    }

    public async Task<V3RealtimeEvent[]> PollAsync(long after, int timeoutMs, CancellationToken ct)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (true)
        {
            V3RealtimeEvent[] found;
            lock (_gate) found = _events.Where(x => x.Sequence > after).Take(256).ToArray();
            if (found.Length > 0 || timeoutMs <= 0 || Environment.TickCount64 >= deadline) return found;
            await Task.Delay(50, ct);
        }
    }
}

internal sealed class V3Metrics
{
    public long RequestCount, ErrorCount, DuplicateCount, ActiveRealtimePollers;
    private long _realtimePublishes, _realtimePayloadBytes, _uploadBytes, _downloadBytes;
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    private readonly ConcurrentQueue<double> _latencies = new();
    private readonly ConcurrentQueue<DateTimeOffset> _recentRequests = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _devices = new(StringComparer.Ordinal);

    public void AddRequest() { Interlocked.Increment(ref RequestCount); _recentRequests.Enqueue(DateTimeOffset.UtcNow); TrimRecent(); }
    public void AddLatency(double ms) { _latencies.Enqueue(ms); while (_latencies.Count > 4096) _latencies.TryDequeue(out _); }
    public void TouchDevice(string? deviceId) { if (!string.IsNullOrWhiteSpace(deviceId)) _devices[deviceId] = DateTimeOffset.UtcNow; }
    public void AddRealtimePublish(long bytes) { Interlocked.Increment(ref _realtimePublishes); Interlocked.Add(ref _realtimePayloadBytes, bytes); }
    public void AddUpload(long bytes) => Interlocked.Add(ref _uploadBytes, bytes);
    public void AddDownload(long bytes) => Interlocked.Add(ref _downloadBytes, bytes);
    private void TrimRecent() { var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1); while (_recentRequests.TryPeek(out var t) && t < cutoff) _recentRequests.TryDequeue(out _); }

    public object Snapshot(long storedEvents, long workingSetBytes, long dbBytes, V2SystemSnapshot system, long latestRealtimeSeq, int realtimeBuffered)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1);
        foreach (var kv in _devices) if (kv.Value < cutoff) _devices.TryRemove(kv.Key, out _);
        TrimRecent();
        var list = _latencies.ToArray(); Array.Sort(list);
        double P(double p) => list.Length == 0 ? 0 : list[Math.Min(list.Length - 1, (int)Math.Ceiling((list.Length - 1) * p))];
        return new
        {
            ok = true, service = "VHDCHY_LAN_AGENT", environment = "BETA", protocol = AgentController.Protocol,
            uptimeSeconds = (long)(DateTimeOffset.UtcNow - StartedAt).TotalSeconds,
            requests = Interlocked.Read(ref RequestCount), requestsPerMinute = _recentRequests.Count,
            errors = Interlocked.Read(ref ErrorCount), duplicates = Interlocked.Read(ref DuplicateCount), activeDevices60s = _devices.Count,
            storedEvents, latencyMs = new { p50 = P(.50), p95 = P(.95), p99 = P(.99), samples = list.Length },
            realtime = new { publishes = Interlocked.Read(ref _realtimePublishes), payloadBytes = Interlocked.Read(ref _realtimePayloadBytes), activePollers = Interlocked.Read(ref ActiveRealtimePollers), latestSequence = latestRealtimeSeq, buffered = realtimeBuffered },
            transfer = new { uploadBytes = Interlocked.Read(ref _uploadBytes), downloadBytes = Interlocked.Read(ref _downloadBytes) },
            processWorkingSetBytes = workingSetBytes, localDbBytes = dbBytes, system, version = VersionInfo.Current, dataDirectory = DataPaths.GetDataDirectory()
        };
    }
}

internal sealed class V3TrayContext : ApplicationContext
{
    private readonly V3AgentController _controller;
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private readonly System.Windows.Forms.Timer _autoUpdateTimer;
    private string? _lastReleaseTag;

    public V3TrayContext(V3AgentController controller)
    {
        _controller = controller;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Mở LAN Dashboard / Test Center", null, (_, _) => _controller.OpenDashboard());
        menu.Items.Add("Xuất log chẩn đoán...", null, async (_, _) => await ExportDiagnosticsAsync());
        menu.Items.Add("Mở thư mục dữ liệu", null, (_, _) => _controller.OpenDataFolder());
        menu.Items.Add("Chọn thư mục dữ liệu...", null, async (_, _) => await SelectDataFolderAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Kiểm tra cập nhật", null, async (_, _) => await CheckUpdateAsync(true));
        menu.Items.Add("Bật/tắt tự khởi động theo user", null, (_, _) => ToggleAutoStart());
        menu.Items.Add("Khởi động lại Agent", null, async (_, _) => await RestartAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Thoát Agent", null, async (_, _) => await ExitAsync());
        _tray = new NotifyIcon { Icon = SystemIcons.Application, Text = "VHDCHY LAN Agent BETA", Visible = true, ContextMenuStrip = menu };
        _tray.DoubleClick += (_, _) => _controller.OpenDashboard();
        _statusTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _statusTimer.Tick += (_, _) => UpdateTrayTooltip(); _statusTimer.Start(); UpdateTrayTooltip();
        _autoUpdateTimer = new System.Windows.Forms.Timer { Interval = 6 * 60 * 60 * 1000 };
        _autoUpdateTimer.Tick += async (_, _) => await CheckUpdateAsync(false); _autoUpdateTimer.Start();
        _ = Task.Run(async () => { await Task.Delay(10000); await CheckUpdateAsync(false); });
    }

    private void UpdateTrayTooltip()
    {
        try { var s = _controller.SampleSystem(); var t = $"CPU {s.CpuPercent:0}%\nRAM {s.MemoryUsedPercent:0}%\nDisk {s.DiskUsedPercent:0}%\nNetwork {V2Format.Rate(s.NetworkBytesPerSecond)}"; _tray.Text = t.Length > 127 ? t[..127] : t; }
        catch { _tray.Text = "VHDCHY LAN Agent BETA"; }
    }

    private async Task ExportDiagnosticsAsync()
    {
        using var d = new SaveFileDialog { Title = "Xuất log chẩn đoán VHDCHY LAN Agent", Filter = "ZIP diagnostics (*.zip)|*.zip", FileName = $"VHDCHY-LAN-Agent-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip", AddExtension = true, DefaultExt = "zip" };
        if (d.ShowDialog() != DialogResult.OK) return;
        try { await _controller.ExportDiagnosticsAsync(d.FileName); MessageBox.Show("Đã xuất log ZIP.", "VHDCHY LAN Agent", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Xuất log thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private async Task SelectDataFolderAsync()
    {
        using var d = new FolderBrowserDialog { Description = "Chọn thư mục cha cho local database VHDCHY LAN BETA" };
        if (d.ShowDialog() != DialogResult.OK) return;
        try { await _controller.ChangeDataDirectoryAsync(d.SelectedPath); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể đổi data directory", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private async Task RestartAsync() { try { await _controller.RestartAsync(); Notify("Agent đã khởi động lại", ToolTipIcon.Info); } catch (Exception ex) { MessageBox.Show(ex.Message); } }
    private async Task CheckUpdateAsync(bool manual)
    {
        try
        {
            var r = await _controller.CheckUpdateAsync(); if (r is null) return;
            if (manual)
            {
                if (MessageBox.Show($"Phiên bản Agent: {VersionInfo.Current}\r\nRelease mới nhất: {r.Tag}\r\n\r\nMở trang tải?", "Cập nhật", MessageBoxButtons.YesNo) == DialogResult.Yes) Shell.Open(r.HtmlUrl);
            }
            else if (!string.Equals(_lastReleaseTag, r.Tag, StringComparison.OrdinalIgnoreCase)) { _lastReleaseTag = r.Tag; Notify("Có release mới: " + r.Tag, ToolTipIcon.Info); }
        }
        catch (Exception ex) { if (manual) MessageBox.Show(ex.Message, "Kiểm tra cập nhật thất bại"); }
    }
    private void ToggleAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            const string name = "VHDCHY LAN Agent BETA"; var cur = key?.GetValue(name) as string;
            if (string.IsNullOrWhiteSpace(cur)) key?.SetValue(name, '"' + Environment.ProcessPath! + '"'); else key?.DeleteValue(name, false);
        }
        catch (Exception ex) { MessageBox.Show("Policy có thể chặn auto-start. Agent vẫn chạy thủ công.\r\n" + ex.Message); }
    }
    private void Notify(string message, ToolTipIcon icon) { _tray.BalloonTipTitle = "VHDCHY LAN Agent BETA"; _tray.BalloonTipText = message; _tray.BalloonTipIcon = icon; _tray.ShowBalloonTip(5000); }
    private async Task ExitAsync() { _statusTimer.Stop(); _autoUpdateTimer.Stop(); _tray.Visible = false; await _controller.StopAsync(); ExitThread(); }
    protected override void Dispose(bool disposing) { if (disposing) { _statusTimer.Dispose(); _autoUpdateTimer.Dispose(); _tray.Dispose(); } base.Dispose(disposing); }
}

internal record V3RealtimePublish(string MessageId, string DeviceId, long ClientSentAtUnixMs, string? Payload);
internal record V3RealtimeEvent(long Sequence, string MessageId, string DeviceId, long ClientSentAtUnixMs, long PublishedAtUnixMs, int PayloadBytes, string Payload);

internal static class V3Safe
{
    public static string Device(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "unknown";
        var s = id.Trim();
        return s.Length <= 12 ? s : s[..4] + "…" + s[^6..];
    }
}

internal static class V3Dashboard
{
    public const string Html = """
<!doctype html><html lang="vi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>VHDCHY LAN BETA Test Center</title>
<style>body{font-family:Segoe UI,Arial;margin:22px;max-width:1180px;color:#202124}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(185px,1fr));gap:9px}.c{border:1px solid #d5d8dc;border-radius:9px;padding:11px}.v{font-size:20px;font-weight:650}.d{font-size:12px;color:#5f6368}.panel{padding:12px;border:1px solid #ddd;border-radius:9px;margin:12px 0}button,.btn{padding:8px 11px;margin:3px;border:1px solid #aaa;border-radius:7px;background:#fff;cursor:pointer}.ok{font-weight:700}pre{white-space:pre-wrap;max-height:260px;overflow:auto}.warn{background:#fff8e1;padding:9px;border-radius:7px}</style></head><body>
<h1>VHDCHY LAN BETA — Test Center</h1><p class="warn"><b>LAN priority:</b> khi Agent BETA hợp lệ khả dụng, APK pilot phải ưu tiên LAN. Không sửa router/firewall/DNS và không dùng Admin.</p>
<div class="grid"><div class="c">Agent<div class="v" id="health">...</div><div class="d">Local service health.</div></div><div class="c">PDA 60s<div class="v" id="devices">0</div><div class="d">Thiết bị vừa giao tiếp trong 60 giây.</div></div><div class="c">Req/phút<div class="v" id="rpm">0</div></div><div class="c">p95<div class="v" id="p95">0 ms</div></div><div class="c">Errors<div class="v" id="errors">0</div></div><div class="c">Agent RAM<div class="v" id="ram">0 MB</div></div><div class="c">Agent CPU<div class="v" id="agentcpu">0%</div></div><div class="c">Realtime pollers<div class="v" id="pollers">0</div></div><div class="c">Realtime publish<div class="v" id="rtpub">0</div></div><div class="c">Upload tổng<div class="v" id="up">0 MB</div></div><div class="c">Download tổng<div class="v" id="down">0 MB</div></div><div class="c">Events SQLite<div class="v" id="events">0</div></div></div>
<div class="panel"><h2>Realtime giữa PDA ↔ laptop ↔ PDA</h2><p>Dashboard đang long-poll Agent. Khi PDA khác publish, số dưới đây phản ánh thời gian từ Agent publish tới lúc browser nhận/hiển thị.</p><div class="grid"><div class="c">Tin đã nhận<div class="v" id="rtrecv">0</div></div><div class="c">Last display latency<div class="v" id="rtlast">—</div></div><div class="c">Display p95<div class="v" id="rtp95">—</div></div><div class="c">Last source<div class="v" id="rtsrc">—</div></div></div><p><button onclick="publishRt(1,32)">Gửi 1 realtime</button><button onclick="publishRt(200,2048)">Nặng: 200 × 2KB</button></p></div>
<div class="panel"><h2>Synthetic load — chỉ đo sức chịu tải Agent</h2><p>Chỉ chạy được khi mở dashboard bằng 127.0.0.1 trên laptop. Không đại diện cho sức chịu tải Wi‑Fi với số PDA tương đương.</p><button onclick="load(10)">10 clients</button><button onclick="load(25)">25 clients</button><button onclick="load(50)">50 clients</button><button onclick="load(100)">100 clients</button><pre id="loadout">Chưa chạy.</pre></div>
<div class="panel"><h2>Kết nối / Chẩn đoán</h2><pre id="net">...</pre><p><a class="btn" href="/api/pilot/diagnostics/export">Xuất log Agent (.zip)</a> <button onclick="checkUpdate()">Kiểm tra cập nhật</button> <span id="update"></span></p></div>
<script>
let after=0,rtCount=0,rtLat=[];const webId='web-'+Math.random().toString(16).slice(2);
function mb(b){return (b/1048576).toFixed(1)+' MB'};function pct(a,p){if(!a.length)return 0;let x=[...a].sort((a,b)=>a-b);return x[Math.min(x.length-1,Math.ceil((x.length-1)*p))]}
async function refresh(){try{let h=await fetch('/health').then(r=>r.json()),m=await fetch('/api/pilot/metrics').then(r=>r.json()),n=await fetch('/api/pilot/network').then(r=>r.json());health.textContent=h.ok?'ONLINE':'ERROR';devices.textContent=m.activeDevices60s;rpm.textContent=m.requestsPerMinute;p95.textContent=m.latencyMs.p95.toFixed(1)+' ms';errors.textContent=m.errors;ram.textContent=mb(m.processWorkingSetBytes);agentcpu.textContent=m.system.agentCpuPercent.toFixed(2)+'%';pollers.textContent=m.realtime.activePollers;rtpub.textContent=m.realtime.publishes;up.textContent=mb(m.transfer.uploadBytes);down.textContent=mb(m.transfer.downloadBytes);events.textContent=m.storedEvents;net.textContent=n.addresses.map(x=>'http://'+x+':'+n.httpPort+'/').join('\n')}catch(e){health.textContent='UNREACHABLE'}}
async function rtLoop(){while(true){try{let x=await fetch('/api/pilot/realtime/poll?after='+after+'&timeoutMs=10000&deviceId='+webId).then(r=>r.json());for(let e of x.events){after=Math.max(after,e.sequence);let l=Math.max(0,Date.now()-e.publishedAtUnixMs);rtLat.push(l);if(rtLat.length>500)rtLat.shift();rtCount++;rtrecv.textContent=rtCount;rtlast.textContent=l+' ms';rtp95.textContent=pct(rtLat,.95).toFixed(0)+' ms';rtsrc.textContent=e.deviceId}}catch(e){await new Promise(r=>setTimeout(r,500))}}}
async function publishRt(count,size){let payload='X'.repeat(size);for(let i=0;i<count;i++)await fetch('/api/pilot/realtime/publish',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({messageId:crypto.randomUUID?crypto.randomUUID():(Date.now()+'-'+i),deviceId:webId,clientSentAtUnixMs:Date.now(),payload})});}
async function load(c){loadout.textContent='Đang chạy '+c+' clients...';try{loadout.textContent=await fetch('/api/pilot/selftest/load?clients='+c+'&requests=100',{method:'POST'}).then(r=>r.text())}catch(e){loadout.textContent='FAIL: '+e}}
async function checkUpdate(){update.textContent='đang kiểm tra...';try{let x=await fetch('/api/pilot/update/check').then(r=>r.json());update.textContent=x.latest?('Latest: '+x.latest.tag):'Chưa có release'}catch(e){update.textContent='Lỗi'}}
refresh();setInterval(refresh,1000);rtLoop();
</script></body></html>
""";
}
