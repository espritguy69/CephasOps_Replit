namespace CephasOps.Domain.Rates.Enums;

/// <summary>
/// Defines the type/purpose of a rate card
/// </summary>
public enum RateKind
{
    /// <summary>
    /// Revenue rate - what Cephas earns from partner/customer
    /// </summary>
    Revenue = 1,

    /// <summary>
    /// Payout rate - what Cephas pays to SI/staff
    /// </summary>
    Payout = 2,

    /// <summary>
    /// Bonus rate - additional incentive payments
    /// </summary>
    Bonus = 3,

    /// <summary>
    /// Commission rate - percentage-based payouts
    /// </summary>
    Commission = 4
}

