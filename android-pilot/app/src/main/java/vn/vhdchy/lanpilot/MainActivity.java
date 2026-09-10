package vn.vhdchy.lanpilot;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.ContentValues;
import android.content.Intent;
import android.content.SharedPreferences;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.SystemClock;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.HttpURLConnection;
import java.net.InetAddress;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.UUID;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

public class MainActivity extends Activity {
    private static final String PROTOCOL = "VHDCHY_LAN_PILOT_V1";
    private static final int DISCOVERY_PORT = 17892;
    private static final String DISCOVERY_MESSAGE = "VHDCHY_DISCOVER_BETA_V1";
    private static final String CLOUD_HEALTH = "https://beta.supra.cc.cd/health";
    private static final String RELEASES_API = "https://api.github.com/repos/tamnv2/vanhanhdchungyen/releases?per_page=20";

    private final ScheduledExecutorService io = Executors.newScheduledThreadPool(2);
    private final AtomicBoolean cycleRunning = new AtomicBoolean(false);
    private SharedPreferences prefs;
    private PilotQueue queueDb;
    private TextView deviceInfo;
    private TextView stateView;
    private TextView endpointView;
    private TextView statusView;
    private TextView queueView;
    private EditText manualEndpoint;
    private volatile String activeLanEndpoint;
    private volatile TransportState state = TransportState.RECONNECTING;
    private int successStreak = 0;
    private int failureStreak = 0;
    private String deviceId;

    enum TransportState {
        CLOUD_ONLY, LAN_AVAILABLE, LAN_ACTIVE, LAN_LOST, RECONNECTING, LOCAL_QUEUE_ONLY
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        prefs = getSharedPreferences("lan_pilot", MODE_PRIVATE);
        queueDb = new PilotQueue();
        deviceId = prefs.getString("device_id", null);
        if (deviceId == null) {
            deviceId = "pda-" + UUID.randomUUID();
            prefs.edit().putString("device_id", deviceId).apply();
        }

        deviceInfo = findViewById(R.id.deviceInfo);
        stateView = findViewById(R.id.state);
        endpointView = findViewById(R.id.endpoint);
        statusView = findViewById(R.id.status);
        queueView = findViewById(R.id.queue);
        manualEndpoint = findViewById(R.id.manualEndpoint);
        manualEndpoint.setText(prefs.getString("manual_endpoint", ""));

        deviceInfo.setText("Thiết bị: " + Build.MANUFACTURER + " " + Build.MODEL + " | Android " + Build.VERSION.RELEASE + " | Device ID: " + deviceId);

        ((Button) findViewById(R.id.discover)).setOnClickListener(v -> io.execute(() -> runAutoCycle(true)));
        ((Button) findViewById(R.id.echo)).setOnClickListener(v -> io.execute(this::runEcho));
        ((Button) findViewById(R.id.event)).setOnClickListener(v -> io.execute(this::enqueueAndFlushEvent));
        ((Button) findViewById(R.id.update)).setOnClickListener(v -> io.execute(() -> checkUpdate(true)));
        ((Button) findViewById(R.id.saveEndpoint)).setOnClickListener(v -> saveManualEndpoint());

        refreshQueueCount();
        io.scheduleWithFixedDelay(() -> runAutoCycle(false), 0, 4, TimeUnit.SECONDS);
        io.scheduleWithFixedDelay(() -> checkUpdate(false), 10, 6 * 60 * 60, TimeUnit.SECONDS);
    }

    private void saveManualEndpoint() {
        String value = normalizeEndpoint(manualEndpoint.getText().toString());
        prefs.edit().putString("manual_endpoint", value).apply();
        manualEndpoint.setText(value);
        Toast.makeText(this, "Đã lưu endpoint recovery. Health identity vẫn bắt buộc.", Toast.LENGTH_SHORT).show();
        io.execute(() -> runAutoCycle(true));
    }

