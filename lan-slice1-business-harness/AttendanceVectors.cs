using Vhdchy.LanService;

namespace Vhdchy.LanSlice1Business.Harness;

internal static class AttendanceVectors
{
    internal static async Task RunAsync(Slice1BusinessAdapter adapter, string databasePath)
    {
        var firstIn = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E3-IN-1", "IDEM-E3-IN-1", "ATTENDANCE_IN", "E3", null,
            "{\"employeeId\":\"E3\",\"businessDate\":\"2026-09-14\",\"occurredAt\":\"2026-09-14T08:00:00+07:00\"}",
            "PDA-1", 2));
        HarnessAssert.That(firstIn.ResultingVersion == 1, "ATTENDANCE_FIRST_IN_FAILED");

        var repeatedIn = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E3-IN-2", "IDEM-E3-IN-2", "ATTENDANCE_IN", "E3", 1,
            "{\"employeeId\":\"E3\",\"businessDate\":\"2026-09-14\",\"occurredAt\":\"2026-09-14T08:01:00+07:00\"}",
            "PDA-1", 3));
        HarnessAssert.That(repeatedIn.ResultingVersion == 2, "ATTENDANCE_REPEATED_IN_FAILED");

        var attendanceOut = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E3-OUT", "IDEM-E3-OUT", "ATTENDANCE_OUT", "E3", 2,
            "{\"employeeId\":\"E3\",\"businessDate\":\"2026-09-14\",\"occurredAt\":\"2026-09-14T09:00:00+07:00\"}",
            "PDA-1", 4));
        HarnessAssert.That(attendanceOut.ResultingVersion == 3, "ATTENDANCE_OUT_FAILED");

        await HarnessAssert.ErrorAsync("INVALID_INPUT", () => adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E3-OUT-2", "IDEM-E3-OUT-2", "ATTENDANCE_OUT", "E3", 3,
            "{\"employeeId\":\"E3\",\"businessDate\":\"2026-09-14\",\"occurredAt\":\"2026-09-14T09:01:00+07:00\"}")));

        var corrected = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E3-CORRECT", "IDEM-E3-CORRECT", "ATTENDANCE_CORRECT", "E3", 3,
            "{\"employeeId\":\"E3\",\"businessDate\":\"2026-09-14\",\"currentState\":\"IN\",\"reason\":\"manual correction\"}"));
        HarnessAssert.That(corrected.ResultingVersion == 4, "ATTENDANCE_CORRECTION_FAILED");
        HarnessAssert.That(await new EdgeActorEvidenceStore(databasePath).ReadActorAsync(corrected.EventId) == "U_ALL", "CORRECTION_ACTOR_EVIDENCE_MISSING");

        await HarnessAssert.ErrorAsync("RUNTIME_DEPENDENCY_UNAVAILABLE", () => adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E3-PORTRAIT", "IDEM-E3-PORTRAIT", "EMPLOYEE_PORTRAIT_REPLACE", "E3", 1,
            "{\"employeeId\":\"E3\"}")));
    }
}
