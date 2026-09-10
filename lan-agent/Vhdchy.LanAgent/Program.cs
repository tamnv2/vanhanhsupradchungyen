using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Vhdchy.LanAgent;

internal static class Program
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

            var controller = new AgentController();
            controller.StartAsync().GetAwaiter().GetResult();
            Application.Run(new TrayContext(controller));
            controller.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "LAN Agent không thể khởi động. Đây có thể là giới hạn policy/port trên laptop công ty.\r\n\r\n" + ex.Message,
                "VHDCHY LAN Agent BETA",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

internal sealed class AgentController : IAsyncDisposable
{
    public const int DefaultHttpPort = 17891;
    public const int DefaultDiscoveryPort = 17892;
    public const string Protocol = "VHDCHY_LAN_PILOT_V1";
    public const string EnvironmentName = "BETA";

    private AgentHost? _host;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public string DataDirectory => DataPaths.GetDataDirectory();
    public int HttpPort => _host?.HttpPort ?? DefaultHttpPort;
    public bool IsRunning => _host is not null;
    public string DashboardUrl => $"http://127.0.0.1:{HttpPort}/";

    public async Task StartAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_host is not null) return;
            var data = DataPaths.GetDataDirectory();
            Directory.CreateDirectory(data);
            _host = new AgentHost(data, DefaultHttpPort, DefaultDiscoveryPort);
            await _host.StartAsync();
        }
        catch
        {
            _host = null;
            throw;
        }
        finally
        {
            _gate.Release();
        }
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
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RestartAsync()
    {
        await StopAsync();
        await StartAsync();
    }

    public void OpenDashboard() => Shell.Open(DashboardUrl);
    public void OpenDataFolder() => Shell.Open(DataDirectory);

    public async Task<string> ChangeDataDirectoryAsync(string selectedParent)
    {
        var target = Path.Combine(Path.GetFullPath(selectedParent), "VHDCHY-LAN-BETA-DATA");
        var source = DataDirectory;
        if (Path.GetFullPath(source).TrimEnd('\\').Equals(Path.GetFullPath(target).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            return target;

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

    public async Task<ReleaseInfo?> CheckUpdateAsync() => await ReleaseChecker.GetLatestPilotReleaseAsync();

    public ValueTask DisposeAsync() => new(StopAsync());
}

internal sealed class AgentHost : IAsyncDisposable
{
    private readonly string _dataDirectory;
    private readonly int _discoveryPort;
    private readonly AgentMetrics _metrics = new();
    private readonly CancellationTokenSource _cts = new();
    private WebApplication? _app;
    private Task? _udpTask;
    private readonly string _instanceId;

    public int HttpPort { get; }

    public AgentHost(string dataDirectory, int httpPort, int discoveryPort)
    {
        _dataDirectory = dataDirectory;
        HttpPort = httpPort;
        _discoveryPort = discoveryPort;
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
            Interlocked.Increment(ref _metrics.RequestCount);
            try
            {
                await next();
                if (context.Response.StatusCode >= 400) Interlocked.Increment(ref _metrics.ErrorCount);
            }
            catch
            {
                Interlocked.Increment(ref _metrics.ErrorCount);
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
            return Results.Json(new
            {
                ok = true,
                service = "VHDCHY_LAN_AGENT",
                environment = AgentController.EnvironmentName,
                protocol = AgentController.Protocol,
                serverTime = DateTimeOffset.UtcNow,
                request.DeviceId,
                request.Payload
            });
        });

        app.MapPost("/api/pilot/event", async (PilotEvent request) =>
        {
            if (string.IsNullOrWhiteSpace(request.EventId) || string.IsNullOrWhiteSpace(request.DeviceId) || request.DeviceSeq <= 0)
                return Results.BadRequest(new { ok = false, code = "INVALID_EVENT" });

            _metrics.TouchDevice(request.DeviceId);
            var inserted = await SqliteStore.InsertEventAsync(Path.Combine(_dataDirectory, "pilot.db"), request);
            if (!inserted) Interlocked.Increment(ref _metrics.DuplicateCount);
            return Results.Json(new { ok = true, accepted = inserted, duplicate = !inserted, request.EventId, request.DeviceId, request.DeviceSeq });
        });

        app.MapGet("/api/pilot/metrics", async () =>
        {
            var dbPath = Path.Combine(_dataDirectory, "pilot.db");
            var stored = await SqliteStore.CountEventsAsync(dbPath);
            var p = Process.GetCurrentProcess();
            return Results.Json(_metrics.Snapshot(stored, p.WorkingSet64, File.Exists(dbPath) ? new FileInfo(dbPath).Length : 0));
        });

        app.MapGet("/api/pilot/network", () => Results.Json(new
        {
            ok = true,
            addresses = NetworkInfo.GetLanIpv4Addresses(),
            httpPort = HttpPort,
            discoveryPort = _discoveryPort,
            note = "No DNS/router/firewall change is assumed. Use LAN IP during feasibility pilot."
        }));

        app.MapGet("/api/pilot/update/check", async () =>
        {
            var release = await ReleaseChecker.GetLatestPilotReleaseAsync();
            return Results.Json(new { ok = true, currentVersion = VersionInfo.Current, latest = release });
        });

        app.MapGet("/", () => Results.Content(Dashboard.Html, "text/html; charset=utf-8"));

        await app.StartAsync(_cts.Token);
        _app = app;
        _udpTask = Task.Run(() => DiscoveryLoopAsync(_cts.Token));
    }

    private object HealthPayload() => new
    {
        ok = true,
        service = "VHDCHY_LAN_AGENT",
        environment = AgentController.EnvironmentName,
        protocol = AgentController.Protocol,
        instanceId = _instanceId,
        version = VersionInfo.Current,
        httpPort = HttpPort,
        discoveryPort = _discoveryPort,
        addresses = NetworkInfo.GetLanIpv4Addresses(),
        startedAt = _metrics.StartedAt
    };

    private async Task DiscoveryLoopAsync(CancellationToken ct)
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, _discoveryPort));
        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult packet;
            try { packet = await udp.ReceiveAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch { await Task.Delay(500, ct); continue; }

            var request = Encoding.UTF8.GetString(packet.Buffer).Trim();
            if (!string.Equals(request, "VHDCHY_DISCOVER_BETA_V1", StringComparison.Ordinal)) continue;

            var payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                ok = true,
                service = "VHDCHY_LAN_AGENT",
                environment = AgentController.EnvironmentName,
                protocol = AgentController.Protocol,
                instanceId = _instanceId,
                httpPort = HttpPort,
                version = VersionInfo.Current
            });
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
        if (_udpTask is not null)
        {
            try { await _udpTask; } catch { }
        }
        _cts.Dispose();
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private readonly AgentController _controller;
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _autoUpdateTimer;
    private string? _lastReleaseTag;

    public TrayContext(AgentController controller)
    {
        _controller = controller;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Mở LAN Dashboard", null, (_, _) => _controller.OpenDashboard());
        menu.Items.Add("Mở thư mục dữ liệu", null, (_, _) => _controller.OpenDataFolder());
        menu.Items.Add("Chọn thư mục dữ liệu...", null, async (_, _) => await SelectDataFolderAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Kiểm tra cập nhật", null, async (_, _) => await CheckUpdateAsync(true));
        menu.Items.Add("Bật/tắt tự khởi động theo user", null, (_, _) => ToggleAutoStart());
        menu.Items.Add("Khởi động lại Agent", null, async (_, _) => await RestartAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Thoát Agent", null, async (_, _) => await ExitAsync());

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "VHDCHY LAN Agent BETA",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => _controller.OpenDashboard();

        _autoUpdateTimer = new System.Windows.Forms.Timer { Interval = 6 * 60 * 60 * 1000 };
        _autoUpdateTimer.Tick += async (_, _) => await CheckUpdateAsync(false);
        _autoUpdateTimer.Start();
        _ = Task.Run(async () => { await Task.Delay(10000); await CheckUpdateAsync(false); });
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
                var open = MessageBox.Show(
                    $"Phiên bản hiện tại: {VersionInfo.Current}\r\nRelease mới nhất: {release.Tag}\r\n\r\nMở trang tải thủ công?",
                    "Cập nhật VHDCHY LAN Agent BETA",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (open == DialogResult.Yes) Shell.Open(release.HtmlUrl);
            }
            else if (!string.Equals(_lastReleaseTag, release.Tag, StringComparison.OrdinalIgnoreCase))
            {
                _lastReleaseTag = release.Tag;
                Notify("Có LAN Pilot BETA release mới: " + release.Tag + ". Chuột phải → Kiểm tra cập nhật để tải thủ công.", ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            if (manual) MessageBox.Show("Kiểm tra cập nhật thất bại nhưng manual recovery vẫn khả dụng qua GitHub Release.\r\n\r\n" + ex.Message, "Cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
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
        catch (Exception ex)
        {
            MessageBox.Show("Corporate policy có thể chặn HKCU auto-start. Agent vẫn có thể chạy thủ công.\r\n\r\n" + ex.Message, "Auto-start", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        _autoUpdateTimer.Stop();
        _tray.Visible = false;
        await _controller.StopAsync();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autoUpdateTimer.Dispose();
            _tray.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal static class DataPaths
{
    private static readonly string BootstrapRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VHDCHY", "LanAgentBeta");
    private static readonly string PointerFile = Path.Combine(BootstrapRoot, "data-path.txt");

    public static string GetDataDirectory()
    {
        Directory.CreateDirectory(BootstrapRoot);
        if (File.Exists(PointerFile))
        {
            var value = File.ReadAllText(PointerFile).Trim();
            if (!string.IsNullOrWhiteSpace(value)) return Path.GetFullPath(value);
        }
        return Path.Combine(BootstrapRoot, "data");
    }

    public static void SetDataDirectory(string path)
    {
        Directory.CreateDirectory(BootstrapRoot);
        var tmp = PointerFile + ".tmp";
        File.WriteAllText(tmp, Path.GetFullPath(path));
        File.Move(tmp, PointerFile, true);
    }
}

internal static class IdentityStore
{
    public static string GetOrCreate(string dataDirectory)
    {
        var file = Path.Combine(dataDirectory, "instance-id.txt");
        if (File.Exists(file))
        {
            var id = File.ReadAllText(file).Trim();
            if (!string.IsNullOrWhiteSpace(id)) return id;
        }
        var created = Guid.NewGuid().ToString("D");
        File.WriteAllText(file, created);
        return created;
    }
}

internal static class SqliteStore
{
    private static string Connection(string db) => new SqliteConnectionStringBuilder { DataSource = db, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared }.ToString();

    public static async Task InitializeAsync(string db)
    {
        await using var c = new SqliteConnection(Connection(db));
        await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=NORMAL;
            PRAGMA busy_timeout=3000;
            CREATE TABLE IF NOT EXISTS pilot_events(
              event_id TEXT PRIMARY KEY,
              device_id TEXT NOT NULL,
              device_seq INTEGER NOT NULL,
              created_at TEXT NOT NULL,
              received_at TEXT NOT NULL,
              payload TEXT,
              UNIQUE(device_id, device_seq)
            );
            CREATE INDEX IF NOT EXISTS ix_pilot_events_received ON pilot_events(received_at);
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<bool> InsertEventAsync(string db, PilotEvent e)
    {
        await using var c = new SqliteConnection(Connection(db));
        await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO pilot_events(event_id,device_id,device_seq,created_at,received_at,payload) VALUES($id,$device,$seq,$created,$received,$payload);";
        cmd.Parameters.AddWithValue("$id", e.EventId);
        cmd.Parameters.AddWithValue("$device", e.DeviceId);
        cmd.Parameters.AddWithValue("$seq", e.DeviceSeq);
        cmd.Parameters.AddWithValue("$created", e.CreatedAt ?? DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$received", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$payload", e.Payload ?? "");
        return await cmd.ExecuteNonQueryAsync() == 1;
    }

    public static async Task<long> CountEventsAsync(string db)
    {
        await using var c = new SqliteConnection(Connection(db));
        await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pilot_events;";
        return (long)(await cmd.ExecuteScalarAsync() ?? 0L);
    }

    public static async Task VerifyAsync(string db)
    {
        await using var c = new SqliteConnection(Connection(db));
        await c.OpenAsync();
        var cmd = c.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check;";
        var result = Convert.ToString(await cmd.ExecuteScalarAsync());
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("SQLite integrity_check failed: " + result);
    }
}

internal sealed class AgentMetrics
{
    public long RequestCount;
    public long ErrorCount;
    public long DuplicateCount;
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    private readonly ConcurrentQueue<double> _latencies = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _devices = new(StringComparer.Ordinal);

    public void AddLatency(double ms)
    {
        _latencies.Enqueue(ms);
        while (_latencies.Count > 2048) _latencies.TryDequeue(out _);
    }

    public void TouchDevice(string? deviceId)
    {
        if (!string.IsNullOrWhiteSpace(deviceId)) _devices[deviceId] = DateTimeOffset.UtcNow;
    }

    public object Snapshot(long storedEvents, long workingSetBytes, long dbBytes)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1);
        foreach (var kv in _devices) if (kv.Value < cutoff) _devices.TryRemove(kv.Key, out _);
        var list = _latencies.ToArray();
        Array.Sort(list);
        double P(double p) => list.Length == 0 ? 0 : list[Math.Min(list.Length - 1, (int)Math.Ceiling((list.Length - 1) * p))];
        return new
        {
            ok = true,
            service = "VHDCHY_LAN_AGENT",
            environment = AgentController.EnvironmentName,
            protocol = AgentController.Protocol,
            uptimeSeconds = (long)(DateTimeOffset.UtcNow - StartedAt).TotalSeconds,
            requests = Interlocked.Read(ref RequestCount),
            errors = Interlocked.Read(ref ErrorCount),
            duplicates = Interlocked.Read(ref DuplicateCount),
            activeDevices60s = _devices.Count,
            storedEvents,
            latencyMs = new { p50 = P(.50), p95 = P(.95), p99 = P(.99), samples = list.Length },
            processWorkingSetBytes = workingSetBytes,
            localDbBytes = dbBytes,
            version = VersionInfo.Current,
            dataDirectory = DataPaths.GetDataDirectory()
        };
    }
}

internal static class NetworkInfo
{
    public static string[] GetLanIpv4Addresses() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
        .Select(a => a.Address.ToString())
        .Distinct()
        .ToArray();
}

internal static class ReleaseChecker
{
    private static readonly HttpClient Http = Create();
    private static HttpClient Create()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("VHDCHY-LanAgent-Beta/0.1");
        return h;
    }

    public static async Task<ReleaseInfo?> GetLatestPilotReleaseAsync()
    {
        using var response = await Http.GetAsync("https://api.github.com/repos/tamnv2/vanhanhdchungyen/releases?per_page=20");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("prerelease", out var pre) || !pre.GetBoolean()) continue;
            var tag = item.GetProperty("tag_name").GetString() ?? "";
            if (!tag.StartsWith("lan-pilot-beta-", StringComparison.OrdinalIgnoreCase)) continue;
            var url = item.GetProperty("html_url").GetString() ?? "https://github.com/tamnv2/vanhanhdchungyen/releases";
            return new ReleaseInfo(tag, url);
        }
        return null;
    }
}

internal static class FileTree
{
    public static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            var name = Path.GetFileName(file);
            if (name.EndsWith("-shm", StringComparison.OrdinalIgnoreCase) || name.EndsWith("-wal", StringComparison.OrdinalIgnoreCase)) continue;
            File.Copy(file, Path.Combine(destination, name), true);
        }
        foreach (var dir in Directory.GetDirectories(source)) CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }
}

internal static class Shell
{
    public static void Open(string pathOrUrl) => Process.Start(new ProcessStartInfo(pathOrUrl) { UseShellExecute = true });
}

internal static class VersionInfo
{
    public static string Current => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
}

internal record EchoRequest(string? DeviceId, string? Payload);
internal record PilotEvent(string EventId, string DeviceId, long DeviceSeq, string? CreatedAt, string? Payload);
internal record ReleaseInfo(string Tag, string HtmlUrl);

internal static class Dashboard
{
    public const string Html = """
<!doctype html><html lang="vi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>VHDCHY LAN BETA</title><style>body{font-family:Segoe UI,Arial;margin:24px;max-width:900px}h1{font-size:22px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:10px}.c{border:1px solid #ccc;border-radius:8px;padding:12px}.v{font-size:20px;font-weight:600}code{word-break:break-all}button{padding:8px 12px}</style></head>
<body><h1>VHDCHY LAN Agent — BETA Pilot</h1><p>Trang này được phục vụ trực tiếp từ laptop. Pilot không yêu cầu DNS/router/admin.</p>
<div class="grid"><div class="c">Agent<div class="v" id="health">...</div></div><div class="c">PDA 60s<div class="v" id="devices">0</div></div><div class="c">Requests<div class="v" id="requests">0</div></div><div class="c">Errors<div class="v" id="errors">0</div></div><div class="c">p95<div class="v" id="p95">0 ms</div></div><div class="c">RAM<div class="v" id="ram">0 MB</div></div><div class="c">Events<div class="v" id="events">0</div></div><div class="c">Duplicates<div class="v" id="dup">0</div></div></div>
<h2>Kết nối</h2><pre id="net">...</pre><h2>Dữ liệu</h2><code id="data">...</code><p><button onclick="checkUpdate()">Kiểm tra cập nhật</button> <span id="update"></span></p>
<script>
async function refresh(){try{let h=await fetch('/health').then(r=>r.json());let m=await fetch('/api/pilot/metrics').then(r=>r.json());let n=await fetch('/api/pilot/network').then(r=>r.json());health.textContent=h.ok?'ONLINE':'ERROR';devices.textContent=m.activeDevices60s;requests.textContent=m.requests;errors.textContent=m.errors;p95.textContent=m.latencyMs.p95.toFixed(1)+' ms';ram.textContent=(m.processWorkingSetBytes/1048576).toFixed(1)+' MB';events.textContent=m.storedEvents;dup.textContent=m.duplicates;net.textContent=n.addresses.map(x=>'http://'+x+':'+n.httpPort+'/').join('\n');data.textContent=m.dataDirectory}catch(e){health.textContent='UNREACHABLE'}}
async function checkUpdate(){update.textContent='đang kiểm tra...';try{let x=await fetch('/api/pilot/update/check').then(r=>r.json());update.textContent=x.latest?('Latest: '+x.latest.tag):'Chưa có release'}catch(e){update.textContent='Lỗi kiểm tra'}}
refresh();setInterval(refresh,2000);
</script></body></html>
""";
}
