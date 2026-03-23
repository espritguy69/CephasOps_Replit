using CephasOps.Domain.Common;

namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// Workflow transition entity - defines allowed status transitions within a workflow
/// </summary>
public class WorkflowTransition : CompanyScopedEntity
{
    /// <summary>
    /// Foreign key to WorkflowDefinition
    /// </summary>
    public Guid WorkflowDefinitionId { get; set; }

    /// <summary>
    /// Starting status (null = initial state)
    /// </summary>
    public string? FromStatus { get; set; }

    /// <summary>
    /// Target status
    /// </summary>
    public string ToStatus { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of role names allowed to trigger this transition (e.g., ["Admin", "SI", "Scheduler"])
    /// </summary>
    public string AllowedRolesJson { get; set; } = "[]";

    /// <summary>
    /// JSON object defining guard conditions that must be met before transition (e.g., {"photosRequired": true, "docketUploaded": true})
    /// </summary>
    public string? GuardConditionsJson { get; set; }

    /// <summary>
    /// JSON object defining side effects/actions on transition (e.g., {"notify": true, "createStockMovement": true})
    /// </summary>
    public string? SideEffectsConfigJson { get; set; }

    /// <summary>
    /// Display order for UI
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether this transition is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User ID who created this transition
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this transition
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>
    /// Navigation property to WorkflowDefinition
    /// </summary>
    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
}

