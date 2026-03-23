using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Automation Rule service interface
/// </summary>
public interface IAutomationRuleService
{
    Task<List<AutomationRuleDto>> GetRulesAsync(Guid companyId, string? ruleType = null, string? entityType = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<AutomationRuleDto?> GetRuleByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<List<AutomationRuleDto>> GetApplicableRulesAsync(Guid companyId, string entityType, string? currentStatus = null, Guid? partnerId = null, Guid? departmentId = null, string? orderType = null, CancellationToken cancellationToken = default);
    Task<AutomationRuleDto> CreateRuleAsync(CreateAutomationRuleDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<AutomationRuleDto> UpdateRuleAsync(Guid id, UpdateAutomationRuleDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteRuleAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<AutomationRuleDto> ToggleActiveAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
}

