using CephasOps.Domain.Common;

namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// Workflow definition entity - defines a workflow configuration for an entity type (e.g., Order, Invoice)
/// </summary>
public class WorkflowDefinition : CompanyScopedEntity
{
    /// <summary>
    /// Workflow name (e.g., "ISP_Order_Workflow", "Invoice_Workflow")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Entity type this workflow applies to (e.g., "Order", "Invoice")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the workflow
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this workflow is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional: Partner ID if this workflow is partner-specific
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Optional: Department ID if this workflow is department-specific (e.g., GPON, CWO, NWO)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Optional: Order type code (e.g. "MODIFICATION", "ASSURANCE", "ACTIVATION") for order-type-specific workflows.
    /// For Orders, resolution uses the parent order type code when the selected OrderType is a subtype.
    /// Null = general workflow (not scoped by order type).
    /// </summary>
    public string? OrderTypeCode { get; set; }

    /// <summary>
    /// User ID who created this workflow definition
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this workflow definition
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>
    /// Navigation property for workflow transitions
    /// </summary>
    public ICollection<WorkflowTransition> Transitions { get; set; } = new List<WorkflowTransition>();
}

