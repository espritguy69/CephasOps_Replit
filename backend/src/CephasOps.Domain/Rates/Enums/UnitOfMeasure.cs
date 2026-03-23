namespace CephasOps.Domain.Rates.Enums;

/// <summary>
/// Defines the unit of measure for rate calculations
/// </summary>
public enum UnitOfMeasure
{
    /// <summary>
    /// Per job/order (most common for GPON)
    /// </summary>
    Job = 1,

    /// <summary>
    /// Per meter (for NWO fibre pull)
    /// </summary>
    Meter = 2,

    /// <summary>
    /// Per unit (for chambers, manholes, joints)
    /// </summary>
    Unit = 3,

    /// <summary>
    /// Per service (for barbershop)
    /// </summary>
    Service = 4,

    /// <summary>
    /// Per passenger (for travel)
    /// </summary>
    Pax = 5,

    /// <summary>
    /// Per session (for spa)
    /// </summary>
    Session = 6,

    /// <summary>
    /// Per device
    /// </summary>
    Device = 7,

    /// <summary>
    /// Per hour
    /// </summary>
    Hour = 8
}

