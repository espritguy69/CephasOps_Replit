using CephasOps.Domain.Orders.Entities;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Maps an order type (and optional subtype) to a rate group for GPON layered pricing.
/// Activation can map with OrderSubtypeId = null. Subtypes override parent mapping when both exist.
/// Does not affect payout resolution until later phases.
/// </summary>
public class OrderTypeSubtypeRateGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderTypeId { get; set; }
    /// <summary>When null, the whole order type (e.g. Activation) maps to the rate group.</summary>
    public Guid? OrderSubtypeId { get; set; }
    public Guid RateGroupId { get; set; }
    public Guid? CompanyId { get; set; }

    public OrderType OrderType { get; set; } = null!;
    public OrderType? OrderSubtype { get; set; }
    public RateGroup RateGroup { get; set; } = null!;
}
