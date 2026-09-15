package vn.vhdchy.app.transport;

import java.net.URI;
import java.util.Map;

public final class TransportBehaviorHarness {
    private static int checks = 0;

    private TransportBehaviorHarness() {}

    public static void main(String[] args) {
        endpointPolicyContract();
        sessionContract();
        sessionPersistenceContract();
        retryContract();
        scannerContract();
        authenticatedRequestContract();
        System.out.println("ANDROID_TRANSPORT_BEHAVIOR_PASS checks=" + checks + " https=PASS session=PASS sessionRestore=PASS retry=PASS scanner=PASS requestBinding=PASS");
    }

    private static void endpointPolicyContract() {
        ServiceEndpointPolicy endpoints = new ServiceEndpointPolicy(
            "https://cloud.example.test/",
            "https://lan.example.test"
        );

        equal(URI.create("https://cloud.example.test"), endpoints.select(ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT), "cloud base");
        equal(URI.create("https://lan.example.test"), endpoints.select(ServiceEndpointPolicy.RuntimeMode.LAN_PRIMARY), "lan base");
        equal(URI.create("https://cloud.example.test/api/v1/auth/me"), endpoints.resolve(ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT, "/api/v1/auth/me"), "cloud api resolution");
        equal(URI.create("https://lan.example.test/api/v1/employees"), endpoints.resolve(ServiceEndpointPolicy.RuntimeMode.LAN_PRIMARY, "/api/v1/employees"), "lan api resolution");

        throwsCode(IllegalArgumentException.class, "CLOUD_ENDPOINT_INVALID", () -> new ServiceEndpointPolicy("http://cloud.example.test", "https://lan.example.test"), "http cloud rejected");
        throwsCode(IllegalArgumentException.class, "CLOUD_ENDPOINT_INVALID", () -> new ServiceEndpointPolicy("https://cloud.example.test/base", "https://lan.example.test"), "base path rejected");
        throwsCode(IllegalArgumentException.class, "CLOUD_ENDPOINT_INVALID", () -> new ServiceEndpointPolicy("https://user@cloud.example.test", "https://lan.example.test"), "userinfo rejected");
        throwsCode(IllegalArgumentException.class, "LAN_ENDPOINT_INVALID", () -> new ServiceEndpointPolicy("https://cloud.example.test", "https://lan.example.test?x=1"), "query rejected");
        throwsCode(IllegalArgumentException.class, "RUNTIME_MODE_REQUIRED", () -> endpoints.select(null), "runtime required");
        throwsCode(IllegalArgumentException.class, "API_PATH_INVALID", () -> endpoints.resolve(ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT, "/health"), "non api path rejected");
        throwsCode(IllegalArgumentException.class, "API_PATH_INVALID", () -> endpoints.resolve(ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT, "/api/v1/../health"), "path traversal rejected");
    }

    private static void sessionContract() {
        PdaSession session = new PdaSession();
        String token = "t".repeat(32);

        check(!session.isUsable(1_000), "empty session unusable");
        throwsCode(IllegalArgumentException.class, "SESSION_TOKEN_INVALID", () -> session.establish("short", 2_000, ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT), "short token rejected");
        throwsCode(IllegalArgumentException.class, "SESSION_EVIDENCE_INVALID", () -> session.establish(token, 0, ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT), "expiry evidence required");
        throwsCode(IllegalArgumentException.class, "SESSION_EVIDENCE_INVALID", () -> session.establish(token, 2_000, null), "runtime evidence required");

        session.establish(token, 2_000, ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT);
        check(session.isUsable(1_999), "session usable before expiry");
        equal("Bearer " + token, session.authorizationHeader(1_999), "authorization header");
        equal(ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT, session.runtimeMode(1_999), "runtime retained");
        check(!session.isUsable(2_000), "session expires exactly at boundary");
        throwsCode(IllegalStateException.class, "SESSION_NOT_USABLE", () -> session.authorizationHeader(2_000), "expired auth blocked");

        session.establish(token, 3_000, ServiceEndpointPolicy.RuntimeMode.LAN_PRIMARY);
        session.clear();
        check(!session.isUsable(2_500), "clear invalidates session");
        throwsCode(IllegalStateException.class, "SESSION_NOT_USABLE", () -> session.runtimeMode(2_500), "cleared runtime blocked");
    }

    private static void sessionPersistenceContract() {
        String token = "p".repeat(48);
        PdaSession source = new PdaSession();
        source.establish(token, 5_000, ServiceEndpointPolicy.RuntimeMode.LAN_PRIMARY);

        PdaSession.Snapshot snapshot = source.snapshot(4_000);
        equal(5_000L, snapshot.expiresAtEpochMs(), "snapshot retains expiry");
        equal(ServiceEndpointPolicy.RuntimeMode.LAN_PRIMARY, snapshot.runtimeMode(), "snapshot retains runtime mode");
        check(!snapshot.toString().contains(token), "snapshot string redacts bearer token");
        check(snapshot.toString().contains("[REDACTED]"), "snapshot string marks redaction");

        PdaSession restored = new PdaSession();
        check(restored.restore(snapshot, 4_999), "snapshot restores before expiry");
        equal("Bearer " + token, restored.authorizationHeader(4_999), "restored bearer retained");
        equal(ServiceEndpointPolicy.RuntimeMode.LAN_PRIMARY, restored.runtimeMode(4_999), "restore preserves original runtime mode");

        PdaSession exactBoundary = new PdaSession();
        exactBoundary.establish("x".repeat(32), 9_000, ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT);
        check(!exactBoundary.restore(snapshot, 5_000), "restore rejects exact expiry boundary");
        check(!exactBoundary.isUsable(4_000), "failed restore clears prior in-memory session");

        PdaSession nullSnapshot = new PdaSession();
        nullSnapshot.establish("y".repeat(32), 9_000, ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT);
        check(!nullSnapshot.restore(null, 4_000), "null snapshot rejected");
        check(!nullSnapshot.isUsable(4_000), "null restore clears prior in-memory session");

        throwsCode(IllegalStateException.class, "SESSION_NOT_USABLE", () -> source.snapshot(5_000), "expired session cannot be snapshotted");
        throwsCode(IllegalArgumentException.class, "SESSION_TOKEN_INVALID", () -> new PdaSession.Snapshot("short", 6_000, ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT), "persisted short token rejected");
        throwsCode(IllegalArgumentException.class, "SESSION_EVIDENCE_INVALID", () -> new PdaSession.Snapshot(token, 6_000, null), "persisted runtime required");
    }

