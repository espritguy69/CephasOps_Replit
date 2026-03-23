using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Material master entity
/// </summary>
public class Material : CompanyScopedEntity
{
    /// <summary>
    /// Item code (unique within company)
    /// </summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>
    /// Material description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category ID (FK to MaterialCategory)
    /// </summary>
    public Guid? MaterialCategoryId { get; set; }

    /// <summary>
    /// Category (legacy string field - kept for backward compatibility)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Vertical flags (legacy comma-separated: ISP, Barbershop, Travel)
    /// DEPRECATED: Use MaterialVerticals navigation property instead
    /// </summary>
    public string? VerticalFlags { get; set; }

    // Navigation properties
    public virtual MaterialCategory? MaterialCategory { get; set; }
    public virtual ICollection<MaterialVertical> MaterialVerticals { get; set; } = new List<MaterialVertical>();
    public virtual ICollection<MaterialTag> MaterialTags { get; set; } = new List<MaterialTag>();
    public virtual ICollection<MaterialAttribute> MaterialAttributes { get; set; } = new List<MaterialAttribute>();

    /// <summary>
    /// Whether this material requires serial number tracking
    /// </summary>
    public bool IsSerialised { get; set; }

    /// <summary>
    /// Unit of measure (pcs, m, kg, etc.)
    /// </summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Default cost for P&amp;L calculations
    /// </summary>
    public decimal? DefaultCost { get; set; }

    /// <summary>
    /// Linked partner ID (if applicable) - DEPRECATED: Use MaterialPartners navigation property instead
    /// Kept for backward compatibility during migration
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Department ID (required - materials must be assigned to a department)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Navigation property for many-to-many relationship with Partners
    /// </summary>
    public ICollection<MaterialPartner> MaterialPartners { get; set; } = new List<MaterialPartner>();

    /// <summary>
    /// Whether this material is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Barcode for quick scanning and lookup (optional)
    /// </summary>
    public string? Barcode { get; set; }
}

