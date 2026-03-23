namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: anomaly event for API response.</summary>
public class TenantAnomalyDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? Details { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
