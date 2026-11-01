
namespace FreshMilk.API.Models.ConsumerOrders;

public sealed class ConsumerOrderDto
{
    public long Id { get; set; }
    public long ConsumerId { get; set; }
    public string ConsumerName { get; set; } = string.Empty;
    public string? ConsumerPhone { get; set; }

    public int MilkTypeId { get; set; }
    public string Label { get; set; } = string.Empty;

    public decimal Litres { get; set; }
    public decimal RatePerLitre { get; set; }
    public decimal Total { get; set; }

    public DateTime DeliveryDate { get; set; }
    public string DeliverySlot { get; set; } = string.Empty;

    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;

    public DateTime PlacedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
