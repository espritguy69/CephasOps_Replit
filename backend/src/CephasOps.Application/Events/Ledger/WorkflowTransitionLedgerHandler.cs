using CephasOps.Application.Events.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Writes WorkflowTransitionCompleted events into the operational event ledger. Replay-safe; idempotent by (SourceEventId, Family).
/// Runs on live and on all replay targets (registered as both IDomainEventHandler and IProjectionEventHandler).
/// </summary>
public sealed class WorkflowTransitionLedgerHandler : IProjectionEventHandler<WorkflowTransitionCompletedEvent>
{
    private readonly ILedgerWriter _ledgerWriter;
    private readonly ILogger<WorkflowTransitionLedgerHandler> _logger;

    public WorkflowTransitionLedgerHandler(ILedgerWriter ledgerWriter, ILogger<WorkflowTransitionLedgerHandler> logger)
    {
        _ledgerWriter = ledgerWriter;
        _logger = logger;
    }

    public async Task HandleAsync(WorkflowTransitionCompletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            domainEvent.WorkflowJobId,
            domainEvent.WorkflowDefinitionId,
            domainEvent.WorkflowTransitionId,
            domainEvent.FromStatus,
            domainEvent.ToStatus
        });
        await _ledgerWriter.AppendFromEventAsync(
            domainEvent.EventId,
            LedgerFamilies.WorkflowTransition,
            domainEvent.EventType,
            domainEvent.OccurredAtUtc,
            domainEvent.CompanyId,
            domainEvent.EntityType,
            domainEvent.EntityId,
            payload,
            domainEvent.CorrelationId,
            domainEvent.TriggeredByUserId,
            OrderingStrategies.OccurredAtUtcAscendingEventIdAscending,
            category: null,
            cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Ledger appended WorkflowTransition Entry for EventId={EventId}", domainEvent.EventId);
    }
}
