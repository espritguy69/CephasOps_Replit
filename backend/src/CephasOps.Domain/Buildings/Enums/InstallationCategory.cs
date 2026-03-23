namespace CephasOps.Domain.Buildings.Enums;

/// <summary>
/// Installation category enum - represents the type of fibre installation
/// </summary>
public enum InstallationCategory
{
    /// <summary>
    /// Fibre To The Home
    /// </summary>
    FTTH,

    /// <summary>
    /// Fibre To The Office
    /// </summary>
    FTTO,

    /// <summary>
    /// Fibre To The Room
    /// </summary>
    FTTR,

    /// <summary>
    /// Fibre To The Curb
    /// </summary>
    FTTC,

    /// <summary>
    /// Fibre To The Building
    /// </summary>
    FTTB,

    /// <summary>
    /// Fibre To The Premises
    /// </summary>
    FTTP
}

/// <summary>
/// Property type enum - represents the type of property/building
/// </summary>
public enum PropertyType
{
    /// <summary>
    /// Condo/Apartment (Multi-Dwelling Unit)
    /// </summary>
    MDU,

    /// <summary>
    /// Landed/Single Dwelling Unit
    /// </summary>
    SDU,

    /// <summary>
    /// Shoplot
    /// </summary>
    Shoplot,

    /// <summary>
    /// Factory
    /// </summary>
    Factory,

    /// <summary>
    /// Office building
    /// </summary>
    Office,

    /// <summary>
    /// Other property type
    /// </summary>
    Other
}

