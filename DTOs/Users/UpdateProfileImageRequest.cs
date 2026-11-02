using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.DTOs.Users;

public class UpdateProfileImageRequest
{
    [Required]
    [RegularExpression(@"^[0-9]{10}$",
        ErrorMessage = "Enter a valid 10-digit mobile number.")]
    public string ActorPhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string ActorPassword { get; set; } = string.Empty;

    [Required]
    public string ImageBase64 { get; set; } = string.Empty;

    [Required]
    public string ContentType { get; set; } = string.Empty;
}
