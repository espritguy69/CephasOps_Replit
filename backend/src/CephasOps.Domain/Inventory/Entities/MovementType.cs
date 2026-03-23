using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Movement type entity (settings-driven)
/// Defines configurable stock movement types (GRN, IssueToSI, ReturnFromSI, etc.)
/// </summary>
public class MovementType : CompanyScopedEntity
{
    /// <summary>
    /// Movement type code (unique within company)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Movement type name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Direction: In, Out, Transfer, Adjust
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Whether this movement type requires source location
    /// </summary>
    public bool RequiresFromLocation { get; set; }

    /// <summary>
    /// Whether this movement type requires destination location
    /// </summary>
    public bool RequiresToLocation { get; set; }

    /// <summary>
    /// Whether this movement type requires order ID
    /// </summary>
    public bool RequiresOrderId { get; set; }

    /// <summary>
    /// Whether this movement type requires service installer ID
    /// </summary>
    public bool RequiresServiceInstallerId { get; set; }

    /// <summary>
    /// Whether this movement type requires partner ID
    /// </summary>
    public bool RequiresPartnerId { get; set; }

    /// <summary>
    /// Whether this movement type affects stock balance (increases or decreases)
    /// </summary>
    public bool AffectsStockBalance { get; set; } = true;

    /// <summary>
    /// Stock impact: Positive (increases stock), Negative (decreases stock), Neutral (transfer)
    /// </summary>
    public string StockImpact { get; set; } = "Neutral"; // Positive, Negative, Neutral

    /// <summary>
    /// Whether this movement type is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; } = 0;
}

