using CephasOps.Application.Workflow.DTOs;

namespace CephasOps.Application.Workflow.Interfaces;

/// <summary>
/// Interface for side effect executors
/// Each executor is a pluggable component that can be registered dynamically
/// Executors are loaded from settings (SideEffectDefinition) - no hardcoding
/// </summary>
public interface ISideEffectExecutor
{
    /// <summary>
    /// Unique key for this executor (must match SideEffectDefinition.Key)
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Entity type this executor applies to (e.g., "Order", "Invoice")
    /// </summary>
    string EntityType { get; }

    /// <summary>
    /// Execute the side effect for an entity
    /// </summary>
    /// <param name="entityId">ID of the entity</param>
    /// <param name="transition">Workflow transition being executed</param>
    /// <param name="payload">Transition payload</param>
    /// <param name="config">Configuration from SideEffectDefinition.ExecutorConfigJson</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteAsync(
        Guid entityId,
        WorkflowTransitionDto transition,
        Dictionary<string, object>? payload,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default);
}

