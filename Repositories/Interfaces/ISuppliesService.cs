using FreshMilk.API.Models.Supplies;

namespace FreshMilk.API.Services.Interfaces;

public interface ISuppliesService
{
    Task<List<SupplyDto>> GetAllAsync(
        long actorUserId, string actorRole,
        DateTime? date, CancellationToken ct);

    Task<SupplyDto?> GetByIdAsync(
        long id, long actorUserId, string actorRole, CancellationToken ct);

    Task<SupplyDto?> CreateAsync(
        SaveSupplyRequest request,
        long actorUserId, string actorRole, CancellationToken ct);

    Task<SupplyDto?> UpdateAsync(
        long id, SaveSupplyRequest request,
        long actorUserId, string actorRole, CancellationToken ct);

    Task<bool> DeleteAsync(
        long id, long actorUserId, string actorRole, CancellationToken ct);

    Task<SupplyDto?> ConfirmAsync(
        long id, long actorUserId, string actorRole, CancellationToken ct);

    Task<SupplyDto?> RequestChangeAsync(
        long id, string note,
        long actorUserId, string actorRole, CancellationToken ct);
}