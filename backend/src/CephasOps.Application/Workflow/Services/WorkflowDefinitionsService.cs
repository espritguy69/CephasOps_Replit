using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Service for managing workflow definitions and transitions
/// </summary>
public class WorkflowDefinitionsService : IWorkflowDefinitionsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkflowDefinitionsService> _logger;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowDefinitionsService(
        ApplicationDbContext context,
        ILogger<WorkflowDefinitionsService> logger,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<List<WorkflowDefinitionDto>> GetWorkflowDefinitionsAsync(
        Guid companyId,
        string? entityType = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Getting workflow definitions for company {CompanyId}, entityType: {EntityType}, isActive: {IsActive}",
            companyId, entityType, isActive);

        // Multi-tenant SaaS — CompanyId filter required.
        var query = _context.WorkflowDefinitions
            .Include(wd => wd.Transitions)
            .Where(wd => wd.CompanyId == companyId);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(wd => wd.EntityType == entityType);
        }

        if (isActive.HasValue)
        {
            query = query.Where(wd => wd.IsActive == isActive.Value);
        }

        // Log the query for debugging
        var sqlQuery = query.ToQueryString();
        _logger.LogDebug("Workflow definitions query: {SqlQuery}", sqlQuery);

        var definitions = await query
            .OrderBy(wd => wd.EntityType)
            .ThenBy(wd => wd.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} workflow definitions", definitions.Count);

        // Fetch department names for all definitions
        var departmentIds = definitions
            .Where(d => d.DepartmentId.HasValue)
            .Select(d => d.DepartmentId!.Value)
            .Distinct()
            .ToList();

        var departmentNames = new Dictionary<Guid, string>();
        if (departmentIds.Any())
        {
            departmentNames = await _context.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);
        }

        return definitions.Select(d => MapToDto(d, d.DepartmentId.HasValue && departmentNames.TryGetValue(d.DepartmentId.Value, out var name) ? name : null)).ToList();
    }

    public async Task<WorkflowDefinitionDto?> GetWorkflowDefinitionAsync(
        Guid companyId,
        Guid definitionId,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Getting workflow definition {DefinitionId} for company {CompanyId}", definitionId, companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var definition = await _context.WorkflowDefinitions
            .Include(wd => wd.Transitions)
            .FirstOrDefaultAsync(wd => wd.Id == definitionId && wd.CompanyId == companyId, cancellationToken);

        if (definition == null) return null;

        string? departmentName = null;
        if (definition.DepartmentId.HasValue)
        {
            departmentName = await _context.Departments
                .Where(d => d.Id == definition.DepartmentId.Value)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return MapToDto(definition, departmentName);
    }

    /// <summary>
    /// Resolution priority (see docs/WORKFLOW_RESOLUTION_RULES.md):
    /// 1) Partner (EntityType + CompanyId + PartnerId + active)
    /// 2) Department (EntityType + CompanyId + DepartmentId, PartnerId null, active)
    /// 3) OrderTypeCode (EntityType + CompanyId + OrderTypeCode, PartnerId and DepartmentId null, active)
    /// 4) General (EntityType + CompanyId + PartnerId/DepartmentId/OrderTypeCode all null, active)
    /// </summary>
    public async Task<WorkflowDefinitionDto?> GetEffectiveWorkflowDefinitionAsync(
        Guid companyId,
        string entityType,
        Guid? partnerId = null,
        Guid? departmentId = null,
        string? orderTypeCode = null,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var normalizedOrderTypeCode = string.IsNullOrWhiteSpace(orderTypeCode) ? null : orderTypeCode.Trim();
        _logger.LogInformation("Getting effective workflow definition for company {CompanyId}, entityType: {EntityType}, partnerId: {PartnerId}, departmentId: {DepartmentId}, orderTypeCode: {OrderTypeCode}",
            companyId, entityType, partnerId, departmentId, normalizedOrderTypeCode);

        // Multi-tenant SaaS — CompanyId filter required.
        var baseQuery = _context.WorkflowDefinitions
            .Include(wd => wd.Transitions)
            .Where(wd => wd.CompanyId == companyId
                && wd.EntityType == entityType
                && wd.IsActive);

        // 1) Partner-specific
        if (partnerId.HasValue)
        {
            var partnerMatch = await baseQuery
                .Where(wd => wd.PartnerId == partnerId.Value)
                .ToListAsync(cancellationToken);
            if (partnerMatch.Count > 1)
                throw new InvalidOperationException($"Multiple active workflow definitions found for EntityType={entityType}, CompanyId={companyId}, PartnerId={partnerId}. Only one active workflow per scope is allowed.");
            if (partnerMatch.Count == 1)
                return MapToDto(partnerMatch[0]);
        }

        // 2) Department-specific (PartnerId must be null on definition)
        if (departmentId.HasValue)
        {
            var deptMatch = await baseQuery
                .Where(wd => wd.PartnerId == null && wd.DepartmentId == departmentId.Value)
                .ToListAsync(cancellationToken);
            if (deptMatch.Count > 1)
                throw new InvalidOperationException($"Multiple active workflow definitions found for EntityType={entityType}, CompanyId={companyId}, DepartmentId={departmentId}. Only one active workflow per scope is allowed.");
            if (deptMatch.Count == 1)
                return MapToDto(deptMatch[0]);
        }

        // 3) Order-type-specific (PartnerId and DepartmentId null on definition)
        if (!string.IsNullOrWhiteSpace(normalizedOrderTypeCode))
        {
            var orderTypeMatch = await baseQuery
                .Where(wd => wd.PartnerId == null && wd.DepartmentId == null && wd.OrderTypeCode == normalizedOrderTypeCode)
                .ToListAsync(cancellationToken);
            if (orderTypeMatch.Count > 1)
                throw new InvalidOperationException($"Multiple active workflow definitions found for EntityType={entityType}, CompanyId={companyId}, OrderTypeCode={normalizedOrderTypeCode}. Only one active workflow per scope is allowed.");
            if (orderTypeMatch.Count == 1)
                return MapToDto(orderTypeMatch[0]);
        }

        // 4) General fallback
        var generalMatch = await baseQuery
            .Where(wd => wd.PartnerId == null && wd.DepartmentId == null && (wd.OrderTypeCode == null || wd.OrderTypeCode == ""))
            .ToListAsync(cancellationToken);
        if (generalMatch.Count > 1)
            throw new InvalidOperationException($"Multiple active general workflow definitions found for EntityType={entityType}, CompanyId={companyId}. Only one active general workflow is allowed.");
        if (generalMatch.Count == 1)
            return MapToDto(generalMatch[0]);

        return null;
    }

    public async Task<WorkflowDefinitionDto> CreateWorkflowDefinitionAsync(
        Guid companyId,
        CreateWorkflowDefinitionDto dto,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating workflow definition for company {CompanyId}, name: {Name}, entityType: {EntityType}",
            companyId, dto.Name, dto.EntityType);

        var definition = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            EntityType = dto.EntityType,
            Description = dto.Description,
            IsActive = dto.IsActive,
            PartnerId = dto.PartnerId,
            DepartmentId = dto.DepartmentId,
            OrderTypeCode = string.IsNullOrWhiteSpace(dto.OrderTypeCode) ? null : dto.OrderTypeCode.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = createdByUserId
        };

        await ValidateUniqueActiveScopeAsync(companyId, definition.EntityType, definition.PartnerId, definition.DepartmentId, definition.OrderTypeCode, definition.IsActive, excludeDefinitionId: null, cancellationToken);

        _context.WorkflowDefinitions.Add(definition);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created workflow definition {DefinitionId}", definition.Id);

        string? departmentName = null;
        if (definition.DepartmentId.HasValue)
        {
            departmentName = await _context.Departments
                .Where(d => d.Id == definition.DepartmentId.Value)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return MapToDto(definition, departmentName);
    }

    public async Task<WorkflowDefinitionDto> UpdateWorkflowDefinitionAsync(
        Guid companyId,
        Guid definitionId,
        UpdateWorkflowDefinitionDto dto,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating workflow definition {DefinitionId} for company {CompanyId}", definitionId, companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var definition = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(wd => wd.Id == definitionId && wd.CompanyId == companyId, cancellationToken);

        if (definition == null)
        {
            throw new KeyNotFoundException($"Workflow definition with ID {definitionId} not found.");
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            definition.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            definition.Description = dto.Description;
        }

        if (dto.IsActive.HasValue)
        {
            definition.IsActive = dto.IsActive.Value;
        }

        if (dto.PartnerId != null)
        {
            definition.PartnerId = dto.PartnerId;
        }

        if (dto.DepartmentId.HasValue)
        {
            definition.DepartmentId = dto.DepartmentId;
        }

        if (dto.OrderTypeCode != null)
        {
            definition.OrderTypeCode = string.IsNullOrWhiteSpace(dto.OrderTypeCode) ? null : dto.OrderTypeCode.Trim();
        }

        var effectivePartnerId = definition.PartnerId;
        var effectiveDepartmentId = definition.DepartmentId;
        var effectiveOrderTypeCode = string.IsNullOrWhiteSpace(definition.OrderTypeCode) ? null : definition.OrderTypeCode;
        var effectiveIsActive = definition.IsActive;
        await ValidateUniqueActiveScopeAsync(companyId, definition.EntityType, effectivePartnerId, effectiveDepartmentId, effectiveOrderTypeCode, effectiveIsActive, excludeDefinitionId: definition.Id, cancellationToken);

        definition.UpdatedAt = DateTime.UtcNow;
        definition.UpdatedByUserId = updatedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated workflow definition {DefinitionId}", definitionId);

        // Reload with transitions
        var updated = await _context.WorkflowDefinitions
            .Include(wd => wd.Transitions)
            .FirstOrDefaultAsync(wd => wd.Id == definitionId, cancellationToken);

        string? departmentName = null;
        if (updated?.DepartmentId.HasValue == true)
        {
            departmentName = await _context.Departments
                .Where(d => d.Id == updated.DepartmentId.Value)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return MapToDto(updated!, departmentName);
    }

    public async Task DeleteWorkflowDefinitionAsync(
        Guid companyId,
        Guid definitionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting workflow definition {DefinitionId} for company {CompanyId}", definitionId, companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var definition = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(wd => wd.Id == definitionId && wd.CompanyId == companyId, cancellationToken);

        if (definition == null)
        {
            throw new KeyNotFoundException($"Workflow definition with ID {definitionId} not found.");
        }

        _context.WorkflowDefinitions.Remove(definition);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted workflow definition {DefinitionId}", definitionId);
    }

    public async Task<List<WorkflowTransitionDto>> GetTransitionsAsync(
        Guid companyId,
        Guid definitionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting transitions for workflow definition {DefinitionId} for company {CompanyId}", definitionId, companyId);

        var transitions = await _context.WorkflowTransitions
            .Where(wt => wt.WorkflowDefinitionId == definitionId && wt.CompanyId == companyId)
            .OrderBy(wt => wt.DisplayOrder)
            .ThenBy(wt => wt.FromStatus ?? "")
            .ThenBy(wt => wt.ToStatus)
            .ToListAsync(cancellationToken);

        return transitions.Select(MapTransitionToDto).ToList();
    }

    public async Task<WorkflowTransitionDto> AddTransitionAsync(
        Guid companyId,
        Guid definitionId,
        CreateWorkflowTransitionDto dto,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding transition to workflow definition {DefinitionId} for company {CompanyId}, from: {FromStatus}, to: {ToStatus}",
            definitionId, companyId, dto.FromStatus, dto.ToStatus);

        // Verify workflow definition exists and belongs to company
        var definition = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(wd => wd.Id == definitionId && wd.CompanyId == companyId, cancellationToken);

        if (definition == null)
        {
            throw new KeyNotFoundException($"Workflow definition with ID {definitionId} not found.");
        }

        // Check for duplicate transition
        var existing = await _context.WorkflowTransitions
            .FirstOrDefaultAsync(wt => wt.WorkflowDefinitionId == definitionId
                && wt.CompanyId == companyId
                && wt.FromStatus == dto.FromStatus
                && wt.ToStatus == dto.ToStatus, cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"Transition from '{dto.FromStatus ?? "null"}' to '{dto.ToStatus}' already exists.");
        }

        var transition = new WorkflowTransition
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WorkflowDefinitionId = definitionId,
            FromStatus = dto.FromStatus,
            ToStatus = dto.ToStatus,
            AllowedRolesJson = JsonSerializer.Serialize(dto.AllowedRoles ?? new List<string>()),
            GuardConditionsJson = dto.GuardConditions != null ? JsonSerializer.Serialize(dto.GuardConditions) : null,
            SideEffectsConfigJson = dto.SideEffectsConfig != null ? JsonSerializer.Serialize(dto.SideEffectsConfig) : null,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = createdByUserId
        };

        _context.WorkflowTransitions.Add(transition);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added transition {TransitionId} to workflow definition {DefinitionId}", transition.Id, definitionId);

        return MapTransitionToDto(transition);
    }

    public async Task<WorkflowTransitionDto> UpdateTransitionAsync(
        Guid companyId,
        Guid transitionId,
        UpdateWorkflowTransitionDto dto,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating transition {TransitionId} for company {CompanyId}", transitionId, companyId);

        var transition = await _context.WorkflowTransitions
            .FirstOrDefaultAsync(wt => wt.Id == transitionId && wt.CompanyId == companyId, cancellationToken);

        if (transition == null)
        {
            throw new KeyNotFoundException($"Workflow transition with ID {transitionId} not found.");
        }

        if (dto.FromStatus != null)
        {
            transition.FromStatus = dto.FromStatus;
        }

        if (dto.ToStatus != null)
        {
            transition.ToStatus = dto.ToStatus;
        }

        if (dto.AllowedRoles != null)
        {
            transition.AllowedRolesJson = JsonSerializer.Serialize(dto.AllowedRoles);
        }

        if (dto.GuardConditions != null)
        {
            transition.GuardConditionsJson = JsonSerializer.Serialize(dto.GuardConditions);
        }

        if (dto.SideEffectsConfig != null)
        {
            transition.SideEffectsConfigJson = JsonSerializer.Serialize(dto.SideEffectsConfig);
        }

        if (dto.DisplayOrder.HasValue)
        {
            transition.DisplayOrder = dto.DisplayOrder.Value;
        }

        if (dto.IsActive.HasValue)
        {
            transition.IsActive = dto.IsActive.Value;
        }

        transition.UpdatedAt = DateTime.UtcNow;
        transition.UpdatedByUserId = updatedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated transition {TransitionId}", transitionId);

        return MapTransitionToDto(transition);
    }

    public async Task DeleteTransitionAsync(
        Guid companyId,
        Guid transitionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting transition {TransitionId} for company {CompanyId}", transitionId, companyId);

        var transition = await _context.WorkflowTransitions
            .FirstOrDefaultAsync(wt => wt.Id == transitionId && wt.CompanyId == companyId, cancellationToken);

        if (transition == null)
        {
            throw new KeyNotFoundException($"Workflow transition with ID {transitionId} not found.");
        }

        _context.WorkflowTransitions.Remove(transition);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted transition {TransitionId}", transitionId);
    }

    /// <summary>
    /// Ensures only one active workflow per scope (EntityType, CompanyId, PartnerId, DepartmentId, OrderTypeCode).
    /// </summary>
    private async Task ValidateUniqueActiveScopeAsync(
        Guid companyId,
        string entityType,
        Guid? partnerId,
        Guid? departmentId,
        string? orderTypeCode,
        bool isActive,
        Guid? excludeDefinitionId,
        CancellationToken cancellationToken)
    {
        if (!isActive) return;

        var otCode = string.IsNullOrWhiteSpace(orderTypeCode) ? null : orderTypeCode.Trim();
        var query = _context.WorkflowDefinitions
            .Where(wd => wd.EntityType == entityType && wd.IsActive);
        if (companyId != Guid.Empty)
            query = query.Where(wd => wd.CompanyId == companyId);
        if (excludeDefinitionId.HasValue)
            query = query.Where(wd => wd.Id != excludeDefinitionId.Value);

        query = query.Where(wd =>
            wd.PartnerId == partnerId
            && wd.DepartmentId == departmentId
            && (wd.OrderTypeCode == null || wd.OrderTypeCode == "" ? otCode == null : wd.OrderTypeCode == otCode));

        var count = await query.CountAsync(cancellationToken);
        if (count > 0)
            throw new InvalidOperationException($"An active workflow definition already exists for EntityType={entityType}, PartnerId={partnerId}, DepartmentId={departmentId}, OrderTypeCode={otCode ?? "(general)"}. Only one active workflow per scope is allowed.");
    }

    private static WorkflowDefinitionDto MapToDto(WorkflowDefinition definition, string? departmentName = null)
    {
        return new WorkflowDefinitionDto
        {
            Id = definition.Id,
            CompanyId = definition.CompanyId,
            Name = definition.Name,
            EntityType = definition.EntityType,
            Description = definition.Description,
            IsActive = definition.IsActive,
            PartnerId = definition.PartnerId,
            DepartmentId = definition.DepartmentId,
            DepartmentName = departmentName,
            OrderTypeCode = definition.OrderTypeCode,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt,
            CreatedByUserId = definition.CreatedByUserId,
            UpdatedByUserId = definition.UpdatedByUserId,
            Transitions = definition.Transitions?.Select(MapTransitionToDto).ToList() ?? new List<WorkflowTransitionDto>()
        };
    }

    private static WorkflowTransitionDto MapTransitionToDto(WorkflowTransition transition)
    {
        var allowedRoles = new List<string>();
        if (!string.IsNullOrEmpty(transition.AllowedRolesJson))
        {
            try
            {
                allowedRoles = JsonSerializer.Deserialize<List<string>>(transition.AllowedRolesJson) ?? new List<string>();
            }
            catch
            {
                // If deserialization fails, leave empty list
            }
        }

        Dictionary<string, object>? guardConditions = null;
        if (!string.IsNullOrEmpty(transition.GuardConditionsJson))
        {
            try
            {
                guardConditions = JsonSerializer.Deserialize<Dictionary<string, object>>(transition.GuardConditionsJson);
            }
            catch
            {
                // If deserialization fails, leave null
            }
        }

        Dictionary<string, object>? sideEffectsConfig = null;
        if (!string.IsNullOrEmpty(transition.SideEffectsConfigJson))
        {
            try
            {
                sideEffectsConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(transition.SideEffectsConfigJson);
            }
            catch
            {
                // If deserialization fails, leave null
            }
        }

        return new WorkflowTransitionDto
        {
            Id = transition.Id,
            CompanyId = transition.CompanyId,
            WorkflowDefinitionId = transition.WorkflowDefinitionId,
            FromStatus = transition.FromStatus,
            ToStatus = transition.ToStatus,
            AllowedRoles = allowedRoles,
            GuardConditions = guardConditions,
            SideEffectsConfig = sideEffectsConfig,
            DisplayOrder = transition.DisplayOrder,
            IsActive = transition.IsActive,
            CreatedAt = transition.CreatedAt,
            UpdatedAt = transition.UpdatedAt,
            CreatedByUserId = transition.CreatedByUserId,
            UpdatedByUserId = transition.UpdatedByUserId
        };
    }
}

