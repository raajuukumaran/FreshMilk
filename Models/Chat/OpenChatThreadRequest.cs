using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.Chat;

public sealed class OpenChatThreadRequest
{
    [Range(1, long.MaxValue)]
    public long ParticipantUserId { get; set; }

    [Required]
    public string ParticipantRole { get; set; } = "";

    public long? RelatedSupplyId { get; set; }
}