namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// HubBox entity - represents hub boxes for landed/SDU infrastructure
/// Hub boxes contain multiple lines/connections serving nearby houses
/// </summary>
public class HubBox
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FK to Building (parent building/area)
    /// </summary>
    public Guid BuildingId { get; set; }

    /// <summary>
    /// FK to Street (optional - which street this hub box serves)
    /// </summary>
    public Guid? StreetId { get; set; }

    /// <summary>
    /// Hub box name/identifier (e.g., "HUB-01", "Box A")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hub box code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Location description (e.g., "Corner of Jalan 1 and Jalan 2")
    /// </summary>
    public string? LocationDescription { get; set; }

    /// <summary>
    /// GPS Latitude
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// GPS Longitude
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Total capacity (number of lines/ports)
    /// </summary>
    public int PortsTotal { get; set; }

    /// <summary>
    /// Number of ports currently in use
    /// </summary>
    public int PortsUsed { get; set; }

    /// <summary>
    /// Hub box status
    /// </summary>
    public HubBoxStatus Status { get; set; } = HubBoxStatus.Active;

    /// <summary>
    /// Installation date
    /// </summary>
    public DateTime? InstalledAt { get; set; }

    /// <summary>
    /// Additional remarks
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Whether this hub box is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Building? Building { get; set; }
    public Street? Street { get; set; }

    // Computed properties
    public int PortsAvailable => PortsTotal - PortsUsed;
    public bool IsFull => PortsUsed >= PortsTotal;
    public decimal UtilizationPercent => PortsTotal > 0 ? (decimal)PortsUsed / PortsTotal * 100 : 0;
}

/// <summary>
/// Hub box status enumeration
/// </summary>
public enum HubBoxStatus
{
    Active,
    Full,
    Faulty,
    MaintenanceRequired,
    Decommissioned
}

