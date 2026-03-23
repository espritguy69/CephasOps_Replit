namespace CephasOps.Application.Scheduler.DTOs;

/// <summary>
/// Calendar view DTO
/// </summary>
public class CalendarDto
{
    public DateTime Date { get; set; }
    public List<ScheduleSlotDto> Slots { get; set; } = new();
    public List<SiAvailabilityDto> Availabilities { get; set; } = new();
}

/// <summary>
/// Schedule slot DTO
/// </summary>
public class ScheduleSlotDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan WindowFrom { get; set; }
    public TimeSpan WindowTo { get; set; }
    public int? PlannedTravelMin { get; set; }
    public int SequenceIndex { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    
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
    
    // Enriched order details
    public string? ServiceId { get; set; }
    public string? TicketId { get; set; }
    public string? ExternalRef { get; set; }
    public string? CustomerName { get; set; }
    public string? BuildingName { get; set; }
    public string? PartnerName { get; set; }
    public Guid? PartnerId { get; set; }
    /// <summary>Display-only: Partner.Code + "-" + OrderCategory.Code (e.g. TIME-FTTH). Not persisted.</summary>
    public string? DerivedPartnerCategoryLabel { get; set; }
    public string? OrderStatus { get; set; }
    
    // Enriched SI details
    public string? ServiceInstallerName { get; set; }
    public bool? ServiceInstallerIsSubcontractor { get; set; }
    public string? ServiceInstallerSiLevel { get; set; }
    
    /// <summary>
    /// Expected job duration in minutes from KPI profile
    /// Per KPI_PROFILE_MODULE.md: MaxJobDurationMinutes from resolved KpiProfile
    /// </summary>
    public int? ExpectedDurationMinutes { get; set; }
    
    /// <summary>
    /// KPI profile name that was used to determine the expected duration
    /// </summary>
    public string? KpiProfileName { get; set; }
}

/// <summary>
/// SI availability DTO
/// </summary>
public class SiAvailabilityDto
{
    public Guid Id { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public DateTime Date { get; set; }
    public bool IsWorkingDay { get; set; }
    public TimeSpan? WorkingFrom { get; set; }
    public TimeSpan? WorkingTo { get; set; }
    public int MaxJobs { get; set; }
    public int CurrentJobsCount { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Create schedule slot request DTO
/// </summary>
public class CreateScheduleSlotDto
{
    public Guid OrderId { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan WindowFrom { get; set; }
    public TimeSpan WindowTo { get; set; }
    public int? PlannedTravelMin { get; set; }
}

/// <summary>
/// Create SI availability request DTO
/// </summary>
public class CreateSiAvailabilityDto
{
    public Guid ServiceInstallerId { get; set; }
    public DateTime Date { get; set; }
    public bool IsWorkingDay { get; set; }
    public TimeSpan? WorkingFrom { get; set; }
    public TimeSpan? WorkingTo { get; set; }
    public int MaxJobs { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Unassigned order DTO (for scheduler backlog)
/// </summary>
public class UnassignedOrderDto
{
    public Guid Id { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string? TicketId { get; set; }
    public string? ExternalRef { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? BuildingName { get; set; }
    public string? PartnerName { get; set; }
    public Guid PartnerId { get; set; }
    /// <summary>Display-only: Partner.Code + "-" + OrderCategory.Code (e.g. TIME-FTTH). Not persisted.</summary>
    public string? DerivedPartnerCategoryLabel { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentWindowFrom { get; set; }
    public TimeSpan AppointmentWindowTo { get; set; }
    public string? Priority { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    
    /// <summary>
    /// Expected job duration in minutes from KPI profile
    /// Per KPI_PROFILE_MODULE.md: MaxJobDurationMinutes from resolved KpiProfile
    /// </summary>
    public int? ExpectedDurationMinutes { get; set; }
    
    /// <summary>
    /// KPI profile name that was used to determine the expected duration
    /// </summary>
    public string? KpiProfileName { get; set; }
    
    /// <summary>
    /// Order type ID (for KPI profile resolution)
    /// </summary>
    public Guid? OrderTypeId { get; set; }
    
    /// <summary>
    /// Building type ID (for KPI profile resolution)
    /// </summary>
    public Guid? BuildingTypeId { get; set; }
}

/// <summary>
/// Update schedule slot request DTO (for rescheduling)
/// </summary>
public class UpdateScheduleSlotDto
{
    public Guid? ServiceInstallerId { get; set; }
    public DateTime? Date { get; set; }
    public TimeSpan? WindowFrom { get; set; }
    public TimeSpan? WindowTo { get; set; }
    public int? PlannedTravelMin { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// Request reschedule DTO (SI-initiated)
/// </summary>
public class RequestRescheduleDto
{
    public DateTime NewDate { get; set; }
    public TimeSpan NewWindowFrom { get; set; }
    public TimeSpan NewWindowTo { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// Reject reschedule DTO (Admin)
/// </summary>
public class RejectRescheduleDto
{
    public string RejectionReason { get; set; } = string.Empty;
}

/// <summary>
/// Schedule conflict DTO (for conflict detection)
/// </summary>
public class ScheduleConflictDto
{
    public Guid SlotId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan WindowFrom { get; set; }
    public TimeSpan WindowTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OrderServiceId { get; set; }
    public string? OrderCustomerName { get; set; }
    public string? OrderBuildingName { get; set; }
    public string ConflictType { get; set; } = string.Empty; // TimeOverlap, etc.
    public string ConflictDescription { get; set; } = string.Empty;
}

/// <summary>
/// Block order request DTO
/// </summary>
public class BlockOrderDto
{
    public string BlockerType { get; set; } = string.Empty; // Customer, Building, Network, SI, Weather, Other
    public string Description { get; set; } = string.Empty;
    public Guid? RaisedBySiId { get; set; }
}

