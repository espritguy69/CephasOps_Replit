namespace CephasOps.Application.Trace;

/// <summary>
/// Stable item type identifiers for the operational trace timeline.
/// Used for filtering, icons, and display in the Trace Explorer.
/// </summary>
public static class TraceTimelineItemTypes
{
    // HTTP / request (when we have persisted request log in future)
    public const string HttpRequestStarted = "HttpRequestStarted";

    // Workflow
    public const string WorkflowTransitionRequested = "WorkflowTransitionRequested";
    public const string WorkflowTransitionStarted = "WorkflowTransitionStarted";
    public const string WorkflowTransitionCompleted = "WorkflowTransitionCompleted";

    // Events
    public const string EventEmitted = "EventEmitted";
    public const string EventProcessed = "EventProcessed";

    // Event handlers
    public const string EventHandlerStarted = "EventHandlerStarted";
    public const string EventHandlerSucceeded = "EventHandlerSucceeded";
    public const string EventHandlerFailed = "EventHandlerFailed";

    // Background jobs
    public const string BackgroundJobQueued = "BackgroundJobQueued";
    public const string BackgroundJobStarted = "BackgroundJobStarted";
    public const string BackgroundJobCompleted = "BackgroundJobCompleted";
    public const string BackgroundJobFailed = "BackgroundJobFailed";

    // Replay / retry (when audit records exist)
    public const string ReplayRequested = "ReplayRequested";
    public const string ReplayExecuted = "ReplayExecuted";
    public const string ManualRetryRequested = "ManualRetryRequested";
}
