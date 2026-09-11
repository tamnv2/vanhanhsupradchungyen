package vn.vhdchy.lanpilot;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.Service;
import android.content.Intent;
import android.os.Build;
import android.os.IBinder;
import android.os.PowerManager;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicBoolean;

public class LanFinishService extends Service {
    private static final String CHANNEL = "vhdchy_lan_finish";
    private static final int NOTIFICATION_ID = 1291;
    private final ExecutorService executor = Executors.newSingleThreadExecutor();
    private final AtomicBoolean running = new AtomicBoolean(false);
    private PilotRepository repo;
    private PowerManager.WakeLock wakeLock;

    @Override public void onCreate() {
        super.onCreate();
        repo = new PilotRepository(this);
        repo.setBackgroundService(true);
        repo.log("BG_SERVICE_START", "created; activeJobs=" + MainActivityV4.activeJobCount() + "; pending=" + repo.countPending());
        createChannel();
    }

    @Override public int onStartCommand(Intent intent, int flags, int startId) {
        startForeground(NOTIFICATION_ID, notification("Đang hoàn tất tác vụ LAN còn dở"));
        if (running.compareAndSet(false, true)) executor.execute(this::finishWork);
        return START_NOT_STICKY;
    }

    private void finishWork() {
        long started = android.os.SystemClock.elapsedRealtime();
        boolean hasInflight = MainActivityV4.activeJobCount() > 0;
        if (hasInflight) acquireWakeLock();
        try {
            // Keep the process alive only while an already-started foreground task is finishing.
            // Hard ceiling prevents a broken task from holding battery forever.
            while (MainActivityV4.activeJobCount() > 0 && android.os.SystemClock.elapsedRealtime() - started < 20L * 60_000L) {
                sleep(1000);
            }
            if (MainActivityV4.activeJobCount() > 0) repo.log("BG_JOB_TIMEOUT", "activeJobs=" + MainActivityV4.activeJobCount());

            // One bounded queue recovery window. If LAN is unavailable, stop and wait for next app open.
            long queueDeadline = android.os.SystemClock.elapsedRealtime() + 30_000L;
            int attempt = 0;
            while (repo.countPending() > 0 && android.os.SystemClock.elapsedRealtime() < queueDeadline && attempt < 3) {
                attempt++;
                repo.increment("diag_bg_flush_attempts", 1);
                String endpoint = repo.lastEndpoint();
                if (endpoint.isEmpty() || !healthOk(endpoint)) {
                    repo.log("BG_QUEUE_WAIT", "attempt=" + attempt + "; endpointHealthy=false; pending=" + repo.countPending());
                    sleep(3000);
                    continue;
                }
                int before = repo.countPending();
                flush(endpoint);
                int after = repo.countPending();
                repo.log("BG_QUEUE_FLUSH", "attempt=" + attempt + "; before=" + before + "; after=" + after);
                if (after >= before) sleep(2500);
            }
        } catch (Exception e) {
            repo.increment("diag_bg_errors", 1);
            repo.log("BG_SERVICE_ERROR", e.getClass().getSimpleName() + ": " + safe(e.getMessage()));
        } finally {
            releaseWakeLock();
            repo.log("BG_SERVICE_STOP", "activeJobs=" + MainActivityV4.activeJobCount() + "; pending=" + repo.countPending() + "; elapsedMs=" + (android.os.SystemClock.elapsedRealtime() - started));
            repo.setBackgroundService(false);
            stopForeground(true);
            stopSelf();
            running.set(false);
        }
    }

