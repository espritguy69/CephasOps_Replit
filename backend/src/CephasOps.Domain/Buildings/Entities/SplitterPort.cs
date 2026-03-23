using CephasOps.Domain.Common;

namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// Splitter port entity - represents individual ports on a splitter
/// Per SPLITTERS_MANAGEMENT_MODULE.md
/// </summary>
public class SplitterPort : CompanyScopedEntity
{
    public Guid SplitterId { get; set; }
    public int PortNumber { get; set; }
    public string Status { get; set; } = "Available"; // Available, Used, Reserved, Standby
    public Guid? OrderId { get; set; } // If assigned to an order
    public DateTime? AssignedAt { get; set; }
    
    /// <summary>
    /// Whether this port is designated as a standby port (e.g., port 32 on 1:32 splitters)
    /// </summary>
    public bool IsStandby { get; set; }
    
    /// <summary>
    /// Whether standby override was approved (required to use a standby port)
    /// Per SPLITTERS_MANAGEMENT_MODULE.md section 4: Standby port rule
    /// </summary>
    public bool StandbyOverrideApproved { get; set; }
    
    /// <summary>
    /// Approval attachment ID (required when using standby port)
    /// Per SPLITTERS_MANAGEMENT_MODULE.md: Must have ApprovalAttachmentId
    /// </summary>
    public Guid? ApprovalAttachmentId { get; set; }
}

