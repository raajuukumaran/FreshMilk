
using FreshMilk.API.Models.ConsumerOrders;
using FreshMilk.API.Models.Notifications;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Services;

public sealed class ConsumerOrdersService : IConsumerOrdersService
{
    private readonly IConsumerOrdersRepository _repository;
    private readonly IAppNotificationsService _notifications;
    private readonly ILogger<ConsumerOrdersService> _logger;

    public ConsumerOrdersService(
        IConsumerOrdersRepository repository,
        IAppNotificationsService notifications,
        ILogger<ConsumerOrdersService> logger)
    {
        _repository = repository;
        _notifications = notifications;
        _logger = logger;
    }

    private async Task NotifyAsync(
        CreateAppNotificationRequest request,
        long actorUserId, string actorRole, CancellationToken ct)
    {
        try
        {
            await _notifications.CreateAsync(request, actorUserId, actorRole, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create order activity notification");
        }
    }

    public async Task<ConsumerOrderDto?> CreateAsync(
        SaveConsumerOrderRequest request,
        long actorUserId,
        string actorRole,
        CancellationToken ct)
    {
        RequireRole(actorUserId, actorRole, "Consumer");

        if (request.MilkTypeId <= 0)
            throw new ArgumentException("A valid milk type is required.");

        if (request.Litres <= 0)
            throw new ArgumentException("Litres must be greater than zero.");

        if (request.DeliveryDate.HasValue &&
            request.DeliveryDate.Value.Date < DateTime.Today)
        {
            throw new ArgumentException(
                "Delivery date cannot be in the past.");
        }

        if (string.IsNullOrWhiteSpace(request.DeliverySlot) ||
            !(request.DeliverySlot.Equals(
                "morning", StringComparison.OrdinalIgnoreCase) ||
              request.DeliverySlot.Equals(
                "evening", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "Delivery slot must be morning or evening.");
        }

        var order = await _repository.CreateAsync(request, actorUserId, ct);
        if (order is not null)
        {
            await NotifyAsync(new CreateAppNotificationRequest
            {
                ToRole = "OWNER",
                NotificationType = "ORDER_PLACED",
                Title = "New consumer order",
                Body = $"{order.ConsumerName} ordered {order.Litres:0.##} L of {order.Label} for {order.DeliveryDate:dd MMM}.",
                RelatedOrderId = order.Id,
            }, actorUserId, "CONSUMER", ct);
        }
        return order;
    }

    public async Task<List<ConsumerOrderDto>> GetMineAsync(
        long actorUserId,
        string actorRole,
        CancellationToken ct)
    {
        RequireRole(actorUserId, actorRole, "Consumer");

        return await _repository.GetMineAsync(actorUserId, ct);
    }

    public async Task<List<ConsumerOrderDto>> GetAllAsync(
        long actorUserId,
        string actorRole,
        CancellationToken ct)
    {
        RequireRole(actorUserId, actorRole, "Owner");

        return await _repository.GetAllAsync(actorUserId, ct);
    }

    public async Task<ConsumerOrderDto?> UpdateStatusAsync(
        long orderId,
        UpdateConsumerOrderStatusRequest request,
        long actorUserId,
        string actorRole,
        CancellationToken ct)
    {
        RequireRole(actorUserId, actorRole, "Owner");

        if (orderId <= 0)
            throw new ArgumentException("A valid order ID is required.");

        var status = request.Status?.Trim();

        if (status is null ||
            !(status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) ||
              status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) ||
              status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "Status must be Confirmed, Delivered, or Cancelled.");
        }

        var order = await _repository.UpdateStatusAsync(
            orderId,
            NormalizeStatus(status),
            actorUserId,
            ct);
        if (order is not null)
        {
            await NotifyAsync(new CreateAppNotificationRequest
            {
                ToUserId = order.ConsumerId,
                ToRole = "CONSUMER",
                NotificationType = $"ORDER_{NormalizeStatus(status).ToUpperInvariant()}",
                Title = $"Order {NormalizeStatus(status).ToLowerInvariant()}",
                Body = $"Your {order.Litres:0.##} L {order.Label} order is now {NormalizeStatus(status).ToLowerInvariant()}.",
                RelatedOrderId = order.Id,
            }, actorUserId, "OWNER", ct);
        }
        return order;
    }

    private static void RequireRole(
        long actorUserId, string actorRole, string requiredRole)
    {
        if (actorUserId <= 0)
            throw new ArgumentException("A valid actor user ID is required.");

        if (!string.Equals(
            actorRole?.Trim(), requiredRole,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Only {requiredRole.ToLowerInvariant()} users can perform this action.");
        }
    }

    private static string NormalizeStatus(string status)
    {
        if (status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
            return "Confirmed";

        if (status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
            return "Delivered";

        return "Cancelled";
    }
}
