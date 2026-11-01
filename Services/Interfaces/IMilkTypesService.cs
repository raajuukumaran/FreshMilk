
using FreshMilk.API.Models.MilkTypes;

namespace FreshMilk.API.Services.Interfaces;

public interface IMilkTypesService
{
    Task<List<MilkTypeDto>> GetAllAsync(bool includeInactive, CancellationToken ct);
    Task<MilkTypeDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<MilkTypeDto> CreateAsync(SaveMilkTypeRequest request, CancellationToken ct);
    Task<MilkTypeDto?> UpdateAsync(int id, SaveMilkTypeRequest request, CancellationToken ct);
    Task<MilkTypeDto?> SetActiveAsync(int id, bool isActive, CancellationToken ct);
}
