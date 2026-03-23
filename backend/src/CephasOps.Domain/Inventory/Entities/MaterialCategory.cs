using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Material category entity
/// </summary>
public class MaterialCategory : CompanyScopedEntity
{
    /// <summary>
    /// Category name (unique within company)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order for sorting in dropdowns
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether this category is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

