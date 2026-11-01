
using FreshMilk.API.Models.MilkRates;

namespace FreshMilk.API.Repositories.Interfaces;

public interface IMilkRatesRepository
{
    Task<List<MilkRateDto>> GetAllAsync(
        bool includeInactive, CancellationToken ct);

    Task<List<MilkRateDto>> GetCurrentAsync(CancellationToken ct);

    Task<MilkRateDto?> GetByIdAsync(
        long id, CancellationToken ct);

    Task<MilkRateDto?> SetRateAsync(
        SaveMilkRateRequest request, CancellationToken ct);
        Task<MilkRateDto?> UpdateAsync(
    long id,
    SaveMilkRateRequest request,
    CancellationToken ct);
}
