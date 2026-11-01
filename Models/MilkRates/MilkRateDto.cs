
namespace FreshMilk.API.Models.MilkRates;

public sealed class MilkRateDto
{
    public long MilkRateId { get; set; }
    public int MilkTypeId { get; set; }
    public string MilkTypeCode { get; set; } = "";
    public string MilkTypeName { get; set; } = "";
    public decimal SupplierRate { get; set; }
    public decimal ConsumerRate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
