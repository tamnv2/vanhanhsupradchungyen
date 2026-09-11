package vn.vhdchy.lanpilot;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;
import android.widget.Toast;

import androidx.core.content.FileProvider;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Locale;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

final class UpdateManagerV4 {
    private static final String RELEASES_API = "https://api.github.com/repos/tamnv2/vanhanhdchungyen/releases?per_page=20";
    private final Activity activity;
    private final PilotRepository repo;
    private final ExecutorService io = Executors.newSingleThreadExecutor();

    UpdateManagerV4(Activity activity, PilotRepository repo) {
        this.activity = activity;
        this.repo = repo;
    }

    void check(boolean manual) {
        io.execute(() -> {
            try {
                Release release = latest();
                String current = BuildConfig.VERSION_NAME;
                boolean newer = release != null && isNewer(release.version, current);
                repo.log("UPDATE_CHECK", "current=" + current + "; latest=" + (release == null ? "—" : release.version) + "; newer=" + newer + "; manual=" + manual);
                if (release == null) {
                    if (manual) ui(() -> Toast.makeText(activity, "Không đọc được release BETA hợp lệ.", Toast.LENGTH_LONG).show());
                    return;
                }
                if (!newer) {
                    if (manual) ui(() -> new AlertDialog.Builder(activity)
                            .setTitle("Cập nhật APK")
                            .setMessage("Đang ở bản mới nhất.\nHiện tại: " + current + "\nRelease: " + release.version)
                            .setPositiveButton("Đóng", null).show());
                    return;
                }

                String last = repo.prefs().getString("last_notified_release_v4", "");
                if (!manual && release.version.equals(last)) return;
                if (!manual) repo.prefs().edit().putString("last_notified_release_v4", release.version).apply();
                ui(() -> showUpdateDialog(release, manual));
            } catch (Exception e) {
                repo.log("UPDATE_CHECK_FAIL", e.getClass().getSimpleName() + ": " + safe(e.getMessage()));
                if (manual) ui(() -> Toast.makeText(activity, "Kiểm tra cập nhật lỗi: " + safe(e.getMessage()), Toast.LENGTH_LONG).show());
            }
        });
    }

