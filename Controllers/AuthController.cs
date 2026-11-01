using Microsoft.AspNetCore.Mvc;
using FreshMilk.API.DTOs.Auth;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMsg91SmsService _msg91SmsService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IMsg91SmsService msg91SmsService,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _msg91SmsService = msg91SmsService;
        _environment = environment;
        _logger = logger;
    }

    // POST: api/auth/send-otp
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp(
        [FromBody] SendOtpRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() && !_msg91SmsService.IsConfigured)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    success = false,
                    message = "OTP delivery is temporarily unavailable."
                });
        }

        var result = await _authService.GenerateOtpAsync(
            request.MobileNumber,
            cancellationToken);

        if (!result.Success)
        {
            return NotFound(new
            {
                success = false,
                message = result.Message
            });
        }

        if (_environment.IsDevelopment())
        {
            return Ok(new
            {
                success = true,
                message = "OTP generated for development testing.",
                otp = result.OTP
            });
        }

        if (string.IsNullOrWhiteSpace(result.OTP))
        {
            _logger.LogError(
                "OTP generation succeeded without returning an OTP for user {UserId}.",
                result.UserId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message = "Unable to generate an OTP. Please try again."
                });
        }

        try
        {
            await _msg91SmsService.SendOtpAsync(
                request.MobileNumber,
                result.OTP,
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "MSG91 could not deliver an OTP to user {UserId}.",
                result.UserId);
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    success = false,
                    message = "OTP delivery failed. Please try again shortly."
                });
        }

        return Ok(new
        {
            success = true,
            message = "OTP sent successfully."
        });
    }

    // POST: api/auth/verify-otp
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyOtpAsync(
            request.MobileNumber,
            request.Otp,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            user = result.User
        });
    }
}