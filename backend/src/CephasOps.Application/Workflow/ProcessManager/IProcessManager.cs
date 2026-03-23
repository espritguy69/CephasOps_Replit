using CephasOps.Domain.Events;

namespace CephasOps.Application.Workflow.ProcessManager;

/// <summary>
/// Process manager (saga) handles domain events and may emit commands. Subclasses implement for specific flows.
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// Handle the event: load state, run subclass logic, send any returned commands.
    /// </summary>
    Task HandleEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
