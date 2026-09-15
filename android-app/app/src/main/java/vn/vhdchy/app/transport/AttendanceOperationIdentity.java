package vn.vhdchy.app.transport;

public final class AttendanceOperationIdentity {
    private final String requestId;
    private final String idempotencyKey;
    private final long deviceSeq;

    public AttendanceOperationIdentity(String requestId, String idempotencyKey, long deviceSeq) {
        this.requestId = requireText(requestId, 200, "ATTENDANCE_REQUEST_ID_INVALID");
        this.idempotencyKey = requireText(idempotencyKey, 240, "ATTENDANCE_IDEMPOTENCY_KEY_INVALID");
        if (deviceSeq < 1) throw new IllegalArgumentException("ATTENDANCE_DEVICE_SEQUENCE_INVALID");
        this.deviceSeq = deviceSeq;
    }

    public String requestId() { return requestId; }
    public String idempotencyKey() { return idempotencyKey; }
    public long deviceSeq() { return deviceSeq; }

    private static String requireText(String value, int max, String code) {
        if (value == null) throw new IllegalArgumentException(code);
        String normalized = value.strip();
        if (normalized.isEmpty() || normalized.length() > max) throw new IllegalArgumentException(code);
        for (int i = 0; i < normalized.length(); i++) {
            if (Character.isISOControl(normalized.charAt(i))) throw new IllegalArgumentException(code);
        }
        return normalized;
    }
}
