using CephasOps.Domain.Common;
using CephasOps.Domain.Assets.Enums;

namespace CephasOps.Domain.Assets.Entities;

/// <summary>
/// Asset disposal record entity
/// </summary>
public class AssetDisposal : CompanyScopedEntity
{
    /// <summary>
    /// Asset ID
    /// </summary>
    public Guid AssetId { get; set; }

    /// <summary>
    /// Method of disposal
    /// </summary>
    public DisposalMethod DisposalMethod { get; set; }

    /// <summary>
    /// Date of disposal
    /// </summary>
    public DateTime DisposalDate { get; set; }

    /// <summary>
    /// Book value at time of disposal
    /// </summary>
    public decimal BookValueAtDisposal { get; set; }

    /// <summary>
    /// Proceeds from disposal (sale price, trade-in value, etc.)
    /// </summary>
    public decimal DisposalProceeds { get; set; }

    /// <summary>
    /// Gain or loss on disposal (Proceeds - BookValue)
    /// </summary>
    public decimal GainLoss { get; set; }

    /// <summary>
    /// P&amp;L Type ID for the gain/loss
    /// </summary>
    public Guid? PnlTypeId { get; set; }

    /// <summary>
    /// Buyer/recipient name (if sold, donated, or transferred)
    /// </summary>
    public string? BuyerName { get; set; }

    /// <summary>
    /// Reference number (sale agreement, donation receipt, etc.)
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Reason for disposal
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Notes about the disposal
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User who processed the disposal
    /// </summary>
    public Guid? ProcessedByUserId { get; set; }

    /// <summary>
    /// Whether disposal has been approved
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// User who approved the disposal
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// Date of approval
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    // Navigation properties

    /// <summary>
    /// Asset being disposed
    /// </summary>
    public Asset? Asset { get; set; }
}

