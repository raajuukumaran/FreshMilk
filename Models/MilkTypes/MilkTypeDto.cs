
namespace FreshMilk.API.Models.MilkTypes;

public sealed class MilkTypeDto
{
    public int MilkTypeId { get; set; }
    public string MilkTypeCode { get; set; } = "";
    public string MilkTypeName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
