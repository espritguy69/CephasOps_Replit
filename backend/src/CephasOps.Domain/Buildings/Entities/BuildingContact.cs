namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// BuildingContact entity - represents contacts for a building (BM, maintenance, security, etc.)
/// </summary>
public class BuildingContact
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
    /// Contact role (e.g., Building Manager, Maintenance, Security, Reception)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Contact name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Contact email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Additional remarks
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Whether this is the primary contact for the role
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Whether this contact is active
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

    // Navigation property
    public Building? Building { get; set; }
}

