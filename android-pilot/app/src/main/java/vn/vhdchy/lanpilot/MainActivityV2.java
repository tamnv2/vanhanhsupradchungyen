package vn.vhdchy.lanpilot;

import android.content.Intent;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

public class MainActivityV2 extends MainActivity {
    private static final int EXPORT_REQUEST = 9401;
    private static final long MAX_LOG_BYTES = 512 * 1024;
    private final ScheduledExecutorService snapshotTimer = Executors.newSingleThreadScheduledExecutor();
    private File diagnosticFile;
    private String lastSnapshot = "";
    private String pendingExport = null;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        diagnosticFile = new File(getFilesDir(), "lan-pilot-diagnostic.log");
        appendLog("APP_START", "version=" + appVersion() + "; android=" + Build.VERSION.RELEASE + "; device=" + Build.MANUFACTURER + " " + Build.MODEL);

        Button export = findViewById(R.id.exportLog);
        export.setOnClickListener(v -> prepareExport());

        snapshotTimer.scheduleWithFixedDelay(() -> runOnUiThread(this::captureUiSnapshot), 1, 2, TimeUnit.SECONDS);
    }

    private void captureUiSnapshot() {
        String snapshot = text(R.id.state) + " | " + text(R.id.endpoint) + " | " + text(R.id.queue) + " | " + text(R.id.status);
        if (!snapshot.equals(lastSnapshot)) {
            lastSnapshot = snapshot;
            appendLog("STATE", snapshot);
        }
    }

    private void prepareExport() {
        captureUiSnapshot();
        pendingExport = buildDiagnosticText();
        Intent intent = new Intent(Intent.ACTION_CREATE_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("text/plain");
        intent.putExtra(Intent.EXTRA_TITLE, "VHDCHY-LAN-Pilot-Diagnostics-" + localStamp() + ".txt");
        startActivityForResult(intent, EXPORT_REQUEST);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != EXPORT_REQUEST || resultCode != RESULT_OK || data == null || data.getData() == null || pendingExport == null) return;
        Uri uri = data.getData();
        try (OutputStream out = getContentResolver().openOutputStream(uri)) {
            if (out == null) throw new IllegalStateException("Không mở được file đích");
            out.write(pendingExport.getBytes(StandardCharsets.UTF_8));
            out.flush();
            appendLog("EXPORT", "Diagnostic export completed");
            Toast.makeText(this, "Đã xuất log. Tải file TXT này vào chat VHDCHY để phân tích.", Toast.LENGTH_LONG).show();
        } catch (Exception e) {
            Toast.makeText(this, "Xuất log thất bại: " + safe(e.getMessage()), Toast.LENGTH_LONG).show();
        } finally {
            pendingExport = null;
        }
    }

    private String buildDiagnosticText() {
        SharedPreferences prefs = getSharedPreferences("lan_pilot", MODE_PRIVATE);
        StringBuilder b = new StringBuilder();
        b.append("VHDCHY LAN Pilot Android Diagnostics\n");
        b.append("ExportedAt: ").append(utcNow()).append('\n');
        b.append("AppVersion: ").append(appVersion()).append('\n');
        b.append("Device: ").append(Build.MANUFACTURER).append(' ').append(Build.MODEL).append('\n');
        b.append("Android: ").append(Build.VERSION.RELEASE).append(" (SDK ").append(Build.VERSION.SDK_INT).append(")\n");
        b.append("DeviceInfoUI: ").append(text(R.id.deviceInfo)).append('\n');
        b.append("Transport: ").append(text(R.id.state)).append('\n');
        b.append("Endpoint: ").append(text(R.id.endpoint)).append('\n');
        b.append("Queue: ").append(text(R.id.queue)).append('\n');
        b.append("CurrentStatus:\n").append(text(R.id.status)).append("\n\n");
        b.append("SavedManualEndpoint: ").append(prefs.getString("manual_endpoint", "")).append('\n');
        b.append("SavedCachedEndpoint: ").append(prefs.getString("cached_lan_endpoint", "")).append('\n');
        b.append("DeviceSeq: ").append(prefs.getLong("device_seq", 0)).append('\n');
        b.append("\n--- Captured diagnostic history ---\n");
        b.append(readLog());
        b.append("\n--- Privacy note ---\n");
        b.append("Pilot diagnostics only. No business credentials are intentionally included. Local LAN IP/endpoint and test-state information may appear because they are required for LAN diagnosis.\n");
        return b.toString();
    }

    private void appendLog(String type, String message) {
        try {
            rotateIfNeeded();
            String line = utcNow() + "\t" + type + "\t" + safe(message).replace('\n', ' ').replace('\r', ' ') + "\n";
            try (FileOutputStream out = new FileOutputStream(diagnosticFile, true)) {
                out.write(line.getBytes(StandardCharsets.UTF_8));
            }
        } catch (Exception ignored) { }
    }

    private void rotateIfNeeded() {
        if (diagnosticFile == null || !diagnosticFile.exists() || diagnosticFile.length() < MAX_LOG_BYTES) return;
        File old = new File(getFilesDir(), "lan-pilot-diagnostic.log.1");
        if (old.exists()) old.delete();
        diagnosticFile.renameTo(old);
    }

    private String readLog() {
        StringBuilder b = new StringBuilder();
        File old = new File(getFilesDir(), "lan-pilot-diagnostic.log.1");
        if (old.exists()) b.append(readFile(old));
        if (diagnosticFile != null && diagnosticFile.exists()) b.append(readFile(diagnosticFile));
        return b.toString();
    }

    private String readFile(File file) {
        try (FileInputStream in = new FileInputStream(file); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] buffer = new byte[8192];
            int n;
            while ((n = in.read(buffer)) >= 0) out.write(buffer, 0, n);
            return new String(out.toByteArray(), StandardCharsets.UTF_8);
        } catch (Exception e) {
            return "[read log failed: " + safe(e.getMessage()) + "]\n";
        }
    }

    private String text(int id) {
        TextView v = findViewById(id);
        return v == null || v.getText() == null ? "" : v.getText().toString();
    }

    private String appVersion() {
        try { return getPackageManager().getPackageInfo(getPackageName(), 0).versionName; }
        catch (Exception e) { return "unknown"; }
    }

    private static String utcNow() {
        SimpleDateFormat f = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US);
        f.setTimeZone(TimeZone.getTimeZone("UTC"));
        return f.format(new Date());
    }

    private static String localStamp() {
        return new SimpleDateFormat("yyyyMMdd-HHmmss", Locale.US).format(new Date());
    }

    private static String safe(String s) { return s == null ? "" : s; }

    @Override
    protected void onDestroy() {
        snapshotTimer.shutdownNow();
        appendLog("APP_STOP", "Activity destroyed");
        super.onDestroy();
    }
}
