namespace CephasOps.Application.Workflow.DTOs;

/// <summary>
/// DTO for a workflow orchestration instance.
/// </summary>
public class WorkflowInstanceDto
{
    public Guid Id { get; set; }
    public Guid? WorkflowDefinitionId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? PayloadJson { get; set; }
    public Guid? CompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
