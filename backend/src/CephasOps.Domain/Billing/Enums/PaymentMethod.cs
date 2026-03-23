namespace CephasOps.Domain.Billing.Enums;

/// <summary>
/// Payment method
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Cash payment
    /// </summary>
    Cash = 1,

    /// <summary>
    /// Bank transfer / wire transfer
    /// </summary>
    BankTransfer = 2,

    /// <summary>
    /// Cheque payment
    /// </summary>
    Cheque = 3,

    /// <summary>
    /// Credit card payment
    /// </summary>
    CreditCard = 4,

    /// <summary>
    /// Debit card payment
    /// </summary>
    DebitCard = 5,

    /// <summary>
    /// Online banking (FPX, etc.)
    /// </summary>
    OnlineBanking = 6,

    /// <summary>
    /// E-wallet (Touch n Go, GrabPay, etc.)
    /// </summary>
    EWallet = 7,

    /// <summary>
    /// Direct debit
    /// </summary>
    DirectDebit = 8,

    /// <summary>
    /// Other payment method
    /// </summary>
    Other = 99
}

