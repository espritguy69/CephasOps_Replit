namespace CephasOps.Application.Events;

/// <summary>
/// Tracks whether the Event Store dispatcher is running. Used by health checks.
/// </summary>
public interface IEventStoreDispatcherState
{
    bool IsRunning { get; }
}

/// <summary>
/// Default implementation; set by EventStoreDispatcherHostedService.
/// </summary>
public sealed class EventStoreDispatcherState : IEventStoreDispatcherState
{
    public bool IsRunning { get; set; }
}
