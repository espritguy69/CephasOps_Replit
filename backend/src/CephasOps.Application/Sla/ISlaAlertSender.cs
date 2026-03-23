namespace CephasOps.Application.Sla;

/// <summary>
/// Sends SLA alerts when critical breaches, repeat failures, or workflow chain stalls are detected.
/// Payload includes CorrelationId, EntityId (target), and Trace Explorer link.
/// </summary>
public interface ISlaAlertSender
{
    /// <summary>
    /// Send alert for a critical or escalated SLA breach. No-op if alerting is disabled.
    /// </summary>
    Task SendBreachAlertAsync(SlaBreachAlertPayload payload, CancellationToken cancellationToken = default);
}

public class SlaBreachAlertPayload
{
    public Guid BreachId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? Title { get; set; }
    public double DurationSeconds { get; set; }
    public DateTime DetectedAtUtc { get; set; }
    /// <summary>Full URL to open this breach's trace in Trace Explorer.</summary>
    public string TraceExplorerLink { get; set; } = string.Empty;
}
