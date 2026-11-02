using System.Data;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Data;
using FreshMilk.API.Repositories.Interfaces;

namespace FreshMilk.API.Repositories;

public sealed class ExpoPushTokenRepository : IExpoPushTokenRepository
{
    private readonly string _connectionString;

    public ExpoPushTokenRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    private SqlCommand CreateCommand(
        SqlConnection connection, string action, long userId,
        string userRole, string? expoPushToken)
    {
        var command = new SqlCommand(
            StoredProcedure.Notifications.ManagePushTokens, connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.Add("@Action", SqlDbType.NVarChar, 20).Value = action;
        command.Parameters.Add("@ActorUserId", SqlDbType.BigInt).Value = userId;
        command.Parameters.Add("@ActorRole", SqlDbType.NVarChar, 20).Value = userRole;
        command.Parameters.Add("@ExpoPushToken", SqlDbType.NVarChar, 255).Value =
            (object?)expoPushToken ?? DBNull.Value;
        return command;
    }

    private async Task<List<string>> QueryAsync(
        string action, long userId, string userRole, string? expoPushToken,
        CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(
            connection, action, userId, userRole, expoPushToken);
        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var tokens = new List<string>();
        while (await reader.ReadAsync(ct))
            tokens.Add(reader.GetString(reader.GetOrdinal("ExpoPushToken")));
        return tokens;
    }

    public async Task RegisterAsync(
        long userId, string userRole, string expoPushToken, CancellationToken ct) =>
        _ = await QueryAsync("REGISTER", userId, userRole, expoPushToken, ct);

    public async Task UnregisterAsync(
        long userId, string userRole, string expoPushToken, CancellationToken ct) =>
        _ = await QueryAsync("UNREGISTER", userId, userRole, expoPushToken, ct);

    public Task<List<string>> GetForUserAsync(
        long userId, string userRole, CancellationToken ct) =>
        QueryAsync("GETMINE", userId, userRole, null, ct);
}
