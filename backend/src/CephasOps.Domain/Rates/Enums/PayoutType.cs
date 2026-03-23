namespace CephasOps.Domain.Rates.Enums;

/// <summary>
/// Defines how the payout is calculated
/// </summary>
public enum PayoutType
{
    /// <summary>
    /// Fixed amount in RM
    /// </summary>
    FixedAmount = 1,

    /// <summary>
    /// Percentage of revenue/sale
    /// </summary>
    Percentage = 2
}

