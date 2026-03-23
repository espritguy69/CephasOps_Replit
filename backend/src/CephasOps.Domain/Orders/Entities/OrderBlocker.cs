using CephasOps.Domain.Common;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// Order blocker entity - represents conditions preventing job progress
/// Per ORDER_LIFECYCLE.md section 3.5: SI must provide evidence when raising blockers
/// </summary>
public class OrderBlocker : CompanyScopedEntity
{
    public Guid OrderId { get; set; }
    public string BlockerType { get; set; } = string.Empty; // CustomerNotHome, BuildingAccess, NetworkIssue, etc.
    
    /// <summary>
    /// Blocker category per ORDER_LIFECYCLE.md: Customer, Building, Network, SI, Weather, Other
    /// </summary>
    public string? BlockerCategory { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public Guid? RaisedBySiId { get; set; }
    public Guid? RaisedByUserId { get; set; }
    public DateTime RaisedAt { get; set; }
    public bool Resolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolutionNotes { get; set; }
    
    /// <summary>
    /// Evidence attachment IDs (JSON array of Guids)
    /// Per ORDER_LIFECYCLE.md: SI must provide evidence (photos, screenshots) when raising blockers
    /// </summary>
    public string? EvidenceAttachmentIds { get; set; }
    
    /// <summary>
    /// Whether evidence is required for this blocker type
    /// </summary>
    public bool EvidenceRequired { get; set; } = true;
    
    /// <summary>
    /// Notes about the evidence provided
    /// </summary>
    public string? EvidenceNotes { get; set; }
}

