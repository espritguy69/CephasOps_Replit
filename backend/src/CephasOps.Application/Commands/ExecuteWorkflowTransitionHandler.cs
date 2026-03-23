using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Services;

namespace CephasOps.Application.Commands;

/// <summary>
/// Handles ExecuteWorkflowTransitionCommand by calling IWorkflowEngineService.ExecuteTransitionAsync.
/// </summary>
public class ExecuteWorkflowTransitionHandler : ICommandHandler<ExecuteWorkflowTransitionCommand, WorkflowJobDto>
{
    private readonly IWorkflowEngineService _workflowEngineService;

    public ExecuteWorkflowTransitionHandler(IWorkflowEngineService workflowEngineService)
    {
        _workflowEngineService = workflowEngineService;
    }

    public async Task<WorkflowJobDto> HandleAsync(ExecuteWorkflowTransitionCommand command, CancellationToken cancellationToken = default)
    {
        var dto = new ExecuteTransitionDto
        {
            EntityId = command.EntityId,
            EntityType = command.EntityType,
            TargetStatus = command.TargetStatus,
            Payload = command.Payload,
            PartnerId = command.PartnerId,
            DepartmentId = command.DepartmentId,
            OrderTypeCode = command.OrderTypeCode,
            CorrelationId = command.CorrelationId
        };
        return await _workflowEngineService.ExecuteTransitionAsync(
            command.CompanyId,
            dto,
            command.InitiatedByUserId,
            cancellationToken).ConfigureAwait(false);
    }
}
