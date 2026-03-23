using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events;

/// <summary>
/// Basic handler for WorkflowTransitionCompleted: logs the event. Optional side effects (e.g. send notification, trigger background job) can be added later.
/// Handlers must remain optional and side-effect safe; this one only logs.
/// </summary>
public class WorkflowTransitionCompletedEventHandler : IDomainEventHandler<WorkflowTransitionCompletedEvent>
{
    private readonly ILogger<WorkflowTransitionCompletedEventHandler> _logger;

    public WorkflowTransitionCompletedEventHandler(ILogger<WorkflowTransitionCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task HandleAsync(WorkflowTransitionCompletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "WorkflowTransitionCompleted: Entity {EntityType}/{EntityId} {FromStatus} → {ToStatus}, JobId={WorkflowJobId}, CorrelationId={CorrelationId}",
            domainEvent.EntityType, domainEvent.EntityId, domainEvent.FromStatus, domainEvent.ToStatus,
            domainEvent.WorkflowJobId, domainEvent.CorrelationId);
        return Task.CompletedTask;
    }
}
