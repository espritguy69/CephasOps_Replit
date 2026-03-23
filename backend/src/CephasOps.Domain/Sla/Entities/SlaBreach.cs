namespace CephasOps.Domain.Sla.Entities;

/// <summary>
/// Record of a detected SLA breach, warning, or escalation. Does not duplicate trace data; references TargetId and CorrelationId for Trace Explorer.
/// </summary>
public class SlaBreach
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Company scope.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Rule that was evaluated.</summary>
    public Guid RuleId { get; set; }

    /// <summary>workflow, event, job.</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>ID of the workflow job, event, or job run that breached.</summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>Correlation ID for Trace Explorer link.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>When the breach was detected (UTC).</summary>
    public DateTime DetectedAtUtc { get; set; }

    /// <summary>Observed duration in seconds.</summary>
    public double DurationSeconds { get; set; }

    /// <summary>Warning, Breach, Critical.</summary>
    public string Severity { get; set; } = "Breach";

    /// <summary>Open, Acknowledged, Resolved.</summary>
    public string Status { get; set; } = "Open";

    /// <summary>Optional: when acknowledged (UTC).</summary>
    public DateTime? AcknowledgedAtUtc { get; set; }

    /// <summary>Optional: when resolved (UTC).</summary>
    public DateTime? ResolvedAtUtc { get; set; }

    /// <summary>Optional: user who acknowledged or resolved.</summary>
    public Guid? ResolvedByUserId { get; set; }

    /// <summary>Optional: short title for display (e.g. workflow transition, job type).</summary>
    public string? Title { get; set; }
}
