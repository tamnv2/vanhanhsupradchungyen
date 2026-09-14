using Vhdchy.LanService;

namespace Vhdchy.LanSlice1Business.Harness;

internal static class EmployeeVectors
{
    internal static async Task RunAsync(Slice1BusinessAdapter adapter, string databasePath)
    {
        await HarnessAssert.ErrorAsync("PERMISSION_DENIED", () => adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_DENY", "REQ-DENY", "IDEM-DENY", "EMPLOYEE_CREATE", "E-DENY", null,
            "{\"employeeId\":\"E-DENY\",\"fullName\":\"Denied User\"}")));

        var createE1 = new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E1-CREATE", "IDEM-E1-CREATE", "EMPLOYEE_CREATE", "E1", null,
            "{\"employeeId\":\"E1\",\"fullName\":\"Employee One\",\"status\":\"ACTIVE\"}",
            "PDA-1", 1);
        var created = await adapter.ExecuteAsync(createE1);
        HarnessAssert.That(created.ResultingVersion == 1 && created.CommitStatus == "LAN_ACCEPTED_PENDING_SYNC", "E1_CREATE_FAILED");
        HarnessAssert.That(await new EdgeActorEvidenceStore(databasePath).ReadActorAsync(created.EventId) == "U_ALL", "E1_ACTOR_EVIDENCE_MISSING");

        var replay = await adapter.ExecuteAsync(createE1 with { RequestId = "REQ-E1-CREATE-RETRY" });
        HarnessAssert.That(replay.AlreadyAccepted && replay.EventId == created.EventId, "E1_REPLAY_NOT_IDEMPOTENT");
        await HarnessAssert.ErrorAsync("IDEMPOTENCY_PAYLOAD_CONFLICT", () => adapter.ExecuteAsync(createE1 with
        {
            AuthenticatedUserId = "U_ALT",
            RequestId = "REQ-E1-CREATE-ALT"
        }));

        await HarnessAssert.ErrorAsync("INVALID_INPUT", () => adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-AUTH-FIELD", "IDEM-AUTH-FIELD", "EMPLOYEE_CREATE", "E-AUTH", null,
            "{\"employeeId\":\"E-AUTH\",\"fullName\":\"Bad Authority\",\"eventCode\":\"FAKE\"}")));

        var updated = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E1-UPDATE", "IDEM-E1-UPDATE", "EMPLOYEE_UPDATE", "E1", 1,
            "{\"employeeId\":\"E1\",\"note\":\"updated\"}"));
        HarnessAssert.That(updated.ResultingVersion == 2, "E1_UPDATE_VERSION_WRONG");

        var inactive = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E1-STATUS", "IDEM-E1-STATUS", "EMPLOYEE_STATUS_CHANGE", "E1", 2,
            "{\"employeeId\":\"E1\",\"status\":\"INACTIVE\",\"reason\":\"test\"}"));
        HarnessAssert.That(inactive.ResultingVersion == 3, "E1_STATUS_VERSION_WRONG");

        var e2 = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E2-CREATE", "IDEM-E2-CREATE", "EMPLOYEE_CREATE", "E2", null,
            "{\"employeeId\":\"E2\",\"fullName\":\"Employee Two\"}"));
        var e3 = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E3-CREATE", "IDEM-E3-CREATE", "EMPLOYEE_CREATE", "E3", null,
            "{\"employeeId\":\"E3\",\"fullName\":\"Employee Three\"}"));
        HarnessAssert.That(e2.ResultingVersion == 1 && e3.ResultingVersion == 1, "EMPLOYEE_SETUP_FAILED");

        var assigned = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-CODE-1", "IDEM-CODE-1", "EMPLOYEE_CODE_ASSIGN", "CODE-ID-1", null,
            "{\"employeeCodeId\":\"CODE-ID-1\",\"employeeId\":\"E2\",\"employeeCode\":\"MNV001\"}"));
        HarnessAssert.That(assigned.ResultingVersion == 1, "CODE_INITIAL_ASSIGN_FAILED");

        await HarnessAssert.ErrorAsync("INVALID_INPUT", () => adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-CODE-ACTIVE-REUSE", "IDEM-CODE-ACTIVE-REUSE", "EMPLOYEE_CODE_ASSIGN", "CODE-ID-1", 1,
            "{\"employeeCodeId\":\"CODE-ID-1\",\"employeeId\":\"E3\",\"employeeCode\":\"MNV001\"}")));

        var e2Inactive = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E2-STATUS", "IDEM-E2-STATUS", "EMPLOYEE_STATUS_CHANGE", "E2", 1,
            "{\"employeeId\":\"E2\",\"status\":\"INACTIVE\",\"reason\":\"MNV reuse test\"}"));
        HarnessAssert.That(e2Inactive.ResultingVersion == 2, "E2_INACTIVE_FAILED");

        var reused = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-CODE-REUSE", "IDEM-CODE-REUSE", "EMPLOYEE_CODE_ASSIGN", "CODE-ID-1", 1,
            "{\"employeeCodeId\":\"CODE-ID-1\",\"employeeId\":\"E3\",\"employeeCode\":\"MNV001\"}"));
        HarnessAssert.That(reused.ResultingVersion == 2, "CODE_REUSE_AFTER_PRIOR_INACTIVE_FAILED");
    }
}
