namespace CephasOps.Application.Tasks.DTOs;

/// <summary>
/// Task DTO
/// </summary>
public class TaskDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid? DepartmentId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid AssignedToUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public Guid? OrderId { get; set; }
}

/// <summary>
/// Create task request DTO
/// </summary>
public class CreateTaskDto
{
    public Guid? DepartmentId { get; set; }
    /// <summary>When set, creation is idempotent: existing task for this order is returned instead of creating a duplicate.</summary>
    public Guid? OrderId { get; set; }
    public Guid AssignedToUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueAt { get; set; }
    public string Priority { get; set; } = "Normal";
}

/// <summary>
/// Update task request DTO
/// </summary>
public class UpdateTaskDto
{
    public Guid? AssignedToUserId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueAt { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
}

