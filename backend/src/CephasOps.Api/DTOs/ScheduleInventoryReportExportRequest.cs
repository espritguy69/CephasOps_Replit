namespace CephasOps.Api.DTOs;

/// <summary>Request to schedule an inventory report export job (Phase 2.2.4).</summary>
public class ScheduleInventoryReportExportRequest
{
    public string? ReportType { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? GroupBy { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? LocationId { get; set; }
    public List<string>? SerialNumbers { get; set; }
    public string? EmailTo { get; set; }
    public Guid? EmailAccountId { get; set; }
}
