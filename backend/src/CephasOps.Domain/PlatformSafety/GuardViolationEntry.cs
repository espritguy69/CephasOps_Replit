namespace CephasOps.Domain.PlatformSafety;

/// <summary>
/// Single platform guard violation record for operational observability.
/// Safe identifiers only; no sensitive payloads.
/// </summary>
public class GuardViolationEntry
{
    public DateTime OccurredAtUtc { get; set; }
    public string GuardName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? EventId { get; set; }
}
