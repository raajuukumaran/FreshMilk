namespace FreshMilk.API.Services.Interfaces;

public interface IMsg91SmsService
{
    bool IsConfigured { get; }

    Task SendOtpAsync(
        string mobileNumber,
        string otp,
        CancellationToken cancellationToken);
}
