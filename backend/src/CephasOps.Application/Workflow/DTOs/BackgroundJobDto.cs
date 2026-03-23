namespace CephasOps.Application.Workflow.DTOs;

/// <summary>
/// DTO for BackgroundJob
/// </summary>
public class BackgroundJobDto
{
    public Guid Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    public Dictionary<string, object> Payload { get; set; } = new();
    public string State { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public string? LastError { get; set; }
    public int Priority { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// DTO for creating a new BackgroundJob
/// </summary>
public class CreateBackgroundJobDto
{
    public string JobType { get; set; } = string.Empty;
    public Dictionary<string, object> Payload { get; set; } = new();
    public int Priority { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public DateTime? ScheduledAt { get; set; }
}

