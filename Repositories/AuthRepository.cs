using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Data;
using FreshMilk.API.Repositories.Interfaces;

namespace FreshMilk.API.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<VerifiedUser> _passwordHasher;
    private readonly ILogger<AuthRepository> _logger;

    public AuthRepository(
        IConfiguration configuration,
        IPasswordHasher<VerifiedUser> passwordHasher,
        ILogger<AuthRepository> logger)
    {
        _configuration = configuration;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    private SqlConnection CreateConnection()
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection is not configured in appsettings.json.");
        }

        return new SqlConnection(connectionString);
    }

    // Generate OTP for a registered mobile number
    public async Task<GenerateOtpResult> GenerateOtpAsync(
        string mobileNumber,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();

        await using var command = new SqlCommand(
            StoredProcedure.Auth.GenerateMobileOTP,
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            "@MobileNumber",
            SqlDbType.NVarChar,
            20).Value = mobileNumber;

        await connection.OpenAsync(cancellationToken);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "The OTP procedure returned no result.");
        }

        return new GenerateOtpResult
        {
            Success = Convert.ToBoolean(reader["Success"]),

            UserId = reader["UserId"] is DBNull
                ? null
                : Convert.ToInt32(reader["UserId"]),

            OTP = reader["OTP"] is DBNull
                ? null
                : Convert.ToString(reader["OTP"]),

            Message = Convert.ToString(reader["Message"])
                ?? "Request processed."
        };
    }

    // Verify OTP and return the verified user
    public async Task<VerifyOtpResult> VerifyOtpAsync(
        string mobileNumber,
        string otp,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();

        await using var command = new SqlCommand(
            StoredProcedure.Auth.VerifyMobileOTP,
            connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(
            "@MobileNumber",
            SqlDbType.NVarChar,
            20).Value = mobileNumber;

        command.Parameters.Add(
            "@OTP",
            SqlDbType.VarChar,
            6).Value = otp;

        await connection.OpenAsync(cancellationToken);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "The verification procedure returned no result.");
        }

        var result = new VerifyOtpResult
        {
            Success = Convert.ToBoolean(reader["Success"]),

            Message = Convert.ToString(reader["Message"])
                ?? "Verification completed."
        };

        // Failed verification returns no user details.
        if (!result.Success)
        {
            return result;
        }

        result.User = new VerifiedUser
        {
            UserId = Convert.ToInt32(reader["UserId"]),

            PhoneNumber = reader["PhoneNumber"] is DBNull
                ? null
                : Convert.ToString(reader["PhoneNumber"]),

            FullName = reader["FullName"] is DBNull
                ? null
                : Convert.ToString(reader["FullName"]),

            UserRole = reader["UserRole"] is DBNull
                ? null
                : Convert.ToString(reader["UserRole"])
        };

        return result;
    }

    public async Task<VerifyOtpResult> LoginWithPasswordAsync(
        string mobileNumber,
        string password,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await using var command = new SqlCommand(
            """
            SELECT TOP (1)
                UserId,
                PhoneNumber,
                FullName,
                UserRole,
                PasswordHash
            FROM dbo.Users
            WHERE PhoneNumber = @MobileNumber
                AND IsActive = 1
                AND UserRole IN (N'owner', N'supplier', N'consumer');
            """,
            connection);

        command.Parameters.Add(
            "@MobileNumber",
            SqlDbType.NVarChar,
            20).Value = mobileNumber;

        await connection.OpenAsync(cancellationToken);

        VerifiedUser user;
        string? passwordHash;
        await using (var reader =
            await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken) ||
                reader["PasswordHash"] is DBNull)
            {
                return InvalidPasswordResult();
            }

            user = new VerifiedUser
            {
                UserId = Convert.ToInt32(reader["UserId"]),
                PhoneNumber = Convert.ToString(reader["PhoneNumber"]),
                FullName = Convert.ToString(reader["FullName"]),
                UserRole = Convert.ToString(reader["UserRole"])
            };
            passwordHash = Convert.ToString(reader["PasswordHash"]);
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return InvalidPasswordResult();
        }

        PasswordVerificationResult verificationResult;
        try
        {
            verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                passwordHash,
                password);
        }
        catch (FormatException exception)
        {
            if (!MatchesLegacyPassword(passwordHash, password))
            {
                _logger.LogWarning(
                    exception,
                    "Stored password credential is malformed for user {UserId}.",
                    user.UserId);
                return InvalidPasswordResult();
            }

            verificationResult = PasswordVerificationResult.SuccessRehashNeeded;
        }

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return InvalidPasswordResult();
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            await ReplacePasswordCredentialAsync(
                connection,
                user.UserId,
                passwordHash,
                _passwordHasher.HashPassword(user, password),
                cancellationToken);
        }

        return new VerifyOtpResult
        {
            Success = true,
            Message = "Login successful.",
            User = user
        };
    }

    private static bool MatchesLegacyPassword(
        string storedCredential,
        string providedPassword)
    {
        var storedBytes = System.Text.Encoding.UTF8.GetBytes(storedCredential);
        var providedBytes = System.Text.Encoding.UTF8.GetBytes(providedPassword);

        return storedBytes.Length == providedBytes.Length &&
            System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                storedBytes,
                providedBytes);
    }

    private static async Task ReplacePasswordCredentialAsync(
        SqlConnection connection,
        int userId,
        string previousCredential,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            """
            UPDATE dbo.Users
            SET PasswordHash = @PasswordHash
            WHERE UserId = @UserId
                AND UserRole IN (N'owner', N'supplier', N'consumer')
                AND PasswordHash = @PreviousCredential;
            """,
            connection);

        command.Parameters.Add(
            "@PasswordHash",
            SqlDbType.NVarChar,
            500).Value = passwordHash;
        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        command.Parameters.Add(
            "@PreviousCredential",
            SqlDbType.NVarChar,
            500).Value = previousCredential;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static VerifyOtpResult InvalidPasswordResult() => new()
    {
        Success = false,
        Message = "Invalid mobile number or password."
    };
}