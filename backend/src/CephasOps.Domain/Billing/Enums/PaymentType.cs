namespace CephasOps.Domain.Billing.Enums;

/// <summary>
/// Type of payment (income or expense)
/// </summary>
public enum PaymentType
{
    /// <summary>
    /// Payment received (income)
    /// </summary>
    Income = 1,

    /// <summary>
    /// Payment made (expense)
    /// </summary>
    Expense = 2
}

