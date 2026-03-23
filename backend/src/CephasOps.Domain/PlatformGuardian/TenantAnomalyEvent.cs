namespace CephasOps.Domain.PlatformGuardian;

/// <summary>Platform Guardian: persisted anomaly event per tenant for diagnostics and reporting.</summary>
public class TenantAnomalyEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    /// <summary>e.g. ApiSpike, RateLimitBreach, StorageSpike, JobFailureSpike, JobCreationSpike, ImpersonationEvent, FailedAuthSpike, ExportSpike</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Info | Warning | Critical</summary>
    public string Severity { get; set; } = "Warning";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Optional JSON or message with details (counts, thresholds, etc.).</summary>
    public string? Details { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
