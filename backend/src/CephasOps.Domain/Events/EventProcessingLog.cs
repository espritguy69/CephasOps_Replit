namespace CephasOps.Domain.Events;

/// <summary>
/// Log of event handler processing for idempotency: at most one successful completion per (EventId, HandlerName).
/// Used to skip handlers that have already completed (retries, replay, resume, rerun-failed, concurrent processing).
/// </summary>
public class EventProcessingLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventId { get; set; }
    /// <summary>Handler type name (e.g. OrderLifecycleLedgerHandler).</summary>
    public string HandlerName { get; set; } = string.Empty;

    /// <summary>Set when processing is part of a replay operation (observability).</summary>
    public Guid? ReplayOperationId { get; set; }

    /// <summary>Processing | Completed | Failed</summary>
    public string State { get; set; } = "Processing";

    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>Last error message when State = Failed (sanitized).</summary>
    public string? Error { get; set; }

    /// <summary>Number of attempts (incremented on each retry/replay attempt).</summary>
    public int AttemptCount { get; set; } = 1;

    public string? CorrelationId { get; set; }

    public static class States
    {
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
