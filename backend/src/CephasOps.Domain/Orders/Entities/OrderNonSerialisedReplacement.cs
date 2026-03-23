using CephasOps.Domain.Common;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// Order non-serialised replacement entity - tracks non-serial material replacements for Assurance orders
/// Per INVENTORY_AND_RMA_MODULE.md: Non-serialised items (patch cord, connector, trunking, clips) 
/// need logging but NO TIME approval required
/// </summary>
public class OrderNonSerialisedReplacement : CompanyScopedEntity
{
    /// <summary>
    /// Order ID this replacement belongs to
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Material ID (e.g., patch cord, connector, trunking, clips)
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Quantity replaced
    /// </summary>
    public decimal QuantityReplaced { get; set; }

    /// <summary>
    /// Unit of measure (e.g., "pcs", "m", "unit")
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Reason for replacement (optional)
    /// </summary>
    public string? ReplacementReason { get; set; }

    /// <summary>
    /// Additional remarks
    /// </summary>
    public string? Remark { get; set; }

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

    // Navigation properties
    public virtual Order? Order { get; set; }
}

