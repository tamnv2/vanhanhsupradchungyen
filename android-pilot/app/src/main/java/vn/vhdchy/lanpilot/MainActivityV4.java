package vn.vhdchy.lanpilot;

import android.app.Activity;
import android.app.ActivityManager;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.net.ConnectivityManager;
import android.net.Network;
import android.net.NetworkCapabilities;
import android.net.Uri;
import android.net.wifi.WifiManager;
import android.os.BatteryManager;
import android.os.Build;
import android.os.Bundle;
import android.os.Debug;
import android.os.PowerManager;
import android.os.SystemClock;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.ByteArrayOutputStream;
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
import java.util.concurrent.atomic.AtomicInteger;

public class MainActivityV4 extends Activity {
    private static final int EXPORT_REQUEST = 9601;
    private static final int DISCOVERY_PORT = 17892;
    private static final String DISCOVERY_MESSAGE = "VHDCHY_DISCOVER_BETA_V1";
    private static final String PROTOCOL = "VHDCHY_LAN_PILOT_V1";
    private static final String CLOUD_HEALTH = "https://beta.supra.cc.cd/health";
    private static final AtomicInteger ACTIVE_JOBS = new AtomicInteger(0);

    private final ScheduledExecutorService scheduler = Executors.newScheduledThreadPool(2);
    private final ExecutorService io = Executors.newFixedThreadPool(4);
    private final AtomicBoolean cycleRunning = new AtomicBoolean(false);
    private final Object realtimeGate = new Object();
    private volatile boolean foreground;
    private volatile boolean destroyed;
    private volatile boolean realtimeThreadStarted;

    private PilotRepository repo;
    private UpdateManagerV4 updates;
    private String deviceId;
    private volatile TransportState state = TransportState.RECONNECTING;
    private volatile String activeEndpoint = "";
    private volatile String discoverySource = "UNKNOWN";
    private volatile String streamEpoch = "";
    private volatile long realtimeAfter = 0;
    private volatile long realtimeReceived = 0;
    private volatile long realtimeGapCount = 0;
    private volatile long realtimeResyncCount = 0;
    private volatile long realtimePollFailures = 0;
    private volatile long clockOffsetMs = 0;
    private volatile long clockCalibrationRttMs = -1;
    private volatile long lastCalibrationElapsed = 0;
    private volatile long lanLostAtElapsed = -1;
    private volatile long lastReacquireMs = -1;
    private volatile int successStreak = 0;
    private volatile int failureStreak = 0;
    private final List<Long> deliveryLatencies = Collections.synchronizedList(new ArrayList<>());
    private final List<Double> publishAckLatencies = Collections.synchronizedList(new ArrayList<>());
    private final List<Long> reacquireLatencies = Collections.synchronizedList(new ArrayList<>());
    private String pendingExport;

    private TextView deviceInfo, stateView, lanPriority, endpointView, statusView, queueView;
    private TextView realtimeStatus, testStatus, transferStatus, suiteStatus, backgroundStatus, resourceStatus;
    private EditText manualEndpoint;

    enum TransportState { CLOUD_ONLY, LAN_AVAILABLE, LAN_ACTIVE, LAN_LOST, RECONNECTING, LOCAL_QUEUE_ONLY }

