
using FreshMilk.API.Models.MilkTypes;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Services;

public sealed class MilkTypesService : IMilkTypesService
{
    private readonly IMilkTypesRepository _repository;

    public MilkTypesService(IMilkTypesRepository repository)
    {
        _repository = repository;
    }

    public Task<List<MilkTypeDto>> GetAllAsync(
        bool includeInactive, CancellationToken ct) =>
        _repository.GetAllAsync(includeInactive, ct);

    public Task<MilkTypeDto?> GetByIdAsync(
        int id, CancellationToken ct) =>
        _repository.GetByIdAsync(id, ct);

    public async Task<MilkTypeDto> CreateAsync(
        SaveMilkTypeRequest request, CancellationToken ct)
    {
        Validate(request);
        request.MilkTypeCode = request.MilkTypeCode.Trim().ToUpperInvariant();
        request.MilkTypeName = request.MilkTypeName.Trim();
        return await _repository.CreateAsync(request, ct);
    }

    public async Task<MilkTypeDto?> UpdateAsync(
        int id, SaveMilkTypeRequest request, CancellationToken ct)
    {
        Validate(request);
        request.MilkTypeCode = request.MilkTypeCode.Trim().ToUpperInvariant();
        request.MilkTypeName = request.MilkTypeName.Trim();
        return await _repository.UpdateAsync(id, request, ct);
    }

    public Task<MilkTypeDto?> SetActiveAsync(
        int id, bool isActive, CancellationToken ct) =>
        _repository.SetActiveAsync(id, isActive, ct);

    private static void Validate(SaveMilkTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MilkTypeCode))
            throw new ArgumentException("Milk type code is required.");

        if (string.IsNullOrWhiteSpace(request.MilkTypeName))
            throw new ArgumentException("Milk type name is required.");
    }
}
