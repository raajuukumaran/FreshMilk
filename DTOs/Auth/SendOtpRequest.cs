using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.DTOs.Auth;

public class SendOtpRequest
{
    [Required]
    [RegularExpression(@"^[0-9]{10}$",
        ErrorMessage = "Enter a valid 10-digit mobile number.")]
    public string MobileNumber { get; set; } = string.Empty;
}