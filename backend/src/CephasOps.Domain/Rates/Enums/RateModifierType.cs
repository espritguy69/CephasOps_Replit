namespace CephasOps.Domain.Rates.Enums;

/// <summary>
/// Dimension that a rate modifier applies to (GPON pricing engine).
/// Application order: InstallationMethod → SITier → Partner.
/// </summary>
public enum RateModifierType
{
    InstallationMethod = 0,
    SITier = 1,
    Partner = 2
}
