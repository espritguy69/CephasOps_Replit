namespace CephasOps.Domain.Assets.Enums;

/// <summary>
/// Type of maintenance performed on an asset
/// </summary>
public enum MaintenanceType
{
    /// <summary>
    /// Scheduled preventive maintenance
    /// </summary>
    Preventive = 1,

    /// <summary>
    /// Corrective/repair maintenance
    /// </summary>
    Corrective = 2,

    /// <summary>
    /// Routine inspection
    /// </summary>
    Inspection = 3,

    /// <summary>
    /// Major overhaul or refurbishment
    /// </summary>
    Overhaul = 4,

    /// <summary>
    /// Emergency repair
    /// </summary>
    Emergency = 5,

    /// <summary>
    /// Calibration (for equipment)
    /// </summary>
    Calibration = 6,

    /// <summary>
    /// Software/firmware update
    /// </summary>
    SoftwareUpdate = 7
}

