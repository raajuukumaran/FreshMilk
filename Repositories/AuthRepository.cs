using System.Data;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Data;
using FreshMilk.API.Repositories.Interfaces;

namespace FreshMilk.API.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly IConfiguration _configuration;

    public AuthRepository(IConfiguration configuration)
    {
        _configuration = configuration;
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
}