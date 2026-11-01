using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.DTOs.Auth;

public class PasswordLoginRequest
{
    [Required]
    [RegularExpression(@"^[0-9]{10}$",
        ErrorMessage = "Enter a valid 10-digit mobile number.")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
