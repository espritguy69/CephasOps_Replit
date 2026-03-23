using CephasOps.Domain.Common;
using CephasOps.Domain.Pnl.Enums;

namespace CephasOps.Domain.Pnl.Entities;

/// <summary>
/// P&amp;L Type entity for hierarchical expense/income categorization
/// </summary>
public class PnlType : CompanyScopedEntity
{
    /// <summary>
    /// Name of the P&amp;L type (e.g., "Vehicle Expenses", "Fuel")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the P&amp;L type (e.g., "EXP-VEH-FUEL")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Description of this P&amp;L type
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category: Income or Expense
    /// </summary>
    public PnlTypeCategory Category { get; set; }

    /// <summary>
    /// Parent P&amp;L type ID for hierarchical structure (null for root level)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Sort order within the same level
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this P&amp;L type is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this type can have transactions (leaf node) or is just a grouping category
    /// </summary>
    public bool IsTransactional { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Parent P&amp;L type
    /// </summary>
    public PnlType? Parent { get; set; }

    /// <summary>
    /// Child P&amp;L types
    /// </summary>
    public ICollection<PnlType> Children { get; set; } = new List<PnlType>();
}

