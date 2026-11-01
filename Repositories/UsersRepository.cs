using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Data;
using FreshMilk.API.DTOs.Users;
using FreshMilk.API.Models.Users;
using FreshMilk.API.Repositories.Interfaces;

namespace FreshMilk.API.Repositories;

public class UsersRepository : IUsersRepository
{
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<VerifiedUser> _passwordHasher;

    public UsersRepository(
        IConfiguration configuration,
        IPasswordHasher<VerifiedUser> passwordHasher)
    {
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    private SqlConnection CreateConnection()
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is missing from appsettings.json.");

        return new SqlConnection(connectionString);
    }

    private static void AddParameters(
        SqlCommand cmd,
        string action,
        long? id = null,
        SaveUserRequest? user = null,
        string? role = null,
        bool includeInactive = false,
        string? search = null)
    {
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 20).Value = action;
        cmd.Parameters.Add("@UserId", SqlDbType.BigInt).Value =
            id.HasValue ? id.Value : DBNull.Value;

        cmd.Parameters.Add("@UserRole", SqlDbType.NVarChar, 20).Value =
            user?.UserRole ?? (object?)role ?? DBNull.Value;

        cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value =
            (object?)user?.FullName ?? DBNull.Value;

        cmd.Parameters.Add("@ProfileImageUrl", SqlDbType.NVarChar, 500).Value =
            (object?)user?.ProfileImageUrl ?? DBNull.Value;

        cmd.Parameters.Add("@Gender", SqlDbType.NVarChar, 20).Value =
            (object?)user?.Gender ?? DBNull.Value;

        cmd.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value =
            user?.DateOfBirth.HasValue == true
                ? user.DateOfBirth.Value.Date
                : DBNull.Value;

        cmd.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value =
            (object?)user?.PhoneNumber ?? DBNull.Value;

        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value =
            (object?)user?.Email ?? DBNull.Value;

        cmd.Parameters.Add("@AddressLine1", SqlDbType.NVarChar, 200).Value =
            (object?)user?.AddressLine1 ?? DBNull.Value;

        cmd.Parameters.Add("@AddressLine2", SqlDbType.NVarChar, 200).Value =
            (object?)user?.AddressLine2 ?? DBNull.Value;

        cmd.Parameters.Add("@VillageOrTown", SqlDbType.NVarChar, 100).Value =
            (object?)user?.VillageOrTown ?? DBNull.Value;

        cmd.Parameters.Add("@District", SqlDbType.NVarChar, 100).Value =
            (object?)user?.District ?? DBNull.Value;

        cmd.Parameters.Add("@State", SqlDbType.NVarChar, 100).Value =
            (object?)user?.State ?? DBNull.Value;

        cmd.Parameters.Add("@Country", SqlDbType.NVarChar, 100).Value =
            user?.Country ?? "India";

        cmd.Parameters.Add("@Pincode", SqlDbType.NVarChar, 10).Value =
            (object?)user?.Pincode ?? DBNull.Value;

        cmd.Parameters.Add("@PreferredLanguage", SqlDbType.NVarChar, 20).Value =
            user?.PreferredLanguage ?? "ta";

        cmd.Parameters.Add("@IncludeInactive", SqlDbType.Bit).Value =
            includeInactive;

        cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 255).Value =
            (object?)search ?? DBNull.Value;
    }

    private static bool HasColumn(SqlDataReader reader, string name)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(
                reader.GetName(i), name,
                StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string? GetString(SqlDataReader reader, string name) =>
        HasColumn(reader, name) && !reader.IsDBNull(reader.GetOrdinal(name))
            ? reader.GetString(reader.GetOrdinal(name))
            : null;

    private static DateTime? GetDate(SqlDataReader reader, string name) =>
        HasColumn(reader, name) && !reader.IsDBNull(reader.GetOrdinal(name))
            ? reader.GetDateTime(reader.GetOrdinal(name))
            : null;

    private static bool GetBool(
        SqlDataReader reader, string name, bool fallback = false) =>
        HasColumn(reader, name) && !reader.IsDBNull(reader.GetOrdinal(name))
            ? reader.GetBoolean(reader.GetOrdinal(name))
            : fallback;

    private static UserDto MapUser(SqlDataReader reader)
    {
        var id = reader.GetOrdinal("UserId");

        return new UserDto
        {
            UserId = reader.GetInt64(id),
            UserCode = GetString(reader, "UserCode"),
            UserRole = GetString(reader, "UserRole"),
            FullName = GetString(reader, "FullName"),
            ProfileImageUrl = GetString(reader, "ProfileImageUrl"),
            Gender = GetString(reader, "Gender"),
            DateOfBirth = GetDate(reader, "DateOfBirth"),
            PhoneNumber = GetString(reader, "PhoneNumber"),
            Email = GetString(reader, "Email"),
            AddressLine1 = GetString(reader, "AddressLine1"),
            AddressLine2 = GetString(reader, "AddressLine2"),
            VillageOrTown = GetString(reader, "VillageOrTown"),
            District = GetString(reader, "District"),
            State = GetString(reader, "State"),
            Country = GetString(reader, "Country"),
            Pincode = GetString(reader, "Pincode"),
            PreferredLanguage = GetString(reader, "PreferredLanguage"),
            IsActive = GetBool(reader, "IsActive"),
            IsPhoneVerified = GetBool(reader, "IsPhoneVerified"),
            IsEmailVerified = GetBool(reader, "IsEmailVerified"),
            CreatedAt = GetDate(reader, "CreatedAt"),
            UpdatedAt = GetDate(reader, "UpdatedAt"),
            LastLoginAt = GetDate(reader, "LastLoginAt")
        };
    }

    private async Task<List<UserDto>> QueryUsersAsync(
        string action,
        long? id,
        SaveUserRequest? user,
        string? role,
        bool includeInactive,
        string? search,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await using var cmd = new SqlCommand(
            StoredProcedure.Users.ManageUsers, connection);

        AddParameters(
            cmd, action, id, user, role, includeInactive, search);

        await connection.OpenAsync(cancellationToken);

        await using var reader =
            await cmd.ExecuteReaderAsync(cancellationToken);

        var users = new List<UserDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            if (HasColumn(reader, "UserId"))
                users.Add(MapUser(reader));
        }

        return users;
    }

    public Task<List<UserDto>> GetAllAsync(
        string? role,
        bool includeInactive,
        string? search,
        CancellationToken cancellationToken) =>
        QueryUsersAsync(
            "GETALL", null, null, role, includeInactive, search,
            cancellationToken);

    public async Task<UserDto?> GetByIdAsync(
        long id, CancellationToken cancellationToken)
    {
        var users = await QueryUsersAsync(
            "GETBYID", id, null, null, true, null, cancellationToken);

        return users.FirstOrDefault();
    }

    public async Task<UserDto?> CreateAsync(
        SaveUserRequest request, CancellationToken cancellationToken)
    {
        var users = await QueryUsersAsync(
            "CREATE", null, request, null, false, null, cancellationToken);

        var user = users.FirstOrDefault();
        if (user is not null && request.Password is not null)
        {
            await SavePasswordHashAsync(
                user.UserId,
                user.UserRole,
                request.Password,
                cancellationToken);
        }

        return user;
    }

    public async Task<UserDto?> UpdateAsync(
        long id, SaveUserRequest request,
        CancellationToken cancellationToken)
    {
        var users = await QueryUsersAsync(
            "UPDATE", id, request, null, false, null, cancellationToken);

        var user = users.FirstOrDefault();
        if (user is not null && request.Password is not null)
        {
            await SavePasswordHashAsync(
                user.UserId,
                user.UserRole,
                request.Password,
                cancellationToken);
        }

        return user;
    }

    private async Task SavePasswordHashAsync(
        long userId,
        string? role,
        string password,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(role, "supplier", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(role, "consumer", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Passwords can only be set for supplier and consumer accounts.");
        }

        var user = new VerifiedUser
        {
            UserId = checked((int)userId),
            UserRole = role
        };
        var passwordHash = _passwordHasher.HashPassword(user, password);

        await using var connection = CreateConnection();
        await using var command = new SqlCommand(
            """
            UPDATE dbo.Users
            SET PasswordHash = @PasswordHash
            OUTPUT INSERTED.UserId
            WHERE UserId = @UserId
                AND UserRole IN (N'supplier', N'consumer');
            """,
            connection);

        command.Parameters.Add(
            "@PasswordHash",
            SqlDbType.NVarChar,
            500).Value = passwordHash;
        command.Parameters.Add("@UserId", SqlDbType.BigInt).Value = userId;

        await connection.OpenAsync(cancellationToken);
        var updatedUserId = await command.ExecuteScalarAsync(cancellationToken);
        if (updatedUserId is null || updatedUserId is DBNull)
        {
            throw new InvalidOperationException(
                "The password could not be saved for this account.");
        }
    }

    public async Task<bool> SetActiveAsync(
        long id, bool isActive, CancellationToken cancellationToken)
    {
        var users = await QueryUsersAsync(
            isActive ? "ACTIVATE" : "DEACTIVATE",
            id, null, null, true, null, cancellationToken);

        return users.Count > 0;
    }
}