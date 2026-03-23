namespace CephasOps.Application.Events;

/// <summary>
/// Platform-standard event type names. Namespace: ops.&lt;area&gt;.&lt;action&gt;.v&lt;version&gt;.
/// Use these for new events; legacy type names remain registered for replay compatibility.
/// </summary>
public static class PlatformEventTypes
{
    public const string WorkflowTransitionCompleted = "ops.workflow.transition_completed.v1";
    public const string OrderStatusChanged = "ops.order.status_changed.v1";
    public const string OrderAssigned = "ops.order.assigned.v1";
    public const string OrderCreated = "ops.order.created.v1";
    public const string OrderCompleted = "ops.order.completed.v1";
    public const string InvoiceGenerated = "ops.billing.invoice_generated.v1";
    public const string MaterialIssued = "ops.inventory.material_issued.v1";
    public const string MaterialReturned = "ops.inventory.material_returned.v1";
    public const string PayrollCalculated = "ops.payroll.calculated.v1";

    public const string JobStarted = "ops.job.started.v1";
    public const string JobCompleted = "ops.job.completed.v1";
    public const string JobFailed = "ops.job.failed.v1";

    /// <summary>Legacy name for WorkflowTransitionCompleted (replay compatibility).</summary>
    public const string LegacyWorkflowTransitionCompleted = "WorkflowTransitionCompleted";
    /// <summary>Legacy name for OrderStatusChanged (replay compatibility).</summary>
    public const string LegacyOrderStatusChanged = "OrderStatusChanged";
    /// <summary>Legacy name for OrderAssigned (replay compatibility).</summary>
    public const string LegacyOrderAssigned = "OrderAssigned";
    /// <summary>Legacy name for OrderCreated (replay compatibility).</summary>
    public const string LegacyOrderCreated = "OrderCreated";
    /// <summary>Legacy name for OrderCompleted (replay compatibility).</summary>
    public const string LegacyOrderCompleted = "OrderCompleted";
    /// <summary>Legacy name for InvoiceGenerated (replay compatibility).</summary>
    public const string LegacyInvoiceGenerated = "InvoiceGenerated";
    /// <summary>Legacy name for MaterialIssued (replay compatibility).</summary>
    public const string LegacyMaterialIssued = "MaterialIssued";
    /// <summary>Legacy name for MaterialReturned (replay compatibility).</summary>
    public const string LegacyMaterialReturned = "MaterialReturned";
    /// <summary>Legacy name for PayrollCalculated (replay compatibility).</summary>
    public const string LegacyPayrollCalculated = "PayrollCalculated";
}