    private void runAutoCycle(boolean forced) {
        if (!cycleRunning.compareAndSet(false, true)) return;
        try {
            Candidate candidate = null;
            String cached = normalizeEndpoint(prefs.getString("cached_lan_endpoint", ""));
            if (!cached.isEmpty()) candidate = verifyLan(cached);

            if (candidate == null) {
                String manual = normalizeEndpoint(prefs.getString("manual_endpoint", ""));
                if (!manual.isEmpty() && !manual.equals(cached)) candidate = verifyLan(manual);
            }

            if (candidate == null) candidate = discoverLan();

            if (candidate != null) {
                failureStreak = 0;
                successStreak++;
                prefs.edit().putString("cached_lan_endpoint", candidate.endpoint).apply();
                activeLanEndpoint = candidate.endpoint;
                if (successStreak >= 2) {
                    setState(TransportState.LAN_ACTIVE, "LAN identity/health PASS " + successStreak + " lần liên tiếp", candidate.endpoint);
                    flushPending();
                } else {
                    setState(TransportState.LAN_AVAILABLE, "Đã phát hiện LAN hợp lệ; đang hysteresis trước khi activate", candidate.endpoint);
                }
            } else {
                successStreak = 0;
                failureStreak++;
                boolean cloud = checkCloud();
                if (state == TransportState.LAN_ACTIVE && failureStreak < 2) {
                    setState(TransportState.LAN_LOST, "Mất 1 health sample; chưa fallback để tránh flapping", activeLanEndpoint);
                } else if (failureStreak < 2) {
                    setState(TransportState.RECONNECTING, "Đang tìm lại LAN", activeLanEndpoint);
                } else if (cloud) {
                    activeLanEndpoint = null;
                    setState(TransportState.CLOUD_ONLY, "LAN không khả dụng; Cloud health PASS", null);
                } else {
                    activeLanEndpoint = null;
                    setState(TransportState.LOCAL_QUEUE_ONLY, "LAN và Cloud đều không khả dụng; giữ test event local", null);
                }
            }

            if (forced) appendStatus("Manual discovery cycle hoàn tất.");
        } finally {
            cycleRunning.set(false);
        }
    }

    private Candidate discoverLan() {
        DatagramSocket socket = null;
        try {
            socket = new DatagramSocket();
            socket.setBroadcast(true);
            socket.setSoTimeout(1200);
            byte[] out = DISCOVERY_MESSAGE.getBytes(StandardCharsets.UTF_8);
            DatagramPacket request = new DatagramPacket(out, out.length, InetAddress.getByName("255.255.255.255"), DISCOVERY_PORT);
            socket.send(request);

            byte[] buffer = new byte[2048];
            DatagramPacket response = new DatagramPacket(buffer, buffer.length);
            socket.receive(response);
            String raw = new String(response.getData(), 0, response.getLength(), StandardCharsets.UTF_8);
            JSONObject j = new JSONObject(raw);
            if (!j.optBoolean("ok") || !"VHDCHY_LAN_AGENT".equals(j.optString("service")) || !"BETA".equals(j.optString("environment")) || !PROTOCOL.equals(j.optString("protocol"))) {
                appendStatus("Discovery response bị từ chối do identity mismatch.");
                return null;
            }
            int port = j.optInt("httpPort", 17891);
            String endpoint = "http://" + response.getAddress().getHostAddress() + ":" + port;
            return verifyLan(endpoint);
        } catch (Exception e) {
            return null;
        } finally {
            if (socket != null) socket.close();
        }
    }

    private Candidate verifyLan(String endpoint) {
        try {
            JSONObject j = getJson(endpoint + "/health", 1200, 1600);
            if (j.optBoolean("ok") && "VHDCHY_LAN_AGENT".equals(j.optString("service")) && "BETA".equals(j.optString("environment")) && PROTOCOL.equals(j.optString("protocol"))) {
                return new Candidate(endpoint, j.optString("instanceId"));
            }
        } catch (Exception ignored) { }
        return null;
    }

    private boolean checkCloud() {
        try {
            JSONObject j = getJson(CLOUD_HEALTH, 1500, 2000);
            return j.optBoolean("ok") && "VHDCHY_WORKER".equals(j.optString("service"));
        } catch (Exception e) {
            return false;
        }
    }

