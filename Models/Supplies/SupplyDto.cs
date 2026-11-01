namespace FreshMilk.API.Models.Supplies;

public sealed class SupplyDto
{
    public long SupplyId { get; set; }
    public long SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
    public string SupplierPhone { get; set; } = "";

    public int MilkTypeId { get; set; }
    public string MilkTypeCode { get; set; } = "";
    public string MilkTypeName { get; set; } = "";

    public long MilkRateId { get; set; }
    public DateTime SupplyDate { get; set; }
    public string Slot { get; set; } = "";
    public string? SlotLabel { get; set; }

    public decimal Litres { get; set; }
    public decimal SupplierRate { get; set; }
    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "";
    public string? ChangeNote { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}