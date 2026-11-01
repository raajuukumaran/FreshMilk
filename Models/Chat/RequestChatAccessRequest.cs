using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.Chat;

public sealed class RequestChatAccessRequest
{
    [Range(1, long.MaxValue)]
    public long TargetUserId { get; set; }

    [Required]
    public string TargetRole { get; set; } = "";

    public long? RelatedSupplyId { get; set; }

    [StringLength(500)]
    public string? RequestNote { get; set; }
}