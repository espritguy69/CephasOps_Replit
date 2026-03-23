namespace CephasOps.Application.Workflow.DTOs;

/// <summary>
/// DTO for SystemLog
/// </summary>
public class SystemLogDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Details { get; set; }
    public Guid? UserId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new SystemLog entry
/// </summary>
public class CreateSystemLogDto
{
    public Guid? CompanyId { get; set; }
    public string Severity { get; set; } = "Info";
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Details { get; set; }
    public Guid? UserId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
}

