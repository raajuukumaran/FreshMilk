using FreshMilk.API.Models.Notifications;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Responses.Notifications;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Services;

public sealed class AppNotificationsService : IAppNotificationsService
{
    private readonly IAppNotificationsRepository _repository;

    public AppNotificationsService(IAppNotificationsRepository repository) =>
        _repository = repository;

    private static string ValidateActor(long actorUserId, string actorRole)
    {
        if (actorUserId <= 0) throw new ArgumentException("Valid actor user ID is required.");
        var role = actorRole.Trim().ToUpperInvariant();
        if (role is not ("OWNER" or "SUPPLIER" or "CONSUMER"))
            throw new ArgumentException("Role must be Owner, Supplier, or Consumer.");
        return role;
    }

    public Task<List<AppNotificationResponse>> GetMineAsync(
        long actorUserId, string actorRole, CancellationToken ct) =>
        _repository.GetMineAsync(actorUserId, ValidateActor(actorUserId, actorRole), ct);

    public Task<AppNotificationResponse?> CreateAsync(
        CreateAppNotificationRequest request,
        long actorUserId, string actorRole, CancellationToken ct)
    {
        var role = ValidateActor(actorUserId, actorRole);
        if (request.ToUserId is <= 0)
            throw new ArgumentException("A valid notification recipient is required.");
        if (request.ToRole.Length > 20 || request.NotificationType.Length is 0 or > 30)
            throw new ArgumentException("Notification recipient role or type is invalid.");
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 120)
            throw new ArgumentException("Notification title is required and limited to 120 characters.");
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 500)
            throw new ArgumentException("Notification body is required and limited to 500 characters.");
        return _repository.CreateAsync(request, actorUserId, role, ct);
    }

    public Task<int> MarkReadAsync(
        long notificationId, long actorUserId, string actorRole, CancellationToken ct)
    {
        ValidateActor(actorUserId, actorRole);
        if (notificationId <= 0) throw new ArgumentException("Invalid notification ID.");
        return _repository.MarkReadAsync(notificationId, actorUserId, actorRole, ct);
    }
}