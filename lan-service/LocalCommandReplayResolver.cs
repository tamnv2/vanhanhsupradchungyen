using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Vhdchy.LanService;

public sealed class LocalCommandReplayResolver
{
    private readonly string _connectionString;
    private readonly string _expectedEnvironment;
    private readonly string _expectedClusterId;

    public LocalCommandReplayResolver(string databasePath, string expectedEnvironment, string expectedClusterId)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required", nameof(databasePath));
        if (string.IsNullOrWhiteSpace(expectedEnvironment)) throw new ArgumentException("Environment is required", nameof(expectedEnvironment));
        if (string.IsNullOrWhiteSpace(expectedClusterId)) throw new ArgumentException("Cluster ID is required", nameof(expectedClusterId));

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
        _expectedEnvironment = expectedEnvironment;
        _expectedClusterId = expectedClusterId;
    }

    public async Task<LanLocalCommandResult?> ResolveAsync(
        LanLocalCommandReplayProbe probe,
        CancellationToken cancellationToken = default)
    {
        if (probe is null) throw new ArgumentNullException(nameof(probe));
        Require(probe.IdempotencyKey, "IDEMPOTENCY_KEY_REQUIRED");
        Require(probe.CommandCode, "COMMAND_CODE_REQUIRED");
        Require(probe.EventCode, "EVENT_CODE_REQUIRED");
        Require(probe.EntityType, "ENTITY_TYPE_REQUIRED");
        Require(probe.EntityId, "ENTITY_ID_REQUIRED");
        if ((probe.DeviceId is null) != (probe.DeviceSeq is null))
            throw new LanLocalCommandException("DEVICE_SEQUENCE_IDENTITY_INVALID", "Device ID and device sequence must be supplied together.");

        var payloadHash = Sha256Hex(CanonicalizeJsonObject(probe.PayloadJson));
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT event_id, request_id, device_id, device_seq, command_code, event_code,
                   entity_type, entity_id, base_version, resulting_version, payload_hash,
                   authority_snapshot_version
            FROM edge_events
            WHERE environment=$environment AND cluster_id=$cluster AND idempotency_key=$idempotency
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$environment", _expectedEnvironment);
        command.Parameters.AddWithValue("$cluster", _expectedClusterId);
        command.Parameters.AddWithValue("$idempotency", probe.IdempotencyKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var eventId = reader.GetString(0);
        var requestId = reader.GetString(1);
        var deviceId = reader.IsDBNull(2) ? null : reader.GetString(2);
        long? deviceSeq = reader.IsDBNull(3) ? null : reader.GetInt64(3);
        var commandCode = reader.GetString(4);
        var eventCode = reader.GetString(5);
        var entityType = reader.GetString(6);
        var entityId = reader.GetString(7);
        long? baseVersion = reader.IsDBNull(8) ? null : reader.GetInt64(8);
        var resultingVersion = reader.GetInt64(9);
        var existingPayloadHash = reader.GetString(10);
        var authorityVersion = reader.GetString(11);

        var same =
            string.Equals(commandCode, probe.CommandCode, StringComparison.Ordinal) &&
            string.Equals(eventCode, probe.EventCode, StringComparison.Ordinal) &&
            string.Equals(entityType, probe.EntityType, StringComparison.Ordinal) &&
            string.Equals(entityId, probe.EntityId, StringComparison.Ordinal) &&
            baseVersion == probe.ExpectedBaseVersion &&
            string.Equals(existingPayloadHash, payloadHash, StringComparison.Ordinal) &&
            string.Equals(deviceId, probe.DeviceId, StringComparison.Ordinal) &&
            deviceSeq == probe.DeviceSeq;

        if (!same)
        {
            throw new LanLocalCommandException(
                "IDEMPOTENCY_PAYLOAD_CONFLICT",
                "The idempotency key already belongs to a different logical command or payload.");
        }

        await reader.DisposeAsync();
        var googleStatus = await ReadGoogleOutputStatusAsync(connection, eventId, cancellationToken);
        return new LanLocalCommandResult(
            EventId: eventId,
            RequestId: requestId,
            IdempotencyKey: probe.IdempotencyKey,
            EntityType: entityType,
            EntityId: entityId,
            ResultingVersion: resultingVersion,
            AuthoritySnapshotVersion: authorityVersion,
            CommitStatus: "LAN_ACCEPTED_PENDING_SYNC",
            GoogleOutputStatus: googleStatus,
            AlreadyAccepted: true);
    }

    private static async Task<string> ReadGoogleOutputStatusAsync(
        SqliteConnection connection,
        string eventId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              (SELECT COUNT(*) FROM google_projection_outbox WHERE event_id=$eventId) +
              (SELECT COUNT(*) FROM drive_upload_outbox WHERE event_id=$eventId)
            """;
        command.Parameters.AddWithValue("$eventId", eventId);
        var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        return count > 0 ? "PENDING" : "NOT_REQUIRED";
    }

    private static string CanonicalizeJsonObject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new LanLocalCommandException("COMMAND_PAYLOAD_INVALID", "Command payload is required.");
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new LanLocalCommandException("COMMAND_PAYLOAD_INVALID", "Command payload must be a JSON object.");
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream)) WriteCanonical(document.RootElement, writer);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (LanLocalCommandException)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new LanLocalCommandException("COMMAND_PAYLOAD_INVALID", "Command payload JSON is invalid.", error);
        }
    }

    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                var properties = element.EnumerateObject().ToArray();
                if (properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length)
                    throw new LanLocalCommandException("COMMAND_PAYLOAD_INVALID", "Command payload contains duplicate property names.");
                foreach (var property in properties.OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteCanonical(item, writer);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText(), skipInputValidation: true);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new LanLocalCommandException("COMMAND_PAYLOAD_INVALID", "Unsupported command payload token.");
        }
    }

    private static void Require(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new LanLocalCommandException(code, code);
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed record LanLocalCommandReplayProbe(
    string IdempotencyKey,
    string CommandCode,
    string EventCode,
    string EntityType,
    string EntityId,
    long? ExpectedBaseVersion,
    string PayloadJson,
    string? DeviceId = null,
    long? DeviceSeq = null);
