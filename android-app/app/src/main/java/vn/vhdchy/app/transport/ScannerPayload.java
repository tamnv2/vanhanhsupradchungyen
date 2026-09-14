package vn.vhdchy.app.transport;

public final class ScannerPayload {
    private static final int MAX_LENGTH = 256;

    private ScannerPayload() {}

    public static String normalize(String raw) {
        if (raw == null) throw new IllegalArgumentException("SCAN_INPUT_REQUIRED");
        String value = raw.strip();
        if (value.isEmpty() || value.length() > MAX_LENGTH) throw new IllegalArgumentException("SCAN_INPUT_INVALID");
        for (int i = 0; i < value.length(); i++) {
            char c = value.charAt(i);
            if (Character.isISOControl(c)) throw new IllegalArgumentException("SCAN_INPUT_INVALID");
        }
        return value;
    }
}
