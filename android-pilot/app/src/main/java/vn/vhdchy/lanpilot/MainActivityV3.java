package vn.vhdchy.lanpilot;

import android.content.Intent;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Bundle;
import android.os.SystemClock;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.HttpURLConnection;
import java.net.InetAddress;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.TimeZone;
import java.util.UUID;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

public class MainActivityV3 extends MainActivityV2 {
    private static final int FULL_EXPORT_REQUEST = 9501;
    private static final int DISCOVERY_PORT = 17892;
    private static final String DISCOVERY_MESSAGE = "VHDCHY_DISCOVER_BETA_V1";
    private final ScheduledExecutorService priorityTimer = Executors.newSingleThreadScheduledExecutor();
    private final ExecutorService testIo = Executors.newFixedThreadPool(3);
    private final AtomicBoolean realtimeLoopStarted = new AtomicBoolean(false);
    private final AtomicBoolean suiteRunning = new AtomicBoolean(false);
    private final List<Long> realtimeLatencies = Collections.synchronizedList(new ArrayList<>());
    private File v3Log;
    private SharedPreferences v3Prefs;
    private String deviceId;
    private volatile long realtimeAfter = 0;
    private volatile long realtimeReceived = 0;
    private volatile long realtimeGapCount = 0;
    private volatile String lastDiscoverySource = "UNKNOWN";
    private volatile long lastAgentStartedAtMs = 0;
    private volatile long lastLanAcquireDelayMs = -1;
    private String pendingFullExport;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        v3Prefs = getSharedPreferences("lan_pilot", MODE_PRIVATE);
        deviceId = v3Prefs.getString("device_id", "pda-unknown");
        v3Log = new File(getFilesDir(), "lan-pilot-v3-test.log");
        appendV3("V3_START", "comprehensive LAN test runtime started");

        ((Button) findViewById(R.id.transfer1)).setOnClickListener(v -> testIo.execute(() -> runTransferTest(1)));
        ((Button) findViewById(R.id.transfer25)).setOnClickListener(v -> testIo.execute(() -> runTransferTest(25)));
        ((Button) findViewById(R.id.realtimeOne)).setOnClickListener(v -> testIo.execute(() -> publishRealtime(1, 64, "single")));
        ((Button) findViewById(R.id.realtimeHeavy)).setOnClickListener(v -> testIo.execute(() -> publishRealtime(200, 2048, "heavy")));
        ((Button) findViewById(R.id.fullSuite)).setOnClickListener(v -> testIo.execute(this::runFullSuite));
        ((Button) findViewById(R.id.fullExport)).setOnClickListener(v -> prepareFullExport());

