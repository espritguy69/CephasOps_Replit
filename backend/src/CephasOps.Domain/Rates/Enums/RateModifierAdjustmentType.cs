namespace CephasOps.Domain.Rates.Enums;

/// <summary>
/// How a rate modifier adjusts the base amount.
/// </summary>
public enum RateModifierAdjustmentType
{
    /// <summary>Add AdjustmentValue to the amount (e.g. +10 MYR).</summary>
    Add = 0,

    /// <summary>Multiply amount by AdjustmentValue (e.g. 1.1 for 10% increase).</summary>
    Multiply = 1
}
