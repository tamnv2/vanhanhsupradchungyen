package vn.vhdchy.app.attendance;

public final class EmployeeCodeScannerPayload {
    public static final int MAX_LENGTH = 120;

    private EmployeeCodeScannerPayload() {}

    public static String normalize(String raw) {
        if (raw == null) throw new IllegalArgumentException("EMPLOYEE_CODE_SCAN_REQUIRED");
        String value = raw.strip();
        if (value.isEmpty() || value.length() > MAX_LENGTH) {
            throw new IllegalArgumentException("EMPLOYEE_CODE_SCAN_INVALID");
        }
        for (int i = 0; i < value.length(); i++) {
            if (Character.isISOControl(value.charAt(i))) {
                throw new IllegalArgumentException("EMPLOYEE_CODE_SCAN_INVALID");
            }
        }
        return value;
    }
}
