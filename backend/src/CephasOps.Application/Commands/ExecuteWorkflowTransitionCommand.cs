using CephasOps.Application.Workflow.DTOs;

namespace CephasOps.Application.Commands;

/// <summary>
/// Command to execute a single workflow transition. Result is WorkflowJobDto.
/// </summary>
public class ExecuteWorkflowTransitionCommand : ICommand<WorkflowJobDto>
{
    public Guid CompanyId { get; set; }
    public Guid? InitiatedByUserId { get; set; }

    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string TargetStatus { get; set; } = string.Empty;
    public Dictionary<string, object>? Payload { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? OrderTypeCode { get; set; }

    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
}
