using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Escalation Rule service implementation
/// </summary>
public class EscalationRuleService : IEscalationRuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EscalationRuleService> _logger;

    public EscalationRuleService(ApplicationDbContext context, ILogger<EscalationRuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EscalationRuleDto>> GetRulesAsync(Guid companyId, string? entityType = null, string? triggerType = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting escalation rules for company {CompanyId}", companyId);

        var query = _context.EscalationRules
            .Where(r => r.CompanyId == companyId);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(r => r.EntityType == entityType);
        }

        if (!string.IsNullOrEmpty(triggerType))
        {
            query = query.Where(r => r.TriggerType == triggerType);
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

    public async Task<EscalationRuleDto?> GetRuleByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting escalation rule {RuleId} for company {CompanyId}", id, companyId);

        var rule = await _context.EscalationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);

        if (rule == null) return null;

        return MapToDto(rule);
    }

    public async Task<List<EscalationRuleDto>> GetApplicableRulesAsync(Guid companyId, string entityType, string? currentStatus = null, Guid? partnerId = null, Guid? departmentId = null, string? orderType = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting applicable escalation rules for company {CompanyId}, entityType {EntityType}, status {Status}", 
            companyId, entityType, currentStatus);

        var now = DateTime.UtcNow;

        var query = _context.EscalationRules
            .Where(r => r.CompanyId == companyId
                && r.EntityType == entityType
                && r.IsActive
                && (!r.EffectiveFrom.HasValue || r.EffectiveFrom <= now)
                && (!r.EffectiveTo.HasValue || r.EffectiveTo >= now));

        if (partnerId.HasValue)
        {
            query = query.Where(r => r.PartnerId == null || r.PartnerId == partnerId);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(r => r.DepartmentId == null || r.DepartmentId == departmentId);
        }

        if (!string.IsNullOrEmpty(orderType))
        {
            query = query.Where(r => r.OrderType == null || r.OrderType == orderType);
        }

        if (!string.IsNullOrEmpty(currentStatus))
        {
            query = query.Where(r => r.TriggerType != "StatusBased" || r.TriggerStatus == currentStatus);
        }

        var rules = await query
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);

        return rules.Select(MapToDto).ToList();
    }

    public async Task<EscalationRuleDto> CreateRuleAsync(CreateEscalationRuleDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating escalation rule for company {CompanyId}", companyId);

        var rule = new EscalationRule
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            Description = dto.Description,
            EntityType = dto.EntityType,
            PartnerId = dto.PartnerId,
            DepartmentId = dto.DepartmentId,
            OrderType = dto.OrderType,
            TriggerType = dto.TriggerType,
            TriggerStatus = dto.TriggerStatus,
            TriggerDelayMinutes = dto.TriggerDelayMinutes,
            TriggerConditionsJson = dto.TriggerConditionsJson,
            EscalationType = dto.EscalationType,
            TargetUserId = dto.TargetUserId,
            TargetRole = dto.TargetRole,
            TargetTeamId = dto.TargetTeamId,
            TargetStatus = dto.TargetStatus,
            NotificationTemplateId = dto.NotificationTemplateId,
            EscalationMessage = dto.EscalationMessage,
            ContinueEscalation = dto.ContinueEscalation,
            NextEscalationRuleId = dto.NextEscalationRuleId,
            NextEscalationDelayMinutes = dto.NextEscalationDelayMinutes,
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

        _context.EscalationRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created escalation rule {RuleId}", rule.Id);

        return MapToDto(rule);
    }

    public async Task<EscalationRuleDto> UpdateRuleAsync(Guid id, UpdateEscalationRuleDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating escalation rule {RuleId} for company {CompanyId}", id, companyId);

        var rule = await _context.EscalationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);

        if (rule == null)
        {
            throw new KeyNotFoundException($"Escalation rule with ID {id} not found");
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

        if (dto.TriggerStatus != null)
        {
            rule.TriggerStatus = dto.TriggerStatus;
        }

        if (dto.TriggerDelayMinutes.HasValue)
        {
            rule.TriggerDelayMinutes = dto.TriggerDelayMinutes;
        }

        if (dto.TriggerConditionsJson != null)
        {
            rule.TriggerConditionsJson = dto.TriggerConditionsJson;
        }

        if (!string.IsNullOrEmpty(dto.EscalationType))
        {
            rule.EscalationType = dto.EscalationType;
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

        if (dto.EscalationMessage != null)
        {
            rule.EscalationMessage = dto.EscalationMessage;
        }

        if (dto.ContinueEscalation.HasValue)
        {
            rule.ContinueEscalation = dto.ContinueEscalation.Value;
        }

        if (dto.NextEscalationRuleId.HasValue)
        {
            rule.NextEscalationRuleId = dto.NextEscalationRuleId;
        }

        if (dto.NextEscalationDelayMinutes.HasValue)
        {
            rule.NextEscalationDelayMinutes = dto.NextEscalationDelayMinutes;
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

        _logger.LogInformation("Updated escalation rule {RuleId}", id);

        return MapToDto(rule);
    }

    public async Task DeleteRuleAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting escalation rule {RuleId} for company {CompanyId}", id, companyId);

        var rule = await _context.EscalationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);

        if (rule == null)
        {
            throw new KeyNotFoundException($"Escalation rule with ID {id} not found");
        }

        _context.EscalationRules.Remove(rule);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted escalation rule {RuleId}", id);
    }

    public async Task<EscalationRuleDto> ToggleActiveAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Toggling escalation rule {RuleId} active status for company {CompanyId}", id, companyId);

        var rule = await _context.EscalationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);

        if (rule == null)
        {
            throw new KeyNotFoundException($"Escalation rule with ID {id} not found");
        }

        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        rule.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Toggled escalation rule {RuleId} to {IsActive}", id, rule.IsActive);

        return MapToDto(rule);
    }

    private static EscalationRuleDto MapToDto(EscalationRule rule)
    {
        return new EscalationRuleDto
        {
            Id = rule.Id,
            CompanyId = rule.CompanyId,
            Name = rule.Name,
            Description = rule.Description,
            EntityType = rule.EntityType,
            PartnerId = rule.PartnerId,
            DepartmentId = rule.DepartmentId,
            OrderType = rule.OrderType,
            TriggerType = rule.TriggerType,
            TriggerStatus = rule.TriggerStatus,
            TriggerDelayMinutes = rule.TriggerDelayMinutes,
            TriggerConditionsJson = rule.TriggerConditionsJson,
            EscalationType = rule.EscalationType,
            TargetUserId = rule.TargetUserId,
            TargetRole = rule.TargetRole,
            TargetTeamId = rule.TargetTeamId,
            TargetStatus = rule.TargetStatus,
            NotificationTemplateId = rule.NotificationTemplateId,
            EscalationMessage = rule.EscalationMessage,
            ContinueEscalation = rule.ContinueEscalation,
            NextEscalationRuleId = rule.NextEscalationRuleId,
            NextEscalationDelayMinutes = rule.NextEscalationDelayMinutes,
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