        startRealtimeLoop();
        priorityTimer.scheduleWithFixedDelay(this::enforceLanPriority, 0, 1, TimeUnit.SECONDS);
        updatePriorityUi("LAN priority: đang khởi tạo...");
    }

    private void enforceLanPriority() {
        try {
            String state = uiText(R.id.state);
            if (!state.contains("LAN_ACTIVE")) {
                runOnUiThread(() -> {
                    Button b = findViewById(R.id.discover);
                    if (b != null) b.performClick();
                });
                CandidateV3 found = discoverLanNow();
                if (found != null) {
                    lastDiscoverySource = "UDP";
                    v3Prefs.edit().putString("cached_lan_endpoint", found.endpoint).apply();
                    appendV3("AUTO_LAN_DISCOVERY", "source=UDP endpoint=" + found.endpoint);
                } else {
                    String cached = normalize(v3Prefs.getString("cached_lan_endpoint", ""));
                    if (!cached.isEmpty() && verifyHealth(cached) != null) {
                        lastDiscoverySource = "CACHE";
                    }
                }
            } else {
                String endpoint = currentEndpoint();
                JSONObject h = verifyHealth(endpoint);
                if (h != null) {
                    long started = h.optLong("startedAtUnixMs", 0);
                    if (started > 0 && started != lastAgentStartedAtMs) {
                        lastAgentStartedAtMs = started;
                        lastLanAcquireDelayMs = Math.max(0, System.currentTimeMillis() - started);
                        appendV3("LAN_PRIORITY_ACQUIRED", "source=" + lastDiscoverySource + " acquireDelayMs=" + lastLanAcquireDelayMs + " endpoint=" + endpoint);
                    }
                }
            }
            String msg = "LAN priority: ON | source=" + lastDiscoverySource + (lastLanAcquireDelayMs >= 0 ? " | Agent-start→LAN_ACTIVE=" + lastLanAcquireDelayMs + " ms" : "");
            updatePriorityUi(msg);
        } catch (Exception e) {
            appendV3("LAN_PRIORITY_ERROR", safe(e.getMessage()));
        }
    }

    private CandidateV3 discoverLanNow() {
        DatagramSocket socket = null;
        try {
            socket = new DatagramSocket();
            socket.setBroadcast(true);
            socket.setSoTimeout(700);
            byte[] out = DISCOVERY_MESSAGE.getBytes(StandardCharsets.UTF_8);
            socket.send(new DatagramPacket(out, out.length, InetAddress.getByName("255.255.255.255"), DISCOVERY_PORT));
            byte[] buffer = new byte[2048];
            DatagramPacket response = new DatagramPacket(buffer, buffer.length);
            socket.receive(response);
            JSONObject j = new JSONObject(new String(response.getData(), 0, response.getLength(), StandardCharsets.UTF_8));
            if (!j.optBoolean("ok") || !"VHDCHY_LAN_AGENT".equals(j.optString("service")) || !"BETA".equals(j.optString("environment"))) return null;
            String endpoint = "http://" + response.getAddress().getHostAddress() + ":" + j.optInt("httpPort", 17891);
            return verifyHealth(endpoint) == null ? null : new CandidateV3(endpoint);
        } catch (Exception e) {
            return null;
        } finally {
            if (socket != null) socket.close();
        }
    }

    private JSONObject verifyHealth(String endpoint) {
        if (endpoint == null || endpoint.isEmpty()) return null;
        try {
            JSONObject j = getJsonV3(endpoint + "/health", 800, 1200);
            if (j.optBoolean("ok") && "VHDCHY_LAN_AGENT".equals(j.optString("service")) && "BETA".equals(j.optString("environment"))) return j;
        } catch (Exception ignored) { }
        return null;
    }

    private void startRealtimeLoop() {
        if (!realtimeLoopStarted.compareAndSet(false, true)) return;
        testIo.execute(() -> {
            while (!Thread.currentThread().isInterrupted()) {
                String endpoint = currentEndpoint();
                if (endpoint.isEmpty() || !uiText(R.id.state).contains("LAN_ACTIVE")) {
                    sleep(300);
                    continue;
                }
                try {
                    String url = endpoint + "/api/pilot/realtime/poll?after=" + realtimeAfter + "&timeoutMs=8000&deviceId=" + Uri.encode(deviceId);
                    JSONObject r = getJsonV3(url, 1500, 10000);
                    JSONArray events = r.optJSONArray("events");
                    if (events == null) continue;
                    for (int i = 0; i < events.length(); i++) {
                        JSONObject e = events.getJSONObject(i);
                        long seq = e.optLong("sequence");
                        if (realtimeAfter > 0 && seq > realtimeAfter + 1) realtimeGapCount += (seq - realtimeAfter - 1);
                        realtimeAfter = Math.max(realtimeAfter, seq);
                        long latency = Math.max(0, System.currentTimeMillis() - e.optLong("publishedAtUnixMs", System.currentTimeMillis()));
                        realtimeLatencies.add(latency);
                        while (realtimeLatencies.size() > 1000) realtimeLatencies.remove(0);
                        realtimeReceived++;
                        updateRealtimeUi(e.optString("deviceId"), latency);
                    }
                } catch (Exception e) {
                    appendV3("REALTIME_POLL_FAIL", safe(e.getMessage()));
                    sleep(400);
                }
            }
        });
    }

    private void publishRealtime(int count, int payloadBytes, String label) {
        String endpoint = currentEndpoint();
        if (endpoint.isEmpty() || !uiText(R.id.state).contains("LAN_ACTIVE")) {
            setTestStatus("Realtime bỏ qua: PDA chưa LAN_ACTIVE.");
            return;
        }
        String payload = repeat('R', payloadBytes);
        long ok = 0, fail = 0;
        long start = SystemClock.elapsedRealtime();
        for (int i = 0; i < count; i++) {
            try {
                JSONObject req = new JSONObject()
                        .put("messageId", UUID.randomUUID().toString())
                        .put("deviceId", deviceId)
                        .put("clientSentAtUnixMs", System.currentTimeMillis())
                        .put("payload", payload);
                JSONObject r = postJsonV3(endpoint + "/api/pilot/realtime/publish", req, 1500, 3000);
                if (r.optBoolean("ok")) ok++; else fail++;
            } catch (Exception e) { fail++; }
        }
        long elapsed = Math.max(1, SystemClock.elapsedRealtime() - start);
        double rate = ok * 1000.0 / elapsed;
        String result = "Realtime " + label + ": " + ok + "/" + count + " PASS, fail=" + fail + ", publish=" + fmt(rate) + " msg/s";
        setTestStatus(result);
        appendV3("REALTIME_PUBLISH", result + "; payloadBytes=" + payloadBytes);
    }

    private void runTransferTest(int mb) {
        String endpoint = currentEndpoint();
        if (endpoint.isEmpty() || !uiText(R.id.state).contains("LAN_ACTIVE")) {
            setTestStatus("Transfer bỏ qua: PDA chưa LAN_ACTIVE.");
            return;
        }
        int bytes = mb * 1024 * 1024;
        try {
            TransferResult up = upload(endpoint, bytes);
            TransferResult down = download(endpoint, bytes);
            String result = "Transfer " + mb + "MB | UP " + fmt(up.mbps) + " Mbps (" + up.elapsedMs + " ms) | DOWN " + fmt(down.mbps) + " Mbps (" + down.elapsedMs + " ms)";
            setTransferStatus(result);
            appendV3("TRANSFER", result);
        } catch (Exception e) {
            String result = "Transfer " + mb + "MB FAIL: " + safe(e.getMessage());
            setTransferStatus(result);
            appendV3("TRANSFER_FAIL", result);
        }
    }

    private TransferResult upload(String endpoint, int bytes) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(endpoint + "/api/pilot/transfer/upload?deviceId=" + Uri.encode(deviceId)).openConnection();
        c.setRequestMethod("POST"); c.setDoOutput(true); c.setConnectTimeout(2000); c.setReadTimeout(30000);
        c.setRequestProperty("Content-Type", "application/octet-stream");
        c.setFixedLengthStreamingMode(bytes);
        byte[] block = new byte[64 * 1024];
        long start = SystemClock.elapsedRealtime();
        try (OutputStream out = c.getOutputStream()) {
            int remaining = bytes;
            while (remaining > 0) { int n = Math.min(block.length, remaining); out.write(block, 0, n); remaining -= n; }
        }
        int code = c.getResponseCode();
        readText(code >= 200 && code < 400 ? c.getInputStream() : c.getErrorStream());
        long elapsed = Math.max(1, SystemClock.elapsedRealtime() - start);
        c.disconnect();
        if (code < 200 || code >= 300) throw new Exception("upload HTTP " + code);
        return new TransferResult(elapsed, bytes * 8.0 / elapsed / 1000.0);
    }

    private TransferResult download(String endpoint, int bytes) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(endpoint + "/api/pilot/transfer/download?bytes=" + bytes + "&deviceId=" + Uri.encode(deviceId)).openConnection();
        c.setRequestMethod("GET"); c.setConnectTimeout(2000); c.setReadTimeout(30000); c.setUseCaches(false);
        long start = SystemClock.elapsedRealtime(); long total = 0;
        try (InputStream in = c.getInputStream()) {
            byte[] block = new byte[64 * 1024]; int n;
            while ((n = in.read(block)) >= 0) total += n;
        }
        int code = c.getResponseCode();
        long elapsed = Math.max(1, SystemClock.elapsedRealtime() - start); c.disconnect();
        if (code < 200 || code >= 300 || total != bytes) throw new Exception("download bytes=" + total + " expected=" + bytes + " HTTP=" + code);
        return new TransferResult(elapsed, total * 8.0 / elapsed / 1000.0);
    }

    private void runFullSuite() {
        if (!suiteRunning.compareAndSet(false, true)) return;
        try {
            String endpoint = currentEndpoint();
            if (endpoint.isEmpty() || !uiText(R.id.state).contains("LAN_ACTIVE")) { setSuiteStatus("FULL SUITE: chưa LAN_ACTIVE."); return; }
            setSuiteStatus("FULL SUITE đang chạy: 100 echo → transfer 1/10/25MB → realtime 200×2KB...");
            List<Double> echo = new ArrayList<>(); long echoFail = 0;
            for (int i = 0; i < 100; i++) {
                long s = SystemClock.elapsedRealtimeNanos();
                try {
                    JSONObject req = new JSONObject().put("deviceId", deviceId).put("payload", "suite-echo-" + i);
                    JSONObject r = postJsonV3(endpoint + "/api/pilot/echo", req, 1500, 2500);
                    if (!r.optBoolean("ok")) echoFail++; else echo.add((SystemClock.elapsedRealtimeNanos() - s) / 1_000_000.0);
                } catch (Exception e) { echoFail++; }
            }
            TransferResult u1 = upload(endpoint, 1 * 1024 * 1024), d1 = download(endpoint, 1 * 1024 * 1024);
            TransferResult u10 = upload(endpoint, 10 * 1024 * 1024), d10 = download(endpoint, 10 * 1024 * 1024);
            TransferResult u25 = upload(endpoint, 25 * 1024 * 1024), d25 = download(endpoint, 25 * 1024 * 1024);
            publishRealtime(200, 2048, "suite-heavy");
            Collections.sort(echo);
            double p50 = percentileDouble(echo, .50), p95 = percentileDouble(echo, .95), p99 = percentileDouble(echo, .99);
            String result = "FULL SUITE DONE | echo PASS=" + echo.size() + "/100 fail=" + echoFail + " p50/p95/p99=" + fmt(p50) + "/" + fmt(p95) + "/" + fmt(p99) + " ms"
                    + " | UP Mbps 1/10/25=" + fmt(u1.mbps) + "/" + fmt(u10.mbps) + "/" + fmt(u25.mbps)
                    + " | DOWN Mbps 1/10/25=" + fmt(d1.mbps) + "/" + fmt(d10.mbps) + "/" + fmt(d25.mbps);
            setSuiteStatus(result); appendV3("FULL_SUITE", result);
        } catch (Exception e) {
            String result = "FULL SUITE FAIL: " + safe(e.getMessage()); setSuiteStatus(result); appendV3("FULL_SUITE_FAIL", result);
        } finally { suiteRunning.set(false); }
    }

    private void prepareFullExport() {
        pendingFullExport = buildFullReport();
        Intent intent = new Intent(Intent.ACTION_CREATE_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE); intent.setType("text/plain");
        intent.putExtra(Intent.EXTRA_TITLE, "VHDCHY-LAN-FULL-Diagnostics-" + localStamp() + ".txt");
        startActivityForResult(intent, FULL_EXPORT_REQUEST);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != FULL_EXPORT_REQUEST || resultCode != RESULT_OK || data == null || data.getData() == null || pendingFullExport == null) return;
        try (OutputStream out = getContentResolver().openOutputStream(data.getData())) {
            if (out == null) throw new IllegalStateException("Không mở được file đích");
            out.write(pendingFullExport.getBytes(StandardCharsets.UTF_8)); out.flush();
            Toast.makeText(this, "Đã xuất FULL diagnostics.", Toast.LENGTH_LONG).show();
        } catch (Exception e) { Toast.makeText(this, "Export FAIL: " + safe(e.getMessage()), Toast.LENGTH_LONG).show(); }
        finally { pendingFullExport = null; }
    }

    private String buildFullReport() {
        StringBuilder b = new StringBuilder();
        b.append("VHDCHY LAN Pilot FULL Diagnostics\nExportedAt: ").append(utcNow()).append('\n');
        b.append("DeviceId: ").append(deviceId).append('\n');
        b.append("State: ").append(uiText(R.id.state)).append('\n');
        b.append("Endpoint: ").append(uiText(R.id.endpoint)).append('\n');
        b.append("LAN priority: ").append(uiText(R.id.lanPriority)).append('\n');
        b.append("Realtime: ").append(uiText(R.id.realtimeStatus)).append('\n');
        b.append("Transfer: ").append(uiText(R.id.transferStatus)).append('\n');
        b.append("Suite: ").append(uiText(R.id.suiteStatus)).append('\n');
        b.append("DiscoverySource: ").append(lastDiscoverySource).append('\n');
        b.append("AgentStartToLanActiveMs: ").append(lastLanAcquireDelayMs).append('\n');
        b.append("RealtimeReceived: ").append(realtimeReceived).append("\nRealtimeSequenceGaps: ").append(realtimeGapCount).append('\n');
        b.append("RealtimeLatencyP50/P95/P99Ms: ").append(percentileLong(.50)).append('/').append(percentileLong(.95)).append('/').append(percentileLong(.99)).append('\n');
        b.append("\n--- V3 test history ---\n").append(readFile(v3Log));
        b.append("\n--- Base diagnostic history ---\n").append(readFile(new File(getFilesDir(), "lan-pilot-diagnostic.log.1"))).append(readFile(new File(getFilesDir(), "lan-pilot-diagnostic.log")));
        return b.toString();
    }

    private JSONObject getJsonV3(String url, int connectTimeout, int readTimeout) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(url).openConnection();
        c.setRequestMethod("GET"); c.setConnectTimeout(connectTimeout); c.setReadTimeout(readTimeout); c.setUseCaches(false);
        int status = c.getResponseCode(); String body = readText(status >= 200 && status < 400 ? c.getInputStream() : c.getErrorStream()); c.disconnect();
        if (status < 200 || status >= 300) throw new Exception("HTTP " + status); return new JSONObject(body);
    }

    private JSONObject postJsonV3(String url, JSONObject payload, int connectTimeout, int readTimeout) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(url).openConnection();
        c.setRequestMethod("POST"); c.setDoOutput(true); c.setConnectTimeout(connectTimeout); c.setReadTimeout(readTimeout); c.setRequestProperty("Content-Type", "application/json; charset=utf-8");
        try (OutputStream out = c.getOutputStream()) { out.write(payload.toString().getBytes(StandardCharsets.UTF_8)); }
        int status = c.getResponseCode(); String body = readText(status >= 200 && status < 400 ? c.getInputStream() : c.getErrorStream()); c.disconnect();
        if (status < 200 || status >= 300) throw new Exception("HTTP " + status); return new JSONObject(body);
    }

    private String currentEndpoint() {
        String s = uiText(R.id.endpoint).replace("LAN endpoint:", "").trim();
        return "—".equals(s) ? "" : normalize(s);
    }
    private String normalize(String s) { if (s == null) return ""; s = s.trim(); while (s.endsWith("/")) s = s.substring(0, s.length() - 1); return s; }
    private String uiText(int id) { TextView t = findViewById(id); return t == null || t.getText() == null ? "" : t.getText().toString(); }
    private void updatePriorityUi(String s) { runOnUiThread(() -> ((TextView)findViewById(R.id.lanPriority)).setText(s)); }
    private void updateRealtimeUi(String source, long latency) { runOnUiThread(() -> ((TextView)findViewById(R.id.realtimeStatus)).setText("Realtime nhận=" + realtimeReceived + " | last=" + latency + " ms | p95=" + percentileLong(.95) + " ms | gaps=" + realtimeGapCount + " | source=" + source)); }
    private void setTransferStatus(String s) { runOnUiThread(() -> ((TextView)findViewById(R.id.transferStatus)).setText(s)); }
    private void setSuiteStatus(String s) { runOnUiThread(() -> ((TextView)findViewById(R.id.suiteStatus)).setText(s)); }
    private void setTestStatus(String s) { runOnUiThread(() -> ((TextView)findViewById(R.id.testStatus)).setText(s)); }

    private long percentileLong(double p) {
        synchronized (realtimeLatencies) {
            if (realtimeLatencies.isEmpty()) return 0;
            List<Long> x = new ArrayList<>(realtimeLatencies); Collections.sort(x);
            return x.get(Math.min(x.size() - 1, (int)Math.ceil((x.size() - 1) * p)));
        }
    }
    private double percentileDouble(List<Double> x, double p) { if (x.isEmpty()) return 0; return x.get(Math.min(x.size() - 1, (int)Math.ceil((x.size() - 1) * p))); }
    private static String repeat(char c, int n) { char[] a = new char[n]; java.util.Arrays.fill(a, c); return new String(a); }
    private static String fmt(double d) { return String.format(Locale.US, "%.1f", d); }
    private static void sleep(long ms) { try { Thread.sleep(ms); } catch (InterruptedException e) { Thread.currentThread().interrupt(); } }
    private static String readText(InputStream in) throws Exception { if (in == null) return ""; StringBuilder b = new StringBuilder(); try (BufferedReader r = new BufferedReader(new InputStreamReader(in, StandardCharsets.UTF_8))) { String line; while ((line = r.readLine()) != null) b.append(line); } return b.toString(); }

    private void appendV3(String type, String msg) {
        try (FileOutputStream out = new FileOutputStream(v3Log, true)) {
            out.write((utcNow() + "\t" + type + "\t" + safe(msg).replace('\n',' ').replace('\r',' ') + "\n").getBytes(StandardCharsets.UTF_8));
        } catch (Exception ignored) { }
    }
    private String readFile(File f) { if (f == null || !f.exists()) return ""; try (FileInputStream in = new FileInputStream(f); ByteArrayOutputStream out = new ByteArrayOutputStream()) { byte[] buf = new byte[8192]; int n; while ((n=in.read(buf))>=0) out.write(buf,0,n); return new String(out.toByteArray(), StandardCharsets.UTF_8); } catch (Exception e) { return "[read fail: "+safe(e.getMessage())+"]\n"; } }
    private static String utcNow() { SimpleDateFormat f = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US); f.setTimeZone(TimeZone.getTimeZone("UTC")); return f.format(new Date()); }
    private static String localStamp() { return new SimpleDateFormat("yyyyMMdd-HHmmss", Locale.US).format(new Date()); }
    private static String safe(String s) { return s == null ? "" : s; }

    @Override
    protected void onDestroy() {
        priorityTimer.shutdownNow(); testIo.shutdownNow(); appendV3("V3_STOP", "activity destroyed"); super.onDestroy();
    }

    private static class CandidateV3 { final String endpoint; CandidateV3(String endpoint) { this.endpoint = endpoint; } }
    private static class TransferResult { final long elapsedMs; final double mbps; TransferResult(long elapsedMs, double mbps) { this.elapsedMs = elapsedMs; this.mbps = mbps; } }
}
