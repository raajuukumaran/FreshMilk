
using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.MilkRates;

public sealed class SaveMilkRateRequest
{
    [Range(1, int.MaxValue)]
    public int MilkTypeId { get; set; }

    [Range(typeof(decimal), "0.01", "99999999.99")]
    public decimal SupplierRate { get; set; }

    [Range(typeof(decimal), "0.01", "99999999.99")]
    public decimal ConsumerRate { get; set; }

    // Null means use the SQL Server's current date/time.
    public DateTime? EffectiveFrom { get; set; }
}
