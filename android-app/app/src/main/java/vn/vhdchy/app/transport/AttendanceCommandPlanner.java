package vn.vhdchy.app.transport;

import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;

public final class AttendanceCommandPlanner {
    public static final String SOURCE = "PDA_ANDROID";

    private AttendanceCommandPlanner() {}

    public static AttendanceCommandPlan plan(
        AttendanceScanContext context,
        AttendanceCommandPlan.Action action,
        AttendanceOperationIdentity operationIdentity,
        String businessDate,
        String occurredAt
    ) {
        if (context == null || action == null || operationIdentity == null) {
            throw new IllegalArgumentException("ATTENDANCE_PLAN_CONTEXT_REQUIRED");
        }
        String normalizedBusinessDate = requireBusinessDate(businessDate);
        String normalizedOccurredAt = requireOccurredAt(occurredAt);

        AttendanceScanContext.Presence presence = context.presence();
        if (action == AttendanceCommandPlan.Action.OUT &&
            (presence == null || presence.currentState() != AttendanceScanContext.Presence.State.IN)) {
            throw new IllegalArgumentException("ATTENDANCE_OUT_REQUIRES_IN_PRESENCE");
        }

        Long expectedVersion = presence == null ? null : presence.entityVersion();
        String commandCode = action == AttendanceCommandPlan.Action.IN ? "ATTENDANCE_IN" : "ATTENDANCE_OUT";
        String bodyJson = buildBody(
            operationIdentity,
            commandCode,
            context.employeeId(),
            expectedVersion,
            normalizedBusinessDate,
            normalizedOccurredAt
        );

        return new AttendanceCommandPlan(
            action,
            operationIdentity,
            context.employeeId(),
            expectedVersion,
            normalizedBusinessDate,
            normalizedOccurredAt,
            SOURCE,
            bodyJson
        );
    }

    private static String buildBody(
        AttendanceOperationIdentity identity,
        String commandCode,
        String employeeId,
        Long expectedVersion,
        String businessDate,
        String occurredAt
    ) {
        return "{"
            + "\"requestId\":" + quote(identity.requestId()) + ","
            + "\"idempotencyKey\":" + quote(identity.idempotencyKey()) + ","
            + "\"commandCode\":" + quote(commandCode) + ","
            + "\"entityId\":" + quote(employeeId) + ","
            + "\"expectedEntityVersion\":" + (expectedVersion == null ? "null" : expectedVersion) + ","
            + "\"payload\":{"
            + "\"employeeId\":" + quote(employeeId) + ","
            + "\"businessDate\":" + quote(businessDate) + ","
            + "\"occurredAt\":" + quote(occurredAt) + ","
            + "\"source\":" + quote(SOURCE)
            + "},"
            + "\"deviceSeq\":" + identity.deviceSeq()
            + "}";
    }

    private static String requireBusinessDate(String value) {
        if (value == null) throw new IllegalArgumentException("ATTENDANCE_BUSINESS_DATE_INVALID");
        String normalized = value.strip();
        try {
            if (!LocalDate.parse(normalized).toString().equals(normalized)) {
                throw new IllegalArgumentException("ATTENDANCE_BUSINESS_DATE_INVALID");
            }
        } catch (DateTimeParseException error) {
            throw new IllegalArgumentException("ATTENDANCE_BUSINESS_DATE_INVALID", error);
        }
        return normalized;
    }

    private static String requireOccurredAt(String value) {
        if (value == null) throw new IllegalArgumentException("ATTENDANCE_OCCURRED_AT_INVALID");
        String normalized = value.strip();
        try {
            OffsetDateTime.parse(normalized);
        } catch (DateTimeParseException error) {
            throw new IllegalArgumentException("ATTENDANCE_OCCURRED_AT_INVALID", error);
        }
        return normalized;
    }

    private static String quote(String value) {
        StringBuilder escaped = new StringBuilder(value.length() + 2);
        escaped.append('"');
        for (int i = 0; i < value.length(); i++) {
            char c = value.charAt(i);
            switch (c) {
                case '"' -> escaped.append("\\\"");
                case '\\' -> escaped.append("\\\\");
                case '\b' -> escaped.append("\\b");
                case '\f' -> escaped.append("\\f");
                case '\n' -> escaped.append("\\n");
                case '\r' -> escaped.append("\\r");
                case '\t' -> escaped.append("\\t");
                default -> {
                    if (c < 0x20) {
                        escaped.append(String.format("\\u%04x", (int)c));
                    } else {
                        escaped.append(c);
                    }
                }
            }
        }
        escaped.append('"');
        return escaped.toString();
    }
}
