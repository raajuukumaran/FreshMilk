namespace FreshMilk.API.Repositories.Interfaces;

public interface IAuthRepository
{
    Task<GenerateOtpResult> GenerateOtpAsync(
        string mobileNumber,
        CancellationToken cancellationToken);

    Task<VerifyOtpResult> VerifyOtpAsync(
        string mobileNumber,
        string otp,
        CancellationToken cancellationToken);

    Task<VerifyOtpResult> LoginWithPasswordAsync(
        string mobileNumber,
        string password,
        CancellationToken cancellationToken);
}

public class GenerateOtpResult
{
    public bool Success { get; set; }
    public int? UserId { get; set; }
    public string? OTP { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class VerifyOtpResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public VerifiedUser? User { get; set; }
}

public class VerifiedUser
{
    public int UserId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FullName { get; set; }
    public string? UserRole { get; set; }
}