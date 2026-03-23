using CephasOps.Domain.Common;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// Order material replacement entity - tracks serialised material swaps for Assurance orders
/// Per INVENTORY_AND_RMA_MODULE.md: Assurance jobs require RMA tracking with TIME approval
/// </summary>
public class OrderMaterialReplacement : CompanyScopedEntity
{
    /// <summary>
    /// Order ID this replacement belongs to
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Old (faulty) material ID
    /// </summary>
    public Guid OldMaterialId { get; set; }

    /// <summary>
    /// Old (faulty) material serial number
    /// </summary>
    public string OldSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Old serialised item ID (if tracked in inventory)
    /// </summary>
    public Guid? OldSerialisedItemId { get; set; }

    /// <summary>
    /// New (replacement) material ID
    /// </summary>
    public Guid NewMaterialId { get; set; }

    /// <summary>
    /// New (replacement) material serial number
    /// </summary>
    public string NewSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// New serialised item ID (if tracked in inventory)
    /// </summary>
    public Guid? NewSerialisedItemId { get; set; }

    /// <summary>
    /// TIME approval - who approved the replacement
    /// Required before Invoice/Docket Verified status
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// TIME approval notes/reference
    /// Required before Invoice/Docket Verified status
    /// </summary>
    public string? ApprovalNotes { get; set; }

    /// <summary>
    /// When the approval was recorded
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Reason for replacement (e.g., "Faulty ONU", "LOSi", "Customer request")
    /// </summary>
    public string? ReplacementReason { get; set; }

    /// <summary>
    /// SI who performed the replacement
    /// </summary>
    public Guid? ReplacedBySiId { get; set; }

    /// <summary>
    /// User who recorded the replacement (if admin)
    /// </summary>
    public Guid? RecordedByUserId { get; set; }

    /// <summary>
    /// When the replacement was recorded
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// RMA request ID if this replacement generated an RMA ticket
    /// </summary>
    public Guid? RmaRequestId { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Order? Order { get; set; }
}