    private void flush(String endpoint) {
        List<PilotRepository.PendingEvent> list = repo.pending(500);
        for (PilotRepository.PendingEvent e : list) {
            try {
                long t0 = android.os.SystemClock.elapsedRealtime();
                JSONObject req = new JSONObject()
                        .put("eventId", e.eventId)
                        .put("deviceId", repo.deviceId())
                        .put("deviceSeq", e.deviceSeq)
                        .put("createdAt", new java.util.Date(e.createdAtMs).toInstant().toString())
                        .put("payload", e.payload);
                JSONObject response = postJson(endpoint + "/api/pilot/event", req, 1800, 3000);
                if (!response.optBoolean("ok")) break;
                repo.ack(e.eventId, e.deviceSeq, android.os.SystemClock.elapsedRealtime() - t0);
            } catch (Exception ex) {
                repo.increment("diag_queue_flush_fail", 1);
                repo.log("BG_QUEUE_FAIL", "seq=" + e.deviceSeq + "; " + ex.getClass().getSimpleName() + ": " + safe(ex.getMessage()));
                break;
            }
        }
    }

    private boolean healthOk(String endpoint) {
        try {
            HttpURLConnection c = (HttpURLConnection) new URL(endpoint + "/health").openConnection();
            c.setConnectTimeout(1000); c.setReadTimeout(1200); c.setUseCaches(false);
            int status = c.getResponseCode();
            String body = read(status >= 200 && status < 400 ? c.getInputStream() : c.getErrorStream());
            c.disconnect();
            if (status < 200 || status >= 300) return false;
            JSONObject j = new JSONObject(body);
            return j.optBoolean("ok") && "VHDCHY_LAN_AGENT".equals(j.optString("service")) && "BETA".equals(j.optString("environment"));
        } catch (Exception e) { return false; }
    }

    private JSONObject postJson(String url, JSONObject payload, int connectTimeout, int readTimeout) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(url).openConnection();
        c.setRequestMethod("POST"); c.setDoOutput(true); c.setConnectTimeout(connectTimeout); c.setReadTimeout(readTimeout);
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
            String line; while ((line = r.readLine()) != null) b.append(line);
        }
        return b.toString();
    }

    private void acquireWakeLock() {
        try {
            PowerManager pm = (PowerManager) getSystemService(POWER_SERVICE);
            wakeLock = pm.newWakeLock(PowerManager.PARTIAL_WAKE_LOCK, "VHDCHY:FinishWork");
            wakeLock.setReferenceCounted(false);
            wakeLock.acquire(20L * 60_000L);
            repo.log("BG_WAKELOCK", "acquired bounded 20min for active foreground-started task");
        } catch (Exception e) { repo.log("BG_WAKELOCK_FAIL", safe(e.getMessage())); }
    }

    private void releaseWakeLock() {
        try { if (wakeLock != null && wakeLock.isHeld()) wakeLock.release(); } catch (Exception ignored) { }
        wakeLock = null;
    }

    private void createChannel() {
        if (Build.VERSION.SDK_INT >= 26) {
            NotificationChannel channel = new NotificationChannel(CHANNEL, "VHDCHY LAN tác vụ đang hoàn tất", NotificationManager.IMPORTANCE_LOW);
            channel.setDescription("Chỉ hiện khi app rời màn hình nhưng còn tác vụ LAN đang chạy dở.");
            ((NotificationManager) getSystemService(NOTIFICATION_SERVICE)).createNotificationChannel(channel);
        }
    }

    private Notification notification(String text) {
        Notification.Builder b = Build.VERSION.SDK_INT >= 26 ? new Notification.Builder(this, CHANNEL) : new Notification.Builder(this);
        return b.setContentTitle("VHDCHY LAN BETA")
                .setContentText(text)
                .setSmallIcon(android.R.drawable.stat_notify_sync)
                .setOngoing(true)
                .build();
    }

    private static void sleep(long ms) { try { Thread.sleep(ms); } catch (InterruptedException e) { Thread.currentThread().interrupt(); } }
    private static String safe(String s) { return s == null ? "" : s; }

    @Override public void onDestroy() {
        releaseWakeLock();
        if (repo != null) { repo.setBackgroundService(false); repo.log("BG_SERVICE_DESTROY", "destroyed"); repo.close(); }
        executor.shutdownNow();
        super.onDestroy();
    }

    @Override public IBinder onBind(Intent intent) { return null; }
}
