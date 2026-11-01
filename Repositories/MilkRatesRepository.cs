using System.Data;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Data;
using FreshMilk.API.Models.MilkRates;
using FreshMilk.API.Repositories.Interfaces;

namespace FreshMilk.API.Repositories;

public sealed class MilkRatesRepository : IMilkRatesRepository
{
    private readonly string _connectionString;

    public MilkRatesRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    private async Task<List<MilkRateDto>> ExecuteAsync(
        string action,
        long? rateId = null,
        SaveMilkRateRequest? request = null,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command = new SqlCommand(
            StoredProcedure.MilkRates.Manage,
            connection);

        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(
            "@Action", SqlDbType.NVarChar, 20).Value = action;

        command.Parameters.Add(
            "@MilkRateId", SqlDbType.BigInt).Value =
            (object?)rateId ?? DBNull.Value;

        command.Parameters.Add(
            "@MilkTypeId", SqlDbType.Int).Value =
            request is null
                ? DBNull.Value
                : request.MilkTypeId;

        var supplier = command.Parameters.Add(
            "@SupplierRate", SqlDbType.Decimal);

        supplier.Precision = 10;
        supplier.Scale = 2;
        supplier.Value = request is null
            ? DBNull.Value
            : request.SupplierRate;

        var consumer = command.Parameters.Add(
            "@ConsumerRate", SqlDbType.Decimal);

        consumer.Precision = 10;
        consumer.Scale = 2;
        consumer.Value = request is null
            ? DBNull.Value
            : request.ConsumerRate;

        command.Parameters.Add(
            "@EffectiveFrom", SqlDbType.DateTime2).Value =
            request?.EffectiveFrom is null
                ? DBNull.Value
                : request.EffectiveFrom.Value;

        command.Parameters.Add(
            "@IncludeInactive", SqlDbType.Bit).Value =
            includeInactive;

        await connection.OpenAsync(ct);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        var result = new List<MilkRateDto>();

        while (await reader.ReadAsync(ct))
        {
            result.Add(new MilkRateDto
            {
                MilkRateId = reader.GetInt64(
                    reader.GetOrdinal("MilkRateId")),

                MilkTypeId = reader.GetInt32(
                    reader.GetOrdinal("MilkTypeId")),

                MilkTypeCode = reader.GetString(
                    reader.GetOrdinal("MilkTypeCode")),

                MilkTypeName = reader.GetString(
                    reader.GetOrdinal("MilkTypeName")),

                SupplierRate = reader.GetDecimal(
                    reader.GetOrdinal("SupplierRate")),

                ConsumerRate = reader.GetDecimal(
                    reader.GetOrdinal("ConsumerRate")),

                EffectiveFrom = reader.GetDateTime(
                    reader.GetOrdinal("EffectiveFrom")),

                EffectiveTo = ReadNullableDateTime(
                    reader, "EffectiveTo"),

                IsActive = reader.GetBoolean(
                    reader.GetOrdinal("IsActive")),

                CreatedAt = ReadDateTimeOrDefault(
                    reader, "CreatedAt"),

                UpdatedAt = ReadNullableDateTime(
                    reader, "UpdatedAt")
            });
        }

        return result;
    }

    private static DateTime? ReadNullableDateTime(
        SqlDataReader reader,
        string column)
    {
        int index = reader.GetOrdinal(column);

        return reader.IsDBNull(index)
            ? null
            : reader.GetDateTime(index);
    }

    private static DateTime ReadDateTimeOrDefault(
        SqlDataReader reader,
        string column)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(
                reader.GetName(i),
                column,
                StringComparison.OrdinalIgnoreCase))
            {
                return reader.IsDBNull(i)
                    ? default
                    : reader.GetDateTime(i);
            }
        }

        return default;
    }

    public Task<List<MilkRateDto>> GetAllAsync(
        bool includeInactive,
        CancellationToken ct) =>
        ExecuteAsync(
            "GETALL",
            includeInactive: includeInactive,
            ct: ct);

    public Task<List<MilkRateDto>> GetCurrentAsync(
        CancellationToken ct) =>
        ExecuteAsync(
            "GETCURRENT",
            ct: ct);

    public async Task<MilkRateDto?> GetByIdAsync(
        long id,
        CancellationToken ct) =>
        (await ExecuteAsync(
            "GETBYID",
            rateId: id,
            ct: ct)).FirstOrDefault();

    public async Task<MilkRateDto?> SetRateAsync(
        SaveMilkRateRequest request,
        CancellationToken ct) =>
        (await ExecuteAsync(
            "SETRATE",
            request: request,
            ct: ct)).FirstOrDefault();

    public async Task<MilkRateDto?> UpdateAsync(
        long id,
        SaveMilkRateRequest request,
        CancellationToken ct) =>
        (await ExecuteAsync(
            "UPDATE",
            rateId: id,
            request: request,
            ct: ct)).FirstOrDefault();
}