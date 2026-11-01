using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API.Services;

public sealed class Msg91SmsService : IMsg91SmsService
{
    private const string Endpoint = "https://control.msg91.com/api/v5/flow/";
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Msg91SmsService> _logger;

    public Msg91SmsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<Msg91SmsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Msg91:AuthKey"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Msg91:TemplateId"]);

    public async Task SendOtpAsync(
        string mobileNumber,
        string otp,
        CancellationToken cancellationToken)
    {
        var authKey = _configuration["Msg91:AuthKey"];
        var templateId = _configuration["Msg91:TemplateId"];
        if (string.IsNullOrWhiteSpace(authKey) ||
            string.IsNullOrWhiteSpace(templateId))
        {
            throw new InvalidOperationException(
                "MSG91 is not configured. Set Msg91__AuthKey and Msg91__TemplateId.");
        }

        var countryCode = _configuration["Msg91:CountryCode"] ?? "91";
        countryCode = countryCode.Trim().TrimStart('+');
        if (countryCode.Length == 0 || countryCode.Any(character => !char.IsDigit(character)))
        {
            throw new InvalidOperationException(
                "Msg91:CountryCode must contain digits only.");
        }

        var otpVariable = _configuration["Msg91:OtpVariable"] ?? "VAR1";
        if (otpVariable.Length == 0 ||
            !char.IsLetter(otpVariable[0]) ||
            otpVariable.Any(character => !char.IsLetterOrDigit(character) && character != '_'))
        {
            throw new InvalidOperationException(
                "Msg91:OtpVariable must be a valid template variable name.");
        }

        var recipient = new Dictionary<string, string>
        {
            ["mobiles"] = $"{countryCode}{mobileNumber}",
            [otpVariable] = otp,
        };
        var payload = new
        {
            template_id = templateId,
            short_url = "0",
            recipients = new[] { recipient },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.TryAddWithoutValidation("authkey", authKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "MSG91 OTP delivery failed with HTTP status {StatusCode}.",
                (int)response.StatusCode);
            throw new HttpRequestException(
                "MSG91 did not accept the OTP delivery request.",
                null,
                response.StatusCode);
        }

        Msg91Response? providerResponse;
        try
        {
            providerResponse =
                await response.Content.ReadFromJsonAsync<Msg91Response>(
                    cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "MSG91 returned an invalid response for OTP delivery.");
            throw new HttpRequestException(
                "MSG91 returned an invalid response.",
                exception,
                response.StatusCode);
        }

        if (string.Equals(
            providerResponse?.Type, "error", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "MSG91 OTP delivery failed with HTTP status {StatusCode} and response type {ResponseType}.",
                (int)response.StatusCode,
                providerResponse?.Type ?? "unspecified");

            throw new HttpRequestException(
                "MSG91 did not accept the OTP delivery request.",
                null,
                response.StatusCode);
        }
    }

    private sealed class Msg91Response
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }
    }
}
