namespace FreshMilk.API.Services.Interfaces;

public sealed record CallAccessIdentity(long UserId, string Role);

public interface ICallAccessTokenService
{
    string Create(long userId, string? role);
    bool TryValidate(string? token, out CallAccessIdentity? identity);
}
