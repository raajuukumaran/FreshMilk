
using System.Data;
using FreshMilk.API.Data;
using FreshMilk.API.Models.ConsumerOrders;
using FreshMilk.API.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace FreshMilk.API.Repositories;

public sealed class ConsumerOrdersRepository : IConsumerOrdersRepository
{
    private readonly string _connectionString;

    public ConsumerOrdersRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing.");
    }

    public async Task<ConsumerOrderDto?> CreateAsync(
        SaveConsumerOrderRequest request,
        long actorUserId,
        CancellationToken ct)
    {
        var parameters = new[]
        {
            new SqlParameter("@Action", SqlDbType.VarChar, 20)
                { Value = "CREATE" },

            new SqlParameter("@ActorUserId", SqlDbType.BigInt)
                { Value = actorUserId },

            new SqlParameter("@MilkTypeId", SqlDbType.Int)
                { Value = request.MilkTypeId },

            new SqlParameter("@Litres", SqlDbType.Decimal)
            {
                Precision = 10,
                Scale = 2,
                Value = request.Litres
            },

            new SqlParameter("@DeliveryDate", SqlDbType.Date)
                { Value = request.DeliveryDate.HasValue
                    ? request.DeliveryDate.Value.Date
                    : DBNull.Value },

            new SqlParameter("@DeliverySlot", SqlDbType.VarChar, 20)
                { Value = request.DeliverySlot.Trim().ToLowerInvariant() },

            new SqlParameter("@DeliveryAddress", SqlDbType.NVarChar, 300)
                { Value = (object?)request.DeliveryAddress ?? DBNull.Value },

            new SqlParameter("@Notes", SqlDbType.NVarChar, 500)
                { Value = (object?)request.Notes ?? DBNull.Value }
        };

        return await ExecuteSingleAsync(parameters, ct);
    }

    public async Task<List<ConsumerOrderDto>> GetMineAsync(
        long actorUserId,
        CancellationToken ct)
    {
        var parameters = new[]
        {
            new SqlParameter("@Action", SqlDbType.VarChar, 20)
                { Value = "GETMINE" },

            new SqlParameter("@ActorUserId", SqlDbType.BigInt)
                { Value = actorUserId }
        };

        return await ExecuteListAsync(parameters, ct);
    }

    public async Task<List<ConsumerOrderDto>> GetAllAsync(
        long actorUserId,
        CancellationToken ct)
    {
        var parameters = new[]
        {
            new SqlParameter("@Action", SqlDbType.VarChar, 20)
                { Value = "GETALL" },

            new SqlParameter("@ActorUserId", SqlDbType.BigInt)
                { Value = actorUserId }
        };

        return await ExecuteListAsync(parameters, ct);
    }

    public async Task<ConsumerOrderDto?> UpdateStatusAsync(
        long orderId,
        string status,
        long actorUserId,
        CancellationToken ct)
    {
        var parameters = new[]
        {
            new SqlParameter("@Action", SqlDbType.VarChar, 20)
                { Value = "UPDATESTATUS" },

            new SqlParameter("@ActorUserId", SqlDbType.BigInt)
                { Value = actorUserId },

            new SqlParameter("@OrderId", SqlDbType.BigInt)
                { Value = orderId },

            new SqlParameter("@Status", SqlDbType.VarChar, 20)
                { Value = status }
        };

        return await ExecuteSingleAsync(parameters, ct);
    }

    private async Task<ConsumerOrderDto?> ExecuteSingleAsync(
        SqlParameter[] parameters,
        CancellationToken ct)
    {
        var results = await ExecuteListAsync(parameters, ct);
        return results.FirstOrDefault();
    }

    private async Task<List<ConsumerOrderDto>> ExecuteListAsync(
        SqlParameter[] parameters,
        CancellationToken ct)
    {
        var results = new List<ConsumerOrderDto>();

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            StoredProcedure.ConsumerOrders.Manage, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddRange(parameters);

        await connection.OpenAsync(ct);

        await using var reader = await command.ExecuteReaderAsync(ct);

        var idOrdinal = reader.GetOrdinal("Id");
        var consumerIdOrdinal = reader.GetOrdinal("ConsumerId");
        var consumerNameOrdinal = reader.GetOrdinal("ConsumerName");
        var milkTypeIdOrdinal = reader.GetOrdinal("MilkTypeId");
        var labelOrdinal = reader.GetOrdinal("Label");
        var litresOrdinal = reader.GetOrdinal("Litres");
        var rateOrdinal = reader.GetOrdinal("RatePerLitre");
        var totalOrdinal = reader.GetOrdinal("Total");
        var deliveryDateOrdinal = reader.GetOrdinal("DeliveryDate");
        var slotOrdinal = reader.GetOrdinal("DeliverySlot");
        var addressOrdinal = reader.GetOrdinal("DeliveryAddress");
        var notesOrdinal = reader.GetOrdinal("Notes");
        var statusOrdinal = reader.GetOrdinal("Status");
        var placedAtOrdinal = reader.GetOrdinal("PlacedAt");
        var updatedAtOrdinal = reader.GetOrdinal("UpdatedAt");
        var deliveredAtOrdinal = reader.GetOrdinal("DeliveredAt");

        var phoneOrdinal = TryGetOrdinal(reader, "ConsumerPhone");

        while (await reader.ReadAsync(ct))
        {
            results.Add(new ConsumerOrderDto
            {
                Id = reader.GetInt64(idOrdinal),
                ConsumerId = reader.GetInt64(consumerIdOrdinal),
                ConsumerName = reader.GetString(consumerNameOrdinal),

                ConsumerPhone = phoneOrdinal >= 0 &&
                    !reader.IsDBNull(phoneOrdinal)
                        ? reader.GetString(phoneOrdinal)
                        : null,

                MilkTypeId = reader.GetInt32(milkTypeIdOrdinal),
                Label = reader.GetString(labelOrdinal),
                Litres = reader.GetDecimal(litresOrdinal),
                RatePerLitre = reader.GetDecimal(rateOrdinal),
                Total = reader.GetDecimal(totalOrdinal),

                DeliveryDate = reader.GetDateTime(deliveryDateOrdinal),
                DeliverySlot = reader.GetString(slotOrdinal),

                DeliveryAddress = reader.IsDBNull(addressOrdinal)
                    ? null : reader.GetString(addressOrdinal),

                Notes = reader.IsDBNull(notesOrdinal)
                    ? null : reader.GetString(notesOrdinal),

                Status = reader.GetString(statusOrdinal),
                PlacedAt = reader.GetDateTime(placedAtOrdinal),

                UpdatedAt = reader.IsDBNull(updatedAtOrdinal)
                    ? null : reader.GetDateTime(updatedAtOrdinal),

                DeliveredAt = reader.IsDBNull(deliveredAtOrdinal)
                    ? null : reader.GetDateTime(deliveredAtOrdinal)
            });
        }

        return results;
    }

    private static int TryGetOrdinal(
        SqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(
                reader.GetName(i), columnName,
                StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
