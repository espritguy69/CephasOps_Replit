using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow.DTOs;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Service interface for workflow engine operations
/// </summary>
public interface IWorkflowEngineService
{
    /// <summary>
    /// Execute a workflow transition for an entity
    /// </summary>
    Task<WorkflowJobDto> ExecuteTransitionAsync(Guid companyId, ExecuteTransitionDto dto, Guid? initiatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get allowed transitions for an entity in its current status
    /// </summary>
    Task<List<WorkflowTransitionDto>> GetAllowedTransitionsAsync(Guid companyId, string entityType, Guid entityId, string currentStatus, List<string> userRoles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if a transition is allowed
    /// </summary>
    Task<bool> CanTransitionAsync(Guid companyId, string entityType, Guid entityId, string fromStatus, string toStatus, List<string> userRoles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow job by ID
    /// </summary>
    Task<WorkflowJobDto?> GetWorkflowJobAsync(Guid companyId, Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow jobs for an entity
    /// </summary>
    Task<List<WorkflowJobDto>> GetWorkflowJobsAsync(Guid companyId, string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow jobs by state
    /// </summary>
    Task<List<WorkflowJobDto>> GetWorkflowJobsByStateAsync(Guid companyId, string state, CancellationToken cancellationToken = default);
}

