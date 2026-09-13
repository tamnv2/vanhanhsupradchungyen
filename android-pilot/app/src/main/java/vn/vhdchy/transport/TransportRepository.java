package vn.vhdchy.transport;

import android.content.ContentValues;
import android.content.Context;
import android.content.SharedPreferences;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

final class TransportRepository extends SQLiteOpenHelper {
    private static final String PREFS = "vhdchy_transport_beta";
    private final SharedPreferences prefs;

    TransportRepository(Context context) {
        super(context.getApplicationContext(), "vhdchy-transport-beta.db", null, 1);
        prefs = context.getApplicationContext().getSharedPreferences(PREFS, Context.MODE_PRIVATE);
    }

    @Override public void onCreate(SQLiteDatabase db) {
        db.execSQL("CREATE TABLE pending_test (idempotency_key TEXT PRIMARY KEY, device_seq INTEGER NOT NULL, payload TEXT NOT NULL, created_at INTEGER NOT NULL)");
    }

    @Override public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) { }

    synchronized String deviceId() {
        String id = prefs.getString("device_id", "");
        if (id == null || id.isBlank()) {
            id = "pda-" + UUID.randomUUID();
            prefs.edit().putString("device_id", id).commit();
        }
        return id;
    }

    synchronized long nextDeviceSeq() {
        long value = prefs.getLong("device_seq", 0L) + 1L;
        prefs.edit().putLong("device_seq", value).commit();
        return value;
    }

    synchronized Pending enqueueTest(String payload) {
        Pending p = new Pending(UUID.randomUUID().toString(), nextDeviceSeq(), payload, System.currentTimeMillis());
        ContentValues values = new ContentValues();
        values.put("idempotency_key", p.idempotencyKey);
        values.put("device_seq", p.deviceSeq);
        values.put("payload", p.payload);
        values.put("created_at", p.createdAtMs);
        getWritableDatabase().insertOrThrow("pending_test", null, values);
        return p;
    }

    synchronized List<Pending> pendingTests(int limit) {
        int bounded = Math.max(1, Math.min(limit, 100));
        List<Pending> out = new ArrayList<>();
        try (Cursor c = getReadableDatabase().rawQuery(
                "SELECT idempotency_key,device_seq,payload,created_at FROM pending_test ORDER BY device_seq LIMIT " + bounded,
                null)) {
            while (c.moveToNext()) {
                out.add(new Pending(c.getString(0), c.getLong(1), c.getString(2), c.getLong(3)));
            }
        }
        return out;
    }

    synchronized void ackTest(String idempotencyKey) {
        getWritableDatabase().delete("pending_test", "idempotency_key=?", new String[]{idempotencyKey});
    }

    synchronized int countPendingTests() {
        try (Cursor c = getReadableDatabase().rawQuery("SELECT COUNT(*) FROM pending_test", null)) {
            return c.moveToFirst() ? c.getInt(0) : 0;
        }
    }

    synchronized String cachedEndpoint() {
        String value = prefs.getString("cached_endpoint", "");
        return value == null ? "" : value.trim();
    }

    synchronized void setCachedEndpoint(String endpoint) {
        prefs.edit().putString("cached_endpoint", endpoint == null ? "" : endpoint.trim()).apply();
    }

    static final class Pending {
        final String idempotencyKey;
        final long deviceSeq;
        final String payload;
        final long createdAtMs;

        Pending(String idempotencyKey, long deviceSeq, String payload, long createdAtMs) {
            this.idempotencyKey = idempotencyKey;
            this.deviceSeq = deviceSeq;
            this.payload = payload;
            this.createdAtMs = createdAtMs;
        }
    }
}
