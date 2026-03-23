using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Automation Rule service implementation
/// </summary>
public class AutomationRuleService : IAutomationRuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AutomationRuleService> _logger;

    public AutomationRuleService(ApplicationDbContext context, ILogger<AutomationRuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AutomationRuleDto>> GetRulesAsync(Guid companyId, string? ruleType = null, string? entityType = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting automation rules for company {CompanyId}", companyId);

        var query = _context.AutomationRules
            .Where(r => r.CompanyId == companyId);

        if (!string.IsNullOrEmpty(ruleType))
        {
            query = query.Where(r => r.RuleType == ruleType);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(r => r.EntityType == entityType);
        }

        if (isActive.HasValue)
        {
            var now = DateTime.UtcNow;
            if (isActive.Value)
            {
                query = query.Where(r => r.IsActive
                    && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= now)
                    && (!r.EffectiveTo.HasValue || r.EffectiveTo >= now));
            }
            else
            {
                query = query.Where(r => !r.IsActive
                    || (r.EffectiveTo.HasValue && r.EffectiveTo < now)
                    || (r.EffectiveFrom.HasValue && r.EffectiveFrom > now));
            }
        }

        var rules = await query
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rules.Select(MapToDto).ToList();
    }

    public async Task<AutomationRuleDto?> GetRuleByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting automation rule {RuleId} for company {CompanyId}", id, companyId);

        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);

        if (rule == null) return null;

        return MapToDto(rule);
    }

    public async Task<List<AutomationRuleDto>> GetApplicableRulesAsync(Guid companyId, string entityType, string? currentStatus = null, Guid? partnerId = null, Guid? departmentId = null, string? orderType = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting applicable automation rules for company {CompanyId}, entityType {EntityType}, status {Status}", 
            companyId, entityType, currentStatus);

        var now = DateTime.UtcNow;

        var query = _context.AutomationRules
            .Where(r => r.CompanyId == companyId
                && r.EntityType == entityType
                && r.IsActive
                && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= now)
                && (!r.EffectiveTo.HasValue || r.EffectiveTo >= now));

        // Filter by partner if specified
        if (partnerId.HasValue)
        {
            query = query.Where(r => r.PartnerId == null || r.PartnerId == partnerId);
        }

        // Filter by department if specified
        if (departmentId.HasValue)
        {
            query = query.Where(r => r.DepartmentId == null || r.DepartmentId == departmentId);
        }

        // Filter by order type if specified
        if (!string.IsNullOrEmpty(orderType))
        {
            query = query.Where(r => r.OrderType == null || r.OrderType == orderType);
        }

        // Filter by trigger status if specified
        if (!string.IsNullOrEmpty(currentStatus))
        {
            query = query.Where(r => r.TriggerType != "StatusChange" || r.TriggerStatus == currentStatus);
        }

        var rules = await query
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);

        return rules.Select(MapToDto).ToList();
    }

    public async Task<AutomationRuleDto> CreateRuleAsync(CreateAutomationRuleDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating automation rule for company {CompanyId}", companyId);

        var rule = new AutomationRule
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            Description = dto.Description,
            RuleType = dto.RuleType,
            EntityType = dto.EntityType,
            PartnerId = dto.PartnerId,
            DepartmentId = dto.DepartmentId,
            OrderType = dto.OrderType,
            TriggerType = dto.TriggerType,
            TriggerConditionsJson = dto.TriggerConditionsJson,
            TriggerStatus = dto.TriggerStatus,
            TriggerDelayMinutes = dto.TriggerDelayMinutes,
            ActionType = dto.ActionType,
            ActionConfigJson = dto.ActionConfigJson,
            TargetUserId = dto.TargetUserId,
            TargetRole = dto.TargetRole,
            TargetTeamId = dto.TargetTeamId,
            TargetStatus = dto.TargetStatus,
            NotificationTemplateId = dto.NotificationTemplateId,
            ConditionsJson = dto.ConditionsJson,
            Priority = dto.Priority,
            IsActive = dto.IsActive,
            StopOnMatch = dto.StopOnMatch,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        _context.AutomationRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created automation rule {RuleId}", rule.Id);

        return MapToDto(rule);
    }

    public async Task<AutomationRuleDto> UpdateRuleAsync(Guid id, UpdateAutomationRuleDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating automation rule {RuleId} for company {CompanyId}", id, companyId);

        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);

        if (rule == null)
        {
            throw new KeyNotFoundException($"Automation rule with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            rule.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            rule.Description = dto.Description;
        }

        if (!string.IsNullOrEmpty(dto.TriggerType))
        {
            rule.TriggerType = dto.TriggerType;
        }

        if (dto.TriggerConditionsJson != null)
        {
            rule.TriggerConditionsJson = dto.TriggerConditionsJson;
        }

        if (dto.TriggerStatus != null)
        {
            rule.TriggerStatus = dto.TriggerStatus;
        }

        if (dto.TriggerDelayMinutes.HasValue)
        {
            rule.TriggerDelayMinutes = dto.TriggerDelayMinutes;
        }

        if (!string.IsNullOrEmpty(dto.ActionType))
        {
            rule.ActionType = dto.ActionType;
        }

        if (dto.ActionConfigJson != null)
        {
            rule.ActionConfigJson = dto.ActionConfigJson;
        }

        if (dto.TargetUserId.HasValue)
        {
            rule.TargetUserId = dto.TargetUserId;
        }

        if (dto.TargetRole != null)
        {
            rule.TargetRole = dto.TargetRole;
        }

        if (dto.TargetTeamId.HasValue)
        {
            rule.TargetTeamId = dto.TargetTeamId;
        }

        if (dto.TargetStatus != null)
        {
            rule.TargetStatus = dto.TargetStatus;
        }

        if (dto.NotificationTemplateId.HasValue)
        {
            rule.NotificationTemplateId = dto.NotificationTemplateId;
        }

        if (dto.ConditionsJson != null)
        {
            rule.ConditionsJson = dto.ConditionsJson;
        }

        if (dto.Priority.HasValue)
        {
            rule.Priority = dto.Priority.Value;
        }

        if (dto.IsActive.HasValue)
        {
            rule.IsActive = dto.IsActive.Value;
        }

        if (dto.StopOnMatch.HasValue)
        {
            rule.StopOnMatch = dto.StopOnMatch.Value;
        }

        if (dto.EffectiveFrom.HasValue)
        {
            rule.EffectiveFrom = dto.EffectiveFrom;
        }

        if (dto.EffectiveTo.HasValue)
        {
            rule.EffectiveTo = dto.EffectiveTo;
        }

        rule.UpdatedAt = DateTime.UtcNow;
        rule.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated automation rule {RuleId}", id);

        return MapToDto(rule);
    }

    public async Task DeleteRuleAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting automation rule {RuleId} for company {CompanyId}", id, companyId);

        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);

        if (rule == null)
        {
            throw new KeyNotFoundException($"Automation rule with ID {id} not found");
        }

        _context.AutomationRules.Remove(rule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted automation rule {RuleId}", id);
    }

    public async Task<AutomationRuleDto> ToggleActiveAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Toggling automation rule {RuleId} active status for company {CompanyId}", id, companyId);

        var rule = await _context.AutomationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);

        if (rule == null)
        {
            throw new KeyNotFoundException($"Automation rule with ID {id} not found");
        }

        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        rule.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Toggled automation rule {RuleId} to {IsActive}", id, rule.IsActive);

        return MapToDto(rule);
    }

    private static AutomationRuleDto MapToDto(AutomationRule rule)
    {
        return new AutomationRuleDto
        {
            Id = rule.Id,
            CompanyId = rule.CompanyId,
            Name = rule.Name,
            Description = rule.Description,
            RuleType = rule.RuleType,
            EntityType = rule.EntityType,
            PartnerId = rule.PartnerId,
            DepartmentId = rule.DepartmentId,
            OrderType = rule.OrderType,
            TriggerType = rule.TriggerType,
            TriggerConditionsJson = rule.TriggerConditionsJson,
            TriggerStatus = rule.TriggerStatus,
            TriggerDelayMinutes = rule.TriggerDelayMinutes,
            ActionType = rule.ActionType,
            ActionConfigJson = rule.ActionConfigJson,
            TargetUserId = rule.TargetUserId,
            TargetRole = rule.TargetRole,
            TargetTeamId = rule.TargetTeamId,
            TargetStatus = rule.TargetStatus,
            NotificationTemplateId = rule.NotificationTemplateId,
            ConditionsJson = rule.ConditionsJson,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            StopOnMatch = rule.StopOnMatch,
            EffectiveFrom = rule.EffectiveFrom,
            EffectiveTo = rule.EffectiveTo,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}

