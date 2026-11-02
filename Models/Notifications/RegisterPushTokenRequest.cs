using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.Models.Notifications;

public sealed class RegisterPushTokenRequest
{
    [Required]
    [StringLength(255)]
    public string ExpoPushToken { get; init; } = "";
}
