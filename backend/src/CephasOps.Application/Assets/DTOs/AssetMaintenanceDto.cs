using CephasOps.Domain.Assets.Enums;

namespace CephasOps.Application.Assets.DTOs;

/// <summary>
/// Asset Maintenance DTO
/// </summary>
public class AssetMaintenanceDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid AssetId { get; set; }
    public string? AssetName { get; set; }
    public string? AssetTag { get; set; }
    public MaintenanceType MaintenanceType { get; set; }
    public string MaintenanceTypeName => MaintenanceType.ToString();
    public string Description { get; set; } = string.Empty;
    public DateTime? ScheduledDate { get; set; }
    public DateTime? PerformedDate { get; set; }
    public DateTime? NextScheduledDate { get; set; }
    public decimal Cost { get; set; }
    public Guid? PnlTypeId { get; set; }
    public string? PerformedBy { get; set; }
    public Guid? SupplierInvoiceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create Asset Maintenance request DTO
/// </summary>
public class CreateAssetMaintenanceDto
{
    public Guid AssetId { get; set; }
    public MaintenanceType MaintenanceType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? ScheduledDate { get; set; }
    public DateTime? PerformedDate { get; set; }
    public DateTime? NextScheduledDate { get; set; }
    public decimal Cost { get; set; }
    public Guid? PnlTypeId { get; set; }
    public string? PerformedBy { get; set; }
    public Guid? SupplierInvoiceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleted { get; set; }
}

/// <summary>
/// Update Asset Maintenance request DTO
/// </summary>
public class UpdateAssetMaintenanceDto
{
    public MaintenanceType? MaintenanceType { get; set; }
    public string? Description { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? PerformedDate { get; set; }
    public DateTime? NextScheduledDate { get; set; }
    public decimal? Cost { get; set; }
    public Guid? PnlTypeId { get; set; }
    public string? PerformedBy { get; set; }
    public Guid? SupplierInvoiceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public bool? IsCompleted { get; set; }
}

