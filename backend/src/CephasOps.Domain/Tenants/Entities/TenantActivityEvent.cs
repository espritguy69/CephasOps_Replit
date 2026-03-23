namespace CephasOps.Domain.Tenants.Entities;

/// <summary>Enterprise: chronological activity timeline per tenant for platform observability.</summary>
public class TenantActivityEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    /// <summary>e.g. OrderCreated, NotificationSent, JobExecuted, IntegrationCall, OrderCompleted, Login, FeatureFlagChanged</summary>
    public string EventType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? Description { get; set; }
    public Guid? UserId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Optional JSON for extra context.</summary>
    public string? MetadataJson { get; set; }
}
