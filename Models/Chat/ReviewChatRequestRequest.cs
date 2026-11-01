using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.Chat;

public sealed class ReviewChatRequestRequest
{
    [Range(5, 1440)]
    public int AccessMinutes { get; set; } = 60;
}