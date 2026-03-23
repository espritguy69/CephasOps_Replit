using CephasOps.Domain.Common;

namespace CephasOps.Domain.Companies.Entities;

/// <summary>
/// Cost centre entity - for overhead and P&amp;L segmentation
/// </summary>
public class CostCentre : CompanyScopedEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

