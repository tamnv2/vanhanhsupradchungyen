using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

internal static class EmployeeCodeUniqueClaimStore
{
    internal const string ModuleId = "IDENTITY_EMPLOYEE_ATTENDANCE";
    internal const string CodeValueNamespace = "EMPLOYEE_CODE_VALUE";
    internal const string EmployeeActiveCodeNamespace = "EMPLOYEE_ACTIVE_CODE";

    internal static async Task EnsureAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path is required", nameof(databasePath));

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000;";
            await pragma.ExecuteNonQueryAsync(cancellationToken);
        }

        using var transaction = connection.BeginTransaction();
        try
        {
            await using (var schema = connection.CreateCommand())
            {
                schema.Transaction = transaction;
                schema.CommandText = SchemaSql;
                await schema.ExecuteNonQueryAsync(cancellationToken);
            }

            // Rebuild the two reviewed claim namespaces from current state at startup.
            // If historical/current state contains a duplicate active MNV or more than
            // one active code for one employee, the PRIMARY KEY constraint fails and
            // startup stays fail-closed instead of silently selecting a winner.
            await using (var rebuild = connection.CreateCommand())
            {
                rebuild.Transaction = transaction;
                rebuild.CommandText = """
                    DELETE FROM edge_unique_claims
                    WHERE claim_namespace IN ('EMPLOYEE_CODE_VALUE','EMPLOYEE_ACTIVE_CODE');

                    INSERT INTO edge_unique_claims(
                      claim_namespace, claim_key,
                      owner_module_id, owner_entity_type, owner_entity_id, updated_at
                    )
                    SELECT
                      'EMPLOYEE_CODE_VALUE',
                      trim(json_extract(state_json, '$.employeeCode')),
                      module_id, entity_type, entity_id, updated_at
                    FROM module_current_state
                    WHERE module_id='IDENTITY_EMPLOYEE_ATTENDANCE'
                      AND entity_type='employee_code'
                      AND json_extract(state_json, '$.status')='ACTIVE';

                    INSERT INTO edge_unique_claims(
                      claim_namespace, claim_key,
                      owner_module_id, owner_entity_type, owner_entity_id, updated_at
                    )
                    SELECT
                      'EMPLOYEE_ACTIVE_CODE',
                      trim(json_extract(state_json, '$.employeeId')),
                      module_id, entity_type, entity_id, updated_at
                    FROM module_current_state
                    WHERE module_id='IDENTITY_EMPLOYEE_ATTENDANCE'
                      AND entity_type='employee_code'
                      AND json_extract(state_json, '$.status')='ACTIVE';
                    """;
                await rebuild.ExecuteNonQueryAsync(cancellationToken);
            }

            transaction.Commit();
        }
        catch (SqliteException error)
        {
            transaction.Rollback();
            throw new InvalidOperationException(
                "EDGE_EMPLOYEE_CODE_UNIQUENESS_INVALID",
                error);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private const string SchemaSql = """
CREATE TABLE IF NOT EXISTS edge_unique_claims (
  claim_namespace TEXT NOT NULL,
  claim_key TEXT NOT NULL,
  owner_module_id TEXT NOT NULL,
  owner_entity_type TEXT NOT NULL,
  owner_entity_id TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  PRIMARY KEY(claim_namespace, claim_key)
);

CREATE INDEX IF NOT EXISTS ix_edge_unique_claim_owner
ON edge_unique_claims(owner_module_id, owner_entity_type, owner_entity_id);

CREATE TRIGGER IF NOT EXISTS trg_employee_code_claim_insert
BEFORE INSERT ON module_current_state
WHEN NEW.module_id='IDENTITY_EMPLOYEE_ATTENDANCE'
  AND NEW.entity_type='employee_code'
  AND json_extract(NEW.state_json, '$.status')='ACTIVE'
BEGIN
  SELECT CASE WHEN EXISTS (
    SELECT 1 FROM edge_unique_claims
    WHERE claim_namespace='EMPLOYEE_CODE_VALUE'
      AND claim_key=trim(json_extract(NEW.state_json, '$.employeeCode'))
      AND NOT (
        owner_module_id=NEW.module_id
        AND owner_entity_type=NEW.entity_type
        AND owner_entity_id=NEW.entity_id
      )
  ) THEN RAISE(ABORT, 'VHDCHY_EMPLOYEE_CODE_VALUE_CLAIM_CONFLICT') END;

  SELECT CASE WHEN EXISTS (
    SELECT 1 FROM edge_unique_claims
    WHERE claim_namespace='EMPLOYEE_ACTIVE_CODE'
      AND claim_key=trim(json_extract(NEW.state_json, '$.employeeId'))
      AND NOT (
        owner_module_id=NEW.module_id
        AND owner_entity_type=NEW.entity_type
        AND owner_entity_id=NEW.entity_id
      )
  ) THEN RAISE(ABORT, 'VHDCHY_EMPLOYEE_CODE_EMPLOYEE_CLAIM_CONFLICT') END;

  INSERT INTO edge_unique_claims(
    claim_namespace, claim_key,
    owner_module_id, owner_entity_type, owner_entity_id, updated_at
  ) VALUES (
    'EMPLOYEE_CODE_VALUE', trim(json_extract(NEW.state_json, '$.employeeCode')),
    NEW.module_id, NEW.entity_type, NEW.entity_id, NEW.updated_at
  )
  ON CONFLICT(claim_namespace, claim_key) DO UPDATE SET
    updated_at=excluded.updated_at
  WHERE edge_unique_claims.owner_module_id=excluded.owner_module_id
    AND edge_unique_claims.owner_entity_type=excluded.owner_entity_type
    AND edge_unique_claims.owner_entity_id=excluded.owner_entity_id;

  INSERT INTO edge_unique_claims(
    claim_namespace, claim_key,
    owner_module_id, owner_entity_type, owner_entity_id, updated_at
  ) VALUES (
    'EMPLOYEE_ACTIVE_CODE', trim(json_extract(NEW.state_json, '$.employeeId')),
    NEW.module_id, NEW.entity_type, NEW.entity_id, NEW.updated_at
  )
  ON CONFLICT(claim_namespace, claim_key) DO UPDATE SET
    updated_at=excluded.updated_at
  WHERE edge_unique_claims.owner_module_id=excluded.owner_module_id
    AND edge_unique_claims.owner_entity_type=excluded.owner_entity_type
    AND edge_unique_claims.owner_entity_id=excluded.owner_entity_id;
END;

CREATE TRIGGER IF NOT EXISTS trg_employee_code_claim_update
BEFORE UPDATE ON module_current_state
WHEN OLD.module_id='IDENTITY_EMPLOYEE_ATTENDANCE'
  AND OLD.entity_type='employee_code'
BEGIN
  DELETE FROM edge_unique_claims
  WHERE owner_module_id=OLD.module_id
    AND owner_entity_type=OLD.entity_type
    AND owner_entity_id=OLD.entity_id
    AND claim_namespace IN ('EMPLOYEE_CODE_VALUE','EMPLOYEE_ACTIVE_CODE');

  SELECT CASE WHEN json_extract(NEW.state_json, '$.status')='ACTIVE' AND EXISTS (
    SELECT 1 FROM edge_unique_claims
    WHERE claim_namespace='EMPLOYEE_CODE_VALUE'
      AND claim_key=trim(json_extract(NEW.state_json, '$.employeeCode'))
  ) THEN RAISE(ABORT, 'VHDCHY_EMPLOYEE_CODE_VALUE_CLAIM_CONFLICT') END;

  SELECT CASE WHEN json_extract(NEW.state_json, '$.status')='ACTIVE' AND EXISTS (
    SELECT 1 FROM edge_unique_claims
    WHERE claim_namespace='EMPLOYEE_ACTIVE_CODE'
      AND claim_key=trim(json_extract(NEW.state_json, '$.employeeId'))
  ) THEN RAISE(ABORT, 'VHDCHY_EMPLOYEE_CODE_EMPLOYEE_CLAIM_CONFLICT') END;

  INSERT INTO edge_unique_claims(
    claim_namespace, claim_key,
    owner_module_id, owner_entity_type, owner_entity_id, updated_at
  )
  SELECT
    'EMPLOYEE_CODE_VALUE', trim(json_extract(NEW.state_json, '$.employeeCode')),
    NEW.module_id, NEW.entity_type, NEW.entity_id, NEW.updated_at
  WHERE json_extract(NEW.state_json, '$.status')='ACTIVE';

  INSERT INTO edge_unique_claims(
    claim_namespace, claim_key,
    owner_module_id, owner_entity_type, owner_entity_id, updated_at
  )
  SELECT
    'EMPLOYEE_ACTIVE_CODE', trim(json_extract(NEW.state_json, '$.employeeId')),
    NEW.module_id, NEW.entity_type, NEW.entity_id, NEW.updated_at
  WHERE json_extract(NEW.state_json, '$.status')='ACTIVE';
END;

CREATE TRIGGER IF NOT EXISTS trg_employee_code_claim_delete
BEFORE DELETE ON module_current_state
WHEN OLD.module_id='IDENTITY_EMPLOYEE_ATTENDANCE'
  AND OLD.entity_type='employee_code'
BEGIN
  DELETE FROM edge_unique_claims
  WHERE owner_module_id=OLD.module_id
    AND owner_entity_type=OLD.entity_type
    AND owner_entity_id=OLD.entity_id
    AND claim_namespace IN ('EMPLOYEE_CODE_VALUE','EMPLOYEE_ACTIVE_CODE');
END;
""";
}