    private void runEcho() {
        String endpoint = activeLanEndpoint;
        if (endpoint == null || state != TransportState.LAN_ACTIVE) {
            appendStatus("Echo bỏ qua: chưa ở LAN_ACTIVE.");
            return;
        }
        try {
            long start = SystemClock.elapsedRealtimeNanos();
            JSONObject req = new JSONObject().put("deviceId", deviceId).put("payload", "mt90-echo");
            JSONObject result = postJson(endpoint + "/api/pilot/echo", req, 1500, 2000);
            double ms = (SystemClock.elapsedRealtimeNanos() - start) / 1_000_000.0;
            appendStatus("Echo " + (result.optBoolean("ok") ? "PASS" : "FAIL") + " | " + String.format(java.util.Locale.US, "%.1f ms", ms));
        } catch (Exception e) {
            appendStatus("Echo FAIL: " + shortMessage(e));
        }
    }

    private void enqueueAndFlushEvent() {
        long seq = prefs.getLong("device_seq", 0) + 1;
        prefs.edit().putLong("device_seq", seq).commit();
        String eventId = UUID.randomUUID().toString();
        queueDb.enqueue(eventId, seq, "pilot-event");
        appendStatus("Đã enqueue event seq=" + seq);
        refreshQueueCount();
        flushPending();
    }

    private void flushPending() {
        String endpoint = activeLanEndpoint;
        if (endpoint == null || state != TransportState.LAN_ACTIVE) return;
        Cursor cursor = queueDb.pending();
        try {
            while (cursor.moveToNext()) {
                String eventId = cursor.getString(0);
                long seq = cursor.getLong(1);
                String payload = cursor.getString(2);
                try {
                    JSONObject req = new JSONObject()
                            .put("eventId", eventId)
                            .put("deviceId", deviceId)
                            .put("deviceSeq", seq)
                            .put("createdAt", new java.util.Date().toInstant().toString())
                            .put("payload", payload);
                    JSONObject result = postJson(endpoint + "/api/pilot/event", req, 1800, 2500);
                    if (result.optBoolean("ok")) queueDb.ack(eventId);
                    else break;
                } catch (Exception e) {
                    break;
                }
            }
        } finally {
            cursor.close();
            refreshQueueCount();
        }
    }

