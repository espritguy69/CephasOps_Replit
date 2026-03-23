using CephasOps.Application.Commands;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.ProcessManager;

/// <summary>
/// Base for process managers. Loads state, calls OnEventAsync (subclass returns commands), then sends commands.
/// No persistence of saga state in base - subclasses can use WorkflowInstance or their own store.
/// </summary>
public abstract class ProcessManagerBase : IProcessManager
{
    protected ICommandBus CommandBus { get; }
    protected IWorkflowOrchestrator WorkflowOrchestrator { get; }
    protected ILogger Logger { get; }

    protected ProcessManagerBase(
        ICommandBus commandBus,
        IWorkflowOrchestrator workflowOrchestrator,
        ILogger logger)
    {
        CommandBus = commandBus;
        WorkflowOrchestrator = workflowOrchestrator;
        Logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var state = await LoadStateAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        var commands = await OnEventAsync(state, domainEvent, cancellationToken).ConfigureAwait(false);
        if (commands == null) return;
        foreach (var command in commands)
        {
            if (command == null) continue;
            var result = await CommandBus.SendAsync(command, null, cancellationToken).ConfigureAwait(false);
            if (!result.Success)
                Logger.LogWarning("Process manager command failed. EventId={EventId}, Error={Error}", domainEvent.EventId, result.ErrorMessage);
        }
    }

    /// <summary>
    /// Load saga/process state from WorkflowInstance or custom store. Return null if no state.
    /// </summary>
    protected abstract Task<object?> LoadStateAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);

    /// <summary>
    /// Handle the event and return commands to send. Base sends each via ICommandBus.
    /// </summary>
    protected abstract Task<IReadOnlyList<object>?> OnEventAsync(object? state, IDomainEvent domainEvent, CancellationToken cancellationToken);
}
