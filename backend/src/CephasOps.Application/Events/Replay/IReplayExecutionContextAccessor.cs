using CephasOps.Domain.Events;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Provides ambient replay execution context (e.g. AsyncLocal). Handlers and services check this to suppress side effects during replay.
/// </summary>
public interface IReplayExecutionContextAccessor
{
    /// <summary>Current replay context, or null/None when not in replay.</summary>
    IReplayExecutionContext? Current { get; }

    /// <summary>Set the current context (e.g. for the duration of a replay dispatch). Call with null to clear.</summary>
    void Set(IReplayExecutionContext? context);
}
