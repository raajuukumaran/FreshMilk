
using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.ConsumerOrders;

public sealed class UpdateConsumerOrderStatusRequest
{
    [Required]
    [RegularExpression(
        "^(Confirmed|Delivered|Cancelled)$",
        ErrorMessage = "Status must be Confirmed, Delivered, or Cancelled.")]
    public string Status { get; init; } = string.Empty;
}
