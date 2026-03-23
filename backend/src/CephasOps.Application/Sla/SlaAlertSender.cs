using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Sla;

/// <summary>
/// Sends SLA breach alerts via webhook (and optionally email). Builds Trace Explorer link from options.
/// </summary>
public class SlaAlertSender : ISlaAlertSender
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<SlaAlertOptions> _options;
    private readonly ILogger<SlaAlertSender> _logger;

    public SlaAlertSender(HttpClient httpClient, IOptions<SlaAlertOptions> options, ILogger<SlaAlertSender> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task SendBreachAlertAsync(SlaBreachAlertPayload payload, CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        var baseUrl = (opts.TraceExplorerBaseUrl ?? "").TrimEnd('/');
        payload.TraceExplorerLink = BuildTraceExplorerLink(baseUrl, payload);

        if (!string.IsNullOrWhiteSpace(opts.WebhookUrl))
        {
            try
            {
                var json = JsonSerializer.Serialize(new
                {
                    payload.BreachId,
                    payload.CompanyId,
                    payload.Severity,
                    payload.TargetType,
                    payload.TargetId,
                    payload.CorrelationId,
                    payload.Title,
                    payload.DurationSeconds,
                    payload.DetectedAtUtc,
                    payload.TraceExplorerLink
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(opts.WebhookUrl, content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("SLA alert webhook returned {StatusCode} for breach {BreachId}", response.StatusCode, payload.BreachId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SLA alert webhook failed for breach {BreachId}", payload.BreachId);
            }
        }

        // Email: could inject IEmailSendingService and send when EmailEnabled and EmailRecipients set
        if (opts.EmailEnabled && !string.IsNullOrWhiteSpace(opts.EmailRecipients))
        {
            _logger.LogInformation("SLA critical breach {BreachId}: email alert would be sent to {Recipients} (implement IEmailSendingService in SlaAlertSender for full support)", payload.BreachId, opts.EmailRecipients);
        }
    }

    private static string BuildTraceExplorerLink(string baseUrl, SlaBreachAlertPayload payload)
    {
        if (string.IsNullOrEmpty(baseUrl)) return string.Empty;
        if (!string.IsNullOrEmpty(payload.CorrelationId))
            return $"{baseUrl}/admin/trace-explorer?correlationId={Uri.EscapeDataString(payload.CorrelationId)}";
        if (payload.TargetType == "workflow")
            return $"{baseUrl}/admin/trace-explorer?workflowJobId={Uri.EscapeDataString(payload.TargetId)}";
        if (payload.TargetType == "event")
            return $"{baseUrl}/admin/trace-explorer?eventId={Uri.EscapeDataString(payload.TargetId)}";
        if (payload.TargetType == "job")
            return $"{baseUrl}/admin/trace-explorer?jobRunId={Uri.EscapeDataString(payload.TargetId)}";
        return $"{baseUrl}/admin/trace-explorer";
    }
}