    @Override protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main_v4);
        repo = new PilotRepository(this);
        updates = new UpdateManagerV4(this, repo);
        deviceId = repo.deviceId();
        bindViews();
        manualEndpoint.setText(repo.manualEndpoint());
        deviceInfo.setText("Thiết bị: " + Build.MANUFACTURER + " " + Build.MODEL + " | Android " + Build.VERSION.RELEASE + " | App " + BuildConfig.VERSION_NAME + " | " + shortDevice(deviceId));
        repo.log("APP_START", "version=" + BuildConfig.VERSION_NAME + "; android=" + Build.VERSION.RELEASE + "; sdk=" + Build.VERSION.SDK_INT + "; device=" + Build.MANUFACTURER + " " + Build.MODEL + "; id=" + shortDevice(deviceId));
        setupButtons();
        refreshQueueUi();
        refreshResourceUi();
        startRealtimeThread();

        scheduler.scheduleWithFixedDelay(() -> {
            if (destroyed) return;
            if (foreground || ACTIVE_JOBS.get() > 0) runAutoCycle(false);
        }, 0, 5, TimeUnit.SECONDS);
        scheduler.scheduleWithFixedDelay(() -> {
            if (foreground) refreshResourceUi();
        }, 2, 10, TimeUnit.SECONDS);
        scheduler.schedule(() -> updates.check(false), 15, TimeUnit.SECONDS);
    }

    private void bindViews() {
        deviceInfo = findViewById(R.id.deviceInfo);
        stateView = findViewById(R.id.state);
        lanPriority = findViewById(R.id.lanPriority);
        endpointView = findViewById(R.id.endpoint);
        statusView = findViewById(R.id.status);
        queueView = findViewById(R.id.queue);
        realtimeStatus = findViewById(R.id.realtimeStatus);
        testStatus = findViewById(R.id.testStatus);
        transferStatus = findViewById(R.id.transferStatus);
        suiteStatus = findViewById(R.id.suiteStatus);
        backgroundStatus = findViewById(R.id.backgroundStatus);
        resourceStatus = findViewById(R.id.resourceStatus);
        manualEndpoint = findViewById(R.id.manualEndpoint);
    }

    private void setupButtons() {
        ((Button)findViewById(R.id.discover)).setOnClickListener(v -> io.execute(() -> runAutoCycle(true)));
        ((Button)findViewById(R.id.echo)).setOnClickListener(v -> io.execute(this::runEcho));
        ((Button)findViewById(R.id.event)).setOnClickListener(v -> io.execute(this::enqueueAndFlushEvent));
        ((Button)findViewById(R.id.realtimeOne)).setOnClickListener(v -> runTracked("realtime-single", () -> publishRealtime(1, 64, "single")));
        ((Button)findViewById(R.id.realtimeHeavy)).setOnClickListener(v -> runTracked("realtime-heavy", () -> publishRealtime(200, 2048, "heavy")));
        ((Button)findViewById(R.id.transfer1)).setOnClickListener(v -> runTracked("transfer-1mb", () -> runTransferTest(1)));
        ((Button)findViewById(R.id.transfer25)).setOnClickListener(v -> runTracked("transfer-25mb", () -> runTransferTest(25)));
        ((Button)findViewById(R.id.fullSuite)).setOnClickListener(v -> runTracked("full-suite", this::runFullSuite));
        ((Button)findViewById(R.id.offlineQueueTest)).setOnClickListener(v -> io.execute(this::enqueueOfflineEvidenceBatch));
        ((Button)findViewById(R.id.exportFull)).setOnClickListener(v -> prepareExport());
        ((Button)findViewById(R.id.update)).setOnClickListener(v -> updates.check(true));
        ((Button)findViewById(R.id.saveEndpoint)).setOnClickListener(v -> {
            String value = PilotRepository.normalize(manualEndpoint.getText().toString());
            repo.setManualEndpoint(value); manualEndpoint.setText(value);
            repo.log("MANUAL_ENDPOINT", "saved=" + value);
            Toast.makeText(this, "Đã lưu endpoint recovery. Không dùng trong vận hành bình thường.", Toast.LENGTH_SHORT).show();
        });
    }

    @Override protected void onStart() {
        super.onStart();
        foreground = true;
        repo.log("LIFECYCLE", "onStart foreground=true; activeJobs=" + ACTIVE_JOBS.get() + "; pending=" + repo.countPending());
        synchronized (realtimeGate) { realtimeGate.notifyAll(); }
        io.execute(() -> runAutoCycle(true));
        updateBackgroundUi();
    }

    @Override protected void onStop() {
        foreground = false;
        repo.log("LIFECYCLE", "onStop foreground=false; activeJobs=" + ACTIVE_JOBS.get() + "; pending=" + repo.countPending());
        if (ACTIVE_JOBS.get() > 0 || repo.countPending() > 0) startFinishService();
        updateBackgroundUi();
        super.onStop();
    }

    @Override protected void onDestroy() {
        destroyed = true;
        foreground = false;
        repo.log("LIFECYCLE", "onDestroy activeJobs=" + ACTIVE_JOBS.get() + "; pending=" + repo.countPending());
        synchronized (realtimeGate) { realtimeGate.notifyAll(); }
        scheduler.shutdownNow();
        if (ACTIVE_JOBS.get() == 0) io.shutdownNow();
        updates.close();
        if (ACTIVE_JOBS.get() == 0) repo.close();
        super.onDestroy();
    }

    static int activeJobCount() { return ACTIVE_JOBS.get(); }

    private void startFinishService() {
        try {
            Intent i = new Intent(this, LanFinishService.class);
            if (Build.VERSION.SDK_INT >= 26) startForegroundService(i); else startService(i);
            repo.log("BG_SERVICE_REQUEST", "activeJobs=" + ACTIVE_JOBS.get() + "; pending=" + repo.countPending());
        } catch (Exception e) {
            repo.log("BG_SERVICE_REQUEST_FAIL", e.getClass().getSimpleName() + ": " + safe(e.getMessage()));
        }
    }

    private void runTracked(String name, Runnable work) {
        io.execute(() -> {
            int jobs = ACTIVE_JOBS.incrementAndGet();
            long start = SystemClock.elapsedRealtime();
            repo.log("JOB_START", "name=" + name + "; activeJobs=" + jobs + "; foreground=" + foreground);
            if (!foreground) startFinishService();
            try { work.run(); }
            catch (Throwable t) { repo.log("JOB_ERROR", "name=" + name + "; " + t.getClass().getSimpleName() + ": " + safe(t.getMessage())); }
            finally {
                int left = ACTIVE_JOBS.decrementAndGet();
                repo.log("JOB_END", "name=" + name + "; elapsedMs=" + (SystemClock.elapsedRealtime() - start) + "; activeJobs=" + left + "; pending=" + repo.countPending());
                updateBackgroundUi();
            }
        });
    }

    private void runAutoCycle(boolean forced) {
        if (!cycleRunning.compareAndSet(false, true)) return;
        long cycleStart = SystemClock.elapsedRealtime();
        try {
            Candidate candidate = null;
            String cached = repo.lastEndpoint();
            if (!cached.isEmpty()) {
                JSONObject health = verifyHealth(cached);
                if (health != null) { candidate = new Candidate(cached, health); discoverySource = "CACHE"; }
            }
            if (candidate == null) {
                Candidate discovered = discoverLanNow();
                if (discovered != null) { candidate = discovered; discoverySource = "UDP"; }
            }
            if (candidate == null) {
                String manual = repo.manualEndpoint();
                if (!manual.isEmpty() && !manual.equals(cached)) {
                    JSONObject health = verifyHealth(manual);
                    if (health != null) { candidate = new Candidate(manual, health); discoverySource = "MANUAL"; }
                }
            }

            if (candidate != null) {
                repo.setLastEndpoint(candidate.endpoint);
                activeEndpoint = candidate.endpoint;
                failureStreak = 0;
                successStreak++;
                String newEpoch = candidate.health.optString("streamEpoch", "");
                if (!newEpoch.isEmpty() && !streamEpoch.isEmpty() && !newEpoch.equals(streamEpoch)) handleEpochChange(streamEpoch, newEpoch, "health");
                if (!newEpoch.isEmpty()) streamEpoch = newEpoch;
                if (successStreak >= 2 || state == TransportState.LAN_ACTIVE) {
                    boolean wasInactive = state != TransportState.LAN_ACTIVE;
                    if (wasInactive && lanLostAtElapsed > 0) {
                        lastReacquireMs = Math.max(0, SystemClock.elapsedRealtime() - lanLostAtElapsed);
                        reacquireLatencies.add(lastReacquireMs);
                        while (reacquireLatencies.size() > 100) reacquireLatencies.remove(0);
                        repo.increment("diag_lan_reacquire", 1);
                        repo.log("LAN_REACQUIRED", "source=" + discoverySource + "; ms=" + lastReacquireMs + "; endpoint=" + activeEndpoint + "; epoch=" + shortEpoch(streamEpoch));
                        lanLostAtElapsed = -1;
                    }
                    setState(TransportState.LAN_ACTIVE, "LAN hợp lệ; ưu tiên LAN đang bật", activeEndpoint);
                    if (lastCalibrationElapsed == 0 || SystemClock.elapsedRealtime() - lastCalibrationElapsed > 5 * 60_000L) calibrateClock(activeEndpoint);
                    flushPending();
                } else {
                    setState(TransportState.LAN_AVAILABLE, "Đã thấy Agent; xác nhận thêm 1 health sample để tránh flapping", activeEndpoint);
                    scheduler.schedule(() -> runAutoCycle(false), 800, TimeUnit.MILLISECONDS);
                }
            } else {
                successStreak = 0;
                failureStreak++;
                if ((state == TransportState.LAN_ACTIVE || state == TransportState.LAN_AVAILABLE) && lanLostAtElapsed < 0) {
                    lanLostAtElapsed = SystemClock.elapsedRealtime();
                    repo.increment("diag_lan_loss", 1);
                    repo.log("LAN_LOST", "endpoint=" + activeEndpoint + "; failureStreak=" + failureStreak);
                }
                boolean cloud = failureStreak >= 2 && checkCloud();
                if (failureStreak < 2) setState(TransportState.LAN_LOST, "Mất 1 health sample; chưa fallback để tránh flapping", activeEndpoint);
                else if (cloud) { activeEndpoint = ""; setState(TransportState.CLOUD_ONLY, "LAN không khả dụng; Cloud health PASS", ""); }
                else { activeEndpoint = ""; setState(TransportState.LOCAL_QUEUE_ONLY, "LAN/Cloud không khả dụng; durable queue giữ local", ""); }
            }
            if (forced) repo.log("LAN_CYCLE", "forced; state=" + state + "; source=" + discoverySource + "; elapsedMs=" + (SystemClock.elapsedRealtime() - cycleStart));
        } catch (Exception e) {
            repo.increment("diag_lan_cycle_error", 1);
            repo.log("LAN_CYCLE_ERROR", e.getClass().getSimpleName() + ": " + safe(e.getMessage()));
        } finally { cycleRunning.set(false); }
    }

    private Candidate discoverLanNow() {
        DatagramSocket socket = null;
        long start = SystemClock.elapsedRealtime();
        try {
            socket = new DatagramSocket();
            socket.setBroadcast(true);
            socket.setSoTimeout(850);
            byte[] out = DISCOVERY_MESSAGE.getBytes(StandardCharsets.UTF_8);
            socket.send(new DatagramPacket(out, out.length, InetAddress.getByName("255.255.255.255"), DISCOVERY_PORT));
            byte[] buffer = new byte[2048];
            DatagramPacket response = new DatagramPacket(buffer, buffer.length);
            socket.receive(response);
            JSONObject j = new JSONObject(new String(response.getData(), 0, response.getLength(), StandardCharsets.UTF_8));
            if (!j.optBoolean("ok") || !"VHDCHY_LAN_AGENT".equals(j.optString("service")) || !"BETA".equals(j.optString("environment")) || !PROTOCOL.equals(j.optString("protocol"))) return null;
            String endpoint = "http://" + response.getAddress().getHostAddress() + ":" + j.optInt("httpPort", 17891);
            JSONObject health = verifyHealth(endpoint);
            if (health == null) return null;
            repo.increment("diag_udp_discovery_success", 1);
            repo.log("UDP_DISCOVERY", "success endpoint=" + endpoint + "; ms=" + (SystemClock.elapsedRealtime() - start) + "; epoch=" + shortEpoch(health.optString("streamEpoch", "")));
            return new Candidate(endpoint, health);
        } catch (Exception e) {
            repo.increment("diag_udp_discovery_fail", 1);
            return null;
        } finally { if (socket != null) socket.close(); }
    }

    private JSONObject verifyHealth(String endpoint) {
        if (endpoint == null || endpoint.isEmpty()) return null;
        try {
            JSONObject j = getJson(endpoint + "/health", 900, 1400);
            if (j.optBoolean("ok") && "VHDCHY_LAN_AGENT".equals(j.optString("service")) && "BETA".equals(j.optString("environment")) && PROTOCOL.equals(j.optString("protocol"))) return j;
        } catch (Exception ignored) { }
        return null;
    }

    private boolean checkCloud() {
        try {
            JSONObject j = getJson(CLOUD_HEALTH, 1300, 1800);
            return j.optBoolean("ok") && "VHDCHY_WORKER".equals(j.optString("service"));
        } catch (Exception e) { return false; }
    }

    private void calibrateClock(String endpoint) {
        try {
            long bestRtt = Long.MAX_VALUE, bestOffset = 0;
            for (int i = 0; i < 5; i++) {
                long t0Wall = System.currentTimeMillis();
                long t0Mono = SystemClock.elapsedRealtime();
                JSONObject j = getJson(endpoint + "/api/pilot/time", 1000, 1500);
                long t1Mono = SystemClock.elapsedRealtime();
                long t1Wall = System.currentTimeMillis();
                long rtt = Math.max(0, t1Mono - t0Mono);
                long server = j.optLong("serverUnixMs", 0);
                if (server > 0 && rtt < bestRtt) {
                    bestRtt = rtt;
                    bestOffset = server - ((t0Wall + t1Wall) / 2L);
                }
            }
            if (bestRtt != Long.MAX_VALUE) {
                clockCalibrationRttMs = bestRtt;
                clockOffsetMs = bestOffset;
                lastCalibrationElapsed = SystemClock.elapsedRealtime();
                repo.log("CLOCK_CALIBRATION", "offsetMs=" + clockOffsetMs + "; bestRttMs=" + clockCalibrationRttMs + "; epoch=" + shortEpoch(streamEpoch));
            }
        } catch (Exception e) { repo.log("CLOCK_CALIBRATION_FAIL", safe(e.getMessage())); }
    }

    private void startRealtimeThread() {
        if (realtimeThreadStarted) return;
        realtimeThreadStarted = true;
        io.execute(() -> {
            repo.log("REALTIME_THREAD", "started; foreground-only policy");
            while (!destroyed) {
                synchronized (realtimeGate) {
                    while (!foreground && !destroyed) {
                        try { realtimeGate.wait(); } catch (InterruptedException e) { Thread.currentThread().interrupt(); return; }
                    }
                }
                if (destroyed) break;
                if (state != TransportState.LAN_ACTIVE || activeEndpoint.isEmpty()) { sleep(250); continue; }
                try {
                    String url = activeEndpoint + "/api/pilot/realtime/poll?after=" + realtimeAfter + "&timeoutMs=8000&deviceId=" + Uri.encode(deviceId) + "&epoch=" + Uri.encode(streamEpoch);
                    JSONObject r = getJson(url, 1500, 10_000);
                    String newEpoch = r.optString("streamEpoch", streamEpoch);
                    if (r.optBoolean("epochChanged") || (!streamEpoch.isEmpty() && !newEpoch.equals(streamEpoch))) {
                        handleEpochChange(streamEpoch, newEpoch, "poll");
                        continue;
                    }
                    if (!newEpoch.isEmpty()) streamEpoch = newEpoch;
                    if (r.optBoolean("requiresResync")) {
                        long oldest = r.optLong("oldestSequence", 1);
                        long missed = realtimeAfter > 0 ? Math.max(0, oldest - realtimeAfter - 1) : 0;
                        realtimeGapCount += missed;
                        realtimeResyncCount++;
                        repo.increment("diag_realtime_resync", 1);
                        repo.log("REALTIME_RESYNC", "reason=buffer-gap; after=" + realtimeAfter + "; oldest=" + oldest + "; missed=" + missed + "; epoch=" + shortEpoch(streamEpoch));
                        realtimeAfter = Math.max(0, oldest - 1);
                    }
                    JSONArray events = r.optJSONArray("events");
                    if (events == null) continue;
                    for (int i = 0; i < events.length(); i++) {
                        JSONObject e = events.getJSONObject(i);
                        long seq = e.optLong("sequence");
                        if (realtimeAfter > 0 && seq > realtimeAfter + 1) {
                            long gap = seq - realtimeAfter - 1;
                            realtimeGapCount += gap;
                            realtimeResyncCount++;
                            repo.log("REALTIME_SEQUENCE_GAP", "after=" + realtimeAfter + "; next=" + seq + "; gap=" + gap + "; epoch=" + shortEpoch(streamEpoch));
                        }
                        realtimeAfter = Math.max(realtimeAfter, seq);
                        long correctedServerNow = System.currentTimeMillis() + clockOffsetMs;
                        long delivery = Math.max(0, correctedServerNow - e.optLong("publishedAtUnixMs", correctedServerNow));
                        addBounded(deliveryLatencies, delivery, 2000);
                        realtimeReceived++;
                        repo.increment("diag_realtime_received", 1);
                        updateRealtimeUi(e.optString("deviceId", "—"), delivery);
                    }
                } catch (Exception e) {
                    realtimePollFailures++;
                    repo.increment("diag_realtime_poll_fail", 1);
                    if (realtimePollFailures <= 5 || realtimePollFailures % 20 == 0)
                        repo.log("REALTIME_POLL_FAIL", "count=" + realtimePollFailures + "; state=" + state + "; " + e.getClass().getSimpleName() + ": " + safe(e.getMessage()));
                    sleep(500);
                }
            }
            repo.log("REALTIME_THREAD", "stopped");
        });
    }

    private void handleEpochChange(String oldEpoch, String newEpoch, String source) {
        if (newEpoch == null) newEpoch = "";
        repo.increment("diag_realtime_epoch_reset", 1);
        realtimeResyncCount++;
        repo.log("REALTIME_EPOCH_CHANGE", "source=" + source + "; old=" + shortEpoch(oldEpoch) + "; new=" + shortEpoch(newEpoch) + "; oldAfter=" + realtimeAfter);
        streamEpoch = newEpoch;
        realtimeAfter = 0;
        calibrateClock(activeEndpoint);
    }

    private void runEcho() {
        if (state != TransportState.LAN_ACTIVE || activeEndpoint.isEmpty()) { appendStatus("Echo bỏ qua: chưa LAN_ACTIVE."); return; }
        try {
            long t0 = SystemClock.elapsedRealtimeNanos();
            JSONObject req = new JSONObject().put("deviceId", deviceId).put("payload", "v4-echo");
            JSONObject r = postJson(activeEndpoint + "/api/pilot/echo", req, 1500, 2500);
            double ms = (SystemClock.elapsedRealtimeNanos() - t0) / 1_000_000d;
            appendStatus("Echo " + (r.optBoolean("ok") ? "PASS" : "FAIL") + " | " + fmt(ms) + " ms");
            repo.log("ECHO", "ok=" + r.optBoolean("ok") + "; rttMs=" + fmt(ms));
        } catch (Exception e) { appendStatus("Echo FAIL: " + safe(e.getMessage())); repo.log("ECHO_FAIL", safe(e.getMessage())); }
    }

    private void enqueueAndFlushEvent() {
        PilotRepository.PendingEvent e = repo.enqueue("pilot-event-v4");
        refreshQueueUi();
        appendStatus("Đã enqueue durable event seq=" + e.deviceSeq);
        flushPending();
    }

    private void enqueueOfflineEvidenceBatch() {
        for (int i = 0; i < 5; i++) repo.enqueue("offline-evidence-v4-" + i);
        repo.log("OFFLINE_EVIDENCE_BATCH", "created=5; state=" + state + "; pending=" + repo.countPending());
        refreshQueueUi();
        if (state == TransportState.LAN_ACTIVE) flushPending();
    }

    private void flushPending() {
        if (state != TransportState.LAN_ACTIVE || activeEndpoint.isEmpty()) { refreshQueueUi(); return; }
        int before = repo.countPending();
        if (before <= 0) return;
        repo.increment("diag_queue_flush_attempt", 1);
        repo.log("QUEUE_FLUSH_START", "pending=" + before + "; endpoint=" + activeEndpoint);
        for (PilotRepository.PendingEvent e : repo.pending(500)) {
            try {
                long t0 = SystemClock.elapsedRealtime();
                JSONObject req = new JSONObject()
                        .put("eventId", e.eventId).put("deviceId", deviceId).put("deviceSeq", e.deviceSeq)
                        .put("createdAt", new Date(e.createdAtMs).toInstant().toString()).put("payload", e.payload);
                JSONObject result = postJson(activeEndpoint + "/api/pilot/event", req, 1800, 3000);
                if (!result.optBoolean("ok")) break;
                repo.ack(e.eventId, e.deviceSeq, SystemClock.elapsedRealtime() - t0);
            } catch (Exception ex) {
                repo.increment("diag_queue_flush_fail", 1);
                repo.log("QUEUE_FLUSH_FAIL", "seq=" + e.deviceSeq + "; " + ex.getClass().getSimpleName() + ": " + safe(ex.getMessage()));
                break;
            }
        }
        int after = repo.countPending();
        repo.log("QUEUE_FLUSH_END", "before=" + before + "; after=" + after);
        refreshQueueUi();
    }

    private void publishRealtime(int count, int payloadBytes, String label) {
        if (state != TransportState.LAN_ACTIVE || activeEndpoint.isEmpty()) { setText(testStatus, "Realtime bỏ qua: chưa LAN_ACTIVE."); return; }
        String payload = repeat('R', payloadBytes);
        long ok = 0, fail = 0;
        long allStart = SystemClock.elapsedRealtime();
        List<Double> localAck = new ArrayList<>();
        for (int i = 0; i < count; i++) {
            try {
                JSONObject req = new JSONObject().put("messageId", UUID.randomUUID().toString()).put("deviceId", deviceId).put("payload", payload);
                long s = SystemClock.elapsedRealtimeNanos();
                JSONObject r = postJson(activeEndpoint + "/api/pilot/realtime/publish", req, 1500, 3500);
                double ackMs = (SystemClock.elapsedRealtimeNanos() - s) / 1_000_000d;
                localAck.add(ackMs); addBoundedDouble(publishAckLatencies, ackMs, 2000);
                if (r.optBoolean("ok")) ok++; else fail++;
            } catch (Exception e) { fail++; }
        }
        long elapsed = Math.max(1, SystemClock.elapsedRealtime() - allStart);
        Collections.sort(localAck);
        String result = "Realtime " + label + ": " + ok + "/" + count + " PASS, fail=" + fail + ", publish=" + fmt(ok * 1000d / elapsed) + " msg/s, ACK p95=" + fmt(percentileDouble(localAck, .95)) + " ms";
        setText(testStatus, result);
        repo.log("REALTIME_PUBLISH", result + "; payloadBytes=" + payloadBytes + "; epoch=" + shortEpoch(streamEpoch));
    }

    private void runTransferTest(int mb) {
        if (state != TransportState.LAN_ACTIVE || activeEndpoint.isEmpty()) { setText(transferStatus, "Transfer bỏ qua: chưa LAN_ACTIVE."); return; }
        int bytes = mb * 1024 * 1024;
        try {
            TransferResult up = upload(activeEndpoint, bytes), down = download(activeEndpoint, bytes);
            String result = "Transfer " + mb + "MB | UP " + fmt(up.mbps) + " Mbps (" + up.elapsedMs + " ms) | DOWN " + fmt(down.mbps) + " Mbps (" + down.elapsedMs + " ms)";
            setText(transferStatus, result); repo.log("TRANSFER", result);
        } catch (Exception e) {
            String result = "Transfer " + mb + "MB FAIL: " + safe(e.getMessage()); setText(transferStatus, result); repo.log("TRANSFER_FAIL", result);
        }
    }

    private void runFullSuite() {
        if (state != TransportState.LAN_ACTIVE || activeEndpoint.isEmpty()) { setText(suiteStatus, "FULL SUITE: chưa LAN_ACTIVE."); return; }
        try {
            setText(suiteStatus, "FULL SUITE: 100 echo → 1/10/25MB → realtime 200×2KB…");
            List<Double> echo = new ArrayList<>(); long fail = 0;
            for (int i = 0; i < 100; i++) {
                long s = SystemClock.elapsedRealtimeNanos();
                try {
                    JSONObject r = postJson(activeEndpoint + "/api/pilot/echo", new JSONObject().put("deviceId", deviceId).put("payload", "suite-" + i), 1500, 2500);
                    if (r.optBoolean("ok")) echo.add((SystemClock.elapsedRealtimeNanos() - s) / 1_000_000d); else fail++;
                } catch (Exception e) { fail++; }
            }
            TransferResult u1 = upload(activeEndpoint, 1*1024*1024), d1 = download(activeEndpoint, 1*1024*1024);
            TransferResult u10 = upload(activeEndpoint, 10*1024*1024), d10 = download(activeEndpoint, 10*1024*1024);
            TransferResult u25 = upload(activeEndpoint, 25*1024*1024), d25 = download(activeEndpoint, 25*1024*1024);
            publishRealtime(200, 2048, "suite-heavy");
            Collections.sort(echo);
            String result = "FULL SUITE DONE | echo PASS=" + echo.size() + "/100 fail=" + fail + " p50/p95/p99=" + fmt(percentileDouble(echo,.5)) + "/" + fmt(percentileDouble(echo,.95)) + "/" + fmt(percentileDouble(echo,.99)) + " ms | UP 1/10/25=" + fmt(u1.mbps)+"/"+fmt(u10.mbps)+"/"+fmt(u25.mbps)+" Mbps | DOWN="+fmt(d1.mbps)+"/"+fmt(d10.mbps)+"/"+fmt(d25.mbps)+" Mbps";
            setText(suiteStatus, result); repo.log("FULL_SUITE", result);
        } catch (Exception e) {
            String result = "FULL SUITE FAIL: " + safe(e.getMessage()); setText(suiteStatus, result); repo.log("FULL_SUITE_FAIL", result);
        }
    }

    private TransferResult upload(String endpoint, int bytes) throws Exception {
        HttpURLConnection c = (HttpURLConnection)new URL(endpoint + "/api/pilot/transfer/upload?deviceId=" + Uri.encode(deviceId)).openConnection();
        c.setRequestMethod("POST"); c.setDoOutput(true); c.setConnectTimeout(2000); c.setReadTimeout(45_000); c.setFixedLengthStreamingMode(bytes); c.setRequestProperty("Content-Type", "application/octet-stream");
        byte[] block = new byte[64*1024]; long start = SystemClock.elapsedRealtime();
        try(OutputStream out=c.getOutputStream()){int remaining=bytes;while(remaining>0){int n=Math.min(block.length,remaining);out.write(block,0,n);remaining-=n;}}
        int code=c.getResponseCode(); readText(code>=200&&code<400?c.getInputStream():c.getErrorStream()); long elapsed=Math.max(1,SystemClock.elapsedRealtime()-start);c.disconnect();
        if(code<200||code>=300)throw new Exception("upload HTTP "+code); return new TransferResult(elapsed,bytes*8d/elapsed/1000d);
    }

    private TransferResult download(String endpoint, int bytes) throws Exception {
        HttpURLConnection c=(HttpURLConnection)new URL(endpoint+"/api/pilot/transfer/download?bytes="+bytes+"&deviceId="+Uri.encode(deviceId)).openConnection();c.setConnectTimeout(2000);c.setReadTimeout(45_000);c.setUseCaches(false);
        long start=SystemClock.elapsedRealtime(),total=0;try(InputStream in=c.getInputStream()){byte[] block=new byte[64*1024];int n;while((n=in.read(block))>=0)total+=n;}int code=c.getResponseCode();long elapsed=Math.max(1,SystemClock.elapsedRealtime()-start);c.disconnect();
        if(code<200||code>=300||total!=bytes)throw new Exception("download bytes="+total+" expected="+bytes+" HTTP="+code);return new TransferResult(elapsed,total*8d/elapsed/1000d);
    }

    private JSONObject getJson(String url, int connect, int read) throws Exception {
        HttpURLConnection c=(HttpURLConnection)new URL(url).openConnection();c.setConnectTimeout(connect);c.setReadTimeout(read);c.setUseCaches(false);c.setRequestProperty("Accept","application/json");c.setRequestProperty("User-Agent","VHDCHY-LAN-Pilot-Android/"+BuildConfig.VERSION_NAME);int status=c.getResponseCode();String body=readText(status>=200&&status<400?c.getInputStream():c.getErrorStream());c.disconnect();if(status<200||status>=300)throw new Exception("HTTP "+status);return new JSONObject(body);
    }

    private JSONObject postJson(String url, JSONObject payload, int connect, int read) throws Exception {
        HttpURLConnection c=(HttpURLConnection)new URL(url).openConnection();c.setRequestMethod("POST");c.setDoOutput(true);c.setConnectTimeout(connect);c.setReadTimeout(read);c.setRequestProperty("Content-Type","application/json; charset=utf-8");byte[] bytes=payload.toString().getBytes(StandardCharsets.UTF_8);try(OutputStream out=c.getOutputStream()){out.write(bytes);}int status=c.getResponseCode();String body=readText(status>=200&&status<400?c.getInputStream():c.getErrorStream());c.disconnect();if(status<200||status>=300)throw new Exception("HTTP "+status);return new JSONObject(body);
    }

    private String readText(InputStream in) throws Exception { if(in==null)return"";StringBuilder b=new StringBuilder();try(BufferedReader r=new BufferedReader(new InputStreamReader(in,StandardCharsets.UTF_8))){String line;while((line=r.readLine())!=null)b.append(line);}return b.toString(); }

    private void setState(TransportState newState, String reason, String endpoint) {
        TransportState old = state; state = newState;
        if (old != newState) repo.log("STATE", "from=" + old + "; to=" + newState + "; reason=" + reason + "; endpoint=" + endpoint + "; source=" + discoverySource);
        runOnUiThread(() -> {
            stateView.setText("Transport: " + newState.name()); endpointView.setText("LAN endpoint: " + (endpoint == null || endpoint.isEmpty() ? "—" : endpoint));
            statusView.setText(reason + "\nSuccess streak: " + successStreak + " | Failure streak: " + failureStreak);
            String reacq = lastReacquireMs >= 0 ? " | reacquire gần nhất=" + lastReacquireMs + " ms" : "";
            lanPriority.setText("LAN priority: ON | source=" + discoverySource + reacq + " | epoch=" + shortEpoch(streamEpoch));
        });
    }

    private void updateRealtimeUi(String sourceDevice, long deliveryMs) {
        List<Long> copy; synchronized(deliveryLatencies){copy=new ArrayList<>(deliveryLatencies);}Collections.sort(copy);
        long p50=percentileLong(copy,.5),p95=percentileLong(copy,.95),p99=percentileLong(copy,.99);
        setText(realtimeStatus,"Realtime nhận="+realtimeReceived+" | last="+deliveryMs+" ms | corrected p50/p95/p99="+p50+"/"+p95+"/"+p99+" ms | gaps="+realtimeGapCount+" | resync="+realtimeResyncCount+" | pollFail="+realtimePollFailures+" | source="+sourceDevice);
    }

    private void refreshQueueUi(){if(repo==null)return;int count=repo.countPending();setText(queueView,"Pending local queue: "+count+" | enqueued="+repo.counter("diag_queue_enqueued")+" | acked="+repo.counter("diag_queue_acked")+" | peak="+repo.counter("diag_queue_peak"));}

    private void refreshResourceUi() {
        if (repo == null) return;
        try {
            Runtime rt=Runtime.getRuntime();long heapUsed=rt.totalMemory()-rt.freeMemory();long pssKb=Debug.getPss();
            Intent battery=registerReceiver(null,new IntentFilter(Intent.ACTION_BATTERY_CHANGED));int level=battery==null?-1:battery.getIntExtra(BatteryManager.EXTRA_LEVEL,-1);int scale=battery==null?100:battery.getIntExtra(BatteryManager.EXTRA_SCALE,100);int status=battery==null?-1:battery.getIntExtra(BatteryManager.EXTRA_STATUS,-1);boolean charging=status==BatteryManager.BATTERY_STATUS_CHARGING||status==BatteryManager.BATTERY_STATUS_FULL;
            PowerManager pm=(PowerManager)getSystemService(POWER_SERVICE);boolean powerSave=pm!=null&&Build.VERSION.SDK_INT>=21&&pm.isPowerSaveMode();
            String net=networkSummary();
            String text="App PSS="+fmt(pssKb/1024d)+" MB | Java heap="+fmt(heapUsed/1048576d)+" MB | threads="+Thread.getAllStackTraces().size()+" | battery="+(level<0?"—":(level*100/Math.max(1,scale))+"%")+(charging?" charging":"")+" | powerSave="+powerSave+" | network="+net;
            setText(resourceStatus,text);
        } catch(Exception e){setText(resourceStatus,"Resource telemetry unavailable: "+safe(e.getMessage()));}
        updateBackgroundUi();
    }

    private String networkSummary() {
        try {
            ConnectivityManager cm=(ConnectivityManager)getSystemService(CONNECTIVITY_SERVICE);Network n=cm.getActiveNetwork();NetworkCapabilities c=cm.getNetworkCapabilities(n);if(c==null)return"none";
            String type=c.hasTransport(NetworkCapabilities.TRANSPORT_WIFI)?"Wi-Fi":c.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR)?"cellular":c.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET)?"ethernet":c.hasTransport(NetworkCapabilities.TRANSPORT_VPN)?"VPN":"other";
            WifiManager wm=(WifiManager)getApplicationContext().getSystemService(Context.WIFI_SERVICE);String ip="";if(wm!=null&&wm.getConnectionInfo()!=null){int v=wm.getConnectionInfo().getIpAddress();if(v!=0)ip=(v&0xff)+"."+((v>>8)&0xff)+"."+((v>>16)&0xff)+"."+((v>>24)&0xff);}return type+(ip.isEmpty()?"":" "+ip);
        } catch(Exception e){return"unknown";}
    }

    private void updateBackgroundUi(){if(repo==null)return;setText(backgroundStatus,"App foreground="+foreground+" | active jobs="+ACTIVE_JOBS.get()+" | background finish service="+repo.backgroundServiceRunning()+" | policy: realtime chỉ foreground; background chỉ hoàn tất job/queue rồi dừng.");}

    private void prepareExport() {
        pendingExport = buildReport();
        Intent intent=new Intent(Intent.ACTION_CREATE_DOCUMENT);intent.addCategory(Intent.CATEGORY_OPENABLE);intent.setType("text/plain");intent.putExtra(Intent.EXTRA_TITLE,"VHDCHY-LAN-FULL-Diagnostics-"+localStamp()+".txt");startActivityForResult(intent,EXPORT_REQUEST);
    }

    private String buildReport() {
        List<Long> delivery; synchronized(deliveryLatencies){delivery=new ArrayList<>(deliveryLatencies);}Collections.sort(delivery);
        List<Double> ack; synchronized(publishAckLatencies){ack=new ArrayList<>(publishAckLatencies);}Collections.sort(ack);
        List<Long> reacq; synchronized(reacquireLatencies){reacq=new ArrayList<>(reacquireLatencies);}Collections.sort(reacq);
        StringBuilder b=new StringBuilder();
        b.append("VHDCHY LAN Pilot V4 FULL Diagnostics\n");b.append("ExportedAt: ").append(new Date().toInstant()).append('\n');b.append("AppVersion: ").append(BuildConfig.VERSION_NAME).append(" (code ").append(BuildConfig.VERSION_CODE).append(")\n");b.append("DeviceId: ").append(deviceId).append('\n');b.append("Device: ").append(Build.MANUFACTURER).append(' ').append(Build.MODEL).append(" | Android ").append(Build.VERSION.RELEASE).append(" SDK ").append(Build.VERSION.SDK_INT).append('\n');
        b.append("State: ").append(state).append(" | endpoint=").append(activeEndpoint).append(" | source=").append(discoverySource).append('\n');b.append("StreamEpoch: ").append(streamEpoch).append(" | realtimeAfter=").append(realtimeAfter).append('\n');b.append("ClockCalibration: offsetMs=").append(clockOffsetMs).append(" | bestRttMs=").append(clockCalibrationRttMs).append('\n');
        b.append("Realtime: received=").append(realtimeReceived).append(" | gaps=").append(realtimeGapCount).append(" | resync=").append(realtimeResyncCount).append(" | pollFail=").append(realtimePollFailures).append(" | delivery p50/p95/p99=").append(percentileLong(delivery,.5)).append('/').append(percentileLong(delivery,.95)).append('/').append(percentileLong(delivery,.99)).append(" ms | publish ACK p50/p95/p99=").append(fmt(percentileDouble(ack,.5))).append('/').append(fmt(percentileDouble(ack,.95))).append('/').append(fmt(percentileDouble(ack,.99))).append(" ms\n");
        b.append("LAN reacquire: samples=").append(reacq.size()).append(" | p50/p95/p99=").append(percentileLong(reacq,.5)).append('/').append(percentileLong(reacq,.95)).append('/').append(percentileLong(reacq,.99)).append(" ms | last=").append(lastReacquireMs).append(" ms\n");
        b.append("Queue: pending=").append(repo.countPending()).append(" | enqueued=").append(repo.counter("diag_queue_enqueued")).append(" | acked=").append(repo.counter("diag_queue_acked")).append(" | peak=").append(repo.counter("diag_queue_peak")).append(" | flushFail=").append(repo.counter("diag_queue_flush_fail")).append('\n');
        b.append("Discovery: success=").append(repo.counter("diag_udp_discovery_success")).append(" | fail=").append(repo.counter("diag_udp_discovery_fail")).append(" | losses=").append(repo.counter("diag_lan_loss")).append(" | reacquire=").append(repo.counter("diag_lan_reacquire")).append('\n');
        b.append("Background: service=").append(repo.backgroundServiceRunning()).append(" | bgFlushAttempts=").append(repo.counter("diag_bg_flush_attempts")).append(" | bgErrors=").append(repo.counter("diag_bg_errors")).append('\n');
        b.append("ResourceNow: ").append(resourceStatus.getText()).append('\n');b.append("NetworkNow: ").append(networkSummary()).append('\n');b.append("\n--- V4 detailed history ---\n").append(repo.readLog());
        return b.toString();
    }

    @Override protected void onActivityResult(int requestCode,int resultCode,Intent data){super.onActivityResult(requestCode,resultCode,data);if(requestCode!=EXPORT_REQUEST||resultCode!=RESULT_OK||data==null||data.getData()==null||pendingExport==null)return;try(OutputStream out=getContentResolver().openOutputStream(data.getData())){if(out!=null)out.write(pendingExport.getBytes(StandardCharsets.UTF_8));repo.log("EXPORT","full diagnostics completed");Toast.makeText(this,"Đã xuất FULL diagnostics.",Toast.LENGTH_LONG).show();}catch(Exception e){Toast.makeText(this,"Export lỗi: "+safe(e.getMessage()),Toast.LENGTH_LONG).show();}finally{pendingExport=null;}}

    private void appendStatus(String text){runOnUiThread(()->statusView.setText(statusView.getText()+"\n"+text));}
    private void setText(TextView v,String text){if(v==null)return;runOnUiThread(()->v.setText(text));}
    private static void sleep(long ms){try{Thread.sleep(ms);}catch(InterruptedException e){Thread.currentThread().interrupt();}}
    private static String repeat(char c,int n){char[] a=new char[n];java.util.Arrays.fill(a,c);return new String(a);}
    private static String safe(String s){return s==null?"":s;}
    private static String fmt(double d){return String.format(Locale.US,"%.1f",d);}
    private static String shortDevice(String id){return id==null?"—":id.length()<=12?id:id.substring(0,4)+"…"+id.substring(id.length()-6);}
    private static String shortEpoch(String e){return e==null||e.isEmpty()?"—":e.length()<=8?e:e.substring(0,8);}
    private static String localStamp(){return new SimpleDateFormat("yyyyMMdd-HHmmss",Locale.US).format(new Date());}
    private static <T> void addBounded(List<T> list,T value,int max){synchronized(list){list.add(value);while(list.size()>max)list.remove(0);}}
    private static void addBoundedDouble(List<Double> list,double value,int max){addBounded(list,value,max);}
    private static long percentileLong(List<Long> list,double p){if(list==null||list.isEmpty())return 0;int i=Math.min(list.size()-1,(int)Math.ceil((list.size()-1)*p));return list.get(i);}
    private static double percentileDouble(List<Double> list,double p){if(list==null||list.isEmpty())return 0;int i=Math.min(list.size()-1,(int)Math.ceil((list.size()-1)*p));return list.get(i);}

    private static final class Candidate { final String endpoint; final JSONObject health; Candidate(String endpoint,JSONObject health){this.endpoint=endpoint;this.health=health;} }
    private static final class TransferResult { final long elapsedMs; final double mbps; TransferResult(long elapsedMs,double mbps){this.elapsedMs=elapsedMs;this.mbps=mbps;} }
}
