namespace CephasOps.Domain.Assets.Enums;

/// <summary>
/// Depreciation calculation methods for assets
/// </summary>
public enum DepreciationMethod
{
    /// <summary>
    /// Straight-line depreciation (equal amounts each period)
    /// </summary>
    StraightLine = 1,

    /// <summary>
    /// Declining balance method (percentage of remaining value)
    /// </summary>
    DecliningBalance = 2,

    /// <summary>
    /// Double declining balance method
    /// </summary>
    DoubleDecliningBalance = 3,

    /// <summary>
    /// Sum of years digits method
    /// </summary>
    SumOfYearsDigits = 4,

    /// <summary>
    /// Units of production method
    /// </summary>
    UnitsOfProduction = 5,

    /// <summary>
    /// No depreciation (e.g., land)
    /// </summary>
    None = 0
}

