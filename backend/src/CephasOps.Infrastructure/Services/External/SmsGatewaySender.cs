using CephasOps.Domain.Notifications;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CephasOps.Infrastructure.Services.External;

/// <summary>
/// SMS Gateway sender implementation
/// Sends SMS via registered Android SMS Gateway device
/// </summary>
public class SmsGatewaySender : ISmsProvider
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmsGatewaySender> _logger;

    public SmsGatewaySender(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<SmsGatewaySender> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SmsResult> SendSmsAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get active gateway from database
            var gateway = await _context.SmsGateways
                .Where(g => g.IsActive)
                .OrderByDescending(g => g.LastSeenAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (gateway == null)
            {
                _logger.LogError("No active SMS Gateway found. Cannot send SMS.");
                return SmsResult.FailedResult("No active SMS Gateway registered. Please register a gateway first.");
            }

            // Build URL: {BaseUrl}/sms/send?apikey={ApiKey}
            var baseUrl = gateway.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/sms/send?apikey={gateway.ApiKey}";

            _logger.LogInformation("Sending SMS via Gateway {DeviceName} to {To}", gateway.DeviceName, to);

            // Prepare request body
            var requestBody = new
            {
                number = to,
                message = message
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Send HTTP POST request
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("SMS sent successfully via Gateway. Response: {Response}", responseContent);

                // Update last seen timestamp
                gateway.Touch();
                await _context.SaveChangesAsync(cancellationToken);

                // Generate a message ID (could be from response if gateway provides one)
                var messageId = $"gateway-{gateway.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                return SmsResult.SuccessResult(messageId, "sent", DateTime.UtcNow);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("SMS Gateway returned error. Status: {Status}, Response: {Response}", 
                    response.StatusCode, errorContent);

                return SmsResult.FailedResult(
                    $"SMS Gateway error: {response.StatusCode} - {errorContent}",
                    response.StatusCode.ToString()
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS via Gateway to {To}", to);
            return SmsResult.FailedResult(
                $"Failed to send SMS via Gateway: {ex.Message}",
                ex.GetType().Name
            );
        }
    }

    public async Task<SmsResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        // SMS Gateway doesn't provide status tracking
        // Return unknown status
        _logger.LogInformation("Status check requested for message {MessageId} - SMS Gateway doesn't support status tracking", messageId);
        return SmsResult.SuccessResult(messageId, "unknown");
    }
}

