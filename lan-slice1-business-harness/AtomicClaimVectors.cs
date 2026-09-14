using Microsoft.Data.Sqlite;
using Vhdchy.LanService;

namespace Vhdchy.LanSlice1Business.Harness;

internal static class AtomicClaimVectors
{
    internal static async Task RunAsync(Slice1BusinessAdapter adapter, string databasePath)
    {
        var e4 = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E4-CREATE", "IDEM-E4-CREATE", "EMPLOYEE_CREATE", "E4", null,
            "{\"employeeId\":\"E4\",\"fullName\":\"Employee Four\"}"));
        var e5 = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-E5-CREATE", "IDEM-E5-CREATE", "EMPLOYEE_CREATE", "E5", null,
            "{\"employeeId\":\"E5\",\"fullName\":\"Employee Five\"}"));
        HarnessAssert.That(e4.ResultingVersion == 1 && e5.ResultingVersion == 1, "ATOMIC_EMPLOYEE_SETUP_FAILED");

        var winner = await adapter.ExecuteAsync(new Slice1BusinessCommandRequest(
            "U_ALL", "REQ-CODE-RACE-WIN", "IDEM-CODE-RACE-WIN", "EMPLOYEE_CODE_ASSIGN", "CODE-RACE-WIN", null,
            "{\"employeeCodeId\":\"CODE-RACE-WIN\",\"employeeId\":\"E4\",\"employeeCode\":\"MNV-RACE\"}"));
        HarnessAssert.That(winner.ResultingVersion == 1, "ATOMIC_WINNER_NOT_ACCEPTED");

        var store = new LocalCommandStore(
            databasePath,
            TestAuthority.Environment,
            TestAuthority.ClusterId,
            TestAuthority.Compatibility);
        var eventsBefore = await store.ReadAcceptedEventCountAsync();
        var outboxBefore = await store.ReadCloudOutboxCountAsync();

        await ExpectClaimConflictAsync(() => store.ExecuteAsync(new LanLocalCommandEnvelope(
            RequestId: "REQ-CODE-RACE-LOSE",
            IdempotencyKey: "IDEM-CODE-RACE-LOSE",
            ModuleId: TestAuthority.ModuleId,
            CommandCode: "EMPLOYEE_CODE_ASSIGN",
            EventCode: "EMPLOYEE_CODE_ASSIGNED",
            EntityType: "employee_code",
            EntityId: "CODE-RACE-LOSE",
            StateKey: "employee_code:CODE-RACE-LOSE",
            ExpectedBaseVersion: null,
            PayloadJson: "{\"employeeCodeId\":\"CODE-RACE-LOSE\",\"employeeId\":\"E5\",\"employeeCode\":\"MNV-RACE\"}",
            NextStateJson: "{\"employeeCodeId\":\"CODE-RACE-LOSE\",\"employeeId\":\"E5\",\"employeeCode\":\"MNV-RACE\",\"status\":\"ACTIVE\",\"entityVersion\":1}")));

        HarnessAssert.That(await store.ReadAcceptedEventCountAsync() == eventsBefore, "ATOMIC_CODE_CONFLICT_APPENDED_EVENT");
        HarnessAssert.That(await store.ReadCloudOutboxCountAsync() == outboxBefore, "ATOMIC_CODE_CONFLICT_APPENDED_OUTBOX");
        HarnessAssert.That(await store.ReadCurrentStateAsync("employee_code:CODE-RACE-LOSE") is null, "ATOMIC_CODE_CONFLICT_MUTATED_STATE");

        await ExpectClaimConflictAsync(() => store.ExecuteAsync(new LanLocalCommandEnvelope(
            RequestId: "REQ-EMPLOYEE-RACE-LOSE",
            IdempotencyKey: "IDEM-EMPLOYEE-RACE-LOSE",
            ModuleId: TestAuthority.ModuleId,
            CommandCode: "EMPLOYEE_CODE_ASSIGN",
            EventCode: "EMPLOYEE_CODE_ASSIGNED",
            EntityType: "employee_code",
            EntityId: "CODE-EMPLOYEE-LOSE",
            StateKey: "employee_code:CODE-EMPLOYEE-LOSE",
            ExpectedBaseVersion: null,
            PayloadJson: "{\"employeeCodeId\":\"CODE-EMPLOYEE-LOSE\",\"employeeId\":\"E4\",\"employeeCode\":\"MNV-OTHER\"}",
            NextStateJson: "{\"employeeCodeId\":\"CODE-EMPLOYEE-LOSE\",\"employeeId\":\"E4\",\"employeeCode\":\"MNV-OTHER\",\"status\":\"ACTIVE\",\"entityVersion\":1}")));

        HarnessAssert.That(await store.ReadAcceptedEventCountAsync() == eventsBefore, "ATOMIC_EMPLOYEE_CONFLICT_APPENDED_EVENT");
        HarnessAssert.That(await store.ReadCloudOutboxCountAsync() == outboxBefore, "ATOMIC_EMPLOYEE_CONFLICT_APPENDED_OUTBOX");
        HarnessAssert.That(await store.ReadCurrentStateAsync("employee_code:CODE-EMPLOYEE-LOSE") is null, "ATOMIC_EMPLOYEE_CONFLICT_MUTATED_STATE");

        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite
        }.ToString());
        await connection.OpenAsync();

        HarnessAssert.That(
            await ScalarAsync(connection,
                "SELECT owner_entity_id FROM edge_unique_claims WHERE claim_namespace='EMPLOYEE_CODE_VALUE' AND claim_key='MNV-RACE'") == "CODE-RACE-WIN",
            "ATOMIC_MNV_WINNER_CLAIM_LOST");
        HarnessAssert.That(
            await ScalarAsync(connection,
                "SELECT owner_entity_id FROM edge_unique_claims WHERE claim_namespace='EMPLOYEE_ACTIVE_CODE' AND claim_key='E4'") == "CODE-RACE-WIN",
            "ATOMIC_EMPLOYEE_WINNER_CLAIM_LOST");
    }

    private static async Task ExpectClaimConflictAsync(Func<Task> action)
    {
        try
        {
            await action();
            throw new InvalidOperationException("ATOMIC_UNIQUE_CLAIM_CONFLICT_NOT_REJECTED");
        }
        catch (LanLocalCommandException error) when (
            error.Code == "LOCAL_COMMAND_COMMIT_FAILED" &&
            error.InnerException is SqliteException sqlite &&
            sqlite.Message.Contains("VHDCHY_EMPLOYEE_CODE_", StringComparison.Ordinal))
        {
        }
    }

    private static async Task<string?> ScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? null : Convert.ToString(value);
    }
}
