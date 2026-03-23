using CephasOps.Domain.Common;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Rate group entity for GPON layered pricing (Phase 1).
/// Groups order types for shared base work rate definitions.
/// Does not affect payout resolution until later phases.
/// </summary>
public class RateGroup : CompanyScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
