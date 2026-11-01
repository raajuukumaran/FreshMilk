using Microsoft.AspNetCore.Mvc;
using FreshMilk.API.Responses;
using FreshMilk.API.Responses.Notifications;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly IAppNotificationsService _service;

    public NotificationsController(IAppNotificationsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AppNotificationResponse>>>> GetMine(
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var notifications = await _service.GetMineAsync(actorUserId, actorRole, ct);
        return Ok(ApiResponse<List<AppNotificationResponse>>.Ok(
            notifications, count: notifications.Count));
    }

    [HttpPatch("{id:long}/read")]
    public async Task<ActionResult<ApiResponse<int>>> MarkRead(
        long id,
        [FromQuery] long actorUserId,
        [FromQuery] string actorRole,
        CancellationToken ct)
    {
        var changed = await _service.MarkReadAsync(id, actorUserId, actorRole, ct);
        return changed == 0
            ? NotFound(ApiResponse<int>.Fail("Notification not found."))
            : Ok(ApiResponse<int>.Ok(changed, "Notification marked as read."));
    }
}