namespace CephasOps.Application.Sla;

/// <summary>
/// Configuration for SLA alerting (webhook, email). Bind from "SlaAlerts" section.
/// </summary>
public class SlaAlertOptions
{
    public const string SectionName = "SlaAlerts";

    /// <summary>Base URL of the frontend (e.g. https://app.cephasops.com) for Trace Explorer links.</summary>
    public string TraceExplorerBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>Webhook URL to POST breach payload (JSON). Empty = disabled.</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>Whether to send email alerts for critical breaches. Requires email configuration.</summary>
    public bool EmailEnabled { get; set; }

    /// <summary>Comma-separated email addresses for critical breach alerts.</summary>
    public string EmailRecipients { get; set; } = string.Empty;
}
