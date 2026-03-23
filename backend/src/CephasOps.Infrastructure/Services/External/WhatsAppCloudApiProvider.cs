using CephasOps.Domain.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CephasOps.Infrastructure.Services.External;

/// <summary>
/// WhatsApp Cloud API provider implementation
/// Sends template messages via Meta's WhatsApp Cloud API
/// </summary>
public class WhatsAppCloudApiProvider : IWhatsAppProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppCloudApiProvider> _logger;

    private string? _phoneNumberId;
    private string? _accessToken;
    private string? _businessAccountId;
    private string? _apiVersion;

    public WhatsAppCloudApiProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WhatsAppCloudApiProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private void LoadConfiguration()
    {
        if (_phoneNumberId != null && _accessToken != null)
            return;

        var section = _configuration.GetSection("WhatsAppCloudApi");
        _phoneNumberId = section["PhoneNumberId"];
        _accessToken = section["AccessToken"];
        _businessAccountId = section["BusinessAccountId"];
        _apiVersion = section["ApiVersion"] ?? "v18.0";

        if (string.IsNullOrEmpty(_phoneNumberId) || string.IsNullOrEmpty(_accessToken))
        {
            throw new InvalidOperationException(
                "WhatsApp Cloud API credentials are not configured. " +
                "Please set WhatsAppCloudApi:PhoneNumberId and WhatsAppCloudApi:AccessToken in appsettings.json");
        }
    }

    /// <summary>
    /// Send a simple text WhatsApp message (not template-based)
    /// </summary>
    public async Task<WhatsAppResult> SendMessageAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            LoadConfiguration();

            // Format phone number (remove + and ensure E.164 format)
            var phoneNumber = FormatPhoneNumber(to);

            _logger.LogInformation("Sending WhatsApp message via Cloud API to {PhoneNumber}", phoneNumber);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var url = $"https://graph.facebook.com/{_apiVersion}/{_phoneNumberId}/messages";

            var requestBody = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = phoneNumber,
                type = "text",
                text = new
                {
                    preview_url = false,
                    body = message
                }
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<WhatsAppCloudApiResponse>(responseContent);
                _logger.LogInformation("WhatsApp message sent successfully. MessageId: {MessageId}", result?.Messages?[0]?.Id);

                return WhatsAppResult.SuccessResult(
                    result?.Messages?[0]?.Id ?? "unknown",
                    "sent",
                    DateTime.UtcNow
                );
            }
            else
            {
                var error = JsonSerializer.Deserialize<WhatsAppCloudApiError>(responseContent);
                _logger.LogError("WhatsApp Cloud API error. Status: {Status}, Error: {Error}", 
                    response.StatusCode, responseContent);

                return WhatsAppResult.FailedResult(
                    error?.Error?.Message ?? $"HTTP {response.StatusCode}: {responseContent}",
                    error?.Error?.Code?.ToString()
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message via Cloud API to {To}", to);
            return WhatsAppResult.FailedResult(
                $"Failed to send WhatsApp message: {ex.Message}",
                ex.GetType().Name
            );
        }
    }

    /// <summary>
    /// Send a WhatsApp template message with dynamic parameters
    /// </summary>
    public async Task<WhatsAppResult> SendTemplateMessageAsync(
        string to,
        string templateName,
        string? languageCode = "en",
        List<WhatsAppTemplateParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            LoadConfiguration();

            var phoneNumber = FormatPhoneNumber(to);

            _logger.LogInformation("Sending WhatsApp template message '{TemplateName}' via Cloud API to {PhoneNumber}", 
                templateName, phoneNumber);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var url = $"https://graph.facebook.com/{_apiVersion}/{_phoneNumberId}/messages";

            var template = new
            {
                name = templateName,
                language = new { code = languageCode ?? "en" }
            };

            // Add parameters if provided
            if (parameters != null && parameters.Count > 0)
            {
                var components = new List<object>
                {
                    new
                    {
                        type = "body",
                        parameters = parameters.Select(p => new
                        {
                            type = p.Type ?? "text",
                            text = p.Value
                        }).ToList()
                    }
                };

                ((dynamic)template).components = components;
            }

            var requestBody = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = phoneNumber,
                type = "template",
                template = template
            };

            var jsonBody = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<WhatsAppCloudApiResponse>(responseContent);
                _logger.LogInformation("WhatsApp template message sent successfully. MessageId: {MessageId}", 
                    result?.Messages?[0]?.Id);

                return WhatsAppResult.SuccessResult(
                    result?.Messages?[0]?.Id ?? "unknown",
                    "sent",
                    DateTime.UtcNow
                );
            }
            else
            {
                var error = JsonSerializer.Deserialize<WhatsAppCloudApiError>(responseContent);
                _logger.LogError("WhatsApp Cloud API error. Status: {Status}, Error: {Error}", 
                    response.StatusCode, responseContent);

                return WhatsAppResult.FailedResult(
                    error?.Error?.Message ?? $"HTTP {response.StatusCode}: {responseContent}",
                    error?.Error?.Code?.ToString()
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp template message '{TemplateName}' to {To}", templateName, to);
            return WhatsAppResult.FailedResult(
                $"Failed to send WhatsApp template message: {ex.Message}",
                ex.GetType().Name
            );
        }
    }

    public async Task<WhatsAppResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            LoadConfiguration();

            _logger.LogInformation("Getting WhatsApp message status for MessageId: {MessageId}", messageId);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Note: WhatsApp Cloud API doesn't provide a direct status endpoint
            // Status updates come via webhooks. For now, return unknown.
            _logger.LogInformation("WhatsApp Cloud API doesn't support direct status queries. Use webhooks for status updates.");
            return WhatsAppResult.SuccessResult(messageId, "unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get WhatsApp message status for MessageId: {MessageId}", messageId);
            return WhatsAppResult.FailedResult(
                $"Failed to get WhatsApp message status: {ex.Message}",
                ex.GetType().Name
            );
        }
    }

    private static string FormatPhoneNumber(string phoneNumber)
    {
        // Remove all non-digit characters except leading +
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        
        // If it starts with +, keep it; otherwise assume it's already in E.164 format
        if (!cleaned.StartsWith("+"))
        {
            // If it starts with country code (e.g., 60 for Malaysia), add +
            if (cleaned.Length >= 10)
            {
                cleaned = "+" + cleaned;
            }
        }

        return cleaned;
    }

    // Response DTOs for WhatsApp Cloud API
    private class WhatsAppCloudApiResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("messages")]
        public List<WhatsAppMessage>? Messages { get; set; }
    }

    private class WhatsAppMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private class WhatsAppCloudApiError
    {
        [System.Text.Json.Serialization.JsonPropertyName("error")]
        public WhatsAppError? Error { get; set; }
    }

    private class WhatsAppError
    {
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string? Message { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("code")]
        public int? Code { get; set; }
    }
}

/// <summary>
/// WhatsApp template parameter for dynamic content
/// </summary>
public class WhatsAppTemplateParameter
{
    public string Value { get; set; } = string.Empty;
    public string? Type { get; set; } = "text"; // text, currency, date_time, etc.
}