    private void checkUpdate(boolean manual) {
        try {
            JSONArray releases = getJsonArray(RELEASES_API, 2500, 4000);
            String tag = null;
            String page = null;
            for (int i = 0; i < releases.length(); i++) {
                JSONObject r = releases.getJSONObject(i);
                String t = r.optString("tag_name", "");
                if (r.optBoolean("prerelease") && t.startsWith("lan-pilot-beta-")) {
                    tag = t;
                    page = r.optString("html_url", "https://github.com/tamnv2/vanhanhdchungyen/releases");
                    break;
                }
            }
            if (tag == null) {
                if (manual) appendStatus("Update: chưa có LAN Pilot BETA release.");
                return;
            }
            String previous = prefs.getString("last_seen_release", "");
            if (manual || !tag.equals(previous)) {
                prefs.edit().putString("last_seen_release", tag).apply();
                final String releaseTag = tag;
                final String releasePage = page;
                runOnUiThread(() -> new AlertDialog.Builder(this)
                        .setTitle("Cập nhật BETA")
                        .setMessage("Có release: " + releaseTag + "\n\nManual update luôn khả dụng nếu auto-check lỗi.")
                        .setNegativeButton("Đóng", null)
                        .setPositiveButton("Mở trang tải", (d, w) -> startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(releasePage))))
                        .show());
            }
        } catch (Exception e) {
            if (manual) appendStatus("Update check FAIL: " + shortMessage(e));
        }
    }

    private JSONObject getJson(String url, int connectTimeout, int readTimeout) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(url).openConnection();
        c.setRequestMethod("GET");
        c.setConnectTimeout(connectTimeout);
        c.setReadTimeout(readTimeout);
        c.setUseCaches(false);
        c.setRequestProperty("Accept", "application/json");
        c.setRequestProperty("User-Agent", "VHDCHY-LAN-Pilot-Android/0.1");
        int status = c.getResponseCode();
        String body = read(status >= 200 && status < 400 ? c.getInputStream() : c.getErrorStream());
        c.disconnect();
        if (status < 200 || status >= 300) throw new Exception("HTTP " + status);
        return new JSONObject(body);
    }

    private JSONArray getJsonArray(String url, int connectTimeout, int readTimeout) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(url).openConnection();
        c.setRequestMethod("GET");
        c.setConnectTimeout(connectTimeout);
        c.setReadTimeout(readTimeout);
        c.setRequestProperty("Accept", "application/vnd.github+json");
        c.setRequestProperty("User-Agent", "VHDCHY-LAN-Pilot-Android/0.1");
        int status = c.getResponseCode();
        String body = read(c.getInputStream());
        c.disconnect();
        if (status < 200 || status >= 300) throw new Exception("HTTP " + status);
        return new JSONArray(body);
    }

    private JSONObject postJson(String url, JSONObject payload, int connectTimeout, int readTimeout) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(url).openConnection();
        c.setRequestMethod("POST");
        c.setDoOutput(true);
        c.setConnectTimeout(connectTimeout);
        c.setReadTimeout(readTimeout);
        c.setRequestProperty("Content-Type", "application/json; charset=utf-8");
        byte[] bytes = payload.toString().getBytes(StandardCharsets.UTF_8);
        try (OutputStream out = c.getOutputStream()) { out.write(bytes); }
        int status = c.getResponseCode();
        String body = read(status >= 200 && status < 400 ? c.getInputStream() : c.getErrorStream());
        c.disconnect();
        if (status < 200 || status >= 300) throw new Exception("HTTP " + status);
        return new JSONObject(body);
    }

    private String read(InputStream in) throws Exception {
        if (in == null) return "";
        StringBuilder b = new StringBuilder();
        try (BufferedReader r = new BufferedReader(new InputStreamReader(in, StandardCharsets.UTF_8))) {
            String line;
            while ((line = r.readLine()) != null) b.append(line);
        }
        return b.toString();
    }

    private void setState(TransportState newState, String reason, String endpoint) {
        state = newState;
        runOnUiThread(() -> {
            stateView.setText("Transport: " + newState.name());
            endpointView.setText("LAN endpoint: " + (endpoint == null ? "—" : endpoint));
            statusView.setText(reason + "\nSuccess streak: " + successStreak + " | Failure streak: " + failureStreak);
        });
    }

    private void appendStatus(String text) {
        runOnUiThread(() -> statusView.setText(statusView.getText() + "\n" + text));
    }

    private void refreshQueueCount() {
        int count = queueDb == null ? 0 : queueDb.count();
        runOnUiThread(() -> queueView.setText("Pending local queue: " + count));
    }

    private String normalizeEndpoint(String value) {
        if (value == null) return "";
        String s = value.trim();
        while (s.endsWith("/")) s = s.substring(0, s.length() - 1);
        return s;
    }

    private String shortMessage(Exception e) {
        String m = e.getMessage();
        return m == null ? e.getClass().getSimpleName() : m;
    }

    @Override
    protected void onDestroy() {
        io.shutdownNow();
        queueDb.close();
        super.onDestroy();
    }

    private static class Candidate {
        final String endpoint;
        final String instanceId;
        Candidate(String endpoint, String instanceId) { this.endpoint = endpoint; this.instanceId = instanceId; }
    }

    private class PilotQueue extends SQLiteOpenHelper {
        PilotQueue() { super(MainActivity.this, "lan-pilot.db", null, 1); }

        @Override
        public void onCreate(SQLiteDatabase db) {
            db.execSQL("CREATE TABLE pending(event_id TEXT PRIMARY KEY, device_seq INTEGER NOT NULL, payload TEXT, created_at INTEGER NOT NULL)");
        }

        @Override
        public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) { }

        void enqueue(String eventId, long seq, String payload) {
            ContentValues v = new ContentValues();
            v.put("event_id", eventId);
            v.put("device_seq", seq);
            v.put("payload", payload);
            v.put("created_at", System.currentTimeMillis());
            getWritableDatabase().insertWithOnConflict("pending", null, v, SQLiteDatabase.CONFLICT_IGNORE);
        }

        Cursor pending() {
            return getReadableDatabase().rawQuery("SELECT event_id,device_seq,payload FROM pending ORDER BY device_seq LIMIT 500", null);
        }

        void ack(String eventId) {
            getWritableDatabase().delete("pending", "event_id=?", new String[]{eventId});
        }

        int count() {
            try (Cursor c = getReadableDatabase().rawQuery("SELECT COUNT(*) FROM pending", null)) {
                return c.moveToFirst() ? c.getInt(0) : 0;
            }
        }
    }
}
