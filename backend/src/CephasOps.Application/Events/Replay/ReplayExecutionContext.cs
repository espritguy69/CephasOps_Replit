using CephasOps.Application.Events.DTOs;
using CephasOps.Domain.Events;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Immutable replay execution context for use during replay dispatch.
/// </summary>
public sealed class ReplayExecutionContext : IReplayExecutionContext
{
    public static readonly IReplayExecutionContext None = new ReplayExecutionContext(false, null, false, null, null);

    public bool IsReplay { get; }
    public Guid? ReplayOperationId { get; }
    public bool SuppressSideEffects { get; }
    public string? ReplayTarget { get; }
    public string? ReplayMode { get; }

    public ReplayExecutionContext(bool isReplay, Guid? replayOperationId, bool suppressSideEffects, string? replayTarget, string? replayMode)
    {
        IsReplay = isReplay;
        ReplayOperationId = replayOperationId;
        SuppressSideEffects = suppressSideEffects;
        ReplayTarget = replayTarget;
        ReplayMode = replayMode;
    }

    public static IReplayExecutionContext ForReplay(Guid operationId, string? target, string mode, bool suppressSideEffects = true)
        => new ReplayExecutionContext(true, operationId, suppressSideEffects, target, mode);

    /// <summary>
    /// Context for single-event retry/replay (no ReplayOperation). SuppressSideEffects = true so async handlers are not enqueued.
    /// </summary>
    public static IReplayExecutionContext ForSingleEventRetry(string replayMode = "Retry")
        => new ReplayExecutionContext(true, null, true, ReplayTargets.EventStore, replayMode);
}
