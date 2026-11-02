using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Services;

public sealed class CallAccessTokenService : ICallAccessTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(30);
    private readonly IDataProtector _protector;

    public CallAccessTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("FreshMilk.CallAccessToken.v1");
    }

    public string Create(long userId, string? role)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        var normalizedRole = role?.Trim().ToUpperInvariant();
        if (normalizedRole is not ("OWNER" or "SUPPLIER" or "CONSUMER"))
            throw new ArgumentException("A valid user role is required.", nameof(role));

        var payload = JsonSerializer.Serialize(new TokenPayload(
            userId,
            normalizedRole,
            DateTimeOffset.UtcNow.Add(TokenLifetime).ToUnixTimeSeconds()));
        return _protector.Protect(payload);
    }

    public bool TryValidate(string? token, out CallAccessIdentity? identity)
    {
        identity = null;
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var payload = JsonSerializer.Deserialize<TokenPayload>(
                _protector.Unprotect(token));
            if (payload is null ||
                payload.UserId <= 0 ||
                payload.ExpiresAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() ||
                payload.Role is not ("OWNER" or "SUPPLIER" or "CONSUMER"))
            {
                return false;
            }

            identity = new CallAccessIdentity(payload.UserId, payload.Role);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record TokenPayload(long UserId, string Role, long ExpiresAt);
}
