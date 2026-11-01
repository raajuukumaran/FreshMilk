using System.ComponentModel.DataAnnotations;

namespace FreshMilk.API.DTOs.Users;

public class SaveUserRequest
{
    [Required]
    [RegularExpression("^(owner|supplier|consumer)$",
        ErrorMessage = "Role must be owner, supplier, or consumer.")]
    public string UserRole { get; set; } = "consumer";

    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9]{10}$",
        ErrorMessage = "Enter a valid 10-digit mobile number.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(128, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*\S).+$",
        ErrorMessage = "Password cannot contain only whitespace.")]
    public string? Password { get; set; }

    [EmailAddress, StringLength(255)]
    public string? Email { get; set; }

    public string? ProfileImageUrl { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? VillageOrTown { get; set; }
    public string? District { get; set; }
    public string? State { get; set; }
    public string Country { get; set; } = "India";
    public string? Pincode { get; set; }
    public string PreferredLanguage { get; set; } = "ta";
}