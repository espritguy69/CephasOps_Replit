using CephasOps.Domain.Common;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// Order reschedule entity - tracks reschedule requests
/// Per ORDER_LIFECYCLE.md section 6.2: Same-day reschedules require customer evidence
/// </summary>
public class OrderReschedule : CompanyScopedEntity
{
    public Guid OrderId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public Guid? RequestedBySiId { get; set; }
    public string RequestedBySource { get; set; } = string.Empty; // Customer, Partner, Internal, SI
    public DateTime RequestedAt { get; set; }
    public DateTime OriginalDate { get; set; }
    public TimeSpan OriginalWindowFrom { get; set; }
    public TimeSpan OriginalWindowTo { get; set; }
    public DateTime NewDate { get; set; }
    public TimeSpan NewWindowFrom { get; set; }
    public TimeSpan NewWindowTo { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ApprovalSource { get; set; } // EmailParser, Manual, AutoPolicy
    public Guid? ApprovalEmailId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled
    public Guid? StatusChangedByUserId { get; set; }
    public DateTime? StatusChangedAt { get; set; }
    
    /// <summary>
    /// Whether this is a same-day reschedule (OriginalDate == NewDate)
    /// Per ORDER_LIFECYCLE.md: Same-day reschedules require evidence
    /// </summary>
    public bool IsSameDayReschedule { get; set; }
    
    /// <summary>
    /// Evidence attachment ID (required for same-day reschedules)
    /// Per ORDER_LIFECYCLE.md section 6.2: Must have customer evidence (WhatsApp, SMS, etc.)
    /// </summary>
    public Guid? SameDayEvidenceAttachmentId { get; set; }
    
    /// <summary>
    /// Notes about the same-day reschedule evidence
    /// </summary>
    public string? SameDayEvidenceNotes { get; set; }
}

