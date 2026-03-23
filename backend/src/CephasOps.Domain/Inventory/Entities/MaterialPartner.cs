using CephasOps.Domain.Common;

namespace CephasOps.Domain.Inventory.Entities;

/// <summary>
/// Join entity for many-to-many relationship between Materials and Partners
/// </summary>
public class MaterialPartner : BaseEntity
{
    /// <summary>
    /// Material ID
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Partner ID
    /// </summary>
    public Guid PartnerId { get; set; }

    /// <summary>
    /// Company ID for scoping (inherited from BaseEntity)
    /// </summary>
    public Guid CompanyId { get; set; }

    // Navigation properties
    public Material? Material { get; set; }
    public Companies.Entities.Partner? Partner { get; set; }
}

