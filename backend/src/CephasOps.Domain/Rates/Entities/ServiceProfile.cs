using CephasOps.Domain.Common;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Service profile for GPON pricing — groups order categories into service families
/// (e.g. RESIDENTIAL_FIBER, BUSINESS_FIBER, MAINTENANCE) so BaseWorkRate can target
/// a profile instead of duplicating rates per category. Used only after mapping is configured.
/// </summary>
public class ServiceProfile : CompanyScopedEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
