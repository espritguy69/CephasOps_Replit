using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Approval Workflow service implementation
/// </summary>
public class ApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApprovalWorkflowService> _logger;

    public ApprovalWorkflowService(ApplicationDbContext context, ILogger<ApprovalWorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ApprovalWorkflowDto>> GetWorkflowsAsync(Guid companyId, string? workflowType = null, string? entityType = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting approval workflows for company {CompanyId}", companyId);

        var query = _context.ApprovalWorkflows
            .Include(w => w.Steps.OrderBy(s => s.StepOrder))
            .Where(w => w.CompanyId == companyId);

        if (!string.IsNullOrEmpty(workflowType))
        {
            query = query.Where(w => w.WorkflowType == workflowType);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(w => w.EntityType == entityType);
        }

        if (isActive.HasValue)
        {
            var now = DateTime.UtcNow;
            if (isActive.Value)
            {
                query = query.Where(w => w.IsActive
                    && (!w.EffectiveFrom.HasValue || w.EffectiveFrom <= now)
                    && (!w.EffectiveTo.HasValue || w.EffectiveTo >= now));
            }
            else
            {
                query = query.Where(w => !w.IsActive
                    || (w.EffectiveTo.HasValue && w.EffectiveTo < now)
                    || (w.EffectiveFrom.HasValue && w.EffectiveFrom > now));
            }
        }

        var workflows = await query.OrderBy(w => w.WorkflowType).ThenBy(w => w.Name).ToListAsync(cancellationToken);

        return workflows.Select(MapToDto).ToList();
    }

    public async Task<ApprovalWorkflowDto?> GetWorkflowByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting approval workflow {WorkflowId} for company {CompanyId}", id, companyId);

        var workflow = await _context.ApprovalWorkflows
            .Include(w => w.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId, cancellationToken);

        if (workflow == null) return null;

        return MapToDto(workflow);
    }

    public async Task<ApprovalWorkflowDto?> GetEffectiveWorkflowAsync(Guid companyId, string workflowType, string entityType, Guid? partnerId = null, Guid? departmentId = null, string? orderType = null, decimal? value = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting effective approval workflow for company {CompanyId}, type {WorkflowType}, entity {EntityType}", 
            companyId, workflowType, entityType);

        var now = DateTime.UtcNow;

        var query = _context.ApprovalWorkflows
            .Include(w => w.Steps.OrderBy(s => s.StepOrder))
            .Where(w => w.CompanyId == companyId
                && w.WorkflowType == workflowType
                && w.EntityType == entityType
                && w.IsActive
                && (!w.EffectiveFrom.HasValue || w.EffectiveFrom <= now)
                && (!w.EffectiveTo.HasValue || w.EffectiveTo >= now));

        if (partnerId.HasValue)
        {
            query = query.Where(w => w.PartnerId == null || w.PartnerId == partnerId);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(w => w.DepartmentId == null || w.DepartmentId == departmentId);
        }

        if (!string.IsNullOrEmpty(orderType))
        {
            query = query.Where(w => w.OrderType == null || w.OrderType == orderType);
        }

        if (value.HasValue && value.Value > 0)
        {
            query = query.Where(w => !w.MinValueThreshold.HasValue || w.MinValueThreshold <= value.Value);
        }

        var workflow = await query
            .OrderByDescending(w => w.PartnerId != null ? 1 : 0)
            .ThenByDescending(w => w.DepartmentId != null ? 1 : 0)
            .ThenByDescending(w => w.MinValueThreshold != null ? 1 : 0)
            .FirstOrDefaultAsync(cancellationToken);

        if (workflow == null)
        {
            // Fallback to default
            workflow = await _context.ApprovalWorkflows
                .Include(w => w.Steps.OrderBy(s => s.StepOrder))
                .Where(w => w.CompanyId == companyId
                    && w.WorkflowType == workflowType
                    && w.IsDefault
                    && w.IsActive
                    && (!w.EffectiveFrom.HasValue || w.EffectiveFrom <= now)
                    && (!w.EffectiveTo.HasValue || w.EffectiveTo >= now))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (workflow == null) return null;

        return MapToDto(workflow);
    }

    public async Task<ApprovalWorkflowDto> CreateWorkflowAsync(CreateApprovalWorkflowDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating approval workflow for company {CompanyId}", companyId);

        var workflow = new ApprovalWorkflow
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            Description = dto.Description,
            WorkflowType = dto.WorkflowType,
            EntityType = dto.EntityType,
            PartnerId = dto.PartnerId,
            DepartmentId = dto.DepartmentId,
            OrderType = dto.OrderType,
            MinValueThreshold = dto.MinValueThreshold,
            RequireAllSteps = dto.RequireAllSteps,
            AllowParallelApproval = dto.AllowParallelApproval,
            TimeoutMinutes = dto.TimeoutMinutes,
            AutoApproveOnTimeout = dto.AutoApproveOnTimeout,
            EscalationRole = dto.EscalationRole,
            EscalationUserId = dto.EscalationUserId,
            IsActive = dto.IsActive,
            IsDefault = dto.IsDefault,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        _context.ApprovalWorkflows.Add(workflow);

        // Add steps
        foreach (var stepDto in dto.Steps.OrderBy(s => s.StepOrder))
        {
            var step = new ApprovalStep
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                ApprovalWorkflowId = workflow.Id,
                Name = stepDto.Name,
                StepOrder = stepDto.StepOrder,
                ApprovalType = stepDto.ApprovalType,
                TargetUserId = stepDto.TargetUserId,
                TargetRole = stepDto.TargetRole,
                TargetTeamId = stepDto.TargetTeamId,
                ExternalSource = stepDto.ExternalSource,
                IsRequired = stepDto.IsRequired,
                CanSkipIfPreviousApproved = stepDto.CanSkipIfPreviousApproved,
                TimeoutMinutes = stepDto.TimeoutMinutes,
                AutoApproveOnTimeout = stepDto.AutoApproveOnTimeout,
                IsActive = stepDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedByUserId = userId
            };

            _context.ApprovalSteps.Add(step);
        }

        if (dto.IsDefault)
        {
            var existingDefaults = await _context.ApprovalWorkflows
                .Where(w => w.CompanyId == companyId 
                    && w.WorkflowType == dto.WorkflowType 
                    && w.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = userId;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created approval workflow {WorkflowId}", workflow.Id);

        return await GetWorkflowByIdAsync(workflow.Id, companyId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created workflow");
    }

    public async Task<ApprovalWorkflowDto> UpdateWorkflowAsync(Guid id, UpdateApprovalWorkflowDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating approval workflow {WorkflowId} for company {CompanyId}", id, companyId);

        var workflow = await _context.ApprovalWorkflows
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId, cancellationToken);

        if (workflow == null)
        {
            throw new KeyNotFoundException($"Approval workflow with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            workflow.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            workflow.Description = dto.Description;
        }

        if (dto.MinValueThreshold.HasValue)
        {
            workflow.MinValueThreshold = dto.MinValueThreshold;
        }

        if (dto.RequireAllSteps.HasValue)
        {
            workflow.RequireAllSteps = dto.RequireAllSteps.Value;
        }

        if (dto.AllowParallelApproval.HasValue)
        {
            workflow.AllowParallelApproval = dto.AllowParallelApproval.Value;
        }

        if (dto.TimeoutMinutes.HasValue)
        {
            workflow.TimeoutMinutes = dto.TimeoutMinutes;
        }

        if (dto.AutoApproveOnTimeout.HasValue)
        {
            workflow.AutoApproveOnTimeout = dto.AutoApproveOnTimeout.Value;
        }

        if (dto.EscalationRole != null)
        {
            workflow.EscalationRole = dto.EscalationRole;
        }

        if (dto.EscalationUserId.HasValue)
        {
            workflow.EscalationUserId = dto.EscalationUserId;
        }

        if (dto.IsActive.HasValue)
        {
            workflow.IsActive = dto.IsActive.Value;
        }

        if (dto.IsDefault.HasValue && dto.IsDefault.Value)
        {
            var existingDefaults = await _context.ApprovalWorkflows
                .Where(w => w.CompanyId == companyId 
                    && w.WorkflowType == workflow.WorkflowType 
                    && w.Id != id
                    && w.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = userId;
            }

            workflow.IsDefault = true;
        }
        else if (dto.IsDefault.HasValue)
        {
            workflow.IsDefault = false;
        }

        if (dto.EffectiveFrom.HasValue)
        {
            workflow.EffectiveFrom = dto.EffectiveFrom;
        }

        if (dto.EffectiveTo.HasValue)
        {
            workflow.EffectiveTo = dto.EffectiveTo;
        }

        workflow.UpdatedAt = DateTime.UtcNow;
        workflow.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated approval workflow {WorkflowId}", id);

        return await GetWorkflowByIdAsync(id, companyId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated workflow");
    }

    public async Task DeleteWorkflowAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting approval workflow {WorkflowId} for company {CompanyId}", id, companyId);

        var workflow = await _context.ApprovalWorkflows
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId, cancellationToken);

        if (workflow == null)
        {
            throw new KeyNotFoundException($"Approval workflow with ID {id} not found");
        }

        _context.ApprovalWorkflows.Remove(workflow);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted approval workflow {WorkflowId}", id);
    }

    public async Task<ApprovalWorkflowDto> SetAsDefaultAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting approval workflow {WorkflowId} as default for company {CompanyId}", id, companyId);

        var workflow = await _context.ApprovalWorkflows
            .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId, cancellationToken);

        if (workflow == null)
        {
            throw new KeyNotFoundException($"Approval workflow with ID {id} not found");
        }

        var existingDefaults = await _context.ApprovalWorkflows
            .Where(w => w.CompanyId == companyId 
                && w.WorkflowType == workflow.WorkflowType 
                && w.Id != id
                && w.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingDefaults)
        {
            existing.IsDefault = false;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;
        }

        workflow.IsDefault = true;
        workflow.UpdatedAt = DateTime.UtcNow;
        workflow.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set approval workflow {WorkflowId} as default", id);

        return await GetWorkflowByIdAsync(id, companyId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated workflow");
    }

    private static ApprovalWorkflowDto MapToDto(ApprovalWorkflow workflow)
    {
        return new ApprovalWorkflowDto
        {
            Id = workflow.Id,
            CompanyId = workflow.CompanyId,
            Name = workflow.Name,
            Description = workflow.Description,
            WorkflowType = workflow.WorkflowType,
            EntityType = workflow.EntityType,
            PartnerId = workflow.PartnerId,
            DepartmentId = workflow.DepartmentId,
            OrderType = workflow.OrderType,
            MinValueThreshold = workflow.MinValueThreshold,
            RequireAllSteps = workflow.RequireAllSteps,
            AllowParallelApproval = workflow.AllowParallelApproval,
            TimeoutMinutes = workflow.TimeoutMinutes,
            AutoApproveOnTimeout = workflow.AutoApproveOnTimeout,
            EscalationRole = workflow.EscalationRole,
            EscalationUserId = workflow.EscalationUserId,
            IsActive = workflow.IsActive,
            IsDefault = workflow.IsDefault,
            EffectiveFrom = workflow.EffectiveFrom,
            EffectiveTo = workflow.EffectiveTo,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            Steps = workflow.Steps.Select(MapStepToDto).OrderBy(s => s.StepOrder).ToList()
        };
    }

    private static ApprovalStepDto MapStepToDto(ApprovalStep step)
    {
        return new ApprovalStepDto
        {
            Id = step.Id,
            ApprovalWorkflowId = step.ApprovalWorkflowId,
            Name = step.Name,
            StepOrder = step.StepOrder,
            ApprovalType = step.ApprovalType,
            TargetUserId = step.TargetUserId,
            TargetRole = step.TargetRole,
            TargetTeamId = step.TargetTeamId,
            ExternalSource = step.ExternalSource,
            IsRequired = step.IsRequired,
            CanSkipIfPreviousApproved = step.CanSkipIfPreviousApproved,
            TimeoutMinutes = step.TimeoutMinutes,
            AutoApproveOnTimeout = step.AutoApproveOnTimeout,
            IsActive = step.IsActive,
            CreatedAt = step.CreatedAt,
            UpdatedAt = step.UpdatedAt
        };
    }
}