    private static void retryContract() {
        PdaRetryPolicy retry = new PdaRetryPolicy();

        check(!retry.mayRetry(1, false, null), "non-idempotent network failure not retried");
        check(!retry.mayRetry(0, true, null), "attempt zero invalid for retry");
        check(retry.mayRetry(1, true, null), "idempotent network failure retried");
        check(retry.mayRetry(1, true, 408), "408 retried");
        check(retry.mayRetry(2, true, 429), "429 retried");
        check(retry.mayRetry(4, true, 503), "5xx retried within bound");
        check(!retry.mayRetry(1, true, 400), "400 not retried");
        check(!retry.mayRetry(1, true, 401), "401 not retried");
        check(!retry.mayRetry(1, true, 409), "409 conflict not blindly retried");
        check(!retry.mayRetry(PdaRetryPolicy.MAX_ATTEMPTS, true, null), "max attempts bounded");
        equal(250L, retry.delayMs(1), "retry delay 1");
        equal(500L, retry.delayMs(2), "retry delay 2");
        equal(1_000L, retry.delayMs(3), "retry delay 3");
        equal(2_000L, retry.delayMs(4), "retry delay 4");
        equal(4_000L, retry.delayMs(5), "retry delay capped");
        throwsCode(IllegalArgumentException.class, "ATTEMPT_INVALID", () -> retry.delayMs(0), "invalid delay attempt rejected");
    }

    private static void scannerContract() {
        equal("MNV001", ScannerPayload.normalize("  MNV001 \r\n"), "scanner edge whitespace normalized");
        equal("DO-12345", ScannerPayload.normalize("DO-12345"), "scanner payload preserved");
        throwsCode(IllegalArgumentException.class, "SCAN_INPUT_REQUIRED", () -> ScannerPayload.normalize(null), "null scan rejected");
        throwsCode(IllegalArgumentException.class, "SCAN_INPUT_INVALID", () -> ScannerPayload.normalize("   \r\n"), "blank scan rejected");
        throwsCode(IllegalArgumentException.class, "SCAN_INPUT_INVALID", () -> ScannerPayload.normalize("A\nB"), "internal control rejected");
        throwsCode(IllegalArgumentException.class, "SCAN_INPUT_INVALID", () -> ScannerPayload.normalize("X".repeat(257)), "oversize scan rejected");
    }

    private static void authenticatedRequestContract() {
        ServiceEndpointPolicy endpoints = new ServiceEndpointPolicy(
            "https://cloud.example.test",
            "https://lan.example.test"
        );
        PdaSession session = new PdaSession();
        String token = "s".repeat(40);

        session.establish(token, 10_000, ServiceEndpointPolicy.RuntimeMode.CLOUD_DIRECT);
        PdaRequestPlan cloud = PdaRequestPlan.authenticated(endpoints, session, "/api/v1/auth/me", 9_000);
        equal(URI.create("https://cloud.example.test/api/v1/auth/me"), cloud.uri(), "request binds cloud runtime");
        equal("Bearer " + token, cloud.headers().get("Authorization"), "request carries bearer");
        equal("application/json", cloud.headers().get("Accept"), "request accept header");
        equal("application/json; charset=utf-8", cloud.headers().get("Content-Type"), "request content type");
        throwsType(UnsupportedOperationException.class, () -> cloud.headers().put("X-Test", "bad"), "headers immutable");

        session.establish(token, 10_000, ServiceEndpointPolicy.RuntimeMode.LAN_PRIMARY);
        PdaRequestPlan lan = PdaRequestPlan.authenticated(endpoints, session, "/api/v1/attendance/presence", 9_000);
        equal(URI.create("https://lan.example.test/api/v1/attendance/presence"), lan.uri(), "request binds lan runtime");
        throwsCode(IllegalStateException.class, "SESSION_NOT_USABLE", () -> PdaRequestPlan.authenticated(endpoints, session, "/api/v1/auth/me", 10_000), "expired request blocked");
        throwsCode(IllegalArgumentException.class, "REQUEST_CONTEXT_REQUIRED", () -> PdaRequestPlan.authenticated(null, session, "/api/v1/auth/me", 9_000), "endpoint context required");
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

    private static <T extends Throwable> void throwsType(Class<T> type, ThrowingRunnable action, String label) {
        checks++;
        try {
            action.run();
        } catch (Throwable error) {
            if (type.isInstance(error)) return;
            throw new AssertionError(label + ": wrong exception " + error, error);
        }
        throw new AssertionError(label + ": expected " + type.getSimpleName());
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
