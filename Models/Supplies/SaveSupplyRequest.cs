using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.Supplies;

public sealed class SaveSupplyRequest
{
    [Range(1, long.MaxValue)]
    public long SupplierId { get; set; }

    [Range(1, int.MaxValue)]
    public int MilkTypeId { get; set; }

    [Range(typeof(decimal), "0.01", "999999.99")]
    public decimal Litres { get; set; }

    [Required]
    [RegularExpression("^(morning|evening|custom)$")]
    public string Slot { get; set; } = "morning";

    [MaxLength(50)]
    public string? SlotLabel { get; set; }

    public DateTime? SupplyDate { get; set; }
}