using CephasOps.Domain.Common;

namespace CephasOps.Domain.Companies.Entities;

/// <summary>
/// Vertical entity - represents business verticals (ISP, Retail, Travel, Barbershop, Mixed, etc.)
/// </summary>
public class Vertical : CompanyScopedEntity
{
    public string Name { get; set; } = string.Empty; // ISP, Retail, Travel, Barbershop, Mixed
    public string Code { get; set; } = string.Empty; // ISP, RETAIL, TRAVEL, BARBERSHOP, MIXED
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

