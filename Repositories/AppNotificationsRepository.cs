using System.Data;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Data;
using FreshMilk.API.Models.Notifications;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Responses.Notifications;

namespace FreshMilk.API.Repositories;

public sealed class AppNotificationsRepository : IAppNotificationsRepository
{
    private readonly string _connectionString;

    public AppNotificationsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    private SqlCommand CreateCommand(
        SqlConnection connection, string action, long actorUserId,
        string actorRole, long? notificationId = null,
        CreateAppNotificationRequest? request = null)
    {
        var command = new SqlCommand(StoredProcedure.Notifications.Manage, connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@Action", SqlDbType.NVarChar, 20).Value = action;
        command.Parameters.Add("@ActorUserId", SqlDbType.BigInt).Value = actorUserId;
        command.Parameters.Add("@ActorRole", SqlDbType.NVarChar, 20).Value = actorRole;
        command.Parameters.Add("@NotificationId", SqlDbType.BigInt).Value =
            (object?)notificationId ?? DBNull.Value;
        command.Parameters.Add("@ToUserId", SqlDbType.BigInt).Value =
            (object?)request?.ToUserId ?? DBNull.Value;
        command.Parameters.Add("@ToRole", SqlDbType.NVarChar, 20).Value =
            (object?)request?.ToRole ?? DBNull.Value;
        command.Parameters.Add("@NotificationType", SqlDbType.NVarChar, 30).Value =
            (object?)request?.NotificationType ?? DBNull.Value;
        command.Parameters.Add("@Title", SqlDbType.NVarChar, 120).Value =
            (object?)request?.Title ?? DBNull.Value;
        command.Parameters.Add("@Body", SqlDbType.NVarChar, 500).Value =
            (object?)request?.Body ?? DBNull.Value;
        command.Parameters.Add("@RelatedSupplyId", SqlDbType.BigInt).Value =
            (object?)request?.RelatedSupplyId ?? DBNull.Value;
        command.Parameters.Add("@RelatedOrderId", SqlDbType.BigInt).Value =
            (object?)request?.RelatedOrderId ?? DBNull.Value;
        command.Parameters.Add("@RelatedThreadId", SqlDbType.BigInt).Value =
            (object?)request?.RelatedThreadId ?? DBNull.Value;
        command.Parameters.Add("@RelatedChatRequestId", SqlDbType.BigInt).Value =
            (object?)request?.RelatedChatRequestId ?? DBNull.Value;
        return command;
    }

    private static AppNotificationResponse Map(SqlDataReader reader) => new()
    {
        NotificationId = reader.GetInt64(reader.GetOrdinal("NotificationId")),
        ToUserId = reader.GetInt64(reader.GetOrdinal("ToUserId")),
        ToRole = reader.GetString(reader.GetOrdinal("ToRole")),
        FromName = reader.GetString(reader.GetOrdinal("FromName")),
        FromRole = reader.GetString(reader.GetOrdinal("FromRole")),
        NotificationType = reader.GetString(reader.GetOrdinal("NotificationType")),
        Title = reader.GetString(reader.GetOrdinal("Title")),
        Body = reader.GetString(reader.GetOrdinal("Body")),
        RelatedSupplyId = ReadNullableLong(reader, "RelatedSupplyId"),
        RelatedOrderId = ReadNullableLong(reader, "RelatedOrderId"),
        RelatedThreadId = ReadNullableLong(reader, "RelatedThreadId"),
        RelatedChatRequestId = ReadNullableLong(reader, "RelatedChatRequestId"),
        IsRead = reader.GetBoolean(reader.GetOrdinal("IsRead")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UnreadCount = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("UnreadCount"))),
    };

    private static long? ReadNullableLong(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private async Task<List<AppNotificationResponse>> QueryAsync(
        string action, long actorUserId, string actorRole,
        CancellationToken ct, long? notificationId = null,
        CreateAppNotificationRequest? request = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(
            connection, action, actorUserId, actorRole, notificationId, request);
        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<AppNotificationResponse>();
        while (await reader.ReadAsync(ct)) rows.Add(Map(reader));
        return rows;
    }

    public Task<List<AppNotificationResponse>> GetMineAsync(
        long actorUserId, string actorRole, CancellationToken ct) =>
        QueryAsync("GETMINE", actorUserId, actorRole, ct);

    public async Task<AppNotificationResponse?> CreateAsync(
        CreateAppNotificationRequest request,
        long actorUserId, string actorRole, CancellationToken ct) =>
        (await QueryAsync("CREATE", actorUserId, actorRole, ct, request: request))
            .FirstOrDefault();

    public async Task<int> MarkReadAsync(
        long notificationId, long actorUserId, string actorRole,
        CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(
            connection, "MARKREAD", actorUserId, actorRole, notificationId);
        await connection.OpenAsync(ct);
        var result = await command.ExecuteScalarAsync(ct);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }
}