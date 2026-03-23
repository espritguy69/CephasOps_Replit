namespace CephasOps.Domain.Rates.Enums;

/// <summary>
/// Defines the context/domain for rate cards
/// </summary>
public enum RateContext
{
    /// <summary>
    /// GPON job rates (Activation, Assurance, Modification)
    /// </summary>
    GponJob = 1,

    /// <summary>
    /// NWO (Network Work Orders) scope rates
    /// </summary>
    NwoScope = 2,

    /// <summary>
    /// CWO (Customer Work Orders) enterprise rates
    /// </summary>
    CwoEnterprise = 3,

    /// <summary>
    /// Barbershop service rates
    /// </summary>
    BarberService = 4,

    /// <summary>
    /// Travel package rates
    /// </summary>
    TravelPackage = 5,

    /// <summary>
    /// Spa treatment rates
    /// </summary>
    SpaTreatment = 6
}

