using CephasOps.Application.Workflow.DTOs;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Service interface for managing workflow definitions and transitions
/// </summary>
public interface IWorkflowDefinitionsService
{
    /// <summary>
    /// Get all workflow definitions for a company
    /// </summary>
    Task<List<WorkflowDefinitionDto>> GetWorkflowDefinitionsAsync(Guid companyId, string? entityType = null, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow definition by ID
    /// </summary>
    Task<WorkflowDefinitionDto?> GetWorkflowDefinitionAsync(Guid companyId, Guid definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get effective workflow definition for an entity type.
    /// Resolution priority: 1) Partner, 2) Department, 3) OrderTypeCode, 4) General (all null).
    /// For Orders: pass partnerId from Order.PartnerId, departmentId from Order.DepartmentId,
    /// orderTypeCode from Order's OrderType (parent.Code when subtype, else own Code).
    /// </summary>
    Task<WorkflowDefinitionDto?> GetEffectiveWorkflowDefinitionAsync(Guid companyId, string entityType, Guid? partnerId = null, Guid? departmentId = null, string? orderTypeCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new workflow definition
    /// </summary>
    Task<WorkflowDefinitionDto> CreateWorkflowDefinitionAsync(Guid companyId, CreateWorkflowDefinitionDto dto, Guid createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing workflow definition
    /// </summary>
    Task<WorkflowDefinitionDto> UpdateWorkflowDefinitionAsync(Guid companyId, Guid definitionId, UpdateWorkflowDefinitionDto dto, Guid updatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a workflow definition
    /// </summary>
    Task DeleteWorkflowDefinitionAsync(Guid companyId, Guid definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transitions for a workflow definition
    /// </summary>
    Task<List<WorkflowTransitionDto>> GetTransitionsAsync(Guid companyId, Guid definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a transition to a workflow definition
    /// </summary>
    Task<WorkflowTransitionDto> AddTransitionAsync(Guid companyId, Guid definitionId, CreateWorkflowTransitionDto dto, Guid createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a workflow transition
    /// </summary>
    Task<WorkflowTransitionDto> UpdateTransitionAsync(Guid companyId, Guid transitionId, UpdateWorkflowTransitionDto dto, Guid updatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a workflow transition
    /// </summary>
    Task DeleteTransitionAsync(Guid companyId, Guid transitionId, CancellationToken cancellationToken = default);
}

