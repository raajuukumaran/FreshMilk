
using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.MilkTypes;

public sealed class SaveMilkTypeRequest
{
    [Required, StringLength(30)]
    public string MilkTypeCode { get; set; } = "";

    [Required, StringLength(100)]
    public string MilkTypeName { get; set; } = "";

    [StringLength(250)]
    public string? Description { get; set; }
}
