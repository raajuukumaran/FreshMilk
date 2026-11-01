using FreshMilk.API.Models.Supplies;

namespace FreshMilk.API.Repositories.Interfaces;

public interface ISuppliesRepository
{
    Task<List<SupplyDto>> GetAllAsync(
        long actorUserId, string actorRole,
        DateTime? supplyDate, CancellationToken ct);

    Task<SupplyDto?> GetByIdAsync(
        long id, long actorUserId, string actorRole,
        CancellationToken ct);

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
        long id, string changeNote,
        long actorUserId, string actorRole, CancellationToken ct);
}