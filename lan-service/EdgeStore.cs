using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

internal sealed class EdgeStore
{
    public const string SchemaVersion = "VHDCHY_EDGE_V2";

    private readonly string _databasePath;
    private readonly string _connectionString;

    public EdgeStore(string databasePath)
    {
        _databasePath = databasePath;
        var directory = Path.GetDirectoryName(databasePath);
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Edge database directory is required", nameof(databasePath));
        Directory.CreateDirectory(directory);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
    }

    public string DatabasePath => _databasePath;

    public async Task InitializeAsync(
        string environment,
        string clusterId,
        string instanceId,
        string edgeEpoch,
        string domainContractVersion,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = SchemaSql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await EnsureIdentityAsync(connection, "environment", environment, cancellationToken);
        await EnsureIdentityAsync(connection, "cluster_id", clusterId, cancellationToken);
        await SetMetaAsync(connection, "edge_schema_version", SchemaVersion, overwrite: true, cancellationToken);
        await SetMetaAsync(connection, "domain_contract_version", domainContractVersion, overwrite: true, cancellationToken);
        await SetMetaAsync(connection, "instance_id", instanceId, overwrite: true, cancellationToken);
        await SetMetaAsync(connection, "edge_epoch", edgeEpoch, overwrite: true, cancellationToken);
        await SetMetaAsync(connection, "readiness", "EDGE_EMPTY", overwrite: false, cancellationToken);
    }

    public async Task<EdgeStoreStatus> ReadStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var meta = await ReadMetaAsync(connection, cancellationToken);

