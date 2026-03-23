using CephasOps.Domain.Common;

namespace CephasOps.Domain.Scheduler.Entities;

/// <summary>
/// SI leave request entity - tracks leave/unavailability
/// </summary>
public class SiLeaveRequest : CompanyScopedEntity
{
    public Guid ServiceInstallerId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public Guid? ApprovedByUserId { get; set; }
}

