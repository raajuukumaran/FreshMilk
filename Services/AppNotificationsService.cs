using FreshMilk.API.Models.Notifications;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Responses.Notifications;
using FreshMilk.API.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace FreshMilk.API.Services;

public sealed class AppNotificationsService : IAppNotificationsService
{
    private readonly IAppNotificationsRepository _repository;
    private readonly IExpoPushService _push;
    private readonly ILogger<AppNotificationsService> _logger;

    public AppNotificationsService(
        IAppNotificationsRepository repository,
        IExpoPushService push,
        ILogger<AppNotificationsService> logger)
    {
        _repository = repository;
        _push = push;
        _logger = logger;
    }

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

    public async Task<AppNotificationResponse?> CreateAsync(
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
        var created = await _repository.CreateAsync(request, actorUserId, role, ct);
        if (created is not null)
        {
            try
            {
                await _push.SendAsync(created, request.ToRole, ct);
            }
            catch (SqlException exception)
            {
                _logger.LogError(
                    exception,
                    "Push delivery setup failed for notification {NotificationId}.",
                    created.NotificationId);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Push delivery failed for notification {NotificationId}.",
                    created.NotificationId);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Push delivery timed out for notification {NotificationId}.",
                    created.NotificationId);
            }
        }
        return created;
    }

    public Task<int> MarkReadAsync(
        long notificationId, long actorUserId, string actorRole, CancellationToken ct)
    {
        ValidateActor(actorUserId, actorRole);
        if (notificationId <= 0) throw new ArgumentException("Invalid notification ID.");
        return _repository.MarkReadAsync(notificationId, actorUserId, actorRole, ct);
    }
}