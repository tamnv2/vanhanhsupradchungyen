using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Vhdchy.LanAgent;

internal static class V2Program
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

            var controller = new V2AgentController();
            controller.StartAsync().GetAwaiter().GetResult();
            Application.Run(new V2TrayContext(controller));
            controller.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "LAN Agent không thể khởi động. Có thể laptop công ty đang chặn ứng dụng/port. Không cần và không nên dùng quyền Admin để vượt policy.\r\n\r\n" + ex.Message,
                "VHDCHY LAN Agent BETA", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

internal sealed class V2AgentController : IAsyncDisposable
{
    private V2AgentHost? _host;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly V2SystemTelemetrySampler _telemetry = new();

    public string DataDirectory => DataPaths.GetDataDirectory();
    public int HttpPort => _host?.HttpPort ?? AgentController.DefaultHttpPort;
    public string DashboardUrl => $"http://127.0.0.1:{HttpPort}/";
    public V2SystemSnapshot SampleSystem() => _telemetry.Sample(DataDirectory);
    public V2AgentHost? Host => _host;

    public async Task StartAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_host is not null) return;
            Directory.CreateDirectory(DataDirectory);
            V2DiagnosticLog.Initialize(DataDirectory);
            V2DiagnosticLog.Write("START", $"Agent start | version={VersionInfo.Current} | data={DataDirectory}");
            _host = new V2AgentHost(DataDirectory, AgentController.DefaultHttpPort, AgentController.DefaultDiscoveryPort, _telemetry);
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
            V2DiagnosticLog.Write("STOP", "Agent stopped");
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
            V2DiagnosticLog.Write("DATA_DIR", "Data directory changed successfully");
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
        V2DiagnosticLog.Write("EXPORT", "Diagnostic bundle exported by user");
    }

    public ValueTask DisposeAsync() => new(StopAsync());
}

internal sealed class V2AgentHost : IAsyncDisposable
{
    private readonly string _dataDirectory;
    private readonly int _discoveryPort;
    private readonly V2Metrics _metrics = new();
    private readonly V2SystemTelemetrySampler _telemetry;
    private readonly CancellationTokenSource _cts = new();
    private readonly string _instanceId;
    private WebApplication? _app;
    private Task? _udpTask;
    public int HttpPort { get; }

