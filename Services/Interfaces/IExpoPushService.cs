using FreshMilk.API.Responses.Notifications;

namespace FreshMilk.API.Services.Interfaces;

public interface IExpoPushService
{
    Task RegisterAsync(
        long userId, string userRole, string expoPushToken, CancellationToken ct);

    Task UnregisterAsync(
        long userId, string userRole, string expoPushToken, CancellationToken ct);

    Task SendAsync(
        AppNotificationResponse notification, string userRole, CancellationToken ct);
}
