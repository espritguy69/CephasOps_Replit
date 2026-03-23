using CephasOps.Application.Workflow.DTOs;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Orchestrates multi-step workflows: start instance, advance steps, query instance.
/// No full saga logic yet - instance and step tracking only.
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Start a new workflow instance and create the first step record.
    /// </summary>
    Task<WorkflowInstanceDto> StartWorkflowAsync(
        string workflowType,
        string entityType,
        Guid entityId,
        string? initialPayloadJson = null,
        Guid? companyId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Advance the instance to the given step and append a step record.
    /// </summary>
    Task AdvanceStepAsync(
        Guid instanceId,
        string stepName,
        string? payloadJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow instance by id.
    /// </summary>
    Task<WorkflowInstanceDto?> GetInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// List workflow instances (operator diagnostics). Ordered by UpdatedAt descending.
    /// </summary>
    Task<(IReadOnlyList<WorkflowInstanceDto> Items, int TotalCount)> ListInstancesAsync(
        string? workflowType = null,
        string? entityType = null,
        string? status = null,
        Guid? companyId = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);
}
