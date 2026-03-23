using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Approval Workflow service interface
/// </summary>
public interface IApprovalWorkflowService
{
    Task<List<ApprovalWorkflowDto>> GetWorkflowsAsync(Guid companyId, string? workflowType = null, string? entityType = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflowDto?> GetWorkflowByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflowDto?> GetEffectiveWorkflowAsync(Guid companyId, string workflowType, string entityType, Guid? partnerId = null, Guid? departmentId = null, string? orderType = null, decimal? value = null, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflowDto> CreateWorkflowAsync(CreateApprovalWorkflowDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflowDto> UpdateWorkflowAsync(Guid id, UpdateApprovalWorkflowDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteWorkflowAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<ApprovalWorkflowDto> SetAsDefaultAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
}

