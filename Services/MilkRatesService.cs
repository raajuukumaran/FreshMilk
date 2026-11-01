
using FreshMilk.API.Models.MilkRates;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Services;

public sealed class MilkRatesService : IMilkRatesService
{
    private readonly IMilkRatesRepository _repository;

    public MilkRatesService(IMilkRatesRepository repository)
    {
        _repository = repository;
    }

    public Task<List<MilkRateDto>> GetAllAsync(
        bool includeInactive, CancellationToken ct) =>
        _repository.GetAllAsync(includeInactive, ct);

    public Task<List<MilkRateDto>> GetCurrentAsync(
        CancellationToken ct) =>
        _repository.GetCurrentAsync(ct);

    public Task<MilkRateDto?> GetByIdAsync(
        long id, CancellationToken ct) =>
        _repository.GetByIdAsync(id, ct);

    public async Task<MilkRateDto?> SetRateAsync(
        SaveMilkRateRequest request, CancellationToken ct)
    {
        if (request.MilkTypeId <= 0)
            throw new ArgumentException("Select a valid milk type.");

        if (request.SupplierRate <= 0 || request.ConsumerRate <= 0)
            throw new ArgumentException("Both rates must be greater than zero.");

        return await _repository.SetRateAsync(request, ct);
    }

    public async Task<MilkRateDto?> UpdateAsync(
    long id,
    SaveMilkRateRequest request,
    CancellationToken ct)
{
    if (id <= 0)
        throw new ArgumentException("Invalid milk rate ID.");

    if (request.MilkTypeId <= 0)
        throw new ArgumentException("Select a valid milk type.");

    if (request.SupplierRate < 0 || request.ConsumerRate < 0)
        throw new ArgumentException("Rates cannot be negative.");

    return await _repository.UpdateAsync(id, request, ct);
}
}
