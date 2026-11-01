using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.Chat;

public sealed class SendChatMessageRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string MessageText { get; set; } = "";
}