using CephasOps.Domain.Common;
using CephasOps.Domain.Rates.Enums;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Modular pricing adjustment applied after resolving base payout (BaseWorkRate or GponSiJobRate).
/// Reduces the number of BaseWorkRate rows by applying dimension-based adjustments.
/// Application order: InstallationMethod → SITier → Partner (before SI Custom override).
/// </summary>
public class RateModifier : CompanyScopedEntity
{
    /// <summary>Dimension this modifier applies to.</summary>
    public RateModifierType ModifierType { get; set; }

    /// <summary>ID of the dimension value (e.g. InstallationMethodId, PartnerGroupId). Null when using ModifierValueString (e.g. SITier).</summary>
    public Guid? ModifierValueId { get; set; }

    /// <summary>String value for dimensions without a Guid (e.g. SiLevel "Junior", "Senior"). Used when ModifierType = SITier.</summary>
    public string? ModifierValueString { get; set; }

    /// <summary>How to apply the adjustment.</summary>
    public RateModifierAdjustmentType AdjustmentType { get; set; }

    /// <summary>For Add: amount to add (MYR). For Multiply: factor (e.g. 1.1 for 10% increase).</summary>
    public decimal AdjustmentValue { get; set; }

    /// <summary>When multiple modifiers match the same type, higher priority is applied first. Default 0.</summary>
    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}
