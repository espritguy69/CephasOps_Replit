using CephasOps.Application.Workflow.DTOs;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Creates WorkflowInstance and step records; AdvanceStepAsync updates CurrentStep and appends WorkflowStepRecord.
/// </summary>
public class WorkflowOrchestratorService : IWorkflowOrchestrator
{
    private readonly ApplicationDbContext _context;

    public WorkflowOrchestratorService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstanceDto> StartWorkflowAsync(
        string workflowType,
        string entityType,
        Guid entityId,
        string? initialPayloadJson = null,
        Guid? companyId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowType = workflowType,
            EntityType = entityType,
            EntityId = entityId,
            CurrentStep = "Started",
            Status = WorkflowInstance.Statuses.Running,
            PayloadJson = initialPayloadJson,
            CompanyId = companyId,
            CorrelationId = correlationId,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.WorkflowInstances.Add(instance);

        var step = new WorkflowStepRecord
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            StepName = "Started",
            Status = WorkflowStepRecord.Statuses.Completed,
            StartedAt = now,
            CompletedAt = now,
            PayloadJson = initialPayloadJson
        };
        _context.WorkflowStepRecords.Add(step);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapToDto(instance);
    }

    /// <inheritdoc />
    public async Task AdvanceStepAsync(
        Guid instanceId,
        string stepName,
        string? payloadJson = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await _context.WorkflowInstances
            .FirstOrDefaultAsync(i => i.Id == instanceId, cancellationToken)
            .ConfigureAwait(false);
        if (instance == null)
            throw new InvalidOperationException($"Workflow instance {instanceId} not found.");
        if (instance.Status != WorkflowInstance.Statuses.Running)
            throw new InvalidOperationException($"Workflow instance {instanceId} is not running (status: {instance.Status}).");

        var now = DateTime.UtcNow;
        instance.CurrentStep = stepName;
        instance.UpdatedAt = now;

        var step = new WorkflowStepRecord
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instanceId,
            StepName = stepName,
            Status = WorkflowStepRecord.Statuses.Completed,
            StartedAt = now,
            CompletedAt = now,
            PayloadJson = payloadJson
        };
        _context.WorkflowStepRecords.Add(step);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstanceDto?> GetInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _context.WorkflowInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == instanceId, cancellationToken)
            .ConfigureAwait(false);
        return instance == null ? null : MapToDto(instance);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<WorkflowInstanceDto> Items, int TotalCount)> ListInstancesAsync(
        string? workflowType = null,
        string? entityType = null,
        string? status = null,
        Guid? companyId = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkflowInstances.AsNoTracking();
        if (!string.IsNullOrEmpty(workflowType))
            query = query.Where(i => i.WorkflowType == workflowType);
        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(i => i.EntityType == entityType);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(i => i.Status == status);
        if (companyId.HasValue)
            query = query.Where(i => i.CompanyId == companyId.Value);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var list = await query
            .OrderByDescending(i => i.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (list.Select(MapToDto).ToList(), totalCount);
    }

    private static WorkflowInstanceDto MapToDto(WorkflowInstance i) => new()
    {
        Id = i.Id,
        WorkflowDefinitionId = i.WorkflowDefinitionId,
        WorkflowType = i.WorkflowType,
        EntityType = i.EntityType,
        EntityId = i.EntityId,
        CurrentStep = i.CurrentStep,
        Status = i.Status,
        CorrelationId = i.CorrelationId,
        PayloadJson = i.PayloadJson,
        CompanyId = i.CompanyId,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt,
        CompletedAt = i.CompletedAt
    };
}
