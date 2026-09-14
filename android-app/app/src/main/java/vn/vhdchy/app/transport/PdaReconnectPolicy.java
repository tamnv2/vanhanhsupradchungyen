package vn.vhdchy.app.transport;

/**
 * Reconnect decisions inside the runtime already selected by the Service/client state.
 * This policy deliberately never invents or auto-switches Cloud/LAN authority.
 */
public final class PdaReconnectPolicy {
    public enum Action {
        RETRY_SAME_RUNTIME,
        WAIT_FOR_NETWORK,
        REAUTHENTICATE,
        STOP
    }

    public record Decision(Action action, long delayMs, String reason) {
        public Decision {
            if (action == null) throw new IllegalArgumentException("RECONNECT_ACTION_REQUIRED");
            if (delayMs < 0) throw new IllegalArgumentException("RECONNECT_DELAY_INVALID");
            if (reason == null || reason.isBlank()) throw new IllegalArgumentException("RECONNECT_REASON_REQUIRED");
        }
    }

    private final PdaRetryPolicy retryPolicy;

    public PdaReconnectPolicy() {
        this(new PdaRetryPolicy());
    }

    PdaReconnectPolicy(PdaRetryPolicy retryPolicy) {
        if (retryPolicy == null) throw new IllegalArgumentException("RETRY_POLICY_REQUIRED");
        this.retryPolicy = retryPolicy;
    }

    public Decision decide(int attempt, boolean networkAvailable, boolean idempotent, Integer httpStatus) {
        if (!networkAvailable) {
            return new Decision(Action.WAIT_FOR_NETWORK, 0, "NETWORK_UNAVAILABLE");
        }
        if (httpStatus != null && httpStatus == 401) {
            return new Decision(Action.REAUTHENTICATE, 0, "SESSION_REJECTED");
        }
        if (retryPolicy.mayRetry(attempt, idempotent, httpStatus)) {
            return new Decision(Action.RETRY_SAME_RUNTIME, retryPolicy.delayMs(attempt), "TRANSIENT_FAILURE");
        }
        return new Decision(Action.STOP, 0, "NON_RETRYABLE_FAILURE");
    }
}
