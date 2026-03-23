namespace CephasOps.Application.Commands.DTOs;

/// <summary>
/// List item for command execution (from CommandProcessingLog).
/// </summary>
public class CommandExecutionListItemDto
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

/// <summary>
/// Detail for a single command execution (operator diagnostics).
/// </summary>
public class CommandExecutionDetailDto
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