        return new EdgeStoreStatus(
            Readiness: meta.GetValueOrDefault("readiness") ?? "EDGE_EMPTY",
            AuthoritySnapshotVersion: meta.GetValueOrDefault("authority_snapshot_version"),
            OperationalSnapshotVersion: meta.GetValueOrDefault("operational_snapshot_version"),
            LastCloudSyncAt: ParseTimestamp(meta.GetValueOrDefault("last_cloud_sync_at")),
            PendingCloudSync: await CountAsync(connection,
                "SELECT COUNT(*) FROM cloud_sync_outbox WHERE state IN ('PENDING','SYNCHRONIZING','RETRY_WAIT')", cancellationToken),
            PendingGoogleWork: await CountAsync(connection,
                "SELECT (SELECT COUNT(*) FROM google_projection_outbox WHERE state IN ('PENDING','SENDING','RETRY_WAIT')) + " +
                "(SELECT COUNT(*) FROM drive_upload_outbox WHERE state IN ('PENDING','UPLOADING','RETRY_WAIT'))", cancellationToken),
            ConflictCount: await CountAsync(connection,
                "SELECT COUNT(*) FROM edge_conflicts WHERE state IN ('OPEN','REVIEW_REQUIRED')", cancellationToken));
    }

    public async Task<EdgeStoreIntegrity> CheckIntegrityAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var foreignKeyViolations = await CountRowsAsync(connection, "PRAGMA foreign_key_check", cancellationToken);
        var quickCheck = await ScalarTextAsync(connection, "PRAGMA quick_check", cancellationToken);
        return new EdgeStoreIntegrity(
            ForeignKeysOk: foreignKeyViolations == 0,
            QuickCheckOk: string.Equals(quickCheck, "ok", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000;";
        await pragma.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }

    private static async Task EnsureIdentityAsync(
        SqliteConnection connection,
        string key,
        string expected,
        CancellationToken cancellationToken)
    {
        var current = await ReadMetaValueAsync(connection, key, cancellationToken);
        if (current is null)
        {
            await SetMetaAsync(connection, key, expected, overwrite: false, cancellationToken);
            return;
        }

        if (!string.Equals(current, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"EDGE_IDENTITY_MISMATCH {key}: existing={current} expected={expected}");
        }
    }

    private static async Task SetMetaAsync(
        SqliteConnection connection,
        string key,
        string value,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = overwrite
            ? "INSERT INTO edge_meta(meta_key, meta_value, updated_at) VALUES ($key, $value, $now) " +
              "ON CONFLICT(meta_key) DO UPDATE SET meta_value=excluded.meta_value, updated_at=excluded.updated_at"
            : "INSERT OR IGNORE INTO edge_meta(meta_key, meta_value, updated_at) VALUES ($key, $value, $now)";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string?> ReadMetaValueAsync(
        SqliteConnection connection,
        string key,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT meta_value FROM edge_meta WHERE meta_key=$key LIMIT 1";
        command.Parameters.AddWithValue("$key", key);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : Convert.ToString(result);
    }

    private static async Task<Dictionary<string, string?>> ReadMetaAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT meta_key, meta_value FROM edge_meta";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result[reader.GetString(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
        }
        return result;
    }

    private static async Task<long> CountAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value ?? 0L);
    }

    private static async Task<long> CountRowsAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        long count = 0;
        while (await reader.ReadAsync(cancellationToken)) count++;
        return count;
    }

    private static async Task<string?> ScalarTextAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }

    private static DateTimeOffset? ParseTimestamp(string? value) =>
        DateTimeOffset.TryParse(value, out var timestamp) ? timestamp : null;

    private const string SchemaSql = """
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS edge_meta (
  meta_key TEXT PRIMARY KEY,
  meta_value TEXT,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS authority_snapshots (
  authority_version TEXT PRIMARY KEY,
  scope_json TEXT NOT NULL,
  source_checkpoint TEXT NOT NULL,
  imported_at TEXT NOT NULL,
  compatibility_version TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('STAGING','VERIFIED','ACTIVE','REPLACED','REJECTED')),
  payload_json TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS operational_snapshot_state (
  snapshot_version TEXT PRIMARY KEY,
  source_checkpoint TEXT NOT NULL,
  imported_at TEXT NOT NULL,
  compatibility_version TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('STAGING','VERIFIED','ACTIVE','REPLACED','REJECTED')),
  payload_json TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS module_current_state (
  state_key TEXT PRIMARY KEY,
  module_id TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  entity_version INTEGER NOT NULL CHECK (entity_version >= 1),
  state_json TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  UNIQUE(module_id, entity_type, entity_id)
);

CREATE TABLE IF NOT EXISTS edge_events (
  event_id TEXT PRIMARY KEY,
  request_id TEXT NOT NULL,
  idempotency_key TEXT NOT NULL,
  environment TEXT NOT NULL,
  cluster_id TEXT NOT NULL,
  device_id TEXT,
  device_seq INTEGER,
  edge_instance_id TEXT NOT NULL,
  edge_epoch TEXT NOT NULL,
  command_code TEXT NOT NULL,
  event_code TEXT NOT NULL,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  base_version INTEGER,
  resulting_version INTEGER,
  payload_json TEXT NOT NULL,
  payload_hash TEXT NOT NULL,
  accepted_at TEXT NOT NULL,
  authority_snapshot_version TEXT NOT NULL,
  FOREIGN KEY(authority_snapshot_version) REFERENCES authority_snapshots(authority_version) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_edge_event_idempotency
ON edge_events(environment, cluster_id, idempotency_key);

CREATE UNIQUE INDEX IF NOT EXISTS ux_edge_event_device_sequence
ON edge_events(device_id, device_seq)
WHERE device_id IS NOT NULL AND device_seq IS NOT NULL;

CREATE TRIGGER IF NOT EXISTS trg_edge_events_no_update
BEFORE UPDATE ON edge_events
BEGIN SELECT RAISE(ABORT, 'edge_events are immutable'); END;

CREATE TRIGGER IF NOT EXISTS trg_edge_events_no_delete
BEFORE DELETE ON edge_events
BEGIN SELECT RAISE(ABORT, 'edge_events are immutable'); END;

CREATE TABLE IF NOT EXISTS edge_reconciliation_state (
  event_id TEXT PRIMARY KEY,
  state TEXT NOT NULL DEFAULT 'PENDING' CHECK (state IN ('PENDING','SYNCHRONIZING','RECONCILED','CONFLICT','REVIEW_REQUIRED')),
  canonical_event_id TEXT,
  canonical_committed_at TEXT,
  last_attempt_at TEXT,
  last_error_code TEXT,
  updated_at TEXT NOT NULL,
  FOREIGN KEY(event_id) REFERENCES edge_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_edge_reconciliation_state
ON edge_reconciliation_state(state, updated_at);

CREATE TABLE IF NOT EXISTS cloud_sync_outbox (
  outbox_id TEXT PRIMARY KEY,
  event_id TEXT NOT NULL UNIQUE,
  state TEXT NOT NULL DEFAULT 'PENDING' CHECK (state IN ('PENDING','SYNCHRONIZING','RETRY_WAIT','RECONCILED','CONFLICT','REVIEW_REQUIRED')),
  attempt_count INTEGER NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
  next_attempt_at TEXT,
  last_attempt_at TEXT,
  last_error_code TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  FOREIGN KEY(event_id) REFERENCES edge_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_cloud_sync_due
ON cloud_sync_outbox(state, next_attempt_at, created_at);

CREATE TABLE IF NOT EXISTS google_projection_outbox (
  projection_id TEXT PRIMARY KEY,
  event_id TEXT NOT NULL,
  projection_key TEXT NOT NULL UNIQUE,
  target_key TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  state TEXT NOT NULL DEFAULT 'PENDING' CHECK (state IN ('PENDING','SENDING','RETRY_WAIT','COMPLETED','REVIEW_REQUIRED')),
  attempt_count INTEGER NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
  next_attempt_at TEXT,
  last_error_code TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  FOREIGN KEY(event_id) REFERENCES edge_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_google_projection_due
ON google_projection_outbox(state, next_attempt_at, created_at);

CREATE TABLE IF NOT EXISTS drive_upload_outbox (
  upload_id TEXT PRIMARY KEY,
  event_id TEXT,
  logical_file_key TEXT NOT NULL UNIQUE,
  local_path TEXT NOT NULL,
  content_hash TEXT NOT NULL,
  content_type TEXT,
  size_bytes INTEGER NOT NULL CHECK (size_bytes >= 0),
  state TEXT NOT NULL DEFAULT 'PENDING' CHECK (state IN ('PENDING','UPLOADING','RETRY_WAIT','COMPLETED','REVIEW_REQUIRED')),
  attempt_count INTEGER NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
  next_attempt_at TEXT,
  last_error_code TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  FOREIGN KEY(event_id) REFERENCES edge_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_drive_upload_due
ON drive_upload_outbox(state, next_attempt_at, created_at);

CREATE TABLE IF NOT EXISTS integration_receipts (
  receipt_id TEXT PRIMARY KEY,
  logical_key TEXT NOT NULL,
  target_kind TEXT NOT NULL CHECK (target_kind IN ('GOOGLE_SHEETS','GOOGLE_DRIVE','CLOUD_CANONICAL')),
  provider_object_id TEXT,
  content_hash TEXT,
  checkpoint TEXT,
  readback_evidence_json TEXT NOT NULL DEFAULT '{}',
  producer_edge_instance_id TEXT NOT NULL,
  producer_edge_epoch TEXT NOT NULL,
  completed_at TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'COMPLETED' CHECK (status IN ('COMPLETED','REVIEW_REQUIRED')),
  UNIQUE(target_kind, logical_key)
);

CREATE TABLE IF NOT EXISTS edge_conflicts (
  conflict_id TEXT PRIMARY KEY,
  event_id TEXT NOT NULL,
  conflict_code TEXT NOT NULL,
  context_json TEXT NOT NULL,
  state TEXT NOT NULL DEFAULT 'OPEN' CHECK (state IN ('OPEN','REVIEW_REQUIRED','RESOLVED')),
  detected_at TEXT NOT NULL,
  resolved_at TEXT,
  resolver_user_id TEXT,
  canonical_resolution_event_id TEXT,
  FOREIGN KEY(event_id) REFERENCES edge_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_edge_conflict_state
ON edge_conflicts(state, detected_at);
""";
}

internal sealed record EdgeStoreStatus(
    string Readiness,
    string? AuthoritySnapshotVersion,
    string? OperationalSnapshotVersion,
    DateTimeOffset? LastCloudSyncAt,
    long PendingCloudSync,
    long PendingGoogleWork,
    long ConflictCount);

internal sealed record EdgeStoreIntegrity(
    bool ForeignKeysOk,
    bool QuickCheckOk)
{
    public bool Ok => ForeignKeysOk && QuickCheckOk;
}
