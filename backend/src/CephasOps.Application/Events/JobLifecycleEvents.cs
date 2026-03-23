using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>Emitted when a job execution starts (Phase 3). ops.job.started.v1</summary>
public class JobStartedEvent : DomainEvent
{
    public Guid JobId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public int AttemptCount { get; set; }

    public JobStartedEvent()
    {
        EventType = PlatformEventTypes.JobStarted;
        Version = "1";
        Source = "JobOrchestration";
    }
}

/// <summary>Emitted when a job execution completes successfully (Phase 3). ops.job.completed.v1</summary>
public class JobCompletedEvent : DomainEvent
{
    public Guid JobId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public int AttemptCount { get; set; }

    public JobCompletedEvent()
    {
        EventType = PlatformEventTypes.JobCompleted;
        Version = "1";
        Source = "JobOrchestration";
    }
}

/// <summary>Emitted when a job execution fails (Phase 3). ops.job.failed.v1</summary>
public class JobFailedEvent : DomainEvent
{
    public Guid JobId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public string? ErrorMessage { get; set; }
    public bool DeadLetter { get; set; }

    public JobFailedEvent()
    {
        EventType = PlatformEventTypes.JobFailed;
        Version = "1";
        Source = "JobOrchestration";
    }
}
