package vn.vhdchy.app.attendance;

public final class AttendanceScreenState {
    public enum Kind {
        WAITING_SCAN,
        RESOLVED,
        SUBMITTING,
        COMMITTED,
        ERROR
    }

    private final Kind kind;
    private final String employeeCode;
    private final String fullName;
    private final String portraitMediaId;
    private final String presenceState;
    private final String businessDate;
    private final Long entityVersion;
    private final String statusText;

    private AttendanceScreenState(
        Kind kind,
        String employeeCode,
        String fullName,
        String portraitMediaId,
        String presenceState,
        String businessDate,
        Long entityVersion,
        String statusText
    ) {
        this.kind = kind;
        this.employeeCode = employeeCode;
        this.fullName = fullName;
        this.portraitMediaId = portraitMediaId;
        this.presenceState = presenceState;
        this.businessDate = businessDate;
        this.entityVersion = entityVersion;
        this.statusText = requireStatus(statusText);
    }

    public static AttendanceScreenState waitingScan() {
        return new AttendanceScreenState(
            Kind.WAITING_SCAN,
            null,
            null,
            null,
            null,
            null,
            null,
            "Quét hoặc nhập mã nhân viên để xác minh trước khi chấm công."
        );
    }

    public static AttendanceScreenState resolved(
        String employeeCode,
        String fullName,
        String portraitMediaId,
        String presenceState,
        String businessDate,
        Long entityVersion
    ) {
        String code = requireText(employeeCode, 120, "ATTENDANCE_UI_EMPLOYEE_CODE_INVALID");
        String name = requireText(fullName, 240, "ATTENDANCE_UI_FULL_NAME_INVALID");
        String state = normalizePresence(presenceState);
        if (state == null) {
            if (businessDate != null || entityVersion != null) {
                throw new IllegalArgumentException("ATTENDANCE_UI_PRESENCE_INVALID");
            }
        } else {
            requireText(businessDate, 32, "ATTENDANCE_UI_BUSINESS_DATE_INVALID");
            if (entityVersion == null || entityVersion < 1) {
                throw new IllegalArgumentException("ATTENDANCE_UI_PRESENCE_INVALID");
            }
        }
        String portrait = portraitMediaId == null
            ? null
            : requireText(portraitMediaId, 240, "ATTENDANCE_UI_PORTRAIT_INVALID");
        return new AttendanceScreenState(
            Kind.RESOLVED,
            code,
            name,
            portrait,
            state,
            businessDate,
            entityVersion,
            "Đã xác minh nhân viên. Chọn thao tác chấm công."
        );
    }

    public static AttendanceScreenState submitting(AttendanceScreenState resolved, String actionLabel) {
        if (resolved == null || resolved.kind != Kind.RESOLVED) {
            throw new IllegalArgumentException("ATTENDANCE_UI_RESOLVED_STATE_REQUIRED");
        }
        String action = requireText(actionLabel, 40, "ATTENDANCE_UI_ACTION_INVALID");
        return new AttendanceScreenState(
            Kind.SUBMITTING,
            resolved.employeeCode,
            resolved.fullName,
            resolved.portraitMediaId,
            resolved.presenceState,
            resolved.businessDate,
            resolved.entityVersion,
            "Đang gửi thao tác " + action + "…"
        );
    }

    public static AttendanceScreenState committed(
        AttendanceScreenState prior,
        String commitStatus
    ) {
        if (prior == null || (prior.kind != Kind.SUBMITTING && prior.kind != Kind.RESOLVED)) {
            throw new IllegalArgumentException("ATTENDANCE_UI_PRIOR_STATE_REQUIRED");
        }
        String status = requireText(commitStatus, 80, "ATTENDANCE_UI_COMMIT_STATUS_INVALID");
        return new AttendanceScreenState(
            Kind.COMMITTED,
            prior.employeeCode,
            prior.fullName,
            prior.portraitMediaId,
            prior.presenceState,
            prior.businessDate,
            prior.entityVersion,
            "Đã ghi nhận thao tác. Trạng thái: " + status
        );
    }

    public static AttendanceScreenState error(String message) {
        return new AttendanceScreenState(
            Kind.ERROR,
            null,
            null,
            null,
            null,
            null,
            null,
            requireText(message, 500, "ATTENDANCE_UI_ERROR_INVALID")
        );
    }

    public Kind kind() { return kind; }
    public String employeeCode() { return employeeCode; }
    public String fullName() { return fullName; }
    public String portraitMediaId() { return portraitMediaId; }
    public String presenceState() { return presenceState; }
    public String businessDate() { return businessDate; }
    public Long entityVersion() { return entityVersion; }
    public String statusText() { return statusText; }
    public boolean actionsEnabled() { return kind == Kind.RESOLVED; }

    private static String normalizePresence(String value) {
        if (value == null) return null;
        String normalized = requireText(value, 8, "ATTENDANCE_UI_PRESENCE_INVALID").toUpperCase();
        if (!normalized.equals("IN") && !normalized.equals("OUT")) {
            throw new IllegalArgumentException("ATTENDANCE_UI_PRESENCE_INVALID");
        }
        return normalized;
    }

    private static String requireStatus(String value) {
        return requireText(value, 500, "ATTENDANCE_UI_STATUS_INVALID");
    }

    private static String requireText(String value, int max, String code) {
        if (value == null) throw new IllegalArgumentException(code);
        String normalized = value.strip();
        if (normalized.isEmpty() || normalized.length() > max) throw new IllegalArgumentException(code);
        for (int i = 0; i < normalized.length(); i++) {
            if (Character.isISOControl(normalized.charAt(i)) && normalized.charAt(i) != '\n') {
                throw new IllegalArgumentException(code);
            }
        }
        return normalized;
    }
}
