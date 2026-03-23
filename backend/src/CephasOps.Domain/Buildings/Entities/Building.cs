using CephasOps.Domain.Common;

namespace CephasOps.Domain.Buildings.Entities;

/// <summary>
/// Building entity - represents buildings where installations occur
/// </summary>
public class Building : CompanyScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string? Area { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    
    /// <summary>
    /// FK to BuildingType entity - represents the building classification
    /// (e.g., Condominium, Office Tower, Terrace House)
    /// This represents WHAT the building is (building type).
    /// </summary>
    public Guid? BuildingTypeId { get; set; }
    
    /// <summary>
    /// FK to InstallationMethod entity - represents the site condition/installation method
    /// (e.g., Prelaid, Non-Prelaid). This determines HOW installations are performed.
    /// </summary>
    public Guid? InstallationMethodId { get; set; }
    
    /// <summary>
    /// Property/Building type (DEPRECATED - use BuildingTypeId instead)
    /// Kept for backward compatibility during migration
    /// </summary>
    [Obsolete("Use BuildingTypeId instead. This field will be removed in a future version.")]
    public string? PropertyType { get; set; }
    
    /// <summary>
    /// Optional: department assignment
    /// </summary>
    public Guid? DepartmentId { get; set; }
    
    /// <summary>
    /// Date when RFB was assigned to us for this building
    /// </summary>
    public DateTime? RfbAssignedDate { get; set; }
    
    /// <summary>
    /// Date of first order in this building
    /// </summary>
    public DateTime? FirstOrderDate { get; set; }
    
    /// <summary>
    /// Additional notes about the building
    /// </summary>
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; } = true;
    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public new DateTime? UpdatedAt { get; set; }
}

