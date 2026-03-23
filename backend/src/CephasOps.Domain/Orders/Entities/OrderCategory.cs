using CephasOps.Domain.Common;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// OrderCategory entity - represents service/product categories (e.g. FTTH, FTTO, FTTR, FTTC).
/// These are the technology/service categories that define what type of service is being provided.
/// Previously known as "InstallationType" but renamed for clarity.
/// Note: SDU and RDF_POLE are Installation Methods (site condition), not Order Categories.
/// </summary>
public class OrderCategory : CompanyScopedEntity
{
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. FTTH, FTTO, FTTR, FTTC
    public string Code { get; set; } = string.Empty; // e.g. FTTH, FTTO, FTTR, FTTC
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

