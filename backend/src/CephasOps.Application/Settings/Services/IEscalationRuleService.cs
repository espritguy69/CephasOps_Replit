using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Escalation Rule service interface
/// </summary>
public interface IEscalationRuleService
{
    Task<List<EscalationRuleDto>> GetRulesAsync(Guid companyId, string? entityType = null, string? triggerType = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<EscalationRuleDto?> GetRuleByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<List<EscalationRuleDto>> GetApplicableRulesAsync(Guid companyId, string entityType, string? currentStatus = null, Guid? partnerId = null, Guid? departmentId = null, string? orderType = null, CancellationToken cancellationToken = default);
    Task<EscalationRuleDto> CreateRuleAsync(CreateEscalationRuleDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<EscalationRuleDto> UpdateRuleAsync(Guid id, UpdateEscalationRuleDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteRuleAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<EscalationRuleDto> ToggleActiveAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
}