    private void showUpdateDialog(Release release, boolean manual) {
        if (activity.isFinishing()) return;
        String prefix = manual ? "Có bản mới" : "Tự động phát hiện bản mới";
        new AlertDialog.Builder(activity)
                .setTitle("Cập nhật VHDCHY LAN BETA")
                .setMessage(prefix + ": " + release.version + "\nHiện tại: " + BuildConfig.VERSION_NAME + "\n\nAPK sẽ được tải trực tiếp, kiểm tra SHA256 rồi mới mở trình cài đặt Android. Android thường vẫn yêu cầu người dùng xác nhận cài.")
                .setNegativeButton("Đóng", null)
                .setNeutralButton("Mở GitHub", (d, w) -> activity.startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(release.htmlUrl))))
                .setPositiveButton("Tải & cài", (d, w) -> downloadAndInstall(release))
                .show();
    }

    private void downloadAndInstall(Release release) {
        io.execute(() -> {
            try {
                if (release.apkUrl.isEmpty() || release.shaUrl.isEmpty()) throw new Exception("Release thiếu APK/SHA256 asset");
                repo.log("UPDATE_DOWNLOAD", "start target=" + release.version);
                byte[] apk = getBytes(release.apkUrl, 15_000, 120_000);
                String shaText = getText(release.shaUrl, 10_000, 20_000);
                String expected = firstToken(shaText).toLowerCase(Locale.US);
                String actual = sha256(apk);
                if (!actual.equals(expected)) throw new Exception("SHA256 mismatch expected=" + expected + " actual=" + actual);

                File dir = new File(activity.getExternalCacheDir() != null ? activity.getExternalCacheDir() : activity.getCacheDir(), "updates");
                if (!dir.exists() && !dir.mkdirs()) throw new Exception("Không tạo được thư mục update");
                File file = new File(dir, "VHDCHY-LAN-Pilot-BETA-" + release.version + ".apk");
                try (FileOutputStream out = new FileOutputStream(file)) { out.write(apk); }
                android.content.pm.PackageInfo pi = activity.getPackageManager().getPackageArchiveInfo(file.getAbsolutePath(), 0);
                if (pi == null || !activity.getPackageName().equals(pi.packageName)) throw new Exception("APK package mismatch: " + (pi == null ? "unreadable" : pi.packageName));
                String archiveVersion = Build.VERSION.SDK_INT >= 28 ? pi.getLongVersionCode() + "/" + pi.versionName : pi.versionCode + "/" + pi.versionName;
                if (pi.versionName == null || !release.version.equals(pi.versionName)) throw new Exception("APK version mismatch: release=" + release.version + " apk=" + pi.versionName);
                repo.log("UPDATE_VERIFIED", "target=" + release.version + "; archive=" + archiveVersion + "; bytes=" + apk.length + "; sha256=" + actual);
                ui(() -> launchInstaller(file));
            } catch (Exception e) {
                repo.log("UPDATE_DOWNLOAD_FAIL", e.getClass().getSimpleName() + ": " + safe(e.getMessage()));
                ui(() -> new AlertDialog.Builder(activity)
                        .setTitle("Cập nhật thất bại")
                        .setMessage("Không dùng quyền đặc biệt để vượt policy. Có thể cập nhật thủ công từ GitHub Release.\n\n" + safe(e.getMessage()))
                        .setPositiveButton("Đóng", null)
                        .setNegativeButton("Mở GitHub", (d, w) -> activity.startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse("https://github.com/tamnv2/vanhanhdchungyen/releases"))))
                        .show());
            }
        });
    }

    private void launchInstaller(File apk) {
        try {
            if (Build.VERSION.SDK_INT >= 26 && !activity.getPackageManager().canRequestPackageInstalls()) {
                repo.log("UPDATE_PERMISSION", "REQUEST_INSTALL_PACKAGES not yet allowed; opening Android settings");
                Intent settings = new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES, Uri.parse("package:" + activity.getPackageName()));
                activity.startActivity(settings);
                Toast.makeText(activity, "Cho phép cài ứng dụng từ nguồn này rồi bấm Cập nhật lại.", Toast.LENGTH_LONG).show();
                return;
            }
            Uri uri = FileProvider.getUriForFile(activity, activity.getPackageName() + ".fileprovider", apk);
            Intent intent = new Intent(Intent.ACTION_VIEW);
            intent.setDataAndType(uri, "application/vnd.android.package-archive");
            intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_ACTIVITY_NEW_TASK);
            repo.log("UPDATE_INSTALLER", "launch file=" + apk.getName());
            activity.startActivity(intent);
        } catch (Exception e) {
            repo.log("UPDATE_INSTALLER_FAIL", e.getClass().getSimpleName() + ": " + safe(e.getMessage()));
            Toast.makeText(activity, "Không mở được trình cài đặt: " + safe(e.getMessage()), Toast.LENGTH_LONG).show();
        }
    }

    private Release latest() throws Exception {
        JSONArray releases = new JSONArray(getText(RELEASES_API, 8000, 15_000));
        for (int i = 0; i < releases.length(); i++) {
            JSONObject r = releases.getJSONObject(i);
            String tag = r.optString("tag_name", "");
            if (!r.optBoolean("prerelease") || !tag.startsWith("lan-pilot-beta-v")) continue;
            String version = tag.substring("lan-pilot-beta-v".length());
            if (!validVersion(version)) continue;
            String apk = "", sha = "";
            JSONArray assets = r.optJSONArray("assets");
            if (assets != null) {
                for (int j = 0; j < assets.length(); j++) {
                    JSONObject a = assets.getJSONObject(j);
                    String name = a.optString("name", "");
                    String url = a.optString("browser_download_url", "");
                    if ("VHDCHY-LAN-Pilot-BETA.apk".equals(name)) apk = url;
                    else if ("VHDCHY-LAN-Pilot-BETA.apk.sha256".equals(name)) sha = url;
                }
            }
            return new Release(version, r.optString("html_url", "https://github.com/tamnv2/vanhanhdchungyen/releases"), apk, sha);
        }
        return null;
    }

    private String getText(String url, int connect, int read) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(url).openConnection();
        c.setConnectTimeout(connect); c.setReadTimeout(read); c.setUseCaches(false);
        c.setRequestProperty("Accept", "application/vnd.github+json");
        c.setRequestProperty("User-Agent", "VHDCHY-LAN-Pilot-Android/" + BuildConfig.VERSION_NAME);
        int status = c.getResponseCode();
        String text = read(c, status);
        c.disconnect();
        if (status < 200 || status >= 300) throw new Exception("HTTP " + status);
        return text;
    }

    private byte[] getBytes(String url, int connect, int read) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(url).openConnection();
        c.setConnectTimeout(connect); c.setReadTimeout(read); c.setUseCaches(false);
        c.setRequestProperty("User-Agent", "VHDCHY-LAN-Pilot-Android/" + BuildConfig.VERSION_NAME);
        int status = c.getResponseCode();
        if (status < 200 || status >= 300) { c.disconnect(); throw new Exception("HTTP " + status); }
        try (InputStream in = c.getInputStream(); java.io.ByteArrayOutputStream out = new java.io.ByteArrayOutputStream()) {
            byte[] buf = new byte[64 * 1024]; int n;
            while ((n = in.read(buf)) >= 0) out.write(buf, 0, n);
            return out.toByteArray();
        } finally { c.disconnect(); }
    }

    private String read(HttpURLConnection c, int status) throws Exception {
        InputStream in = status >= 200 && status < 400 ? c.getInputStream() : c.getErrorStream();
        if (in == null) return "";
        StringBuilder b = new StringBuilder();
        try (BufferedReader r = new BufferedReader(new InputStreamReader(in, StandardCharsets.UTF_8))) {
            String line; while ((line = r.readLine()) != null) b.append(line);
        }
        return b.toString();
    }

    private static String sha256(byte[] data) throws Exception {
        byte[] hash = MessageDigest.getInstance("SHA-256").digest(data);
        StringBuilder b = new StringBuilder();
        for (byte x : hash) b.append(String.format(Locale.US, "%02x", x));
        return b.toString();
    }

    private static String firstToken(String s) {
        if (s == null) return "";
        String[] p = s.trim().split("\\s+");
        return p.length == 0 ? "" : p[0];
    }

    private static boolean validVersion(String v) {
        return v != null && v.matches("\\d+\\.\\d+\\.\\d+");
    }

    private static boolean isNewer(String candidate, String current) {
        int[] a = parse(candidate), b = parse(current);
        for (int i = 0; i < 3; i++) if (a[i] != b[i]) return a[i] > b[i];
        return false;
    }

    private static int[] parse(String v) {
        int[] out = {0,0,0};
        try {
            String[] p = v.split("\\.");
            for (int i = 0; i < Math.min(3, p.length); i++) out[i] = Integer.parseInt(p[i].replaceAll("[^0-9].*$", ""));
        } catch (Exception ignored) { }
        return out;
    }

    private void ui(Runnable r) { activity.runOnUiThread(r); }
    private static String safe(String s) { return s == null ? "" : s; }

    void close() { io.shutdownNow(); }

    private static final class Release {
        final String version, htmlUrl, apkUrl, shaUrl;
        Release(String version, String htmlUrl, String apkUrl, String shaUrl) {
            this.version = version; this.htmlUrl = htmlUrl; this.apkUrl = apkUrl; this.shaUrl = shaUrl;
        }
    }
}
