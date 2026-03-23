using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Material vertical entity (ISP, Barbershop, Travel, etc.)
/// Represents business verticals that materials can be associated with
/// </summary>
public class MaterialVertical : CompanyScopedEntity
{
    /// <summary>
    /// Vertical code (unique within company)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Vertical name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether this vertical is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
}

