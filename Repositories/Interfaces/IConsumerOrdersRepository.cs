
using FreshMilk.API.Models.ConsumerOrders;

namespace FreshMilk.API.Repositories.Interfaces;

public interface IConsumerOrdersRepository
{
    Task<ConsumerOrderDto?> CreateAsync(
        SaveConsumerOrderRequest request,
        long actorUserId,
        CancellationToken ct);

    Task<List<ConsumerOrderDto>> GetMineAsync(
        long actorUserId,
        CancellationToken ct);

    Task<List<ConsumerOrderDto>> GetAllAsync(
        long actorUserId,
        CancellationToken ct);

    Task<ConsumerOrderDto?> UpdateStatusAsync(
        long orderId,
        string status,
        long actorUserId,
        CancellationToken ct);
}
