
using System.Data;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Data;
using FreshMilk.API.Models.MilkTypes;
using FreshMilk.API.Repositories.Interfaces;

namespace FreshMilk.API.Repositories;

public sealed class MilkTypesRepository : IMilkTypesRepository
{
    private readonly string _connectionString;

    public MilkTypesRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    private async Task<List<MilkTypeDto>> ExecuteAsync(
        string action,
        int? id = null,
        SaveMilkTypeRequest? request = null,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            StoredProcedure.MilkTypes.Manage, connection);

        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@Action", SqlDbType.NVarChar, 20).Value = action;
        command.Parameters.Add("@MilkTypeId", SqlDbType.Int).Value =
            (object?)id ?? DBNull.Value;
        command.Parameters.Add("@MilkTypeCode", SqlDbType.NVarChar, 30).Value =
            (object?)request?.MilkTypeCode?.Trim() ?? DBNull.Value;
        command.Parameters.Add("@MilkTypeName", SqlDbType.NVarChar, 100).Value =
            (object?)request?.MilkTypeName?.Trim() ?? DBNull.Value;
        command.Parameters.Add("@Description", SqlDbType.NVarChar, 250).Value =
            (object?)request?.Description?.Trim() ?? DBNull.Value;
        command.Parameters.Add("@IncludeInactive", SqlDbType.Bit).Value =
            includeInactive;

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var result = new List<MilkTypeDto>();

        while (await reader.ReadAsync(ct))
        {
            result.Add(new MilkTypeDto
            {
                MilkTypeId = reader.GetInt32(reader.GetOrdinal("MilkTypeId")),
                MilkTypeCode = reader.GetString(reader.GetOrdinal("MilkTypeCode")),
                MilkTypeName = reader.GetString(reader.GetOrdinal("MilkTypeName")),
                Description = ReadNullableString(reader, "Description"),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = ReadNullableDateTime(reader, "UpdatedAt")
            });
        }

        return result;
    }

    private static string? ReadNullableString(
        SqlDataReader reader, string column)
    {
        int index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetString(index);
    }

    private static DateTime? ReadNullableDateTime(
        SqlDataReader reader, string column)
    {
        int index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    public Task<List<MilkTypeDto>> GetAllAsync(
        bool includeInactive, CancellationToken ct) =>
        ExecuteAsync("GETALL", includeInactive: includeInactive, ct: ct);

    public async Task<MilkTypeDto?> GetByIdAsync(
        int id, CancellationToken ct) =>
        (await ExecuteAsync("GETBYID", id: id, ct: ct)).FirstOrDefault();

    public async Task<MilkTypeDto> CreateAsync(
        SaveMilkTypeRequest request, CancellationToken ct)
    {
        var result = await ExecuteAsync("CREATE", request: request, ct: ct);
        return result.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Create milk type returned no data.");
    }

    public async Task<MilkTypeDto?> UpdateAsync(
        int id, SaveMilkTypeRequest request, CancellationToken ct) =>
        (await ExecuteAsync(
            "UPDATE", id: id, request: request, ct: ct)).FirstOrDefault();

    public async Task<MilkTypeDto?> SetActiveAsync(
        int id, bool isActive, CancellationToken ct) =>
        (await ExecuteAsync(
            isActive ? "ACTIVATE" : "DEACTIVATE",
            id: id, ct: ct)).FirstOrDefault();
}
