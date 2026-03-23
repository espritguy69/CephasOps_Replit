namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// BuildingSplitter entity - represents splitters installed in a building
/// Tracks port usage and status for capacity management
/// </summary>
public class BuildingSplitter
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// FK to Building
    /// </summary>
    public Guid BuildingId { get; set; }

    /// <summary>
    /// FK to BuildingBlock (optional, for MDU buildings)
    /// </summary>
    public Guid? BlockId { get; set; }

    /// <summary>
    /// FK to SplitterType
    /// </summary>
    public Guid SplitterTypeId { get; set; }

    /// <summary>
    /// Splitter name/identifier (e.g., "SPL-A-01", "Splitter 1")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Floor where splitter is located
    /// </summary>
    public int? Floor { get; set; }

    /// <summary>
    /// Location description (e.g., "Riser Room", "Utility Closet Level 5")
    /// </summary>
    public string? LocationDescription { get; set; }

    /// <summary>
    /// Total number of ports on this splitter
    /// </summary>
    public int PortsTotal { get; set; }

    /// <summary>
    /// Number of ports currently in use
    /// </summary>
    public int PortsUsed { get; set; }

    /// <summary>
    /// Splitter status
    /// </summary>
    public SplitterStatus Status { get; set; } = SplitterStatus.Active;

    /// <summary>
    /// Serial number if tracked
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Installation date
    /// </summary>
    public DateTime? InstalledAt { get; set; }

    /// <summary>
    /// Last maintenance date
    /// </summary>
    public DateTime? LastMaintenanceAt { get; set; }

    /// <summary>
    /// Additional remarks
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Whether this splitter requires attention (computed or manually flagged)
    /// </summary>
    public bool NeedsAttention { get; set; }

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
    public BuildingBlock? Block { get; set; }

    // Computed properties
    public int PortsAvailable => PortsTotal - PortsUsed;
    public bool IsFull => PortsUsed >= PortsTotal;
    public decimal UtilizationPercent => PortsTotal > 0 ? (decimal)PortsUsed / PortsTotal * 100 : 0;
}

/// <summary>
/// Splitter status enumeration
/// </summary>
public enum SplitterStatus
{
    Active,
    Full,
    Faulty,
    MaintenanceRequired,
    Decommissioned
}

