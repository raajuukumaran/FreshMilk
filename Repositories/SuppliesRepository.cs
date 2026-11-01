using System.Data;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Models.Supplies;
using FreshMilk.API.Repositories.Interfaces;

namespace FreshMilk.API.Repositories;

public sealed class SuppliesRepository : ISuppliesRepository
{
    private readonly string _connectionString;

    public SuppliesRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    private SqlCommand CreateCommand(
        SqlConnection connection,
        string action,
        long actorUserId,
        string actorRole,
        long? supplyId = null,
        SaveSupplyRequest? request = null,
        string? changeNote = null,
        DateTime? supplyDate = null)
    {
        var command = new SqlCommand("dbo.ManageSupplies", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@Action", SqlDbType.NVarChar, 30).Value = action;
        command.Parameters.Add("@ActorUserId", SqlDbType.BigInt).Value = actorUserId;
        command.Parameters.Add("@ActorRole", SqlDbType.NVarChar, 20).Value = actorRole;
        command.Parameters.Add("@SupplyId", SqlDbType.BigInt).Value =
            (object?)supplyId ?? DBNull.Value;

        command.Parameters.Add("@SupplierId", SqlDbType.BigInt).Value =
            request is null ? DBNull.Value : request.SupplierId;

        command.Parameters.Add("@MilkTypeId", SqlDbType.Int).Value =
            request is null ? DBNull.Value : request.MilkTypeId;

        var litres = command.Parameters.Add("@Litres", SqlDbType.Decimal);
        litres.Precision = 10;
        litres.Scale = 2;
        litres.Value = request is null ? DBNull.Value : request.Litres;

        command.Parameters.Add("@Slot", SqlDbType.NVarChar, 20).Value =
            request is null ? DBNull.Value : request.Slot;

        command.Parameters.Add("@SlotLabel", SqlDbType.NVarChar, 50).Value =
            (object?)request?.SlotLabel ?? DBNull.Value;

        command.Parameters.Add("@ChangeNote", SqlDbType.NVarChar, 500).Value =
            (object?)changeNote ?? DBNull.Value;

        command.Parameters.Add("@SupplyDate", SqlDbType.Date).Value =
            request?.SupplyDate?.Date
            ?? supplyDate?.Date
            ?? (object)DBNull.Value;

        return command;
    }

    private static SupplyDto Map(SqlDataReader reader)
    {
        return new SupplyDto
        {
            SupplyId = reader.GetInt64(reader.GetOrdinal("SupplyId")),
            SupplierId = reader.GetInt64(reader.GetOrdinal("SupplierId")),
            SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
            SupplierPhone = reader.GetString(reader.GetOrdinal("SupplierPhone")),
            MilkTypeId = reader.GetInt32(reader.GetOrdinal("MilkTypeId")),
            MilkTypeCode = reader.GetString(reader.GetOrdinal("MilkTypeCode")),
            MilkTypeName = reader.GetString(reader.GetOrdinal("MilkTypeName")),
            MilkRateId = reader.GetInt64(reader.GetOrdinal("MilkRateId")),
            SupplyDate = reader.GetDateTime(reader.GetOrdinal("SupplyDate")),
            Slot = reader.GetString(reader.GetOrdinal("Slot")),
            SlotLabel = reader.IsDBNull(reader.GetOrdinal("SlotLabel"))
                ? null : reader.GetString(reader.GetOrdinal("SlotLabel")),
            Litres = reader.GetDecimal(reader.GetOrdinal("Litres")),
            SupplierRate = reader.GetDecimal(reader.GetOrdinal("SupplierRate")),
            TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            ChangeNote = reader.IsDBNull(reader.GetOrdinal("ChangeNote"))
                ? null : reader.GetString(reader.GetOrdinal("ChangeNote")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = ReadNullableDateTime(reader, "UpdatedAt"),
            ConfirmedAt = ReadNullableDateTime(reader, "ConfirmedAt"),
            CanEdit = reader.GetBoolean(reader.GetOrdinal("CanEdit")),
            CanDelete = reader.GetBoolean(reader.GetOrdinal("CanDelete"))
        };
    }

    private static DateTime? ReadNullableDateTime(
        SqlDataReader reader, string column)
    {
        var index = reader.GetOrdinal(column);
        return reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }

    private async Task<List<SupplyDto>> QueryAsync(
        string action,
        long actorUserId,
        string actorRole,
        long? supplyId = null,
        SaveSupplyRequest? request = null,
        string? changeNote = null,
        DateTime? supplyDate = null,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(
            connection, action, actorUserId, actorRole,
            supplyId, request, changeNote, supplyDate);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var result = new List<SupplyDto>();
        while (await reader.ReadAsync(ct))
            result.Add(Map(reader));

        return result;
    }

    public Task<List<SupplyDto>> GetAllAsync(
        long actorUserId, string actorRole,
        DateTime? supplyDate, CancellationToken ct) =>
        QueryAsync("GETALL", actorUserId, actorRole,
            supplyDate: supplyDate, ct: ct);

    public async Task<SupplyDto?> GetByIdAsync(
        long id, long actorUserId, string actorRole,
        CancellationToken ct) =>
        (await QueryAsync("GETBYID", actorUserId, actorRole,
            supplyId: id, ct: ct)).FirstOrDefault();

    public async Task<SupplyDto?> CreateAsync(
        SaveSupplyRequest request,
        long actorUserId, string actorRole, CancellationToken ct) =>
        (await QueryAsync("CREATE", actorUserId, actorRole,
            request: request, ct: ct)).FirstOrDefault();

    public async Task<SupplyDto?> UpdateAsync(
        long id, SaveSupplyRequest request,
        long actorUserId, string actorRole, CancellationToken ct) =>
        (await QueryAsync("UPDATE", actorUserId, actorRole,
            supplyId: id, request: request, ct: ct)).FirstOrDefault();

    public async Task<bool> DeleteAsync(
        long id, long actorUserId, string actorRole, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = CreateCommand(
            connection, "DELETE", actorUserId, actorRole, supplyId: id);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);

        return await reader.ReadAsync(ct)
            && reader.GetBoolean(reader.GetOrdinal("Success"));
    }

    public async Task<SupplyDto?> ConfirmAsync(
        long id, long actorUserId, string actorRole, CancellationToken ct) =>
        (await QueryAsync("CONFIRM", actorUserId, actorRole,
            supplyId: id, ct: ct)).FirstOrDefault();

    public async Task<SupplyDto?> RequestChangeAsync(
        long id, string changeNote,
        long actorUserId, string actorRole, CancellationToken ct) =>
        (await QueryAsync("REQUESTCHANGE", actorUserId, actorRole,
            supplyId: id, changeNote: changeNote, ct: ct)).FirstOrDefault();
}