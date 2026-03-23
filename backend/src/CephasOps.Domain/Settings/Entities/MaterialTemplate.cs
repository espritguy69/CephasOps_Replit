using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Material template entity - defines standard material kits
/// Templates are determined by: Department + Installation Method + Order Type + Partner
/// </summary>
public class MaterialTemplate : CompanyScopedEntity
{
    /// <summary>
    /// Department ID - templates can be department-specific
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Template name (e.g. "TIME Prelaid High-Rise kit")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Order type / Job type (Activation, Assurance, etc.)
    /// </summary>
    public string OrderType { get; set; } = string.Empty;

    /// <summary>
    /// Installation Method ID - Prelaid, Non-Prelaid, SDU, RDF Pole
    /// Links to InstallationMethod entity (Site Condition)
    /// </summary>
    public Guid? InstallationMethodId { get; set; }

    /// <summary>
    /// Building type ID (DEPRECATED - use InstallationMethodId instead)
    /// This field is kept for backward compatibility only and will be removed in a future version.
    /// Building types (Prelaid, Non-Prelaid, SDU, RDF_POLE) are now represented as Installation Methods.
    /// </summary>
    [Obsolete("Use InstallationMethodId instead. BuildingType entity is deprecated.")]
    public Guid? BuildingTypeId { get; set; }

    /// <summary>
    /// Partner ID (nullable - if template is partner-specific)
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Whether this is the default template for (CompanyId, OrderType, BuildingTypeId)
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this template is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User ID who created this template
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this template
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>
    /// Template items
    /// </summary>
    public List<MaterialTemplateItem> Items { get; set; } = new();
}

/// <summary>
/// Material template item - defines a material and quantity in a template
/// </summary>
public class MaterialTemplateItem
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Material template ID
    /// </summary>
    public Guid MaterialTemplateId { get; set; }

    /// <summary>
    /// Material ID
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Default quantity
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure
    /// </summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Whether this material is serialised (mirror of Material.IsSerialised)
    /// </summary>
    public bool IsSerialised { get; set; }

    /// <summary>
    /// Optional notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Timestamp when created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

