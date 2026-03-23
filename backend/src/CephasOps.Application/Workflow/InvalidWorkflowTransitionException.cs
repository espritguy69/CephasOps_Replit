namespace CephasOps.Application.Workflow;

/// <summary>
/// Thrown when an order (or other workflow entity) status transition is not allowed.
/// Source of truth for allowed transitions: WorkflowTransitions (seeded by 07_gpon_order_workflow.sql).
/// </summary>
public class InvalidWorkflowTransitionException : InvalidOperationException
{
    public string CurrentStatus { get; }
    public string AttemptedStatus { get; }
    public IReadOnlyList<string> AllowedNextStatuses { get; }

    public InvalidWorkflowTransitionException(
        string currentStatus,
        string attemptedStatus,
        IReadOnlyList<string> allowedNextStatuses,
        string? entityType = null)
        : base(BuildMessage(currentStatus, attemptedStatus, allowedNextStatuses, entityType))
    {
        CurrentStatus = currentStatus;
        AttemptedStatus = attemptedStatus;
        AllowedNextStatuses = allowedNextStatuses;
    }

    private static string BuildMessage(
        string currentStatus,
        string attemptedStatus,
        IReadOnlyList<string> allowedNextStatuses,
        string? entityType)
    {
        var entitySuffix = string.IsNullOrEmpty(entityType) ? "" : $" for entity type '{entityType}'";
        var allowed = allowedNextStatuses.Count == 0
            ? "None (terminal or invalid current status)."
            : string.Join(", ", allowedNextStatuses);
        return $"Invalid workflow transition from {currentStatus} to {attemptedStatus}{entitySuffix}. Allowed next statuses: {allowed}";
    }
}
