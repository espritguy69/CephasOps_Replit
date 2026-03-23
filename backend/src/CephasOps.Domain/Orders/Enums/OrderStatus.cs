namespace CephasOps.Domain.Orders.Enums;

/// <summary>
/// Order status constants following ORDER_LIFECYCLE.md specification.
/// Status flow: Pending → Assigned → OnTheWay → MetCustomer → OrderCompleted → DocketsReceived → DocketsVerified → DocketsUploaded → ReadyForInvoice → Invoiced → SubmittedToPortal → Completed
/// 
/// This is the SINGLE SOURCE OF TRUTH for order status values.
/// All status strings must match these constants exactly (case-sensitive).
/// </summary>
public static class OrderStatus
{
    // Main flow statuses
    public const string Pending = "Pending";
    public const string Assigned = "Assigned";
    public const string OnTheWay = "OnTheWay";
    public const string MetCustomer = "MetCustomer";
    public const string OrderCompleted = "OrderCompleted";
    public const string DocketsReceived = "DocketsReceived";
    public const string DocketsVerified = "DocketsVerified";
    /// <summary>Docket rejected by admin (verification failed). SI must correct and resubmit.</summary>
    public const string DocketsRejected = "DocketsRejected";
    public const string DocketsUploaded = "DocketsUploaded";
    public const string ReadyForInvoice = "ReadyForInvoice";
    public const string Invoiced = "Invoiced";
    public const string SubmittedToPortal = "SubmittedToPortal";
    public const string Completed = "Completed";

    // Side states
    public const string Blocker = "Blocker";
    public const string ReschedulePendingApproval = "ReschedulePendingApproval";
    /// <summary>Invoice rejected by partner/MyInvois. Display name: "Invoice Rejected". Doc conceptual: InvoiceRejected.</summary>
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
    public const string Reinvoice = "Reinvoice";

    /// <summary>
    /// All valid order statuses (17 total: 12 main flow + 5 side states)
    /// </summary>
    public static readonly string[] AllStatuses = new[]
    {
        Pending, Assigned, OnTheWay, MetCustomer, OrderCompleted,
        DocketsReceived, DocketsVerified, DocketsRejected, DocketsUploaded, ReadyForInvoice, Invoiced, SubmittedToPortal, Completed,
        Blocker, ReschedulePendingApproval, Rejected, Cancelled, Reinvoice
    };

    /// <summary>
    /// Statuses that allow Pre-Customer blockers (before meeting customer)
    /// </summary>
    public static readonly string[] PreCustomerBlockerAllowedStatuses = new[]
    {
        Assigned, OnTheWay
    };

    /// <summary>
    /// Statuses that allow Post-Customer blockers (after meeting customer)
    /// </summary>
    public static readonly string[] PostCustomerBlockerAllowedStatuses = new[]
    {
        MetCustomer
    };

    /// <summary>
    /// Check if a status is valid
    /// </summary>
    public static bool IsValid(string status) => AllStatuses.Contains(status);

    /// <summary>
    /// Check if blocker can be set from the given status
    /// </summary>
    public static bool CanSetBlocker(string currentStatus) =>
        PreCustomerBlockerAllowedStatuses.Contains(currentStatus) ||
        PostCustomerBlockerAllowedStatuses.Contains(currentStatus);

    /// <summary>
    /// Determine if this is a Pre-Customer blocker context
    /// </summary>
    public static bool IsPreCustomerBlockerContext(string currentStatus) =>
        PreCustomerBlockerAllowedStatuses.Contains(currentStatus);

    /// <summary>
    /// Determine if this is a Post-Customer blocker context
    /// </summary>
    public static bool IsPostCustomerBlockerContext(string currentStatus) =>
        PostCustomerBlockerAllowedStatuses.Contains(currentStatus);
}

