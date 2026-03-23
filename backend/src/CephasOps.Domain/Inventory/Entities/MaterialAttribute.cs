using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Material attribute entity (key-value pairs)
/// Allows storing flexible, custom attributes for materials
/// </summary>
public class MaterialAttribute : CompanyScopedEntity
{
    /// <summary>
    /// Material ID (FK)
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Attribute key/name
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Attribute value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Data type (String, Number, Boolean, Date, etc.)
    /// </summary>
    public string DataType { get; set; } = "String";

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    // Navigation properties
    public virtual Material? Material { get; set; }
}

