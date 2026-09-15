package vn.vhdchy.app.transport;

public final class AttendanceCommandPlan {
    public static final String API_PATH = "/api/v1/data/commands";

    private final Action action;
    private final AttendanceOperationIdentity operationIdentity;
    private final String entityId;
    private final Long expectedEntityVersion;
    private final String businessDate;
    private final String occurredAt;
    private final String source;
    private final String bodyJson;

    AttendanceCommandPlan(
        Action action,
        AttendanceOperationIdentity operationIdentity,
        String entityId,
        Long expectedEntityVersion,
        String businessDate,
        String occurredAt,
        String source,
        String bodyJson
    ) {
        this.action = action;
        this.operationIdentity = operationIdentity;
        this.entityId = entityId;
        this.expectedEntityVersion = expectedEntityVersion;
        this.businessDate = businessDate;
        this.occurredAt = occurredAt;
        this.source = source;
        this.bodyJson = bodyJson;
    }

    public Action action() { return action; }
    public String commandCode() { return action == Action.IN ? "ATTENDANCE_IN" : "ATTENDANCE_OUT"; }
    public AttendanceOperationIdentity operationIdentity() { return operationIdentity; }
    public String entityId() { return entityId; }
    public Long expectedEntityVersion() { return expectedEntityVersion; }
    public String businessDate() { return businessDate; }
    public String occurredAt() { return occurredAt; }
    public String source() { return source; }
    public String apiPath() { return API_PATH; }
    public String bodyJson() { return bodyJson; }

    public enum Action { IN, OUT }
}
