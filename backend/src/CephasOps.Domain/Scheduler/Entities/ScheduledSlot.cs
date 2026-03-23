using CephasOps.Domain.Common;

namespace CephasOps.Domain.Scheduler.Entities;

/// <summary>
/// Scheduled slot entity - binding between Order and SI time slot
/// </summary>
public class ScheduledSlot : CompanyScopedEntity
{
    public Guid OrderId { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan WindowFrom { get; set; }
    public TimeSpan WindowTo { get; set; }
    public int? PlannedTravelMin { get; set; }
    public int SequenceIndex { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Confirmed, Posted, RescheduleRequested, RescheduleApproved, RescheduleRejected, InProgress, Completed, Cancelled
    public Guid CreatedByUserId { get; set; }
    
    // Confirmation and posting tracking
    public Guid? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public DateTime? PostedAt { get; set; }
    
    // Reschedule request fields
    public DateTime? RescheduleRequestedDate { get; set; }
    public TimeSpan? RescheduleRequestedTime { get; set; }
    public string? RescheduleReason { get; set; }
    public string? RescheduleNotes { get; set; }
    public Guid? RescheduleRequestedBySiId { get; set; }
    public DateTime? RescheduleRequestedAt { get; set; }
}

