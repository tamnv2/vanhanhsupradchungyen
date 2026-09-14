using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

internal static class Slice1OperationalStateMaterializer
{
    internal const string ModuleId = "IDENTITY_EMPLOYEE_ATTENDANCE";

    internal static async Task<int> MaterializeAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string canonicalStateJson,
        string updatedAt,
        CancellationToken cancellationToken)
    {
        await EnsureNoPendingLocalWorkAsync(connection, transaction, cancellationToken);
        var rows = ParseRows(canonicalStateJson);

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM module_current_state WHERE module_id=$module";
            delete.Parameters.AddWithValue("$module", ModuleId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var row in rows)
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO module_current_state(
                  state_key, module_id, entity_type, entity_id,
                  entity_version, state_json, updated_at
                ) VALUES (
                  $stateKey, $moduleId, $entityType, $entityId,
                  $entityVersion, $stateJson, $updatedAt
                )
                """;
            insert.Parameters.AddWithValue("$stateKey", row.StateKey);
            insert.Parameters.AddWithValue("$moduleId", ModuleId);
            insert.Parameters.AddWithValue("$entityType", row.EntityType);
            insert.Parameters.AddWithValue("$entityId", row.EntityId);
            insert.Parameters.AddWithValue("$entityVersion", row.EntityVersion);
            insert.Parameters.AddWithValue("$stateJson", row.StateJson);
            insert.Parameters.AddWithValue("$updatedAt", updatedAt);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        return rows.Count;
    }

    private static async Task EnsureNoPendingLocalWorkAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT COUNT(*)
            FROM cloud_sync_outbox
            WHERE state <> 'RECONCILED'
            """;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (Convert.ToInt64(value ?? 0L) != 0)
        {
            throw new OperationalSnapshotException(
                "OPERATIONAL_PENDING_LOCAL_WORK",
                "Operational snapshot cannot replace Slice-1 current state while locally accepted work is unreconciled.");
        }
    }

    private static IReadOnlyList<MaterializedRow> ParseRows(string canonicalStateJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalStateJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw Invalid("Slice-1 operational state must be an object.");

            var employees = RequiredArray(root, "employees");
            var presence = RequiredArray(root, "presence");
            var employeeCodes = OptionalArray(root, "employeeCodes");

            var rows = new List<MaterializedRow>();
            var stateKeys = new HashSet<string>(StringComparer.Ordinal);
            var entityIdentities = new HashSet<string>(StringComparer.Ordinal);
            var employeeIds = new HashSet<string>(StringComparer.Ordinal);
            var activeCodes = new HashSet<string>(StringComparer.Ordinal);
            var activeCodeEmployees = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in employees.EnumerateArray())
            {
                RequireObject(item, "employee");
                var employeeId = RequiredString(item, "employeeId");
                var version = RequiredVersion(item);
                var status = RequiredString(item, "status");
                if (status is not ("ACTIVE" or "INACTIVE" or "LEFT" or "ARCHIVED"))
                    throw Invalid($"Employee {employeeId} has invalid status: {status}.");
                if (!employeeIds.Add(employeeId))
                    throw Invalid($"Duplicate employee identity: {employeeId}.");

                AddRow(rows, stateKeys, entityIdentities,
                    $"employee:{employeeId}", "employee", employeeId, version, item.GetRawText());
            }

            foreach (var item in employeeCodes.EnumerateArray())
            {
                RequireObject(item, "employeeCode");
                var employeeCodeId = RequiredString(item, "employeeCodeId");
                var employeeId = RequiredString(item, "employeeId");
                var employeeCode = RequiredString(item, "employeeCode");
                var version = RequiredVersion(item);
                var status = RequiredString(item, "status");
                if (!employeeIds.Contains(employeeId))
                    throw Invalid($"Employee code {employeeCodeId} references missing employee: {employeeId}.");
                if (status is not ("ACTIVE" or "RELEASED" or "ARCHIVED"))
                    throw Invalid($"Employee code {employeeCodeId} has invalid status: {status}.");
                if (status == "ACTIVE")
                {
                    if (!activeCodes.Add(employeeCode))
                        throw Invalid($"Active employee code is duplicated: {employeeCode}.");
                    if (!activeCodeEmployees.Add(employeeId))
                        throw Invalid($"Employee has more than one active employee code: {employeeId}.");
                }

                AddRow(rows, stateKeys, entityIdentities,
                    $"employee_code:{employeeCodeId}", "employee_code", employeeCodeId, version, item.GetRawText());
            }

            foreach (var item in presence.EnumerateArray())
            {
                RequireObject(item, "presence");
                var employeeId = RequiredString(item, "employeeId");
                var version = RequiredVersion(item);
                var currentState = RequiredString(item, "currentState");
                if (!employeeIds.Contains(employeeId))
                    throw Invalid($"Presence state references missing employee: {employeeId}.");
                if (currentState is not ("IN" or "OUT"))
                    throw Invalid($"Presence state for {employeeId} must be IN or OUT.");

                AddRow(rows, stateKeys, entityIdentities,
                    $"attendance:{employeeId}", "attendance", employeeId, version, item.GetRawText());
            }

            return rows;
        }
        catch (OperationalSnapshotException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new OperationalSnapshotException(
                "SLICE1_OPERATIONAL_STATE_INVALID",
                "Slice-1 operational state JSON is invalid.",
                error);
        }
    }

    private static void AddRow(
        ICollection<MaterializedRow> rows,
        ISet<string> stateKeys,
        ISet<string> entityIdentities,
        string stateKey,
        string entityType,
        string entityId,
        long entityVersion,
        string stateJson)
    {
        if (!stateKeys.Add(stateKey))
            throw Invalid($"Duplicate materialized state key: {stateKey}.");
        if (!entityIdentities.Add($"{entityType}\n{entityId}"))
            throw Invalid($"Duplicate materialized entity identity: {entityType}/{entityId}.");
        rows.Add(new MaterializedRow(stateKey, entityType, entityId, entityVersion, stateJson));
    }

    private static JsonElement RequiredArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
            throw Invalid($"Slice-1 operational state requires array: {propertyName}.");
        return value;
    }

    private static JsonElement OptionalArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            using var empty = JsonDocument.Parse("[]");
            return empty.RootElement.Clone();
        }
        if (value.ValueKind != JsonValueKind.Array)
            throw Invalid($"Slice-1 operational state property must be an array: {propertyName}.");
        return value;
    }

    private static void RequireObject(JsonElement item, string kind)
    {
        if (item.ValueKind != JsonValueKind.Object)
            throw Invalid($"Every {kind} snapshot item must be an object.");
    }

    private static string RequiredString(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw Invalid($"Missing or invalid string: {propertyName}.");
        }
        return value.GetString()!.Trim();
    }

    private static long RequiredVersion(JsonElement item)
    {
        if (!item.TryGetProperty("entityVersion", out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt64(out var version) ||
            version < 1)
        {
            throw Invalid("Missing or invalid entityVersion; expected integer >= 1.");
        }
        return version;
    }

    private static OperationalSnapshotException Invalid(string message) =>
        new("SLICE1_OPERATIONAL_STATE_INVALID", message);

    private sealed record MaterializedRow(
        string StateKey,
        string EntityType,
        string EntityId,
        long EntityVersion,
        string StateJson);
}