    public V2AgentHost(string dataDirectory, int httpPort, int discoveryPort, V2SystemTelemetrySampler telemetry)
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
                if (context.Response.StatusCode >= 400)
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
            V2DiagnosticLog.Write(inserted ? "EVENT" : "DUPLICATE", $"deviceSeq={request.DeviceSeq}; accepted={inserted}");
            return Results.Json(new { ok = true, accepted = inserted, duplicate = !inserted, request.EventId, request.DeviceId, request.DeviceSeq });
        });

        app.MapGet("/api/pilot/metrics", async () => Results.Json(await BuildMetricsAsync()));
        app.MapGet("/api/pilot/network", () => Results.Json(new
        {
            ok = true,
            addresses = NetworkInfo.GetLanIpv4Addresses(),
            httpPort = HttpPort,
            discoveryPort = _discoveryPort,
            note = "Pilot assumes no admin, no router access and no DNS/firewall changes."
        }));
        app.MapGet("/api/pilot/update/check", async () => Results.Json(new { ok = true, currentVersion = VersionInfo.Current, latest = await ReleaseChecker.GetLatestPilotReleaseAsync() }));
        app.MapGet("/api/pilot/diagnostics/export", async (HttpContext context) =>
        {
            var remote = context.Connection.RemoteIpAddress;
            if (remote is null || !IPAddress.IsLoopback(remote)) return Results.StatusCode(StatusCodes.Status403Forbidden);
            var summary = await BuildDiagnosticsSummaryJsonAsync();
            var bytes = await V2DiagnosticExporter.CreateZipBytesAsync(_dataDirectory, summary);
            var name = $"VHDCHY-LAN-Agent-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
            V2DiagnosticLog.Write("EXPORT", "Diagnostic bundle downloaded from local dashboard");
            return Results.File(bytes, "application/zip", name);
        });
        app.MapGet("/", () => Results.Content(V2Dashboard.Html, "text/html; charset=utf-8"));

        await app.StartAsync(_cts.Token);
        _app = app;
        _udpTask = Task.Run(() => DiscoveryLoopAsync(_cts.Token));
        V2DiagnosticLog.Write("LISTEN", $"HTTP={HttpPort}; UDP={_discoveryPort}; IPs={string.Join(',', NetworkInfo.GetLanIpv4Addresses())}");
    }

    private object HealthPayload() => new
    {
        ok = true, service = "VHDCHY_LAN_AGENT", environment = "BETA", protocol = AgentController.Protocol,
        instanceId = _instanceId, version = VersionInfo.Current, httpPort = HttpPort, discoveryPort = _discoveryPort,
        addresses = NetworkInfo.GetLanIpv4Addresses(), startedAt = _metrics.StartedAt
    };

    private async Task<object> BuildMetricsAsync()
    {
        var dbPath = Path.Combine(_dataDirectory, "pilot.db");
        var stored = await SqliteStore.CountEventsAsync(dbPath);
        var p = Process.GetCurrentProcess();
        var system = _telemetry.Sample(_dataDirectory);
        return _metrics.Snapshot(stored, p.WorkingSet64, File.Exists(dbPath) ? new FileInfo(dbPath).Length : 0, system);
    }

    public async Task<string> BuildDiagnosticsSummaryJsonAsync()
    {
        var summary = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            health = HealthPayload(),
            metrics = await BuildMetricsAsync(),
            network = new { addresses = NetworkInfo.GetLanIpv4Addresses(), httpPort = HttpPort, discoveryPort = _discoveryPort },
            note = "Pilot diagnostics only; no business payload/database is included in export."
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
            catch (Exception ex)
            {
                V2DiagnosticLog.Write("UDP_ERROR", ex.GetType().Name + ": " + ex.Message);
                try { await Task.Delay(500, ct); } catch { break; }
                continue;
            }
            var request = Encoding.UTF8.GetString(packet.Buffer).Trim();
            if (!string.Equals(request, "VHDCHY_DISCOVER_BETA_V1", StringComparison.Ordinal)) continue;
            var payload = JsonSerializer.SerializeToUtf8Bytes(new { ok = true, service = "VHDCHY_LAN_AGENT", environment = "BETA", protocol = AgentController.Protocol, instanceId = _instanceId, httpPort = HttpPort, version = VersionInfo.Current });
            try { await udp.SendAsync(payload, packet.RemoteEndPoint, ct); } catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_app is not null)
        {
            try { await _app.StopAsync(TimeSpan.FromSeconds(3)); } catch { }
            await _app.DisposeAsync();
        }
        if (_udpTask is not null) { try { await _udpTask; } catch { } }
        _cts.Dispose();
    }
}

internal sealed class V2TrayContext : ApplicationContext
{
    private readonly V2AgentController _controller;
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private readonly System.Windows.Forms.Timer _autoUpdateTimer;
    private string? _lastReleaseTag;

    public V2TrayContext(V2AgentController controller)
    {
        _controller = controller;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Mở LAN Dashboard", null, (_, _) => _controller.OpenDashboard());
        menu.Items.Add("Trạng thái nhanh / Giải thích thông số", null, (_, _) => ShowQuickStatus());
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
        _statusTimer.Tick += (_, _) => UpdateTrayTooltip();
        _statusTimer.Start();
        UpdateTrayTooltip();

        _autoUpdateTimer = new System.Windows.Forms.Timer { Interval = 6 * 60 * 60 * 1000 };
        _autoUpdateTimer.Tick += async (_, _) => await CheckUpdateAsync(false);
        _autoUpdateTimer.Start();
        _ = Task.Run(async () => { await Task.Delay(10000); await CheckUpdateAsync(false); });
    }

