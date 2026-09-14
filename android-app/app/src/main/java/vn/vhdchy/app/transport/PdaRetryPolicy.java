package vn.vhdchy.app.transport;

public final class PdaRetryPolicy {
    public static final int MAX_ATTEMPTS = 5;
    private static final long BASE_DELAY_MS = 250;
    private static final long MAX_DELAY_MS = 4000;

    public boolean mayRetry(int completedAttempts, boolean idempotentOperation, Integer httpStatus) {
        if (!idempotentOperation || completedAttempts < 1 || completedAttempts >= MAX_ATTEMPTS) return false;
        if (httpStatus == null) return true;
        return httpStatus == 408 || httpStatus == 429 || httpStatus >= 500;
    }

    public long delayMs(int completedAttempts) {
        if (completedAttempts < 1) throw new IllegalArgumentException("ATTEMPT_INVALID");
        long multiplier = 1L << Math.min(completedAttempts - 1, 8);
        return Math.min(MAX_DELAY_MS, BASE_DELAY_MS * multiplier);
    }
}
