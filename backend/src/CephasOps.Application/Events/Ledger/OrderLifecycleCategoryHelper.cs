using CephasOps.Domain.Orders.Enums;

namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Derives OrderLifecycle ledger category from order status transition.
/// Uses Domain OrderStatus constants; fallback is StatusChanged. Documented in EVENT_LEDGER_FOUNDATION.md.
/// </summary>
public static class OrderLifecycleCategoryHelper
{
    public const string StatusChanged = "StatusChanged";
    public const string Assignment = "Assignment";
    public const string FieldProgress = "FieldProgress";
    public const string Docket = "Docket";
    public const string InvoiceReadiness = "InvoiceReadiness";
    public const string Completion = "Completion";

    /// <summary>
    /// Get category for an order status change. Based on NewStatus only (stable and sufficient for current lifecycle).
    /// </summary>
    public static string GetCategoryForNewStatus(string? newStatus)
    {
        if (string.IsNullOrEmpty(newStatus)) return StatusChanged;

        switch (newStatus)
        {
            case OrderStatus.Assigned:
                return Assignment;
            case OrderStatus.OnTheWay:
            case OrderStatus.MetCustomer:
            case OrderStatus.OrderCompleted:
                return FieldProgress;
            case OrderStatus.DocketsReceived:
            case OrderStatus.DocketsVerified:
            case OrderStatus.DocketsRejected:
            case OrderStatus.DocketsUploaded:
                return Docket;
            case OrderStatus.ReadyForInvoice:
            case OrderStatus.Invoiced:
            case OrderStatus.SubmittedToPortal:
                return InvoiceReadiness;
            case OrderStatus.Completed:
            case OrderStatus.Cancelled:
            case OrderStatus.Rejected:
                return Completion;
            default:
                return StatusChanged;
        }
    }
}
