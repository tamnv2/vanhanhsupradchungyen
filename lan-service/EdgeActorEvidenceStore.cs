using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class EdgeActorEvidenceStore
{
    private readonly string _connectionString;

    public EdgeActorEvidenceStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
    }

    public async Task StageAsync(
        string requestId,
        string idempotencyKey,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        Require(requestId, "REQUEST_ID_REQUIRED");
        Require(idempotencyKey, "IDEMPOTENCY_KEY_REQUIRED");
        Require(actorUserId, "ACTOR_USER_ID_REQUIRED");

        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        using var transaction = connection.BeginTransaction();
        try
        {
            await using var read = connection.CreateCommand();
            read.Transaction = transaction;
            read.CommandText = """
                SELECT actor_user_id
                FROM edge_actor_context
                WHERE request_id=$requestId AND idempotency_key=$idempotency
                LIMIT 1
                """;
            read.Parameters.AddWithValue("$requestId", requestId);
            read.Parameters.AddWithValue("$idempotency", idempotencyKey);
            var existing = await read.ExecuteScalarAsync(cancellationToken);
            if (existing is not null && existing is not DBNull &&
                !string.Equals(Convert.ToString(existing), actorUserId, StringComparison.Ordinal))
            {
                throw new EdgeActorEvidenceException(
                    "ACTOR_CONTEXT_CONFLICT",
                    "The request/idempotency identity is already staged for a different authenticated actor.");
            }

            await using var write = connection.CreateCommand();
            write.Transaction = transaction;
            write.CommandText = """
                INSERT INTO edge_actor_context(request_id, idempotency_key, actor_user_id, staged_at)
                VALUES ($requestId, $idempotency, $actorUserId, $stagedAt)
                ON CONFLICT(request_id, idempotency_key)
                DO UPDATE SET staged_at=excluded.staged_at
                """;
            write.Parameters.AddWithValue("$requestId", requestId);
            write.Parameters.AddWithValue("$idempotency", idempotencyKey);
            write.Parameters.AddWithValue("$actorUserId", actorUserId);
            write.Parameters.AddWithValue("$stagedAt", DateTimeOffset.UtcNow.ToString("O"));
            await write.ExecuteNonQueryAsync(cancellationToken);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<string?> ReadActorAsync(
        string eventId,
        CancellationToken cancellationToken = default)
    {
        Require(eventId, "EVENT_ID_REQUIRED");
        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT actor_user_id FROM edge_event_actor_evidence WHERE event_id=$eventId LIMIT 1";
        command.Parameters.AddWithValue("$eventId", eventId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }

    public async Task CleanupContextAsync(
        string requestId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        Require(requestId, "REQUEST_ID_REQUIRED");
        Require(idempotencyKey, "IDEMPOTENCY_KEY_REQUIRED");
        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM edge_actor_context WHERE request_id=$requestId AND idempotency_key=$idempotency";
        command.Parameters.AddWithValue("$requestId", requestId);
        command.Parameters.AddWithValue("$idempotency", idempotencyKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
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

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS edge_actor_context (
              request_id TEXT NOT NULL,
              idempotency_key TEXT NOT NULL,
              actor_user_id TEXT NOT NULL,
              staged_at TEXT NOT NULL,
              PRIMARY KEY(request_id, idempotency_key)
            );

            CREATE TABLE IF NOT EXISTS edge_event_actor_evidence (
              event_id TEXT PRIMARY KEY,
              actor_user_id TEXT NOT NULL,
              captured_at TEXT NOT NULL,
              FOREIGN KEY(event_id) REFERENCES edge_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
            );

            CREATE TRIGGER IF NOT EXISTS trg_edge_event_actor_capture
            AFTER INSERT ON edge_events
            WHEN EXISTS (
              SELECT 1
              FROM edge_actor_context
              WHERE request_id=NEW.request_id
                AND idempotency_key=NEW.idempotency_key
            )
            BEGIN
              INSERT INTO edge_event_actor_evidence(event_id, actor_user_id, captured_at)
              SELECT NEW.event_id, actor_user_id, NEW.accepted_at
              FROM edge_actor_context
              WHERE request_id=NEW.request_id
                AND idempotency_key=NEW.idempotency_key;
            END;

            CREATE TRIGGER IF NOT EXISTS trg_edge_event_actor_evidence_no_update
            BEFORE UPDATE ON edge_event_actor_evidence
            BEGIN SELECT RAISE(ABORT, 'edge event actor evidence is immutable'); END;

            CREATE TRIGGER IF NOT EXISTS trg_edge_event_actor_evidence_no_delete
            BEFORE DELETE ON edge_event_actor_evidence
            BEGIN SELECT RAISE(ABORT, 'edge event actor evidence is immutable'); END;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Require(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new EdgeActorEvidenceException(code, code);
    }
}

public sealed class EdgeActorEvidenceException : InvalidOperationException
{
    public EdgeActorEvidenceException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}
