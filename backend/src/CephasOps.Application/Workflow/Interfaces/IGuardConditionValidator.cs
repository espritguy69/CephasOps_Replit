namespace CephasOps.Application.Workflow.Interfaces;

/// <summary>
/// Interface for guard condition validators
/// Each validator is a pluggable component that can be registered dynamically
/// Validators are loaded from settings (GuardConditionDefinition) - no hardcoding
/// </summary>
public interface IGuardConditionValidator
{
    /// <summary>
    /// Unique key for this validator (must match GuardConditionDefinition.Key)
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Entity type this validator applies to (e.g., "Order", "Invoice")
    /// </summary>
    string EntityType { get; }

    /// <summary>
    /// Validate the guard condition for an entity
    /// </summary>
    /// <param name="entityId">ID of the entity to validate</param>
    /// <param name="config">Configuration from GuardConditionDefinition.ValidatorConfigJson</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if condition is met, false otherwise</returns>
    Task<bool> ValidateAsync(
        Guid entityId,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default);
}

