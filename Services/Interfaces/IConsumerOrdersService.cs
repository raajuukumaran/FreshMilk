
using FreshMilk.API.Models.ConsumerOrders;

namespace FreshMilk.API.Services.Interfaces;

public interface IConsumerOrdersService
{
    Task<ConsumerOrderDto?> CreateAsync(
        SaveConsumerOrderRequest request,
        long actorUserId,
        string actorRole,
        CancellationToken ct);

    Task<List<ConsumerOrderDto>> GetMineAsync(
        long actorUserId,
        string actorRole,
        CancellationToken ct);

    Task<List<ConsumerOrderDto>> GetAllAsync(
        long actorUserId,
        string actorRole,
        CancellationToken ct);

    Task<ConsumerOrderDto?> UpdateStatusAsync(
        long orderId,
        UpdateConsumerOrderStatusRequest request,
        long actorUserId,
        string actorRole,
        CancellationToken ct);
}
