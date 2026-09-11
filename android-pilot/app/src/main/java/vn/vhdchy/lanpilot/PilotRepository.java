package vn.vhdchy.lanpilot;

import android.content.ContentValues;
import android.content.Context;
import android.content.SharedPreferences;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;

import java.io.File;
import java.io.FileOutputStream;
import java.io.FileInputStream;
import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.TimeZone;
import java.util.UUID;

final class PilotRepository extends SQLiteOpenHelper {
    static final String PREFS = "lan_pilot";
    private static final long MAX_LOG_BYTES = 2L * 1024 * 1024;
    private final Context context;
    private final SharedPreferences prefs;
    private final File logFile;

    PilotRepository(Context context) {
        super(context.getApplicationContext(), "lan-pilot.db", null, 1);
        this.context = context.getApplicationContext();
        this.prefs = this.context.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
        this.logFile = new File(this.context.getFilesDir(), "lan-pilot-v4.log");
    }

    @Override public void onCreate(SQLiteDatabase db) {
        db.execSQL("CREATE TABLE pending(event_id TEXT PRIMARY KEY, device_seq INTEGER NOT NULL, payload TEXT, created_at INTEGER NOT NULL)");
    }
    @Override public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) { }

    SharedPreferences prefs() { return prefs; }

    synchronized String deviceId() {
        String id = prefs.getString("device_id", null);
        if (id == null || id.trim().isEmpty()) {
            id = "pda-" + UUID.randomUUID();
            prefs.edit().putString("device_id", id).apply();
        }
        return id;
    }

    synchronized long nextDeviceSeq() {
        long seq = prefs.getLong("device_seq", 0) + 1;
        prefs.edit().putLong("device_seq", seq).commit();
        return seq;
    }

    synchronized PendingEvent enqueue(String payload) {
        long seq = nextDeviceSeq();
        PendingEvent e = new PendingEvent(UUID.randomUUID().toString(), seq, payload, System.currentTimeMillis());
        ContentValues v = new ContentValues();
        v.put("event_id", e.eventId);
        v.put("device_seq", e.deviceSeq);
        v.put("payload", e.payload);
        v.put("created_at", e.createdAtMs);
        getWritableDatabase().insertWithOnConflict("pending", null, v, SQLiteDatabase.CONFLICT_IGNORE);
        increment("diag_queue_enqueued", 1);
        int now = countPending();
        long peak = prefs.getLong("diag_queue_peak", 0);
        if (now > peak) prefs.edit().putLong("diag_queue_peak", now).apply();
        log("QUEUE_ENQUEUE", "seq=" + seq + "; pending=" + now);
        return e;
    }

    synchronized List<PendingEvent> pending(int limit) {
        List<PendingEvent> list = new ArrayList<>();
        try (Cursor c = getReadableDatabase().rawQuery("SELECT event_id,device_seq,payload,created_at FROM pending ORDER BY device_seq LIMIT " + Math.max(1, Math.min(limit, 1000)), null)) {
            while (c.moveToNext()) list.add(new PendingEvent(c.getString(0), c.getLong(1), c.getString(2), c.getLong(3)));
        }
        return list;
    }

    synchronized void ack(String eventId, long seq, long ackMs) {
        int deleted = getWritableDatabase().delete("pending", "event_id=?", new String[]{eventId});
        if (deleted > 0) {
            increment("diag_queue_acked", 1);
            log("QUEUE_ACK", "seq=" + seq + "; ackMs=" + ackMs + "; pending=" + countPending());
        }
    }

    synchronized int countPending() {
        try (Cursor c = getReadableDatabase().rawQuery("SELECT COUNT(*) FROM pending", null)) {
            return c.moveToFirst() ? c.getInt(0) : 0;
        }
    }

    void increment(String key, long delta) {
        synchronized (prefs) {
            prefs.edit().putLong(key, prefs.getLong(key, 0) + delta).apply();
        }
    }

    long counter(String key) { return prefs.getLong(key, 0); }

    void setLastEndpoint(String endpoint) { prefs.edit().putString("cached_lan_endpoint", endpoint == null ? "" : endpoint).apply(); }
    String lastEndpoint() { return normalize(prefs.getString("cached_lan_endpoint", "")); }
    void setManualEndpoint(String endpoint) { prefs.edit().putString("manual_endpoint", normalize(endpoint)).apply(); }
    String manualEndpoint() { return normalize(prefs.getString("manual_endpoint", "")); }
    void setBackgroundService(boolean running) { prefs.edit().putBoolean("diag_background_service", running).apply(); }
    boolean backgroundServiceRunning() { return prefs.getBoolean("diag_background_service", false); }

    synchronized void log(String type, String message) {
        try {
            rotateIfNeeded();
            String line = utcNow() + "\t" + safe(type) + "\t" + safe(message).replace("\r", " ").replace("\n", " ") + "\r\n";
            try (FileOutputStream out = new FileOutputStream(logFile, true)) {
                out.write(line.getBytes(StandardCharsets.UTF_8));
            }
        } catch (Exception ignored) { }
    }

    synchronized String readLog() {
        if (!logFile.exists()) return "";
        try (FileInputStream in = new FileInputStream(logFile); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] buf = new byte[16 * 1024]; int n;
            while ((n = in.read(buf)) >= 0) out.write(buf, 0, n);
            return new String(out.toByteArray(), StandardCharsets.UTF_8);
        } catch (Exception e) { return ""; }
    }

    private void rotateIfNeeded() {
        if (!logFile.exists() || logFile.length() < MAX_LOG_BYTES) return;
        File old = new File(logFile.getParentFile(), logFile.getName() + ".1");
        try { if (old.exists()) old.delete(); } catch (Exception ignored) { }
        try { logFile.renameTo(old); } catch (Exception ignored) { }
    }

    static String normalize(String value) {
        if (value == null) return "";
        String s = value.trim();
        while (s.endsWith("/")) s = s.substring(0, s.length() - 1);
        return s;
    }

    private static String utcNow() {
        SimpleDateFormat f = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US);
        f.setTimeZone(TimeZone.getTimeZone("UTC"));
        return f.format(new Date());
    }

    private static String safe(String s) { return s == null ? "" : s; }

    static final class PendingEvent {
        final String eventId;
        final long deviceSeq;
        final String payload;
        final long createdAtMs;
        PendingEvent(String eventId, long deviceSeq, String payload, long createdAtMs) {
            this.eventId = eventId; this.deviceSeq = deviceSeq; this.payload = payload; this.createdAtMs = createdAtMs;
        }
    }
}
