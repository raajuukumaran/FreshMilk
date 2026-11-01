
using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.ConsumerOrders;

public sealed class SaveConsumerOrderRequest
{
    [Required]
    public int MilkTypeId { get; init; }

    [Range(typeof(decimal), "0.01", "99999999")]
    public decimal Litres { get; init; }

    public DateTime? DeliveryDate { get; init; }

    [Required]
    [RegularExpression(
        "^(morning|evening)$",
        ErrorMessage = "Delivery slot must be morning or evening.")]
    public string DeliverySlot { get; init; } = "morning";

    [StringLength(300)]
    public string? DeliveryAddress { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}
