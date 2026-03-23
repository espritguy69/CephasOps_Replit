using CephasOps.Domain.Common;
using CephasOps.Domain.Rates.Enums;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// CustomRate entity - per-staff/per-subcon rate overrides.
/// Takes highest priority in rate resolution.
/// Per RATE_ENGINE.md specification.
/// </summary>
public class CustomRate : CompanyScopedEntity
{
    /// <summary>
    /// User ID (employee / subcon / barber / agent)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Department ID
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Vertical ID
    /// </summary>
    public Guid? VerticalId { get; set; }

    /// <summary>
    /// First dimension key (e.g., OrderType for GPON)
    /// </summary>
    public string? Dimension1 { get; set; }

    /// <summary>
    /// Second dimension key (e.g., InstallationType for GPON)
    /// </summary>
    public string? Dimension2 { get; set; }

    /// <summary>
    /// Third dimension key (e.g., InstallationMethod for GPON)
    /// </summary>
    public string? Dimension3 { get; set; }

    /// <summary>
    /// Fourth dimension key (e.g., PartnerGroup for GPON)
    /// </summary>
    public string? Dimension4 { get; set; }

    /// <summary>
    /// Custom rate amount (overrides default payout rate)
    /// </summary>
    public decimal CustomRateAmount { get; set; }

    /// <summary>
    /// Unit of measure
    /// </summary>
    public UnitOfMeasure UnitOfMeasure { get; set; } = UnitOfMeasure.Job;

    /// <summary>
    /// Currency (default MYR)
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Effective to date (null = no end date)
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Whether this custom rate is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Reason for custom rate (for audit purposes)
    /// </summary>
    public string? Reason { get; set; }
}

