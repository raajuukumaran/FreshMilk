using FreshMilk.API.Models.Supplies;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Services.Interfaces;
using FreshMilk.API.Models.Notifications;

namespace FreshMilk.API.Services;

public sealed class SuppliesService : ISuppliesService
{
    private readonly ISuppliesRepository _repository;
    private readonly IAppNotificationsService _notifications;
    private readonly ILogger<SuppliesService> _logger;

    public SuppliesService(
        ISuppliesRepository repository,
        IAppNotificationsService notifications,
        ILogger<SuppliesService> logger)
    {
        _repository = repository;
        _notifications = notifications;
        _logger = logger;
    }

    private async Task NotifyAsync(
        long actorUserId, string actorRole,
        long? toUserId, string toRole,
        string type, string title, string body,
        long supplyId, CancellationToken ct)
    {
        try
        {
            await _notifications.CreateAsync(new CreateAppNotificationRequest
            {
                ToUserId = toUserId,
                ToRole = toRole,
                NotificationType = type,
                Title = title,
                Body = body,
                RelatedSupplyId = supplyId,
            }, actorUserId, actorRole, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create supply notification for {SupplyId}", supplyId);
        }
    }

    private static string ValidateRole(string role)
    {
        var normalized = role.Trim().ToUpperInvariant();

        if (normalized is not ("OWNER" or "SUPPLIER"))
            throw new ArgumentException("Role must be Owner or Supplier.");

        return normalized;
    }

    private static void ValidateActor(long actorUserId)
    {
        if (actorUserId <= 0)
            throw new ArgumentException("Valid actor user ID is required.");
    }

    public Task<List<SupplyDto>> GetAllAsync(
        long actorUserId, string actorRole,
        DateTime? date, CancellationToken ct)
    {
        ValidateActor(actorUserId);
        return _repository.GetAllAsync(
            actorUserId, ValidateRole(actorRole), date, ct);
    }

    public Task<SupplyDto?> GetByIdAsync(
        long id, long actorUserId, string actorRole, CancellationToken ct)
    {
        ValidateActor(actorUserId);
        if (id <= 0) throw new ArgumentException("Invalid supply ID.");

        return _repository.GetByIdAsync(
            id, actorUserId, ValidateRole(actorRole), ct);
    }

    public async Task<SupplyDto?> CreateAsync(
        SaveSupplyRequest request,
        long actorUserId, string actorRole, CancellationToken ct)
    {
        ValidateActor(actorUserId);
        if (request.Litres <= 0)
            throw new ArgumentException("Litres must be greater than zero.");

        if (request.Slot == "custom" &&
            string.IsNullOrWhiteSpace(request.SlotLabel))
            throw new ArgumentException("Custom slot time is required.");

        var role = ValidateRole(actorRole);
        var supply = await _repository.CreateAsync(request, actorUserId, role, ct);
        if (supply is not null)
        {
            await NotifyAsync(actorUserId, role, supply.SupplierId, "SUPPLIER",
                "SUPPLY_CREATED", "New supply to review",
                $"{supply.Litres:0.##} L of {supply.MilkTypeName} was recorded for {supply.SupplyDate:dd MMM}.",
                supply.SupplyId, ct);
        }
        return supply;
    }

    public async Task<SupplyDto?> UpdateAsync(
        long id, SaveSupplyRequest request,
        long actorUserId, string actorRole, CancellationToken ct)
    {
        ValidateActor(actorUserId);
        if (id <= 0) throw new ArgumentException("Invalid supply ID.");
        if (request.Litres <= 0)
            throw new ArgumentException("Litres must be greater than zero.");

        var role = ValidateRole(actorRole);
        var supply = await _repository.UpdateAsync(id, request, actorUserId, role, ct);
        if (supply is not null)
        {
            await NotifyAsync(actorUserId, role, supply.SupplierId, "SUPPLIER",
                "SUPPLY_UPDATED", "Supply details updated",
                $"The owner updated the {supply.Litres:0.##} L {supply.MilkTypeName} supply. Please review it.",
                supply.SupplyId, ct);
        }
        return supply;
    }

    public async Task<bool> DeleteAsync(
        long id, long actorUserId, string actorRole, CancellationToken ct)
    {
        ValidateActor(actorUserId);
        if (id <= 0) throw new ArgumentException("Invalid supply ID.");

        var role = ValidateRole(actorRole);
        var existing = await _repository.GetByIdAsync(id, actorUserId, role, ct);
        var deleted = await _repository.DeleteAsync(id, actorUserId, role, ct);
        if (deleted && existing is not null)
        {
            var ownerAction = role == "OWNER";
            await NotifyAsync(actorUserId, role,
                ownerAction ? existing.SupplierId : null,
                ownerAction ? "SUPPLIER" : "OWNER",
                "SUPPLY_DELETED", "Supply entry removed",
                $"A {existing.Litres:0.##} L {existing.MilkTypeName} supply entry was removed.",
                id, ct);
        }
        return deleted;
    }

    public async Task<SupplyDto?> ConfirmAsync(
        long id, long actorUserId, string actorRole, CancellationToken ct)
    {
        ValidateActor(actorUserId);
        if (id <= 0) throw new ArgumentException("Invalid supply ID.");

        var role = ValidateRole(actorRole);
        var supply = await _repository.ConfirmAsync(id, actorUserId, role, ct);
        if (supply is not null)
        {
            await NotifyAsync(actorUserId, role, null, "OWNER",
                "SUPPLY_CONFIRMED", "Supplier confirmed a supply",
                $"{supply.SupplierName} confirmed {supply.Litres:0.##} L of {supply.MilkTypeName}.",
                supply.SupplyId, ct);
        }
        return supply;
    }

    public async Task<SupplyDto?> RequestChangeAsync(
        long id, string note,
        long actorUserId, string actorRole, CancellationToken ct)
    {
        ValidateActor(actorUserId);
        if (id <= 0) throw new ArgumentException("Invalid supply ID.");
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Correction note is required.");

        var role = ValidateRole(actorRole);
        var supply = await _repository.RequestChangeAsync(id, note.Trim(), actorUserId, role, ct);
        if (supply is not null)
        {
            var notePreview = note.Trim().Length > 250 ? note.Trim()[..250] : note.Trim();
            await NotifyAsync(actorUserId, role, null, "OWNER",
                "SUPPLY_CHANGE_REQUESTED", "Supplier requested a correction",
                $"{supply.SupplierName}: {notePreview}", supply.SupplyId, ct);
        }
        return supply;
    }
}