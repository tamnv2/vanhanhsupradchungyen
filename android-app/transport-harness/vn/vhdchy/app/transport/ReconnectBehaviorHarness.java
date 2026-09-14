package vn.vhdchy.app.transport;

public final class ReconnectBehaviorHarness {
    private static int checks = 0;

    private ReconnectBehaviorHarness() {}

    public static void main(String[] args) {
        PdaReconnectPolicy policy = new PdaReconnectPolicy();

        decision(policy.decide(1, false, true, null), PdaReconnectPolicy.Action.WAIT_FOR_NETWORK, 0, "offline waits without fabricating runtime");
        decision(policy.decide(1, true, true, null), PdaReconnectPolicy.Action.RETRY_SAME_RUNTIME, 250, "idempotent network failure retries");
        decision(policy.decide(2, true, true, 503), PdaReconnectPolicy.Action.RETRY_SAME_RUNTIME, 500, "transient 5xx retries");
        decision(policy.decide(1, true, true, 401), PdaReconnectPolicy.Action.REAUTHENTICATE, 0, "401 requires reauthentication");
        decision(policy.decide(1, true, true, 403), PdaReconnectPolicy.Action.STOP, 0, "permission failure is not transport retry");
        decision(policy.decide(1, true, false, null), PdaReconnectPolicy.Action.STOP, 0, "non-idempotent network failure is not blindly retried");
        decision(policy.decide(PdaRetryPolicy.MAX_ATTEMPTS, true, true, 503), PdaReconnectPolicy.Action.STOP, 0, "retry bound is enforced");

        System.out.println("ANDROID_RECONNECT_BEHAVIOR_PASS checks=" + checks + " sameRuntime=PASS offlineWait=PASS reauth=PASS boundedRetry=PASS");
    }

    private static void decision(PdaReconnectPolicy.Decision actual, PdaReconnectPolicy.Action action, long delayMs, String label) {
        checks++;
        if (actual.action() != action || actual.delayMs() != delayMs) {
            throw new AssertionError(label + ": action=" + actual.action() + " delay=" + actual.delayMs());
        }
    }
}
