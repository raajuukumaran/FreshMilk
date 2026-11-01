using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/supplies/notifications")]
public sealed class SupplyNotificationsController : ControllerBase
{
    private readonly string _connectionString;

    public SupplyNotificationsController(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    private static bool IsSupportedRole(string? role)
    {
        return role?.Trim().ToUpperInvariant() is
            "OWNER" or "SUPPLIER" or "CONSUMER";
    }

    private async Task<bool> IsActiveUserAsync(
        SqlConnection connection,
        long userId,
        string role,
        CancellationToken ct)
    {
        await using var command = new SqlCommand("""
            SELECT COUNT_BIG(1)
            FROM dbo.Users
            WHERE UserId = @UserId
              AND IsActive = 1
              AND UPPER(LTRIM(RTRIM(UserRole))) = @Role;
            """, connection);

        command.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;
        command.Parameters.Add("@Role", SqlDbType.NVarChar, 20).Value =
            role.Trim().ToUpperInvariant();

        var result = await command.ExecuteScalarAsync(ct);

        return Convert.ToInt64(result) > 0;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        if (actorUserId <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid user ID."
            });
        }

        if (!IsSupportedRole(actorRole))
        {
            return BadRequest(new
            {
                success = false,
                message = "Role must be Owner, Supplier, or Consumer."
            });
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        if (!await IsActiveUserAsync(
                connection, actorUserId, actorRole, ct))
        {
            return NotFound(new
            {
                success = false,
                message = "Active user not found or role mismatch."
            });
        }

        await using var command = new SqlCommand("""
            SELECT
                NotificationId,
                SupplyId,
                Title,
                Body,
                IsRead,
                CreatedAt
            FROM dbo.SupplyNotifications
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC;
            """, connection);

        command.Parameters.Add("@UserId", SqlDbType.BigInt).Value =
            actorUserId;

        await using var reader = await command.ExecuteReaderAsync(ct);

        var notifications = new List<object>();

        while (await reader.ReadAsync(ct))
        {
            notifications.Add(new
            {
                notificationId = reader.GetInt64(0),
                supplyId = reader.IsDBNull(1)
                    ? (long?)null
                    : reader.GetInt64(1),
                title = reader.GetString(2),
                body = reader.GetString(3),
                isRead = reader.GetBoolean(4),
                createdAt = reader.GetDateTime(5)
            });
        }

        return Ok(new
        {
            success = true,
            count = notifications.Count,
            data = notifications
        });
    }

    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkRead(
        long id,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        if (id <= 0 || actorUserId <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid notification ID or user ID."
            });
        }

        if (!IsSupportedRole(actorRole))
        {
            return BadRequest(new
            {
                success = false,
                message = "Role must be Owner, Supplier, or Consumer."
            });
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        if (!await IsActiveUserAsync(
                connection, actorUserId, actorRole, ct))
        {
            return NotFound(new
            {
                success = false,
                message = "Active user not found or role mismatch."
            });
        }

        await using var command = new SqlCommand("""
            UPDATE dbo.SupplyNotifications
            SET IsRead = 1
            WHERE NotificationId = @NotificationId
              AND UserId = @UserId;

            SELECT @@ROWCOUNT;
            """, connection);

        command.Parameters.Add("@NotificationId", SqlDbType.BigInt)
            .Value = id;

        command.Parameters.Add("@UserId", SqlDbType.BigInt)
            .Value = actorUserId;

        var affected = Convert.ToInt32(
            await command.ExecuteScalarAsync(ct));

        if (affected == 0)
        {
            return NotFound(new
            {
                success = false,
                message = "Notification not found."
            });
        }

        return Ok(new
        {
            success = true,
            message = "Notification marked as read."
        });
    }
}