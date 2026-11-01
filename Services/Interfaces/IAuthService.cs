using FreshMilk.API.Repositories.Interfaces;

namespace FreshMilk.API.Services.Interfaces;

public interface IAuthService
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