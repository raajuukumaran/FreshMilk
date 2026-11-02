using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using FreshMilk.API.DTOs.Users;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Services.Interfaces;
using System.Security.Cryptography;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUsersRepository _repository;
    private readonly IAuthService _authService;
    private readonly ILogger<UsersController> _logger;
    private readonly string _profileImageDirectory;

    public UsersController(
        IUsersRepository repository,
        IAuthService authService,
        ILogger<UsersController> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _repository = repository;
        _authService = authService;
        _logger = logger;
        _profileImageDirectory =
            configuration["ProfileImages:Directory"] ??
            Path.Combine(environment.ContentRootPath, "wwwroot", "profile-images");
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
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                success = false,
                message = "Set a password of at least 8 characters for the new account."
            });
        }

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

    [HttpPut("{id:long}/profile-image")]
    public async Task<IActionResult> UpdateProfileImage(
        long id,
        [FromBody] UpdateProfileImageRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await _authService.LoginWithPasswordAsync(
            request.ActorPhoneNumber,
            request.ActorPassword,
            cancellationToken);
        if (!actor.Success || actor.User is null)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Enter a valid account phone number and password."
            });
        }

        if (actor.User.UserId != id &&
            !string.Equals(
                actor.User.UserRole, "owner", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!TryDecodeImage(request, out var imageBytes, out var extension))
        {
            return BadRequest(new
            {
                success = false,
                message = "Choose a valid JPEG, PNG, or WebP image under 5 MB."
            });
        }

        var existingUser = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingUser is null)
        {
            return NotFound(new { success = false, message = "User not found." });
        }

        var fileName = $"{id}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}.{extension}";
        Directory.CreateDirectory(_profileImageDirectory);
        var filePath = Path.Combine(_profileImageDirectory, fileName);
        var imageUrl = $"/profile-images/{fileName}";

        try
        {
            await System.IO.File.WriteAllBytesAsync(
                filePath, imageBytes, cancellationToken);

            var updated = await _repository.UpdateProfileImageUrlAsync(
                id, imageUrl, cancellationToken);
            if (!updated)
            {
                System.IO.File.Delete(filePath);
                return NotFound(new
                {
                    success = false,
                    message = "User not found."
                });
            }

            DeleteStoredProfileImage(existingUser.ProfileImageUrl);

            return Ok(new
            {
                success = true,
                message = "Profile photo updated.",
                data = new { profileImageUrl = imageUrl }
            });
        }
        catch (Exception ex)
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _logger.LogError(ex, "Failed to update profile photo for user {UserId}.", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Unable to update profile photo."
            });
        }
    }

    private static bool TryDecodeImage(
        UpdateProfileImageRequest request,
        out byte[] imageBytes,
        out string extension)
    {
        imageBytes = [];
        extension = string.Empty;

        if (request.ImageBase64.Length > 7_000_000)
        {
            return false;
        }

        try
        {
            imageBytes = Convert.FromBase64String(request.ImageBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        if (imageBytes.Length == 0 || imageBytes.Length > 5 * 1024 * 1024)
        {
            return false;
        }

        var isJpeg = request.ContentType == "image/jpeg" &&
            imageBytes.Length >= 3 &&
            imageBytes[0] == 0xFF &&
            imageBytes[1] == 0xD8 &&
            imageBytes[2] == 0xFF;
        var isPng = request.ContentType == "image/png" &&
            imageBytes.Length >= 8 &&
            imageBytes.AsSpan(0, 8).SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var isWebp = request.ContentType == "image/webp" &&
            imageBytes.Length >= 12 &&
            imageBytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            imageBytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);

        extension = isJpeg ? "jpg" : isPng ? "png" : isWebp ? "webp" : string.Empty;
        return extension.Length > 0;
    }

    private void DeleteStoredProfileImage(string? imageUrl)
    {
        const string prefix = "/profile-images/";
        if (imageUrl is null || !imageUrl.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        var fileName = Path.GetFileName(imageUrl[prefix.Length..]);
        if (!string.IsNullOrEmpty(fileName))
        {
            var oldPath = Path.Combine(_profileImageDirectory, fileName);
            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Delete(oldPath);
            }
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