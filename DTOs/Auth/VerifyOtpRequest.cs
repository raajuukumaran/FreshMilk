using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.DTOs.Auth;

public class VerifyOtpRequest
{
    [Required]
    [RegularExpression(@"^[0-9]{10}$")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9]{6}$")]
    public string Otp { get; set; } = string.Empty;
}