namespace CephasOps.Domain.Assets.Enums;

/// <summary>
/// Method of asset disposal
/// </summary>
public enum DisposalMethod
{
    /// <summary>
    /// Asset was sold
    /// </summary>
    Sale = 1,

    /// <summary>
    /// Asset was scrapped/recycled
    /// </summary>
    Scrap = 2,

    /// <summary>
    /// Asset was donated
    /// </summary>
    Donation = 3,

    /// <summary>
    /// Asset was traded in for a new asset
    /// </summary>
    TradeIn = 4,

    /// <summary>
    /// Asset was transferred to another entity
    /// </summary>
    Transfer = 5,

    /// <summary>
    /// Asset was written off due to loss/theft
    /// </summary>
    WriteOff = 6,

    /// <summary>
    /// Asset was returned to vendor/lessor
    /// </summary>
    Return = 7
}

