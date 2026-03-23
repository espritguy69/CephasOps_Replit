using CephasOps.Domain.Common;

namespace CephasOps.Domain.Companies.Entities;

/// <summary>
/// Partner group entity - logical grouping of partners
/// </summary>
public class PartnerGroup : CompanyScopedEntity
{
    public string Name { get; set; } = string.Empty;
}

