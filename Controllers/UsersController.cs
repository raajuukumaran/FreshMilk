using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using FreshMilk.API.DTOs.Users;
using FreshMilk.API.Repositories.Interfaces;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUsersRepository _repository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUsersRepository repository,
        ILogger<UsersController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? role,
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _repository.GetAllAsync(
                role, includeInactive, search, cancellationToken);

            return Ok(new
            {
                success = true,
                count = users.Count,
                data = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve users.");
            return StatusCode(500, new
            {
                success = false,
                message = "Unable to retrieve users."
            });
        }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _repository.GetByIdAsync(id, cancellationToken);

            if (user is null)
                return NotFound(new
                {
                    success = false,
                    message = "User not found."
                });

            return Ok(new { success = true, data = user });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user {UserId}.", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Unable to retrieve user."
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _repository.CreateAsync(
                request, cancellationToken);

            if (user is null)
                return Conflict(new
                {
                    success = false,
                    message = "User could not be created."
                });

            return CreatedAtAction(
                nameof(GetById),
                new { id = user.UserId },
                new { success = true, message = "User created.", data = user });
        }
        catch (SqlException ex) when (ex.Number == 50000)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user.");
            return StatusCode(500, new
            {
                success = false,
                message = "Unable to create user."
            });
        }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] SaveUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _repository.UpdateAsync(
                id, request, cancellationToken);

            if (user is null)
                return NotFound(new
                {
                    success = false,
                    message = "User not found or update failed."
                });

            return Ok(new
            {
                success = true,
                message = "User updated.",
                data = user
            });
        }
        catch (SqlException ex) when (ex.Number == 50000)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}.", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Unable to update user."
            });
        }
    }
    
[HttpDelete("{id:long}")]
public async Task<IActionResult> Delete(
    long id,
    CancellationToken cancellationToken)
{
    try
    {
        // Soft delete: keep the record and mark it inactive.
        var updated = await _repository.SetActiveAsync(
            id, false, cancellationToken);

        if (!updated)
        {
            return NotFound(new
            {
                success = false,
                message = "User not found."
            });
        }

        return Ok(new
        {
            success = true,
            message = "User deleted (deactivated)."
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete user {UserId}.", id);

        return StatusCode(500, new
        {
            success = false,
            message = "Unable to delete user."
        });
    }
}


    [HttpPatch("{id:long}/deactivate")]
    public Task<IActionResult> Deactivate(
        long id, CancellationToken cancellationToken) =>
        ChangeStatus(id, false, cancellationToken);

    [HttpPatch("{id:long}/activate")]
    public Task<IActionResult> Activate(
        long id, CancellationToken cancellationToken) =>
        ChangeStatus(id, true, cancellationToken);

    private async Task<IActionResult> ChangeStatus(
        long id, bool active, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _repository.SetActiveAsync(
                id, active, cancellationToken);

            if (!updated)
                return NotFound(new
                {
                    success = false,
                    message = "User not found."
                });

            return Ok(new
            {
                success = true,
                message = active
                    ? "User activated."
                    : "User deactivated."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change status for user {UserId}.", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Unable to change user status."
            });
        }
    }
}