    private void UpdateTrayTooltip()
    {
        try
        {
            var s = _controller.SampleSystem();
            var text = $"CPU {s.CpuPercent:0}%\nRAM {s.MemoryUsedPercent:0}%\nDisk {s.DiskUsedPercent:0}%\nNetwork {V2Format.Rate(s.NetworkBytesPerSecond)}";
            _tray.Text = text.Length > 127 ? text[..127] : text;
        }
        catch { _tray.Text = "VHDCHY LAN Agent BETA"; }
    }

    private void ShowQuickStatus()
    {
        var s = _controller.SampleSystem();
        MessageBox.Show(
            $"CPU {s.CpuPercent:0.0}% — tải CPU toàn laptop.\r\n" +
            $"RAM {s.MemoryUsedPercent:0.0}% — tỷ lệ RAM toàn laptop đang dùng.\r\n" +
            $"Disk {s.DiskUsedPercent:0.0}% — tỷ lệ ổ đĩa chứa dữ liệu Agent đang dùng.\r\n" +
            $"Network {V2Format.Rate(s.NetworkBytesPerSecond)} — tổng lưu lượng mạng gần đây của các card mạng đang hoạt động.\r\n\r\n" +
            $"Agent CPU {s.AgentCpuPercent:0.00}% — CPU riêng tiến trình Agent.\r\n" +
            $"Agent RAM {s.AgentWorkingSetBytes / 1048576d:0.0} MB — RAM riêng tiến trình Agent.\r\n\r\n" +
            "Các số này chỉ phục vụ đánh giá pilot; không cần quyền Admin.",
            "VHDCHY LAN Agent — Trạng thái nhanh", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task ExportDiagnosticsAsync()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Xuất log chẩn đoán VHDCHY LAN Agent",
            Filter = "ZIP diagnostics (*.zip)|*.zip",
            FileName = $"VHDCHY-LAN-Agent-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
            AddExtension = true,
            DefaultExt = "zip"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            await _controller.ExportDiagnosticsAsync(dialog.FileName);
            MessageBox.Show("Đã xuất log. Anh có thể tải file ZIP này vào ChatGPT để em phân tích.", "Xuất log thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Xuất log thất bại:\r\n\r\n" + ex.Message, "VHDCHY LAN Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RestartAsync()
    {
        try { await _controller.RestartAsync(); Notify("Agent đã khởi động lại", ToolTipIcon.Info); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể restart Agent", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task SelectDataFolderAsync()
    {
        using var dialog = new FolderBrowserDialog { Description = "Chọn thư mục cha cho local database VHDCHY LAN BETA" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            var target = await _controller.ChangeDataDirectoryAsync(dialog.SelectedPath);
            MessageBox.Show("Đã chuyển local data an toàn tới:\r\n" + target, "VHDCHY LAN Agent", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể đổi data directory; Agent đã rollback về vị trí cũ.\r\n\r\n" + ex.Message, "VHDCHY LAN Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task CheckUpdateAsync(bool manual)
    {
        try
        {
            var release = await _controller.CheckUpdateAsync();
            if (release is null)
            {
                if (manual) MessageBox.Show("Chưa có LAN Pilot BETA release trên GitHub.", "Cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (manual)
            {
                var open = MessageBox.Show($"Phiên bản Agent: {VersionInfo.Current}\r\nRelease mới nhất: {release.Tag}\r\n\r\nMở trang tải?", "Cập nhật VHDCHY LAN Agent BETA", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (open == DialogResult.Yes) Shell.Open(release.HtmlUrl);
            }
            else if (!string.Equals(_lastReleaseTag, release.Tag, StringComparison.OrdinalIgnoreCase))
            {
                _lastReleaseTag = release.Tag;
                Notify("Có LAN Pilot BETA release mới: " + release.Tag + ". Chuột phải → Kiểm tra cập nhật.", ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            V2DiagnosticLog.Write("UPDATE_FAIL", ex.GetType().Name + ": " + ex.Message);
            if (manual) MessageBox.Show("Kiểm tra cập nhật thất bại. Vẫn có thể tải thủ công từ GitHub Release.\r\n\r\n" + ex.Message, "Cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ToggleAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            const string name = "VHDCHY LAN Agent BETA";
            var current = key?.GetValue(name) as string;
            if (string.IsNullOrWhiteSpace(current)) { key?.SetValue(name, '"' + Environment.ProcessPath! + '"'); Notify("Đã bật auto-start theo user (HKCU).", ToolTipIcon.Info); }
            else { key?.DeleteValue(name, false); Notify("Đã tắt auto-start theo user.", ToolTipIcon.Info); }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Corporate policy có thể chặn HKCU auto-start. Agent vẫn chạy thủ công bình thường.\r\n\r\n" + ex.Message, "Auto-start", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Notify(string message, ToolTipIcon icon)
    {
        _tray.BalloonTipTitle = "VHDCHY LAN Agent BETA";
        _tray.BalloonTipText = message;
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
        if (disposing) { _statusTimer.Dispose(); _autoUpdateTimer.Dispose(); _tray.Dispose(); }
        base.Dispose(disposing);
    }
}

internal sealed class V2Metrics
{
    public long RequestCount;
    public long ErrorCount;
    public long DuplicateCount;
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    private readonly ConcurrentQueue<double> _latencies = new();
    private readonly ConcurrentQueue<DateTimeOffset> _recentRequests = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _devices = new(StringComparer.Ordinal);

    public void AddRequest()
    {
        Interlocked.Increment(ref RequestCount);
        _recentRequests.Enqueue(DateTimeOffset.UtcNow);
        TrimRecent();
    }
    public void AddLatency(double ms) { _latencies.Enqueue(ms); while (_latencies.Count > 2048) _latencies.TryDequeue(out _); }
    public void TouchDevice(string? deviceId) { if (!string.IsNullOrWhiteSpace(deviceId)) _devices[deviceId] = DateTimeOffset.UtcNow; }
    private void TrimRecent() { var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1); while (_recentRequests.TryPeek(out var t) && t < cutoff) _recentRequests.TryDequeue(out _); }

    public object Snapshot(long storedEvents, long workingSetBytes, long dbBytes, V2SystemSnapshot system)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1);
        foreach (var kv in _devices) if (kv.Value < cutoff) _devices.TryRemove(kv.Key, out _);
        TrimRecent();
        var list = _latencies.ToArray();
        Array.Sort(list);
        double P(double p) => list.Length == 0 ? 0 : list[Math.Min(list.Length - 1, (int)Math.Ceiling((list.Length - 1) * p))];
        return new
        {
            ok = true, service = "VHDCHY_LAN_AGENT", environment = "BETA", protocol = AgentController.Protocol,
            uptimeSeconds = (long)(DateTimeOffset.UtcNow - StartedAt).TotalSeconds,
            requests = Interlocked.Read(ref RequestCount), requestsPerMinute = _recentRequests.Count,
            errors = Interlocked.Read(ref ErrorCount), duplicates = Interlocked.Read(ref DuplicateCount), activeDevices60s = _devices.Count,
            storedEvents, latencyMs = new { p50 = P(.50), p95 = P(.95), p99 = P(.99), samples = list.Length },
            processWorkingSetBytes = workingSetBytes, localDbBytes = dbBytes, system, version = VersionInfo.Current,
            dataDirectory = DataPaths.GetDataDirectory()
        };
    }
}

internal sealed class V2SystemTelemetrySampler
{
    private readonly object _gate = new();
    private ulong _lastIdle, _lastKernel, _lastUser, _lastNet;
    private DateTimeOffset _lastAt;
    private TimeSpan _lastProcessCpu;
    private bool _initialized;

    public V2SystemSnapshot Sample(string dataDirectory)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            var process = Process.GetCurrentProcess();
            var cpu = 0d;
            var agentCpu = 0d;
            if (GetSystemTimes(out var idleFt, out var kernelFt, out var userFt))
            {
                var idle = ToUInt64(idleFt); var kernel = ToUInt64(kernelFt); var user = ToUInt64(userFt);
                if (_initialized)
                {
                    var total = (kernel - _lastKernel) + (user - _lastUser);
                    var idleDelta = idle - _lastIdle;
                    if (total > 0) cpu = Math.Clamp((total - idleDelta) * 100d / total, 0, 100);
                    var elapsedMs = Math.Max(1, (now - _lastAt).TotalMilliseconds);
                    agentCpu = Math.Clamp((process.TotalProcessorTime - _lastProcessCpu).TotalMilliseconds / elapsedMs / Math.Max(1, Environment.ProcessorCount) * 100d, 0, 100);
                }
                _lastIdle = idle; _lastKernel = kernel; _lastUser = user;
            }

            var memoryPercent = 0d;
            var mem = new MemoryStatusEx();
            mem.dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>();
            if (GlobalMemoryStatusEx(ref mem)) memoryPercent = mem.dwMemoryLoad;

            var diskPercent = 0d;
            try
            {
                var root = Path.GetPathRoot(Path.GetFullPath(dataDirectory));
                if (!string.IsNullOrWhiteSpace(root))
                {
                    var drive = new DriveInfo(root);
                    if (drive.IsReady && drive.TotalSize > 0) diskPercent = (drive.TotalSize - drive.AvailableFreeSpace) * 100d / drive.TotalSize;
                }
            }
            catch { }

            ulong network = 0;
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    var s = nic.GetIPv4Statistics();
                    network += (ulong)Math.Max(0, s.BytesReceived) + (ulong)Math.Max(0, s.BytesSent);
                }
            }
            catch { }
            var netRate = 0d;
            if (_initialized)
            {
                var seconds = Math.Max(.1, (now - _lastAt).TotalSeconds);
                if (network >= _lastNet) netRate = (network - _lastNet) / seconds;
            }

            _lastNet = network;
            _lastAt = now;
            _lastProcessCpu = process.TotalProcessorTime;
            _initialized = true;
            return new V2SystemSnapshot(cpu, memoryPercent, diskPercent, netRate, agentCpu, process.WorkingSet64);
        }
    }

    private static ulong ToUInt64(FileTime ft) => ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime { public uint dwLowDateTime; public uint dwHighDateTime; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint dwLength; public uint dwMemoryLoad;
        public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
    }
}

internal record V2SystemSnapshot(double CpuPercent, double MemoryUsedPercent, double DiskUsedPercent, double NetworkBytesPerSecond, double AgentCpuPercent, long AgentWorkingSetBytes);

internal static class V2DiagnosticLog
{
    private static readonly object Gate = new();
    private static string? _file;
    private const long MaxBytes = 2 * 1024 * 1024;
    public static string? FilePath => _file;

    public static void Initialize(string dataDirectory)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(dataDirectory);
            _file = Path.Combine(dataDirectory, "agent-diagnostic.log");
            RotateIfNeeded();
        }
    }

    public static void Write(string type, string message)
    {
        lock (Gate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_file)) return;
                RotateIfNeeded();
                File.AppendAllText(_file, $"{DateTimeOffset.UtcNow:O}\t{type}\t{message.Replace("\r", " ").Replace("\n", " ")}\r\n", Encoding.UTF8);
            }
            catch { }
        }
    }

    private static void RotateIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(_file) || !File.Exists(_file)) return;
        if (new FileInfo(_file).Length < MaxBytes) return;
        var old = _file + ".1";
        try { File.Delete(old); } catch { }
        try { File.Move(_file, old, true); } catch { }
    }
}

internal static class V2DiagnosticExporter
{
    public static async Task ExportToFileAsync(string dataDirectory, string destination, string summaryJson)
    {
        var bytes = await CreateZipBytesAsync(dataDirectory, summaryJson);
        await File.WriteAllBytesAsync(destination, bytes);
    }

    public static async Task<byte[]> CreateZipBytesAsync(string dataDirectory, string summaryJson)
    {
        await using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            var summary = zip.CreateEntry("summary.json", CompressionLevel.Fastest);
            await using (var s = summary.Open()) await s.WriteAsync(Encoding.UTF8.GetBytes(summaryJson));
            var readme = zip.CreateEntry("README.txt", CompressionLevel.Fastest);
            await using (var s = readme.Open())
            {
                var text = "VHDCHY LAN Pilot diagnostic export. Contains health/metrics/network and Agent diagnostic log. Does NOT include pilot.db or business data. Upload this ZIP to the VHDCHY ChatGPT session for analysis.\r\n";
                await s.WriteAsync(Encoding.UTF8.GetBytes(text));
            }
            foreach (var name in new[] { "agent-diagnostic.log", "agent-diagnostic.log.1" })
            {
                var path = Path.Combine(dataDirectory, name);
                if (!File.Exists(path)) continue;
                var e = zip.CreateEntry(name, CompressionLevel.Fastest);
                await using var output = e.Open();
                await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await input.CopyToAsync(output);
            }
        }
        return ms.ToArray();
    }
}

internal static class V2Format
{
    public static string Rate(double bytesPerSecond)
    {
        if (bytesPerSecond >= 1024 * 1024) return $"{bytesPerSecond / 1048576d:0.0} MB/s";
        if (bytesPerSecond >= 1024) return $"{bytesPerSecond / 1024d:0.0} KB/s";
        return $"{bytesPerSecond:0} B/s";
    }
}

internal static class V2Json
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
}

