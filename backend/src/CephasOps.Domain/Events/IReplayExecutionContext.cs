namespace CephasOps.Domain.Events;

/// <summary>
/// Execution context for operational replay. When set, downstream code can detect replay and suppress side effects (notifications, external calls, live job dispatch).
/// </summary>
public interface IReplayExecutionContext
{
    /// <summary>True when the current execution is part of an operational replay.</summary>
    bool IsReplay { get; }

    /// <summary>Replay operation id for audit and correlation.</summary>
    Guid? ReplayOperationId { get; }

    /// <summary>When true, handlers must not send notifications, SMS, email, or trigger live external side effects.</summary>
    bool SuppressSideEffects { get; }

    /// <summary>Replay target (e.g. EventStore, Workflow, Financial, Parser, Projection).</summary>
    string? ReplayTarget { get; }

    /// <summary>Replay mode (e.g. DryRun, Apply).</summary>
    string? ReplayMode { get; }
}
