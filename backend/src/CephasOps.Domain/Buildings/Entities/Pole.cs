namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// Pole entity - represents poles for aerial fibre installations
/// Used in SDU/RDF pole installations
/// </summary>
public class Pole
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
    /// FK to Street (which street this pole is on)
    /// </summary>
    public Guid? StreetId { get; set; }

    /// <summary>
    /// Pole number/identifier (e.g., "P-001", "Pole 15")
    /// </summary>
    public string PoleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Pole type (e.g., "TNB", "Telekom", "Private", "JKR")
    /// </summary>
    public string? PoleType { get; set; }

    /// <summary>
    /// Location description
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
    /// Pole height in meters
    /// </summary>
    public decimal? HeightMeters { get; set; }

    /// <summary>
    /// Whether pole has existing fibre
    /// </summary>
    public bool HasExistingFibre { get; set; }

    /// <summary>
    /// Number of fibre drops from this pole
    /// </summary>
    public int DropsCount { get; set; }

    /// <summary>
    /// Pole condition status
    /// </summary>
    public PoleStatus Status { get; set; } = PoleStatus.Good;

    /// <summary>
    /// Additional remarks
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Whether this pole record is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Survey/inspection date
    /// </summary>
    public DateTime? LastInspectedAt { get; set; }

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
}

/// <summary>
/// Pole status enumeration
/// </summary>
public enum PoleStatus
{
    Good,
    NeedsInspection,
    Damaged,
    Unusable,
    PendingApproval
}

