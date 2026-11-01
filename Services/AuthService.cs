using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;

    public AuthService(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public Task<GenerateOtpResult> GenerateOtpAsync(
        string mobileNumber,
        CancellationToken cancellationToken)
    {
        return _authRepository.GenerateOtpAsync(
            mobileNumber,
            cancellationToken);
    }

    public Task<VerifyOtpResult> VerifyOtpAsync(
        string mobileNumber,
        string otp,
        CancellationToken cancellationToken)
    {
        return _authRepository.VerifyOtpAsync(
            mobileNumber,
            otp,
            cancellationToken);
    }

    public Task<VerifyOtpResult> LoginWithPasswordAsync(
        string mobileNumber,
        string password,
        CancellationToken cancellationToken)
    {
        return _authRepository.LoginWithPasswordAsync(
            mobileNumber,
            password,
            cancellationToken);
    }
}