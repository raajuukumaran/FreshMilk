namespace FreshMilk.API.Models.Users;

public class UserDto
{
    public long UserId { get; set; }
    public string? UserCode { get; set; }
    public string? UserRole { get; set; }
    public string? FullName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? VillageOrTown { get; set; }
    public string? District { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Pincode { get; set; }
    public string? PreferredLanguage { get; set; }
    public bool IsActive { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}