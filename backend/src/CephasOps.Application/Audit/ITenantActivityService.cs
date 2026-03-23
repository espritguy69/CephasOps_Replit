namespace CephasOps.Application.Audit;

/// <summary>Enterprise: record and query tenant activity timeline for platform observability.</summary>
public interface ITenantActivityService
{
    /// <summary>Record an activity event for the tenant. Call from tenant-scoped code with valid TenantScope or companyId.</summary>
    Task RecordAsync(
        Guid tenantId,
        string eventType,
        string? entityType = null,
        Guid? entityId = null,
        string? description = null,
        Guid? userId = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>Get last N events for a tenant (platform bypass only). Ordered by timestamp descending.</summary>
    Task<IReadOnlyList<TenantActivityEventDto>> GetTimelineAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default);
}

public class TenantActivityEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? Description { get; set; }
    public Guid? UserId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? MetadataJson { get; set; }
}
