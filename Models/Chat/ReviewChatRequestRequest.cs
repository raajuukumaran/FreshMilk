using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.Chat;

public sealed class ReviewChatRequestRequest
{
    [Required]
    public DateOnly? AccessStartDate { get; set; }

    [Required]
    public DateOnly? AccessEndDate { get; set; }
}