using FreshMilk.API.DTOs.Users;
using FreshMilk.API.Models.Users;

namespace FreshMilk.API.Repositories.Interfaces;

public interface IUsersRepository
{
    Task<List<UserDto>> GetAllAsync(
        string? role,
        bool includeInactive,
        string? search,
        CancellationToken cancellationToken);

    Task<UserDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);

    Task<UserDto?> CreateAsync(
        SaveUserRequest request,
        CancellationToken cancellationToken);

    Task<UserDto?> UpdateAsync(
        long id,
        SaveUserRequest request,
        CancellationToken cancellationToken);

    Task<bool> SetActiveAsync(
        long id,
        bool isActive,
        CancellationToken cancellationToken);
}