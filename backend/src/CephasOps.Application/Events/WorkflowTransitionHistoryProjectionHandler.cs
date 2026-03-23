using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events;

/// <summary>
/// Replay-safe projection: writes WorkflowTransitionCompleted events into WorkflowTransitionHistory for workflow history read model.
/// Idempotent by EventId (upsert). Only runs when replay target is Projection.
/// </summary>
public class WorkflowTransitionHistoryProjectionHandler : IProjectionEventHandler<WorkflowTransitionCompletedEvent>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkflowTransitionHistoryProjectionHandler> _logger;

    public WorkflowTransitionHistoryProjectionHandler(
        ApplicationDbContext context,
        ILogger<WorkflowTransitionHistoryProjectionHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(WorkflowTransitionCompletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var existing = await _context.WorkflowTransitionHistory
            .FirstOrDefaultAsync(e => e.EventId == domainEvent.EventId, cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
        {
            existing.WorkflowJobId = domainEvent.WorkflowJobId;
            existing.CompanyId = domainEvent.CompanyId;
            existing.EntityType = domainEvent.EntityType;
            existing.EntityId = domainEvent.EntityId;
            existing.FromStatus = domainEvent.FromStatus;
            existing.ToStatus = domainEvent.ToStatus;
            existing.OccurredAtUtc = domainEvent.OccurredAtUtc;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Projection updated WorkflowTransitionHistory EventId={EventId}", domainEvent.EventId);
            return;
        }

        _context.WorkflowTransitionHistory.Add(new WorkflowTransitionHistoryEntry
        {
            EventId = domainEvent.EventId,
            WorkflowJobId = domainEvent.WorkflowJobId,
            CompanyId = domainEvent.CompanyId,
            EntityType = domainEvent.EntityType,
            EntityId = domainEvent.EntityId,
            FromStatus = domainEvent.FromStatus,
            ToStatus = domainEvent.ToStatus,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Projection inserted WorkflowTransitionHistory EventId={EventId}", domainEvent.EventId);
    }
}
