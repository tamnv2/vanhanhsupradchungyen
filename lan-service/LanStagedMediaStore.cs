using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class LanStagedMediaStore
{
    private readonly string _connectionString;
    private readonly string _stagingRoot;

    public LanStagedMediaStore(string databasePath, string stagingRoot)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        if (string.IsNullOrWhiteSpace(stagingRoot)) throw new ArgumentException("Staging root is required", nameof(stagingRoot));

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
        _stagingRoot = Path.GetFullPath(stagingRoot);
        Directory.CreateDirectory(_stagingRoot);
    }

    public static async Task EnsureAsync(
        string databasePath,
        string stagingRoot,
        CancellationToken cancellationToken = default)
    {
        var store = new LanStagedMediaStore(databasePath, stagingRoot);
        await using var connection = await store.OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
    }

    public async Task<LanStagedMediaResult> StageAsync(
        string logicalFileKey,
        Stream content,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        RequireBounded(logicalFileKey, "LOGICAL_FILE_KEY_REQUIRED", 1, 360);
        if (content is null) throw new LanStagedMediaException("STAGED_MEDIA_CONTENT_REQUIRED", "Media content is required.");
        if (!content.CanRead) throw new LanStagedMediaException("STAGED_MEDIA_CONTENT_UNREADABLE", "Media content stream is not readable.");
        if (contentType is not null) RequireBounded(contentType, "CONTENT_TYPE_INVALID", 1, 240);

        var finalPath = BuildTrustedPath(logicalFileKey);
        var tempPath = finalPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        string hash;
        long size;
        var createdFinalFile = false;

        try
        {
            (hash, size) = await WriteDurableTempAsync(content, tempPath, cancellationToken);

            await using var connection = await OpenAsync(cancellationToken);
            await EnsureSchemaAsync(connection, cancellationToken);
            using var transaction = connection.BeginTransaction();
            try
            {
                var existing = await ReadRowAsync(connection, transaction, logicalFileKey, cancellationToken);
                if (existing is not null)
                {
                    if (!string.Equals(existing.ContentHash, hash, StringComparison.Ordinal) ||
                        existing.SizeBytes != size ||
                        !string.Equals(existing.ContentType, contentType, StringComparison.Ordinal))
                    {
                        throw new LanStagedMediaException(
                            "STAGED_MEDIA_IDENTITY_CONFLICT",
                            "The logical file key already belongs to different staged media evidence.");
                    }

                    await VerifyRowFileAsync(existing, cancellationToken);
                    transaction.Commit();
                    SafeDelete(tempPath);
                    return ToResult(existing, alreadyStaged: true);
                }

                if (File.Exists(finalPath))
                {
                    var orphanEvidence = await HashFileAsync(finalPath, cancellationToken);
                    if (!string.Equals(orphanEvidence.Hash, hash, StringComparison.Ordinal) || orphanEvidence.Size != size)
                    {
                        throw new LanStagedMediaException(
                            "STAGED_MEDIA_PATH_CONFLICT",
                            "The trusted staging path already contains different media evidence.");
                    }
                    SafeDelete(tempPath);
                }
                else
                {
                    File.Move(tempPath, finalPath, overwrite: false);
                    createdFinalFile = true;
                }

                var now = DateTimeOffset.UtcNow.ToString("O");
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO edge_staged_media(
                      logical_file_key, local_path, content_hash, content_type,
                      size_bytes, state, linked_event_id, created_at, updated_at
                    ) VALUES (
                      $logicalKey, $localPath, $hash, $contentType,
                      $sizeBytes, 'STAGED', NULL, $now, $now
                    )
                    """;
                insert.Parameters.AddWithValue("$logicalKey", logicalFileKey);
                insert.Parameters.AddWithValue("$localPath", finalPath);
                insert.Parameters.AddWithValue("$hash", hash);
                insert.Parameters.AddWithValue("$contentType", (object?)contentType ?? DBNull.Value);
                insert.Parameters.AddWithValue("$sizeBytes", size);
                insert.Parameters.AddWithValue("$now", now);
                await insert.ExecuteNonQueryAsync(cancellationToken);
                transaction.Commit();

                return new LanStagedMediaResult(
                    logicalFileKey,
                    finalPath,
                    hash,
                    contentType,
                    size,
                    "STAGED",
                    null,
                    AlreadyStaged: false);
            }
            catch
            {
                transaction.Rollback();
                if (createdFinalFile) SafeDelete(finalPath);
                throw;
            }
        }
        finally
        {
            SafeDelete(tempPath);
        }
    }

    public async Task<LanStagedMediaResult> ReadVerifiedAsync(
        string logicalFileKey,
        CancellationToken cancellationToken = default)
    {
        RequireBounded(logicalFileKey, "LOGICAL_FILE_KEY_REQUIRED", 1, 360);
        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        var row = await ReadRowAsync(connection, transaction: null, logicalFileKey, cancellationToken)
            ?? throw new LanStagedMediaException("STAGED_MEDIA_NOT_FOUND", "Staged media was not found.");
        await VerifyRowFileAsync(row, cancellationToken);
        return ToResult(row, alreadyStaged: true);
    }

    public async Task<LanDriveUploadWork> CreateDriveUploadWorkAsync(
        string logicalFileKey,
        CancellationToken cancellationToken = default)
    {
        var verified = await ReadVerifiedAsync(logicalFileKey, cancellationToken);
        if (verified.State is not ("STAGED" or "QUEUED"))
        {
            throw new LanStagedMediaException(
                "STAGED_MEDIA_STATE_INVALID",
                $"Staged media is not available for Drive work in state {verified.State}.");
        }

        return new LanDriveUploadWork(
            verified.LogicalFileKey,
            verified.LocalPath,
            verified.ContentHash,
            verified.ContentType,
            verified.SizeBytes);
    }

    public async Task<LanStagedMediaInspection> InspectAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              COUNT(*),
              COALESCE(SUM(CASE WHEN state='STAGED' THEN 1 ELSE 0 END),0),
              COALESCE(SUM(CASE WHEN state='QUEUED' THEN 1 ELSE 0 END),0),
              COALESCE(SUM(CASE WHEN state='COMPLETED' THEN 1 ELSE 0 END),0),
              COALESCE(SUM(CASE WHEN state='REVIEW_REQUIRED' THEN 1 ELSE 0 END),0)
            FROM edge_staged_media
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new LanStagedMediaInspection(
            Total: reader.GetInt64(0),
            Staged: reader.GetInt64(1),
            Queued: reader.GetInt64(2),
            Completed: reader.GetInt64(3),
            ReviewRequired: reader.GetInt64(4));
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

    private string BuildTrustedPath(string logicalFileKey)
    {
        var fileName = Sha256Hex(logicalFileKey) + ".media";
        var path = Path.GetFullPath(Path.Combine(_stagingRoot, fileName));
        EnsurePathInsideRoot(path);
        return path;
    }

    private void EnsurePathInsideRoot(string path)
    {
        var root = _stagingRoot.EndsWith(Path.DirectorySeparatorChar)
            ? _stagingRoot
            : _stagingRoot + Path.DirectorySeparatorChar;
        if (!path.StartsWith(root, StringComparison.Ordinal))
            throw new LanStagedMediaException("STAGED_MEDIA_PATH_INVALID", "Staged media path escaped the trusted staging root.");
    }

    private async Task VerifyRowFileAsync(StagedMediaRow row, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(row.LocalPath);
        EnsurePathInsideRoot(fullPath);
        if (!File.Exists(fullPath))
            throw new LanStagedMediaException("STAGED_MEDIA_FILE_MISSING", "Staged media metadata exists but its local file is missing.");

        var evidence = await HashFileAsync(fullPath, cancellationToken);
        if (!string.Equals(evidence.Hash, row.ContentHash, StringComparison.Ordinal) || evidence.Size != row.SizeBytes)
        {
            throw new LanStagedMediaException(
                "STAGED_MEDIA_FILE_MISMATCH",
                "Staged media file hash/size no longer matches durable metadata.");
        }
    }

    private static async Task<(string Hash, long Size)> WriteDurableTempAsync(
        Stream content,
        string path,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var incremental = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long size = 0;
        var buffer = new byte[128 * 1024];

        await using var destination = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        while (true)
        {
            var read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0) break;
            incremental.AppendData(buffer, 0, read);
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            size = checked(size + read);
        }

        await destination.FlushAsync(cancellationToken);
        destination.Flush(flushToDisk: true);
        var hash = Convert.ToHexString(incremental.GetHashAndReset()).ToLowerInvariant();
        return (hash, size);
    }

    private static async Task<(string Hash, long Size)> HashFileAsync(
        string path,
        CancellationToken cancellationToken)
    {
        using var incremental = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long size = 0;
        var buffer = new byte[128 * 1024];
        await using var source = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0) break;
            incremental.AppendData(buffer, 0, read);
            size = checked(size + read);
        }
        return (Convert.ToHexString(incremental.GetHashAndReset()).ToLowerInvariant(), size);
    }

    private static async Task<StagedMediaRow?> ReadRowAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string logicalFileKey,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT logical_file_key, local_path, content_hash, content_type,
                   size_bytes, state, linked_event_id
            FROM edge_staged_media
            WHERE logical_file_key=$logicalKey
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$logicalKey", logicalFileKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new StagedMediaRow(
            LogicalFileKey: reader.GetString(0),
            LocalPath: reader.GetString(1),
            ContentHash: reader.GetString(2),
            ContentType: reader.IsDBNull(3) ? null : reader.GetString(3),
            SizeBytes: reader.GetInt64(4),
            State: reader.GetString(5),
            LinkedEventId: reader.IsDBNull(6) ? null : reader.GetString(6));
    }

    private static LanStagedMediaResult ToResult(StagedMediaRow row, bool alreadyStaged) =>
        new(
            row.LogicalFileKey,
            row.LocalPath,
            row.ContentHash,
            row.ContentType,
            row.SizeBytes,
            row.State,
            row.LinkedEventId,
            alreadyStaged);

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SchemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Cleanup failure must not mask the authoritative staging/database result.
        }
    }

    private static void RequireBounded(string? value, string code, int min, int max)
    {
        var length = value?.Length ?? 0;
        if (length < min || length > max) throw new LanStagedMediaException(code, code);
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private const string SchemaSql = """
CREATE TABLE IF NOT EXISTS edge_staged_media (
  logical_file_key TEXT PRIMARY KEY,
  local_path TEXT NOT NULL UNIQUE,
  content_hash TEXT NOT NULL,
  content_type TEXT,
  size_bytes INTEGER NOT NULL CHECK (size_bytes >= 0),
  state TEXT NOT NULL DEFAULT 'STAGED' CHECK (state IN ('STAGED','QUEUED','COMPLETED','REVIEW_REQUIRED')),
  linked_event_id TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  FOREIGN KEY(linked_event_id) REFERENCES edge_events(event_id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_edge_staged_media_state
ON edge_staged_media(state, updated_at);

CREATE TRIGGER IF NOT EXISTS trg_edge_staged_media_evidence_immutable
BEFORE UPDATE OF logical_file_key, local_path, content_hash, content_type, size_bytes
ON edge_staged_media
BEGIN SELECT RAISE(ABORT, 'staged media evidence is immutable'); END;

CREATE TRIGGER IF NOT EXISTS trg_drive_upload_staged_media_mismatch
BEFORE INSERT ON drive_upload_outbox
WHEN EXISTS (
  SELECT 1 FROM edge_staged_media
  WHERE logical_file_key=NEW.logical_file_key
    AND (
      local_path<>NEW.local_path
      OR content_hash<>NEW.content_hash
      OR size_bytes<>NEW.size_bytes
      OR COALESCE(content_type,'')<>COALESCE(NEW.content_type,'')
    )
)
BEGIN SELECT RAISE(ABORT, 'VHDCHY_STAGED_MEDIA_EVIDENCE_MISMATCH'); END;

CREATE TRIGGER IF NOT EXISTS trg_drive_upload_staged_media_link
AFTER INSERT ON drive_upload_outbox
WHEN NEW.event_id IS NOT NULL
  AND EXISTS (
    SELECT 1 FROM edge_staged_media
    WHERE logical_file_key=NEW.logical_file_key
      AND local_path=NEW.local_path
      AND content_hash=NEW.content_hash
      AND size_bytes=NEW.size_bytes
      AND COALESCE(content_type,'')=COALESCE(NEW.content_type,'')
  )
BEGIN
  UPDATE edge_staged_media
  SET state='QUEUED', linked_event_id=NEW.event_id, updated_at=NEW.updated_at
  WHERE logical_file_key=NEW.logical_file_key;
END;

CREATE TRIGGER IF NOT EXISTS trg_drive_upload_staged_media_completed
AFTER UPDATE OF state ON drive_upload_outbox
WHEN NEW.state='COMPLETED'
BEGIN
  UPDATE edge_staged_media
  SET state='COMPLETED', updated_at=NEW.updated_at
  WHERE logical_file_key=NEW.logical_file_key
    AND linked_event_id=NEW.event_id;
END;
""";

    private sealed record StagedMediaRow(
        string LogicalFileKey,
        string LocalPath,
        string ContentHash,
        string? ContentType,
        long SizeBytes,
        string State,
        string? LinkedEventId);
}

public sealed record LanStagedMediaResult(
    string LogicalFileKey,
    string LocalPath,
    string ContentHash,
    string? ContentType,
    long SizeBytes,
    string State,
    string? LinkedEventId,
    bool AlreadyStaged);

public sealed record LanStagedMediaInspection(
    long Total,
    long Staged,
    long Queued,
    long Completed,
    long ReviewRequired);

public sealed class LanStagedMediaException : InvalidOperationException
{
    public LanStagedMediaException(string code, string message) : base(message) => Code = code;
    public LanStagedMediaException(string code, string message, Exception innerException) : base(message, innerException) => Code = code;
    public string Code { get; }
}
