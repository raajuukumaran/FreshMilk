using Microsoft.AspNetCore.Mvc;
using FreshMilk.API.DTOs.Auth;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    // POST: api/auth/send-otp
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp(
        [FromBody] SendOtpRequest request,
        CancellationToken cancellationToken)
    {
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

        // Development testing only. Integrate an SMS provider
        // before using this flow in production.
        if (_environment.IsDevelopment())
        {
            return Ok(new
            {
                success = true,
                message = "OTP generated for development testing.",
                otp = result.OTP
            });
        }

        return StatusCode(
            StatusCodes.Status501NotImplemented,
            new
            {
                success = false,
                message = "SMS delivery is not configured yet."
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