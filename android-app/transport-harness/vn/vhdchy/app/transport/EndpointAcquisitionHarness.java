package vn.vhdchy.app.transport;

import java.net.URI;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

public final class EndpointAcquisitionHarness {
    private static int checks = 0;

    private EndpointAcquisitionHarness() {}

    public static void main(String[] args) {
        cachedHealthyWinsWithoutDiscoveryFlap();
        unhealthyCacheFallsThroughToDiscovery();
        invalidDiscoveryIsIgnored();
        manualRecoveryIsLast();
        invalidManualEndpointFailsClosed();
        probeFailureIsUnhealthy();
        noHealthyEndpointReturnsEmpty();

        System.out.println(
            "ANDROID_ENDPOINT_ACQUISITION_PASS checks=" + checks
                + " cacheFirst=PASS discoverySecond=PASS manualLast=PASS httpsOnly=PASS healthProbe=PASS"
        );
    }

    private static void cachedHealthyWinsWithoutDiscoveryFlap() {
        LanEndpointAcquisitionPolicy policy = new LanEndpointAcquisitionPolicy();
        List<URI> probes = new ArrayList<>();

        Optional<LanEndpointAcquisitionPolicy.Selection> selected = policy.acquire(
            "https://cache.lan.local/",
            List.of("https://discovered.lan.local"),
            "https://manual.lan.local",
            endpoint -> {
                probes.add(endpoint);
                return true;
            }
        );

        check(selected.isPresent(), "cached selection present");
        equal(URI.create("https://cache.lan.local"), selected.orElseThrow().endpoint(), "cached endpoint normalized");
        equal(LanEndpointAcquisitionPolicy.Source.CACHE, selected.orElseThrow().source(), "cache source retained");
        check(!selected.orElseThrow().shouldCache(), "already cached endpoint does not request cache rewrite");
        equal(List.of(URI.create("https://cache.lan.local")), probes, "healthy cache prevents discovery flapping");
    }

    private static void unhealthyCacheFallsThroughToDiscovery() {
        LanEndpointAcquisitionPolicy policy = new LanEndpointAcquisitionPolicy();
        List<URI> probes = new ArrayList<>();

        Optional<LanEndpointAcquisitionPolicy.Selection> selected = policy.acquire(
            "https://stale.lan.local",
            List.of("https://first.lan.local", "https://healthy.lan.local", "https://later.lan.local"),
            "https://manual.lan.local",
            endpoint -> {
                probes.add(endpoint);
                return "healthy.lan.local".equals(endpoint.getHost());
            }
        );

        check(selected.isPresent(), "discovered selection present");
        equal(URI.create("https://healthy.lan.local"), selected.orElseThrow().endpoint(), "first healthy discovery selected");
        equal(LanEndpointAcquisitionPolicy.Source.DISCOVERY, selected.orElseThrow().source(), "discovery source retained");
        check(selected.orElseThrow().shouldCache(), "healthy discovery should replace stale cache");
        equal(
            List.of(
                URI.create("https://stale.lan.local"),
                URI.create("https://first.lan.local"),
                URI.create("https://healthy.lan.local")
            ),
            probes,
            "manual and later discovery are not probed after healthy discovery"
        );
    }

    private static void invalidDiscoveryIsIgnored() {
        LanEndpointAcquisitionPolicy policy = new LanEndpointAcquisitionPolicy();
        List<URI> probes = new ArrayList<>();

        Optional<LanEndpointAcquisitionPolicy.Selection> selected = policy.acquire(
            "http://cached-not-trusted.local",
            List.of(
                "http://discovered-insecure.local",
                "https://discovered-path.local/base",
                "https://valid.lan.local"
            ),
            null,
            endpoint -> {
                probes.add(endpoint);
                return true;
            }
        );

        check(selected.isPresent(), "valid discovery survives invalid network candidates");
        equal(URI.create("https://valid.lan.local"), selected.orElseThrow().endpoint(), "only valid HTTPS discovery probed");
        equal(List.of(URI.create("https://valid.lan.local")), probes, "invalid cache/discovery never reaches health probe");
    }

