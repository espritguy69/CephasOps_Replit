using CephasOps.Domain.Orders.Enums;
using CephasOps.Infrastructure;

namespace CephasOps.Application.Workflow;

/// <summary>
/// Enforces canonical Service Installer (SI) order workflow transitions.
/// Normal flow: Assigned → OnTheWay → MetCustomer → OrderCompleted (then docket/billing path to Completed).
/// Exception flows (Blocker, ReschedulePendingApproval, Cancelled) are explicit and reason-driven elsewhere.
/// This guard prevents invalid status jumps even if WorkflowTransitions table is modified.
/// No schema changes; uses existing OrderStatus constants only.
/// </summary>
public static class SiWorkflowGuard
{
    /// <summary>Order entity type name used when invoking this guard.</summary>
    public const string OrderEntityType = "Order";

    // Allowed (FromStatus, ToStatus) pairs for Order. Matches 07_gpon_order_workflow.sql seed.
    // Any transition not in this set for Order entity is rejected (defense-in-depth over DB).
    private static readonly HashSet<(string From, string To)> AllowedOrderTransitions = new()
    {
        (OrderStatus.Pending, OrderStatus.Assigned),
        (OrderStatus.Pending, OrderStatus.Cancelled),
        (OrderStatus.Assigned, OrderStatus.OnTheWay),
        (OrderStatus.Assigned, OrderStatus.Blocker),
        (OrderStatus.Assigned, OrderStatus.ReschedulePendingApproval),
        (OrderStatus.Assigned, OrderStatus.Cancelled),
        (OrderStatus.OnTheWay, OrderStatus.MetCustomer),
        (OrderStatus.OnTheWay, OrderStatus.Blocker),
        (OrderStatus.MetCustomer, OrderStatus.OrderCompleted),
        (OrderStatus.MetCustomer, OrderStatus.Blocker),
        (OrderStatus.Blocker, OrderStatus.MetCustomer),
        (OrderStatus.Blocker, OrderStatus.Assigned),
        (OrderStatus.Blocker, OrderStatus.ReschedulePendingApproval),
        (OrderStatus.Blocker, OrderStatus.Cancelled),
        (OrderStatus.ReschedulePendingApproval, OrderStatus.Assigned),
        (OrderStatus.ReschedulePendingApproval, OrderStatus.Cancelled),
        (OrderStatus.OrderCompleted, OrderStatus.DocketsReceived),
        (OrderStatus.DocketsReceived, OrderStatus.DocketsVerified),
        (OrderStatus.DocketsReceived, OrderStatus.DocketsRejected),
        (OrderStatus.DocketsRejected, OrderStatus.DocketsReceived),
        (OrderStatus.DocketsVerified, OrderStatus.DocketsUploaded),
        (OrderStatus.DocketsUploaded, OrderStatus.ReadyForInvoice),
        (OrderStatus.ReadyForInvoice, OrderStatus.Invoiced),
        (OrderStatus.Invoiced, OrderStatus.SubmittedToPortal),
        (OrderStatus.Invoiced, OrderStatus.Rejected),
        (OrderStatus.SubmittedToPortal, OrderStatus.Completed),
        (OrderStatus.SubmittedToPortal, OrderStatus.Rejected),
        (OrderStatus.Rejected, OrderStatus.ReadyForInvoice),
        (OrderStatus.Rejected, OrderStatus.Reinvoice),
        (OrderStatus.Reinvoice, OrderStatus.Invoiced),
    };

    /// <summary>
    /// Validates that an Order status transition is in the canonical allowed set.
    /// Throws <see cref="InvalidOperationException"/> if the transition is not allowed.
    /// Call this for entity type "Order" before executing the transition (e.g. in workflow engine).
    /// </summary>
    /// <param name="currentStatus">Current order status.</param>
    /// <param name="targetStatus">Requested target status.</param>
    /// <param name="operationName">Optional operation name for the exception message.</param>
    public static void RequireValidOrderTransition(string currentStatus, string targetStatus, string? operationName = null)
    {
        if (string.IsNullOrEmpty(currentStatus))
        {
            PlatformGuardLogger.LogViolation("SiWorkflowGuard", operationName ?? "Order status transition", "Current status is required and cannot be empty.", entityType: OrderEntityType);
            throw new InvalidOperationException("Order transition validation requires a non-empty current status.");
        }
        if (string.IsNullOrEmpty(targetStatus))
        {
            PlatformGuardLogger.LogViolation("SiWorkflowGuard", operationName ?? "Order status transition", "Target status is required and cannot be empty.", entityType: OrderEntityType);
            throw new InvalidOperationException("Order transition validation requires a non-empty target status.");
        }

        var key = (currentStatus, targetStatus);
        if (AllowedOrderTransitions.Contains(key))
            return;

        var op = string.IsNullOrEmpty(operationName) ? "Order status transition" : operationName;
        var allowedFromCurrent = AllowedOrderTransitions
            .Where(t => t.From == currentStatus)
            .Select(t => t.To)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        var allowedStr = allowedFromCurrent.Count == 0
            ? "None (terminal or invalid current status)."
            : string.Join(", ", allowedFromCurrent);
        var message = $"Invalid transition from '{currentStatus}' to '{targetStatus}'. Allowed from current: {allowedStr}.";
        PlatformGuardLogger.LogViolation("SiWorkflowGuard", op, message, entityType: OrderEntityType);
        throw new InvalidOperationException(
            $"{op}: invalid transition from '{currentStatus}' to '{targetStatus}'. Allowed next statuses from '{currentStatus}': {allowedStr}.");
    }

    /// <summary>
    /// Returns whether the given transition is allowed for Order (for tests or diagnostics).
    /// </summary>
    public static bool IsAllowedOrderTransition(string currentStatus, string targetStatus)
    {
        if (string.IsNullOrEmpty(currentStatus) || string.IsNullOrEmpty(targetStatus))
            return false;
        return AllowedOrderTransitions.Contains((currentStatus, targetStatus));
    }

    /// <summary>
    /// Requires a non-empty reschedule reason when transitioning to ReschedulePendingApproval.
    /// Ensures reschedule/issue states are not left without an auditable reason.
    /// </summary>
    /// <param name="reason">Reason from payload or request (e.g. "reason" key).</param>
    /// <param name="operationName">Optional operation name for the exception message.</param>
    public static void RequireRescheduleReason(string? reason, string? operationName = null)
    {
        if (!string.IsNullOrWhiteSpace(reason))
            return;
        var op = string.IsNullOrEmpty(operationName) ? "Order workflow" : operationName;
        var message = "Reschedule reason is required when transitioning to ReschedulePendingApproval. Provide a reason (e.g. customer request, building issue) for auditability.";
        PlatformGuardLogger.LogViolation("SiWorkflowGuard", op, message, entityType: OrderEntityType);
        throw new InvalidOperationException($"{op}: {message}");
    }
}
