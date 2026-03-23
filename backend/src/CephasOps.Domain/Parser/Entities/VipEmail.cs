using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// VIP email entity - represents email addresses that should be treated as VIP
/// </summary>
public class VipEmail : CompanyScopedEntity
{
    /// <summary>
    /// The email address to match (exact match)
    /// </summary>
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Display name for this VIP entry
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Optional description/notes about this VIP
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// VIP Group this email belongs to (inherits notification settings)
    /// </summary>
    public Guid? VipGroupId { get; set; }

    /// <summary>
    /// User ID to notify when email from this address is received (overrides group setting)
    /// </summary>
    public Guid? NotifyUserId { get; set; }

    /// <summary>
    /// Role name to notify (all users with this role) when email from this address is received
    /// </summary>
    public string? NotifyRole { get; set; }

    /// <summary>
    /// Department this VIP email is associated with (for filtering/routing)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Whether this VIP entry is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User who created this entry
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// User who last updated this entry
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

