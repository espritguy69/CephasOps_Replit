namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Record that a payout anomaly was sent to an alert channel. Used to avoid duplicate alerts.
/// Read-only with respect to payout and anomaly detection; only tracks delivery.
/// </summary>
public class PayoutAnomalyAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Anomaly fingerprint (same as PayoutAnomalyReview.AnomalyFingerprintId).</summary>
    public string AnomalyFingerprintId { get; set; } = "";

    /// <summary>Channel used: Email, Slack, Telegram, etc.</summary>
    public string Channel { get; set; } = "Email";

    /// <summary>When the alert was sent (UTC).</summary>
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Status: Sent, Failed, Pending.</summary>
    public string Status { get; set; } = "Sent";

    /// <summary>Number of send attempts (1 = first try, 2+ = retries).</summary>
    public int RetryCount { get; set; }

    /// <summary>Optional error message if Status = Failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Optional: recipient identifier (e.g. email address or channel id) for deduplication per channel+recipient.</summary>
    public string? RecipientId { get; set; }
}
