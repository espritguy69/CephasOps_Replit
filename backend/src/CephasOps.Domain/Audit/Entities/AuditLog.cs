namespace CephasOps.Domain.Audit.Entities;

/// <summary>
/// Append-only audit log for business-level changes (who did what, when).
/// Used for compliance and debugging. Per LOGGING_AND_AUDIT_MODULE.md.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>When the action occurred (UTC).</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Company context (nullable for system-wide events).</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>User who performed the action (null if System).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Entity type: Order, Invoice, Material, GlobalSetting, etc.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Target entity ID.</summary>
    public Guid EntityId { get; set; }

    /// <summary>Action: Created, Updated, Deleted, StatusChanged, Login, Logout.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>JSON array of { field, oldValue, newValue }.</summary>
    public string? FieldChangesJson { get; set; }

    /// <summary>Channel: AdminWeb, SIApp, Api, BackgroundJob.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Client IP (optional).</summary>
    public string? IpAddress { get; set; }

    /// <summary>Extra context (JSON).</summary>
    public string? MetadataJson { get; set; }
}