    private static void manualRecoveryIsLast() {
        LanEndpointAcquisitionPolicy policy = new LanEndpointAcquisitionPolicy();
        List<URI> probes = new ArrayList<>();

        Optional<LanEndpointAcquisitionPolicy.Selection> selected = policy.acquire(
            "https://cache.lan.local",
            List.of("https://discovered.lan.local"),
            "https://manual.lan.local/",
            endpoint -> {
                probes.add(endpoint);
                return "manual.lan.local".equals(endpoint.getHost());
            }
        );

        check(selected.isPresent(), "manual selection present");
        equal(URI.create("https://manual.lan.local"), selected.orElseThrow().endpoint(), "manual endpoint normalized");
        equal(LanEndpointAcquisitionPolicy.Source.MANUAL, selected.orElseThrow().source(), "manual source retained");
        check(selected.orElseThrow().shouldCache(), "healthy manual endpoint should be cached");
        equal(
            List.of(
                URI.create("https://cache.lan.local"),
                URI.create("https://discovered.lan.local"),
                URI.create("https://manual.lan.local")
            ),
            probes,
            "manual probe occurs only after cache and discovery fail"
        );
    }

    private static void invalidManualEndpointFailsClosed() {
        LanEndpointAcquisitionPolicy policy = new LanEndpointAcquisitionPolicy();
        throwsCode(
            IllegalArgumentException.class,
            "MANUAL_ENDPOINT_INVALID",
            () -> policy.acquire(null, List.of(), "http://manual.lan.local", endpoint -> true),
            "manual HTTP rejected"
        );
        throwsCode(
            IllegalArgumentException.class,
            "MANUAL_ENDPOINT_INVALID",
            () -> policy.acquire(null, List.of(), "https://manual.lan.local/base", endpoint -> true),
            "manual base path rejected"
        );
    }

    private static void probeFailureIsUnhealthy() {
        LanEndpointAcquisitionPolicy policy = new LanEndpointAcquisitionPolicy();
        Optional<LanEndpointAcquisitionPolicy.Selection> selected = policy.acquire(
            "https://cache.lan.local",
            List.of("https://discovered.lan.local"),
            null,
            endpoint -> {
                if ("cache.lan.local".equals(endpoint.getHost())) throw new IllegalStateException("network down");
                return true;
            }
        );

        check(selected.isPresent(), "probe exception does not abort discovery");
        equal(URI.create("https://discovered.lan.local"), selected.orElseThrow().endpoint(), "discovery follows probe failure");
    }

    private static void noHealthyEndpointReturnsEmpty() {
        LanEndpointAcquisitionPolicy policy = new LanEndpointAcquisitionPolicy();
        Optional<LanEndpointAcquisitionPolicy.Selection> selected = policy.acquire(
            "https://cache.lan.local",
            List.of("https://discovered.lan.local"),
            "https://manual.lan.local",
            endpoint -> false
        );
        check(selected.isEmpty(), "no endpoint is fabricated when every health probe fails");
        throwsCode(
            IllegalArgumentException.class,
            "LAN_HEALTH_PROBE_REQUIRED",
            () -> policy.acquire(null, null, null, null),
            "health probe required"
        );
    }

    private static void check(boolean condition, String label) {
        checks++;
        if (!condition) throw new AssertionError(label);
    }

    private static void equal(Object expected, Object actual, String label) {
        checks++;
        if (!expected.equals(actual)) {
            throw new AssertionError(label + ": expected=" + expected + " actual=" + actual);
        }
    }

    private static <T extends Throwable> void throwsCode(Class<T> type, String code, ThrowingRunnable action, String label) {
        checks++;
        try {
            action.run();
        } catch (Throwable error) {
            if (!type.isInstance(error)) {
                throw new AssertionError(label + ": wrong exception " + error, error);
            }
            if (!code.equals(error.getMessage())) {
                throw new AssertionError(label + ": expected code=" + code + " actual=" + error.getMessage(), error);
            }
            return;
        }
        throw new AssertionError(label + ": expected " + type.getSimpleName() + "(" + code + ")");
    }

    @FunctionalInterface
    private interface ThrowingRunnable {
        void run() throws Exception;
    }
}
