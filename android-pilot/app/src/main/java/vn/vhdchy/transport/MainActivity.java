package vn.vhdchy.transport;

import android.app.Activity;
import android.os.Bundle;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import java.util.Locale;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

public final class MainActivity extends Activity {
    private enum State { RECONNECTING, LAN_AVAILABLE, LAN_ACTIVE, LAN_LOST, LOCAL_QUEUE_ONLY }

    private final ScheduledExecutorService scheduler = Executors.newSingleThreadScheduledExecutor();
    private final ExecutorService io = Executors.newFixedThreadPool(2);
    private final AtomicBoolean cycleRunning = new AtomicBoolean(false);

    private TransportRepository repository;
    private String deviceId;
    private volatile State state = State.RECONNECTING;
    private volatile String endpoint = "";
    private volatile int successStreak = 0;
    private volatile int failureStreak = 0;
    private volatile boolean foreground = false;

    private TextView status;
    private TextView details;
    private TextView queue;

    @Override protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        repository = new TransportRepository(this);
        deviceId = repository.deviceId();
        setContentView(buildUi());
        render("Khởi tạo transport pilot");
        scheduler.scheduleWithFixedDelay(() -> {
            if (foreground) runCycle(false);
        }, 0, 5, TimeUnit.SECONDS);
    }

    @Override protected void onStart() {
        super.onStart();
        foreground = true;
        io.execute(() -> runCycle(true));
    }

    @Override protected void onStop() {
        foreground = false;
        super.onStop();
    }

    @Override protected void onDestroy() {
        scheduler.shutdownNow();
        io.shutdownNow();
        repository.close();
        super.onDestroy();
    }

    private ScrollView buildUi() {
        int pad = (int) (16 * getResources().getDisplayMetrics().density);
        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setPadding(pad, pad, pad, pad);

        TextView title = new TextView(this);
        title.setText("VHDCHY Transport BETA");
        title.setTextSize(24);
        root.addView(title);

        TextView warning = new TextView(this);
        warning.setText("Transport test only — chưa truyền credential/PII/business mutation qua LAN.");
        root.addView(warning);

        status = new TextView(this);
        status.setTextSize(19);
        root.addView(status);

        details = new TextView(this);
        root.addView(details);

        queue = new TextView(this);
        root.addView(queue);

        Button discover = new Button(this);
        discover.setText("Tìm LAN Agent");
        discover.setOnClickListener(v -> io.execute(() -> runCycle(true)));
        root.addView(discover);

        Button echo = new Button(this);
        echo.setText("Test Echo");
        echo.setOnClickListener(v -> io.execute(this::runEcho));
        root.addView(echo);

        Button enqueue = new Button(this);
        enqueue.setText("Tạo event test durable");
        enqueue.setOnClickListener(v -> io.execute(() -> {
            TransportRepository.Pending pending = repository.enqueueTest("transport-test-only");
            render("Đã enqueue test seq=" + pending.deviceSeq);
            flushPendingTests();
        }));
        root.addView(enqueue);

        Button flush = new Button(this);
        flush.setText("Gửi lại queue test");
        flush.setOnClickListener(v -> io.execute(this::flushPendingTests));
        root.addView(flush);

        ScrollView scroll = new ScrollView(this);
        scroll.addView(root);
        return scroll;
    }

    private void runCycle(boolean forced) {
        if (!cycleRunning.compareAndSet(false, true)) return;
        try {
            LanTransportClient.Health health = LanTransportClient.discover(repository);
            if (health != null) {
                endpoint = health.endpoint;
                failureStreak = 0;
                successStreak++;
                if (successStreak >= 2 || state == State.LAN_ACTIVE) {
                    state = State.LAN_ACTIVE;
                    flushPendingTests();
                    render("LAN_ACTIVE qua " + health.source);
                } else {
                    state = State.LAN_AVAILABLE;
                    render("LAN_AVAILABLE — cần thêm 1 health sample");
                    scheduler.schedule(() -> runCycle(false), 800, TimeUnit.MILLISECONDS);
                }
            } else {
                successStreak = 0;
                failureStreak++;
                if (failureStreak < 2 && (state == State.LAN_ACTIVE || state == State.LAN_AVAILABLE)) {
                    state = State.LAN_LOST;
                    render("LAN_LOST — giữ hysteresis, chưa bỏ Agent sau 1 lỗi");
                } else {
                    endpoint = "";
                    state = State.LOCAL_QUEUE_ONLY;
                    render("LOCAL_QUEUE_ONLY — chưa thấy Agent hợp lệ");
                }
            }
            if (forced) render("Chu kỳ tìm LAN hoàn tất");
        } finally {
            cycleRunning.set(false);
        }
    }

    private void runEcho() {
        if (state != State.LAN_ACTIVE || endpoint.isEmpty()) {
            render("Echo bỏ qua: chưa LAN_ACTIVE");
            return;
        }
        try {
            double ms = LanTransportClient.echo(endpoint, deviceId);
            render(String.format(Locale.US, "Echo PASS %.1f ms", ms));
        } catch (Exception e) {
            render("Echo FAIL: " + safe(e.getMessage()));
        }
    }

    private void flushPendingTests() {
        if (state != State.LAN_ACTIVE || endpoint.isEmpty()) {
            render("Queue giữ local: chưa LAN_ACTIVE");
            return;
        }
        int before = repository.countPendingTests();
        for (TransportRepository.Pending pending : repository.pendingTests(100)) {
            try {
                LanTransportClient.Ack ack = LanTransportClient.sendTestEvent(endpoint, deviceId, pending);
                if (!"TEST_ACCEPTED_AGENT_ONLY".equals(ack.ackType)) {
                    render("ACK không hợp lệ cho transport-test: " + ack.ackType);
                    break;
                }
                repository.ackTest(pending.idempotencyKey);
            } catch (Exception e) {
                render("Queue retry later: " + safe(e.getMessage()));
                break;
            }
        }
        int after = repository.countPendingTests();
        if (before != after) render("Queue test " + before + " → " + after);
        else render("Queue test pending=" + after);
    }

    private void render(String note) {
        runOnUiThread(() -> {
            status.setText("Trạng thái: " + state);
            details.setText("App " + BuildConfig.VERSION_NAME + "\nDevice: " + shortId(deviceId) + "\nEndpoint: " + (endpoint.isEmpty() ? "—" : endpoint) + "\nSuccess/Failure streak: " + successStreak + "/" + failureStreak + "\n" + note);
            queue.setText("Pending transport-test: " + repository.countPendingTests());
        });
    }

    private static String shortId(String value) {
        if (value == null) return "—";
        return value.length() <= 16 ? value : value.substring(0, 16) + "…";
    }

    private static String safe(String value) {
        return value == null ? "" : value;
    }
}
