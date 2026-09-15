package vn.vhdchy.app.transport;

public final class AttendanceScanContext {
    private static final int MAX_CODE_LENGTH = 120;
    private static final int MAX_ID_LENGTH = 240;

    private final String employeeCodeId;
    private final String employeeCode;
    private final String employeeId;
    private final String fullName;
    private final String currentPortraitMediaId;
    private final Presence presence;

    public AttendanceScanContext(
        String employeeCodeId,
        String employeeCode,
        String employeeId,
        String fullName,
        String currentPortraitMediaId,
        Presence presence
    ) {
        this.employeeCodeId = requireText(employeeCodeId, MAX_ID_LENGTH, "SCAN_CONTEXT_CODE_ID_INVALID");
        this.employeeCode = requireText(employeeCode, MAX_CODE_LENGTH, "SCAN_CONTEXT_EMPLOYEE_CODE_INVALID");
        this.employeeId = requireText(employeeId, MAX_ID_LENGTH, "SCAN_CONTEXT_EMPLOYEE_ID_INVALID");
        this.fullName = requireText(fullName, MAX_ID_LENGTH, "SCAN_CONTEXT_FULL_NAME_INVALID");
        this.currentPortraitMediaId = optionalText(currentPortraitMediaId, MAX_ID_LENGTH, "SCAN_CONTEXT_PORTRAIT_INVALID");
        this.presence = presence;
    }

    public String employeeCodeId() { return employeeCodeId; }
    public String employeeCode() { return employeeCode; }
    public String employeeId() { return employeeId; }
    public String fullName() { return fullName; }
    public String currentPortraitMediaId() { return currentPortraitMediaId; }
    public Presence presence() { return presence; }

    private static String requireText(String value, int max, String code) {
        if (value == null) throw new IllegalArgumentException(code);
        String normalized = value.strip();
        if (normalized.isEmpty() || normalized.length() > max) throw new IllegalArgumentException(code);
        for (int i = 0; i < normalized.length(); i++) {
            if (Character.isISOControl(normalized.charAt(i))) throw new IllegalArgumentException(code);
        }
        return normalized;
    }

    private static String optionalText(String value, int max, String code) {
        if (value == null) return null;
        return requireText(value, max, code);
    }

    public static final class Presence {
        public enum State { IN, OUT }

        private final State currentState;
        private final String businessDate;
        private final long entityVersion;

        public Presence(State currentState, String businessDate, long entityVersion) {
            if (currentState == null) throw new IllegalArgumentException("SCAN_CONTEXT_PRESENCE_STATE_INVALID");
            this.currentState = currentState;
            this.businessDate = requireText(businessDate, 32, "SCAN_CONTEXT_BUSINESS_DATE_INVALID");
            if (entityVersion < 1) throw new IllegalArgumentException("SCAN_CONTEXT_PRESENCE_VERSION_INVALID");
            this.entityVersion = entityVersion;
        }

        public State currentState() { return currentState; }
        public String businessDate() { return businessDate; }
        public long entityVersion() { return entityVersion; }
    }
}
