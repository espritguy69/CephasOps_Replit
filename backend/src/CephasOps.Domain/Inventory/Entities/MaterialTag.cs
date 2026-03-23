using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Material tag entity
/// Represents tags that can be assigned to materials for flexible categorization
/// </summary>
public class MaterialTag : CompanyScopedEntity
{
    /// <summary>
    /// Tag name (unique within company)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tag description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tag color (hex code) for UI display
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether this tag is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
}

