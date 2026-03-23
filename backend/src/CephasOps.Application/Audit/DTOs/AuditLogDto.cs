namespace CephasOps.Application.Audit.DTOs;

/// <summary>
/// DTO for a single audit log entry (read-only).
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FieldChangesJson { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? MetadataJson { get; set; }
}

/// <summary>
/// DTO for a security (auth) activity entry with user email resolved. v1.4 Phase 1.
/// </summary>
public class SecurityActivityEntryDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? MetadataJson { get; set; }
}

/// <summary>
/// DTO for a suspicious activity alert from anomaly detection. v1.4 Phase 2.
/// </summary>
public class SecurityAlertDto
{
    public DateTime DetectedAtUtc { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IpSummary { get; set; }
    public int EventCount { get; set; }
    public int WindowMinutes { get; set; }
}
