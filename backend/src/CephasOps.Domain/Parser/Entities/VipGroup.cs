using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// VIP Group entity - defines a reusable template for VIP email notification routing
/// </summary>
public class VipGroup : CompanyScopedEntity
{
    /// <summary>
    /// Group name (e.g., "Procurement VIP", "GPON VIP")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the group (e.g., "PROCUREMENT_VIP", "GPON_VIP")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Description of this VIP group
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Department ID to route notifications/tasks to
    /// </summary>
    public Guid? NotifyDepartmentId { get; set; }

    /// <summary>
    /// Specific user ID to notify when VIP email arrives
    /// </summary>
    public Guid? NotifyUserId { get; set; }

    /// <summary>
    /// HOD/Supervisor user ID to also notify
    /// </summary>
    public Guid? NotifyHodUserId { get; set; }

    /// <summary>
    /// Role name to notify (all users with this role)
    /// </summary>
    public string? NotifyRole { get; set; }

    /// <summary>
    /// Priority level (higher = more important)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this VIP group is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User who created this group
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// User who last updated this group
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

