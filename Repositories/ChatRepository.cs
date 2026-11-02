using System.Data;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Data;
using FreshMilk.API.Models.Chat;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Responses.Chat;

namespace FreshMilk.API.Repositories;

public sealed class ChatRepository : IChatRepository
{
    private readonly string _connectionString;

    public ChatRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    private SqlCommand CreateCommand(
        SqlConnection connection, string action, long actorUserId, string actorRole,
        long? threadId = null, long? participantUserId = null,
        string? participantRole = null, string? messageText = null,
        long? relatedSupplyId = null)
    {
        var command = new SqlCommand(StoredProcedure.Chat.Manage, connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@Action", SqlDbType.NVarChar, 20).Value = action;
        command.Parameters.Add("@ActorUserId", SqlDbType.BigInt).Value = actorUserId;
        command.Parameters.Add("@ActorRole", SqlDbType.NVarChar, 20).Value = actorRole;
        command.Parameters.Add("@ThreadId", SqlDbType.BigInt).Value =
            (object?)threadId ?? DBNull.Value;
        command.Parameters.Add("@ParticipantUserId", SqlDbType.BigInt).Value =
            (object?)participantUserId ?? DBNull.Value;
        command.Parameters.Add("@ParticipantRole", SqlDbType.NVarChar, 20).Value =
            (object?)participantRole ?? DBNull.Value;
        command.Parameters.Add("@MessageText", SqlDbType.NVarChar, 2000).Value =
            (object?)messageText ?? DBNull.Value;
        command.Parameters.Add("@RelatedSupplyId", SqlDbType.BigInt).Value =
            (object?)relatedSupplyId ?? DBNull.Value;
        return command;
    }

    private async Task<List<T>> QueryAsync<T>(
        string action, long actorUserId, string actorRole,
        Func<SqlDataReader, T> map, CancellationToken ct,
        long? threadId = null, long? participantUserId = null,
        string? participantRole = null, string? messageText = null,
        long? relatedSupplyId = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(
            connection, action, actorUserId, actorRole, threadId,
            participantUserId, participantRole, messageText, relatedSupplyId);
        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<T>();
        while (await reader.ReadAsync(ct)) rows.Add(map(reader));
        return rows;
    }

    private static ChatThreadResponse MapThread(SqlDataReader reader) => new()
    {
        ThreadId = reader.GetInt64(reader.GetOrdinal("ThreadId")),
        ParticipantUserId = reader.GetInt64(reader.GetOrdinal("ParticipantUserId")),
        ParticipantName = reader.IsDBNull(reader.GetOrdinal("ParticipantName"))
            ? "User" : reader.GetString(reader.GetOrdinal("ParticipantName")),
        ParticipantRole = reader.GetString(reader.GetOrdinal("ParticipantRole")),
        ThreadKind = reader.GetString(reader.GetOrdinal("ThreadKind")),
        RelatedSupplyId = ReadNullableLong(reader, "RelatedSupplyId"),
        AccessStartsAt = ReadNullableDateTimeIfPresent(reader, "AccessStartsAt"),
        AccessEndsAt = ReadNullableDateTimeIfPresent(reader, "AccessEndsAt"),
        CanSend = reader.GetBoolean(reader.GetOrdinal("CanSend")),
        LastMessage = reader.IsDBNull(reader.GetOrdinal("LastMessage"))
            ? null : reader.GetString(reader.GetOrdinal("LastMessage")),
        LastMessageAt = ReadNullableDate(reader, "LastMessageAt"),
        UnreadCount = reader.GetInt32(reader.GetOrdinal("UnreadCount")),
    };

    private static ChatMessageResponse MapMessage(SqlDataReader reader) => new()
    {
        MessageId = reader.GetInt64(reader.GetOrdinal("MessageId")),
        ThreadId = reader.GetInt64(reader.GetOrdinal("ThreadId")),
        SenderUserId = reader.GetInt64(reader.GetOrdinal("SenderUserId")),
        SenderName = reader.IsDBNull(reader.GetOrdinal("SenderName"))
            ? "User" : reader.GetString(reader.GetOrdinal("SenderName")),
        RecipientUserId = reader.GetInt64(reader.GetOrdinal("RecipientUserId")),
        OwnerUserId = reader.GetInt64(reader.GetOrdinal("OwnerUserId")),
        ThreadKind = reader.GetString(reader.GetOrdinal("ThreadKind")),
        MessageText = reader.GetString(reader.GetOrdinal("MessageText")),
        IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        ReadAt = ReadNullableDate(reader, "ReadAt"),
    };

    private static DateTime? ReadNullableDate(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static long? ReadNullableLong(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static DateTime? ReadNullableDateTime(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static DateTime? ReadNullableDateTimeIfPresent(
        SqlDataReader reader, string name)
    {
        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            if (string.Equals(reader.GetName(ordinal), name, StringComparison.OrdinalIgnoreCase))
            {
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
        }

        return null;
    }

    private static ChatAccessRequestResponse MapRequest(SqlDataReader reader) => new()
    {
        RequestId = reader.GetInt64(reader.GetOrdinal("RequestId")),
        RequesterUserId = reader.GetInt64(reader.GetOrdinal("RequesterUserId")),
        RequesterName = reader.GetString(reader.GetOrdinal("RequesterName")),
        RequesterRole = reader.GetString(reader.GetOrdinal("RequesterRole")),
        TargetUserId = reader.GetInt64(reader.GetOrdinal("TargetUserId")),
        TargetName = reader.GetString(reader.GetOrdinal("TargetName")),
        TargetRole = reader.GetString(reader.GetOrdinal("TargetRole")),
        RelatedSupplyId = ReadNullableLong(reader, "RelatedSupplyId"),
        AccessStartsAt = ReadNullableDateTimeIfPresent(reader, "AccessStartsAt"),
        AccessEndsAt = ReadNullableDateTimeIfPresent(reader, "AccessEndsAt"),
        RequestNote = reader.IsDBNull(reader.GetOrdinal("RequestNote"))
            ? null : reader.GetString(reader.GetOrdinal("RequestNote")),
        RequestStatus = reader.GetString(reader.GetOrdinal("RequestStatus")),
        ThreadId = ReadNullableLong(reader, "ThreadId"),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        ReviewedByOwnerId = ReadNullableLong(reader, "ReviewedByOwnerId"),
        ReviewedAt = ReadNullableDateTime(reader, "ReviewedAt"),
    };

    private SqlCommand CreateRequestCommand(
        SqlConnection connection, string action,
        long actorUserId, string actorRole,
        long? requestId = null, RequestChatAccessRequest? request = null,
        DateOnly? accessStartDate = null, DateOnly? accessEndDate = null)
    {
        var command = new SqlCommand(StoredProcedure.Chat.ManageRequests, connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@Action", SqlDbType.NVarChar, 20).Value = action;
        command.Parameters.Add("@ActorUserId", SqlDbType.BigInt).Value = actorUserId;
        command.Parameters.Add("@ActorRole", SqlDbType.NVarChar, 20).Value = actorRole;
        command.Parameters.Add("@RequestId", SqlDbType.BigInt).Value =
            (object?)requestId ?? DBNull.Value;
        command.Parameters.Add("@TargetUserId", SqlDbType.BigInt).Value =
            request is null ? DBNull.Value : request.TargetUserId;
        command.Parameters.Add("@TargetRole", SqlDbType.NVarChar, 20).Value =
            (object?)request?.TargetRole ?? DBNull.Value;
        command.Parameters.Add("@RelatedSupplyId", SqlDbType.BigInt).Value =
            (object?)request?.RelatedSupplyId ?? DBNull.Value;
        command.Parameters.Add("@RequestNote", SqlDbType.NVarChar, 500).Value =
            (object?)request?.RequestNote ?? DBNull.Value;
        command.Parameters.Add("@AccessStartDate", SqlDbType.Date).Value =
            accessStartDate.HasValue
                ? accessStartDate.Value.ToDateTime(TimeOnly.MinValue)
                : DBNull.Value;
        command.Parameters.Add("@AccessEndDate", SqlDbType.Date).Value =
            accessEndDate.HasValue
                ? accessEndDate.Value.ToDateTime(TimeOnly.MinValue)
                : DBNull.Value;
        return command;
    }

    private async Task<List<ChatAccessRequestResponse>> QueryRequestsAsync(
        string action, long actorUserId, string actorRole, CancellationToken ct,
        long? requestId = null, RequestChatAccessRequest? request = null,
        DateOnly? accessStartDate = null, DateOnly? accessEndDate = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateRequestCommand(
            connection, action, actorUserId, actorRole, requestId, request,
            accessStartDate, accessEndDate);
        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<ChatAccessRequestResponse>();
        while (await reader.ReadAsync(ct)) rows.Add(MapRequest(reader));
        return rows;
    }

    public Task<List<ChatThreadResponse>> GetThreadsAsync(
        long actorUserId, string actorRole, CancellationToken ct) =>
        QueryAsync("GETTHREADS", actorUserId, actorRole, MapThread, ct);

    public async Task<ChatThreadResponse?> OpenThreadAsync(
        long actorUserId, string actorRole, string participantRole, long participantUserId,
        long? relatedSupplyId, CancellationToken ct) =>
        (await QueryAsync("GETORCREATE", actorUserId, actorRole, MapThread, ct,
            participantUserId: participantUserId, participantRole: participantRole,
            relatedSupplyId: relatedSupplyId)).FirstOrDefault();

    public Task<List<ChatAccessRequestResponse>> GetRequestsAsync(
        long actorUserId, string actorRole, CancellationToken ct) =>
        QueryRequestsAsync("GETMINE", actorUserId, actorRole, ct);

    public async Task<ChatAccessRequestResponse?> RequestAccessAsync(
        RequestChatAccessRequest request, long actorUserId, string actorRole,
        CancellationToken ct) =>
        (await QueryRequestsAsync("REQUEST", actorUserId, actorRole, ct,
            request: request)).FirstOrDefault();

    public async Task<ChatAccessRequestResponse?> RespondToRequestAsync(
        long requestId, bool accept, long actorUserId, string actorRole,
        CancellationToken ct) =>
        (await QueryRequestsAsync(accept ? "TARGETACCEPT" : "TARGETREJECT",
            actorUserId, actorRole, ct, requestId: requestId)).FirstOrDefault();

    public async Task<ChatAccessRequestResponse?> ReviewRequestAsync(
        long requestId, bool approve, DateOnly? accessStartDate,
        DateOnly? accessEndDate,
        long actorUserId, string actorRole,
        CancellationToken ct) =>
        (await QueryRequestsAsync(approve ? "APPROVE" : "REJECT",
            actorUserId, actorRole, ct, requestId: requestId,
            accessStartDate: accessStartDate,
            accessEndDate: accessEndDate)).FirstOrDefault();

    public Task<List<ChatMessageResponse>> GetMessagesAsync(
        long actorUserId, string actorRole, long threadId, CancellationToken ct) =>
        QueryAsync("GETMESSAGES", actorUserId, actorRole, MapMessage, ct, threadId);

    public async Task<ChatMessageResponse?> SendMessageAsync(
        long actorUserId, string actorRole, long threadId, string messageText,
        CancellationToken ct) =>
        (await QueryAsync("SEND", actorUserId, actorRole, MapMessage, ct,
            threadId, messageText: messageText)).FirstOrDefault();

    public async Task<int> MarkReadAsync(
        long actorUserId, string actorRole, long threadId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(
            connection, "MARKREAD", actorUserId, actorRole, threadId);
        await connection.OpenAsync(ct);
        var result = await command.ExecuteScalarAsync(ct);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }
}