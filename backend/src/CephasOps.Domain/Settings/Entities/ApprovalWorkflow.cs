using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Approval Workflow entity - defines multi-step approval processes for critical actions
/// </summary>
public class ApprovalWorkflow : CompanyScopedEntity
{
    /// <summary>
    /// Workflow name (e.g. "RMA Serialized Material Approval", "High-Value Invoice Approval")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Workflow type: RescheduleApproval, RmaApproval, InvoiceApproval, SplitterOverrideApproval, Custom
    /// </summary>
    public string WorkflowType { get; set; } = string.Empty;

    /// <summary>
    /// Entity type this workflow applies to (e.g., "Order", "Invoice", "RMA")
    /// </summary>
    public string EntityType { get; set; } = "Order";

    /// <summary>
    /// Partner ID (nullable) - if set, applies only to this partner
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Department ID (nullable) - if set, applies only to this department
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Order type filter (nullable) - if set, applies only to this order type
    /// </summary>
    public string? OrderType { get; set; }

    /// <summary>
    /// Minimum value threshold (for value-based approvals, e.g., invoice amount)
    /// </summary>
    public decimal? MinValueThreshold { get; set; }

    /// <summary>
    /// Whether this workflow requires all steps to be approved (true) or just one (false)
    /// </summary>
    public bool RequireAllSteps { get; set; } = true;

    /// <summary>
    /// Whether approvals can be done in parallel (true) or must be sequential (false)
    /// </summary>
    public bool AllowParallelApproval { get; set; } = false;

    /// <summary>
    /// Timeout in minutes - if approval not received within this time, escalate
    /// </summary>
    public int? TimeoutMinutes { get; set; }

    /// <summary>
    /// Whether to auto-approve if timeout is reached
    /// </summary>
    public bool AutoApproveOnTimeout { get; set; } = false;

    /// <summary>
    /// Role to escalate to if timeout reached
    /// </summary>
    public string? EscalationRole { get; set; }

    /// <summary>
    /// User ID to escalate to if timeout reached
    /// </summary>
    public Guid? EscalationUserId { get; set; }

    /// <summary>
    /// Whether this workflow is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is the default workflow for (CompanyId, WorkflowType)
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective to date (nullable for ongoing workflows)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// User ID who created this workflow
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this workflow
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>
    /// Navigation property for approval steps
    /// </summary>
    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
}

/// <summary>
/// Approval Step entity - individual step in an approval workflow
/// </summary>
public class ApprovalStep : CompanyScopedEntity
{
    /// <summary>
    /// Foreign key to ApprovalWorkflow
    /// </summary>
    public Guid ApprovalWorkflowId { get; set; }

    /// <summary>
    /// Step name (e.g. "TIME Approval", "Finance Approval", "HOD Approval")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Step order (1, 2, 3, etc.)
    /// </summary>
    public int StepOrder { get; set; }

    /// <summary>
    /// Approval type: User, Role, Team, External (e.g., TIME portal)
    /// </summary>
    public string ApprovalType { get; set; } = string.Empty;

    /// <summary>
    /// Target user ID (for User approval type)
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// Target role (for Role approval type)
    /// </summary>
    public string? TargetRole { get; set; }

    /// <summary>
    /// Target team ID (for Team approval type)
    /// </summary>
    public Guid? TargetTeamId { get; set; }

    /// <summary>
    /// External approval source (e.g., "TimePortal", "Email", "WhatsApp")
    /// </summary>
    public string? ExternalSource { get; set; }

    /// <summary>
    /// Whether this step is required (false = optional)
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Whether this step can be skipped if previous step is approved
    /// </summary>
    public bool CanSkipIfPreviousApproved { get; set; } = false;

    /// <summary>
    /// Timeout in minutes for this step (overrides workflow timeout if set)
    /// </summary>
    public int? TimeoutMinutes { get; set; }

    /// <summary>
    /// Whether to auto-approve if timeout is reached for this step
    /// </summary>
    public bool AutoApproveOnTimeout { get; set; } = false;

    /// <summary>
    /// Whether this step is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User ID who created this step
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this step
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>
    /// Navigation property to ApprovalWorkflow
    /// </summary>
    public ApprovalWorkflow ApprovalWorkflow { get; set; } = null!;
}

