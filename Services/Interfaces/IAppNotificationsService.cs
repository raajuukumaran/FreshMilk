using FreshMilk.API.Models.Notifications;
using FreshMilk.API.Responses.Notifications;

namespace FreshMilk.API.Services.Interfaces;

public interface IAppNotificationsService
{
    Task<List<AppNotificationResponse>> GetMineAsync(
        long actorUserId, string actorRole, CancellationToken ct);

    Task<AppNotificationResponse?> CreateAsync(
        CreateAppNotificationRequest request,
        long actorUserId, string actorRole, CancellationToken ct);

    Task<int> MarkReadAsync(
        long notificationId, long actorUserId, string actorRole,
        CancellationToken ct);
}