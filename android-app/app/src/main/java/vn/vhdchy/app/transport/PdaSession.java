package vn.vhdchy.app.transport;

public final class PdaSession {
    private String bearerToken;
    private long expiresAtEpochMs;
    private ServiceEndpointPolicy.RuntimeMode runtimeMode;

    public void establish(String token, long expiresAtEpochMs, ServiceEndpointPolicy.RuntimeMode runtimeMode) {
        validateEvidence(token, expiresAtEpochMs, runtimeMode);
        this.bearerToken = token;
        this.expiresAtEpochMs = expiresAtEpochMs;
        this.runtimeMode = runtimeMode;
    }

    public boolean isUsable(long nowEpochMs) {
        return bearerToken != null && runtimeMode != null && expiresAtEpochMs > nowEpochMs;
    }

    public String authorizationHeader(long nowEpochMs) {
        if (!isUsable(nowEpochMs)) throw new IllegalStateException("SESSION_NOT_USABLE");
        return "Bearer " + bearerToken;
    }

    public ServiceEndpointPolicy.RuntimeMode runtimeMode(long nowEpochMs) {
        if (!isUsable(nowEpochMs)) throw new IllegalStateException("SESSION_NOT_USABLE");
        return runtimeMode;
    }

    public Snapshot snapshot(long nowEpochMs) {
        if (!isUsable(nowEpochMs)) throw new IllegalStateException("SESSION_NOT_USABLE");
        return new Snapshot(bearerToken, expiresAtEpochMs, runtimeMode);
    }

    public boolean restore(Snapshot snapshot, long nowEpochMs) {
        clear();
        if (snapshot == null || snapshot.expiresAtEpochMs() <= nowEpochMs) return false;
        try {
            validateEvidence(snapshot.bearerToken(), snapshot.expiresAtEpochMs(), snapshot.runtimeMode());
        } catch (IllegalArgumentException ignored) {
            return false;
        }
        bearerToken = snapshot.bearerToken();
        expiresAtEpochMs = snapshot.expiresAtEpochMs();
        runtimeMode = snapshot.runtimeMode();
        return true;
    }

    public void clear() {
        bearerToken = null;
        expiresAtEpochMs = 0;
        runtimeMode = null;
    }

    private static void validateEvidence(
        String token,
        long expiresAtEpochMs,
        ServiceEndpointPolicy.RuntimeMode runtimeMode
    ) {
        if (token == null || token.length() < 32) throw new IllegalArgumentException("SESSION_TOKEN_INVALID");
        if (expiresAtEpochMs <= 0 || runtimeMode == null) throw new IllegalArgumentException("SESSION_EVIDENCE_INVALID");
    }

    public static final class Snapshot {
        private final String bearerToken;
        private final long expiresAtEpochMs;
        private final ServiceEndpointPolicy.RuntimeMode runtimeMode;

        public Snapshot(
            String bearerToken,
            long expiresAtEpochMs,
            ServiceEndpointPolicy.RuntimeMode runtimeMode
        ) {
            validateEvidence(bearerToken, expiresAtEpochMs, runtimeMode);
            this.bearerToken = bearerToken;
            this.expiresAtEpochMs = expiresAtEpochMs;
            this.runtimeMode = runtimeMode;
        }

        public String bearerToken() { return bearerToken; }
        public long expiresAtEpochMs() { return expiresAtEpochMs; }
        public ServiceEndpointPolicy.RuntimeMode runtimeMode() { return runtimeMode; }

        @Override
        public String toString() {
            return "PdaSession.Snapshot{bearerToken=[REDACTED], expiresAtEpochMs="
                + expiresAtEpochMs + ", runtimeMode=" + runtimeMode + "}";
        }
    }
}
