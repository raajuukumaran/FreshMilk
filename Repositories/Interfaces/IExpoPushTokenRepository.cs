namespace FreshMilk.API.Repositories.Interfaces;

public interface IExpoPushTokenRepository
{
    Task RegisterAsync(
        long userId, string userRole, string expoPushToken, CancellationToken ct);

    Task UnregisterAsync(
        long userId, string userRole, string expoPushToken, CancellationToken ct);

    Task<List<string>> GetForUserAsync(
        long userId, string userRole, CancellationToken ct);
}
