package vn.vhdchy.app.transport;

public final class PdaSession {
    private String bearerToken;
    private long expiresAtEpochMs;
    private ServiceEndpointPolicy.RuntimeMode runtimeMode;

    public void establish(String token, long expiresAtEpochMs, ServiceEndpointPolicy.RuntimeMode runtimeMode) {
        if (token == null || token.length() < 32) throw new IllegalArgumentException("SESSION_TOKEN_INVALID");
        if (expiresAtEpochMs <= 0 || runtimeMode == null) throw new IllegalArgumentException("SESSION_EVIDENCE_INVALID");
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

    public void clear() {
        bearerToken = null;
        expiresAtEpochMs = 0;
        runtimeMode = null;
    }
}
