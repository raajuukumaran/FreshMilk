using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.Supplies;

public sealed class RequestSupplyChange
{
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string ChangeNote { get; set; } = "";
}