using CephasOps.Application.Events;
using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events;

/// <summary>
/// Records JobRun entries for event handler execution so handler runs are observable (Request → Workflow → Event → JobRun).
/// </summary>
public class JobRunRecorderForEvents : IJobRunRecorderForEvents
{
    private readonly IJobRunRecorder _jobRunRecorder;
    private readonly ILogger<JobRunRecorderForEvents> _logger;

    public JobRunRecorderForEvents(IJobRunRecorder jobRunRecorder, ILogger<JobRunRecorderForEvents> logger)
    {
        _jobRunRecorder = jobRunRecorder;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Guid> StartHandlerRunAsync(IDomainEvent domainEvent, string handlerName, CancellationToken cancellationToken = default)
    {
        var jobName = $"{domainEvent.EventType} - {handlerName}";
        string? relatedEntityType = null;
        string? relatedEntityId = null;
        if (domainEvent is WorkflowTransitionCompletedEvent wtc)
        {
            relatedEntityType = wtc.EntityType;
            relatedEntityId = wtc.EntityId.ToString();
        }

        return await _jobRunRecorder.StartAsync(new StartJobRunDto
        {
            CompanyId = domainEvent.CompanyId,
            JobName = jobName,
            JobType = "EventHandling",
            TriggerSource = "EventBus",
            CorrelationId = domainEvent.CorrelationId,
            QueueOrChannel = "InProcess",
            PayloadSummary = $"EventId={domainEvent.EventId:N}",
            RetryCount = 0,
            InitiatedByUserId = domainEvent.TriggeredByUserId,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            EventId = domainEvent.EventId
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task CompleteHandlerRunAsync(Guid jobRunId, CancellationToken cancellationToken = default)
        => _jobRunRecorder.CompleteAsync(jobRunId, cancellationToken);

    /// <inheritdoc />
    public Task FailHandlerRunAsync(Guid jobRunId, Exception ex, CancellationToken cancellationToken = default)
        => _jobRunRecorder.FailAsync(jobRunId, new FailJobRunDto
        {
            ErrorMessage = ex.Message,
            ErrorDetails = ex.ToString(),
            Status = "Failed"
        }, cancellationToken);
}
