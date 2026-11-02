using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Responses.Notifications;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Services;

public sealed class ExpoPushService : IExpoPushService
{
    private const string ExpoPushUrl = "https://exp.host/--/api/v2/push/send";
    private readonly HttpClient _httpClient;
    private readonly IExpoPushTokenRepository _tokens;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpoPushService> _logger;

    public ExpoPushService(
        HttpClient httpClient,
        IExpoPushTokenRepository tokens,
        IConfiguration configuration,
        ILogger<ExpoPushService> logger)
    {
        _httpClient = httpClient;
        _tokens = tokens;
        _configuration = configuration;
        _logger = logger;
    }

    public Task RegisterAsync(
        long userId, string userRole, string expoPushToken, CancellationToken ct)
    {
        if (userId <= 0) throw new ArgumentException("Valid actor user ID is required.");
        if (!Regex.IsMatch(
            expoPushToken,
            @"^(ExpoPushToken|ExponentPushToken)\[[A-Za-z0-9._-]{8,}\]$"))
            throw new ArgumentException("A valid Expo push token is required.");
        return _tokens.RegisterAsync(
            userId, NormalizeRole(userRole), expoPushToken, ct);
    }

    public Task UnregisterAsync(
        long userId, string userRole, string expoPushToken, CancellationToken ct) =>
        _tokens.UnregisterAsync(userId, NormalizeRole(userRole), expoPushToken, ct);

    public async Task SendAsync(
        AppNotificationResponse notification, string userRole, CancellationToken ct)
    {
        List<string> tokens;
        try
        {
            tokens = await _tokens.GetForUserAsync(
                notification.ToUserId, NormalizeRole(userRole), ct);
        }
        catch (SqlException exception)
        {
            _logger.LogError(
                exception,
                "Could not load push tokens for notification {NotificationId}.",
                notification.NotificationId);
            return;
        }

        foreach (var token in tokens)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, ExpoPushUrl)
                {
                    Content = JsonContent.Create(new ExpoPushMessage
                    {
                        To = token,
                        Title = notification.Title,
                        Body = notification.Body,
                        Data = new
                        {
                            notificationId = notification.NotificationId,
                            notification.RelatedSupplyId,
                            notification.RelatedOrderId,
                            notification.RelatedThreadId,
                            notification.RelatedChatRequestId,
                        },
                    }),
                };
                request.Headers.Accept.ParseAdd("application/json");
                var accessToken = _configuration["ExpoPush:AccessToken"];
                if (!string.IsNullOrWhiteSpace(accessToken))
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue(
                            "Bearer", accessToken);

                using var response = await _httpClient.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Expo push send failed with status {StatusCode} for notification {NotificationId}: {ResponseBody}",
                        (int)response.StatusCode,
                        notification.NotificationId,
                        body);
                    continue;
                }

                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                var result = JsonSerializer.Deserialize<ExpoPushResponse>(body, options);
                foreach (var error in result?.Errors ?? [])
                {
                    _logger.LogWarning(
                        "Expo rejected notification {NotificationId}: {ErrorCode} {ErrorMessage}",
                        notification.NotificationId,
                        error.Code,
                        error.Message);
                }
                var tickets = result?.Data.ValueKind switch
                {
                    JsonValueKind.Array => result.Data
                        .EnumerateArray()
                        .Select(item => item.Deserialize<ExpoPushTicket>(options))
                        .Where(item => item is not null)
                        .Select(item => item!),
                    JsonValueKind.Object => new[]
                    {
                        result.Data.Deserialize<ExpoPushTicket>(options),
                    }.Where(item => item is not null).Select(item => item!),
                    _ => Enumerable.Empty<ExpoPushTicket>(),
                };
                foreach (var ticket in tickets.Where(item => item.Status == "error"))
                {
                    var error = ticket.Details?.Error;
                    _logger.LogWarning(
                        "Expo rejected push token for notification {NotificationId}: {Error}",
                        notification.NotificationId,
                        error ?? "Unknown ticket error");
                    if (error == "DeviceNotRegistered")
                    {
                        await _tokens.UnregisterAsync(
                            notification.ToUserId, userRole, token, ct);
                    }
                }
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Could not send push notification {NotificationId} through Expo.",
                    notification.NotificationId);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Expo push request timed out for notification {NotificationId}.",
                    notification.NotificationId);
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Expo returned an unreadable push response for notification {NotificationId}.",
                    notification.NotificationId);
            }
        }
    }

    private static string NormalizeRole(string role)
    {
        var normalized = role.Trim().ToUpperInvariant();
        if (normalized is not ("OWNER" or "SUPPLIER" or "CONSUMER"))
            throw new ArgumentException("Role must be Owner, Supplier, or Consumer.");
        return normalized;
    }

    private sealed class ExpoPushMessage
    {
        public required string To { get; init; }
        public string Sound { get; init; } = "default";
        public required string Title { get; init; }
        public required string Body { get; init; }
        public object? Data { get; init; }
        public string Priority { get; init; } = "high";
    }

    private sealed class ExpoPushResponse
    {
        public JsonElement Data { get; init; }
        public List<ExpoPushError>? Errors { get; init; }
    }

    private sealed class ExpoPushTicket
    {
        public string? Status { get; init; }
        public ExpoPushDetails? Details { get; init; }
    }

    private sealed class ExpoPushDetails
    {
        public string? Error { get; init; }
    }

    private sealed class ExpoPushError
    {
        public string? Code { get; init; }
        public string? Message { get; init; }
    }
}
