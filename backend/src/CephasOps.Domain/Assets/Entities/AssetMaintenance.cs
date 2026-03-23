using CephasOps.Domain.Common;
using CephasOps.Domain.Assets.Enums;

namespace CephasOps.Domain.Assets.Entities;

/// <summary>
/// Asset maintenance record entity
/// </summary>
public class AssetMaintenance : CompanyScopedEntity
{
    /// <summary>
    /// Asset ID
    /// </summary>
    public Guid AssetId { get; set; }

    /// <summary>
    /// Type of maintenance performed
    /// </summary>
    public MaintenanceType MaintenanceType { get; set; }

    /// <summary>
    /// Description of work performed
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled date for maintenance
    /// </summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>
    /// Actual date maintenance was performed
    /// </summary>
    public DateTime? PerformedDate { get; set; }

    /// <summary>
    /// Next scheduled maintenance date (for recurring maintenance)
    /// </summary>
    public DateTime? NextScheduledDate { get; set; }

    /// <summary>
    /// Cost of maintenance
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// P&amp;L Type ID for this expense
    /// </summary>
    public Guid? PnlTypeId { get; set; }

    /// <summary>
    /// Supplier/vendor who performed the maintenance
    /// </summary>
    public string? PerformedBy { get; set; }

    /// <summary>
    /// Supplier invoice ID if linked to a supplier invoice
    /// </summary>
    public Guid? SupplierInvoiceId { get; set; }

    /// <summary>
    /// Reference number (work order, invoice, etc.)
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Notes about the maintenance
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether maintenance was completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// User who recorded this maintenance
    /// </summary>
    public Guid? RecordedByUserId { get; set; }

    // Navigation properties

    /// <summary>
    /// Asset this maintenance is for
    /// </summary>
    public Asset? Asset { get; set; }
}