internal static class V2Dashboard
{
    public const string Html = """
<!doctype html><html lang="vi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>VHDCHY LAN BETA</title><style>body{font-family:Segoe UI,Arial;margin:24px;max-width:1050px;color:#202124}h1{font-size:22px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:10px}.c{border:1px solid #d5d8dc;border-radius:10px;padding:12px}.v{font-size:21px;font-weight:650;margin:4px 0}.d{font-size:12px;color:#5f6368;line-height:1.35}.ok{font-weight:600}code{word-break:break-all}button,.btn{padding:9px 12px;border:1px solid #aaa;border-radius:7px;background:#fff;color:#111;text-decoration:none;display:inline-block;cursor:pointer}pre{white-space:pre-wrap}.note{padding:10px;background:#f5f5f5;border-radius:8px}</style></head>
<body><h1>VHDCHY LAN Agent — BETA Pilot</h1><p>Chạy user-mode trên laptop công ty: không yêu cầu Admin, không giả định quyền router/DNS/firewall.</p>
<h2>Thông số cơ bản của laptop</h2><div class="grid">
<div class="c">CPU máy<div class="v" id="cpu">...</div><div class="d">Mức tải CPU toàn laptop. Dùng để biết pilot có làm máy bị nặng hay không.</div></div>
<div class="c">RAM máy<div class="v" id="mem">...</div><div class="d">Tỷ lệ RAM toàn laptop đang được sử dụng.</div></div>
<div class="c">Disk<div class="v" id="disk">...</div><div class="d">Tỷ lệ đã dùng của ổ đĩa đang chứa dữ liệu Agent.</div></div>
<div class="c">Network<div class="v" id="network">...</div><div class="d">Tổng tốc độ nhận + gửi gần đây của các card mạng đang hoạt động.</div></div>
<div class="c">Agent CPU<div class="v" id="agentcpu">...</div><div class="d">CPU riêng của tiến trình VHDCHY LAN Agent; đây là số chính để đánh giá footprint Agent.</div></div>
<div class="c">Agent RAM<div class="v" id="ram">...</div><div class="d">RAM riêng của Agent. Mục tiêu là giữ thấp và ổn định khi idle/tải.</div></div></div>
<h2>Kết nối và chất lượng</h2><div class="grid">
<div class="c">Agent<div class="v" id="health">...</div><div class="d">ONLINE = local service đang phục vụ bình thường.</div></div>
<div class="c">PDA hoạt động 60s<div class="v" id="devices">0</div><div class="d">Số PDA đã gửi request trong 60 giây gần nhất.</div></div>
<div class="c">Request/phút<div class="v" id="rpm">0</div><div class="d">Lưu lượng request Agent xử lý trong 60 giây gần nhất.</div></div>
<div class="c">Lỗi<div class="v" id="errors">0</div><div class="d">Tổng response HTTP lỗi từ lúc Agent chạy.</div></div>
<div class="c">p95 latency<div class="v" id="p95">0 ms</div><div class="d">95% request nhanh hơn mức này. Số càng thấp càng tốt.</div></div>
<div class="c">Events lưu<div class="v" id="events">0</div><div class="d">Số test event durable đã ghi vào SQLite local.</div></div>
<div class="c">Duplicates<div class="v" id="dup">0</div><div class="d">Event gửi lặp đã được nhận diện; dùng để kiểm tra idempotency khi reconnect/retry.</div></div></div>
<h2>Kết nối LAN</h2><pre id="net">...</pre><h2>Dữ liệu</h2><code id="data">...</code>
<h2>Chẩn đoán</h2><p><a class="btn" href="/api/pilot/diagnostics/export">Xuất log Agent (.zip)</a> <button onclick="checkUpdate()">Kiểm tra cập nhật</button> <span id="update"></span></p>
<p class="note"><b>Xuất log:</b> ZIP chỉ chứa health/metrics/network + diagnostic log, không chứa pilot.db hay dữ liệu nghiệp vụ. Sau khi test, tải ZIP này vào chat VHDCHY để phân tích.</p>
<script>
function rate(b){if(b>=1048576)return (b/1048576).toFixed(1)+' MB/s';if(b>=1024)return (b/1024).toFixed(1)+' KB/s';return b.toFixed(0)+' B/s'}
async function refresh(){try{let h=await fetch('/health').then(r=>r.json());let m=await fetch('/api/pilot/metrics').then(r=>r.json());let n=await fetch('/api/pilot/network').then(r=>r.json());health.textContent=h.ok?'ONLINE':'ERROR';devices.textContent=m.activeDevices60s;rpm.textContent=m.requestsPerMinute;errors.textContent=m.errors;p95.textContent=m.latencyMs.p95.toFixed(1)+' ms';ram.textContent=(m.processWorkingSetBytes/1048576).toFixed(1)+' MB';events.textContent=m.storedEvents;dup.textContent=m.duplicates;cpu.textContent=m.system.cpuPercent.toFixed(1)+'%';mem.textContent=m.system.memoryUsedPercent.toFixed(1)+'%';disk.textContent=m.system.diskUsedPercent.toFixed(1)+'%';network.textContent=rate(m.system.networkBytesPerSecond);agentcpu.textContent=m.system.agentCpuPercent.toFixed(2)+'%';net.textContent=n.addresses.map(x=>'http://'+x+':'+n.httpPort+'/').join('\n');data.textContent=m.dataDirectory}catch(e){health.textContent='UNREACHABLE'}}
async function checkUpdate(){update.textContent='đang kiểm tra...';try{let x=await fetch('/api/pilot/update/check').then(r=>r.json());update.textContent=x.latest?('Latest: '+x.latest.tag):'Chưa có release'}catch(e){update.textContent='Lỗi kiểm tra'}}
refresh();setInterval(refresh,2000);
</script></body></html>
""";
}
