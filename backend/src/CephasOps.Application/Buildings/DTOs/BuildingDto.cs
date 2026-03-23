namespace CephasOps.Application.Buildings.DTOs;

/// <summary>
/// Building DTO
/// </summary>
public class BuildingDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
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
    
    // Property type (MDU, SDU, Shoplot, Factory, Office, etc.)
    public string? PropertyType { get; set; }
    
    // Legacy BuildingType reference
    public Guid? BuildingTypeId { get; set; }
    public string? BuildingTypeName { get; set; }
    
    // New InstallationMethod reference
    public Guid? InstallationMethodId { get; set; }
    public string? InstallationMethodName { get; set; }
    public string? InstallationMethodCode { get; set; }
    
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    
    // RFB and order tracking
    public DateTime? RfbAssignedDate { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Computed/related counts
    public int ContactsCount { get; set; }
    public int OrdersCount { get; set; }
}

/// <summary>
/// A building suggested by fuzzy match with similarity score (0–1). Used for parser "pick building" modal.
/// </summary>
public class BuildingMatchCandidateDto
{
    public BuildingListItemDto Building { get; set; } = new();
    public double SimilarityScore { get; set; }
}

/// <summary>
/// Building list item DTO (lighter for list views)
/// </summary>
public class BuildingListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? PropertyType { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public string? InstallationMethodName { get; set; }
    public Guid? BuildingTypeId { get; set; }
    public string? BuildingTypeName { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? Area { get; set; }
    public DateTime? RfbAssignedDate { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public bool IsActive { get; set; }
    public int OrdersCount { get; set; }
}

/// <summary>
/// Create building request DTO
/// </summary>
public class CreateBuildingDto
{
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
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
    public string? PropertyType { get; set; }
    public Guid? BuildingTypeId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public DateTime? RfbAssignedDate { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Update building request DTO
/// </summary>
public class UpdateBuildingDto
{
    public Guid? DepartmentId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
    public string? Area { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PropertyType { get; set; }
    public Guid? BuildingTypeId { get; set; }
    public Guid? InstallationMethodId { get; set; }
    public DateTime? RfbAssignedDate { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Building contact DTO
/// </summary>
public class BuildingContactDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Remarks { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Create/update building contact DTO
/// </summary>
public class SaveBuildingContactDto
{
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Remarks { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Building rules DTO
/// </summary>
public class BuildingRulesDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string? AccessRules { get; set; }
    public string? InstallationRules { get; set; }
    public string? OtherNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Save building rules DTO
/// </summary>
public class SaveBuildingRulesDto
{
    public string? AccessRules { get; set; }
    public string? InstallationRules { get; set; }
    public string? OtherNotes { get; set; }
}

/// <summary>
/// Building detail DTO - includes all related data
/// </summary>
public class BuildingDetailDto : BuildingDto
{
    public List<BuildingContactDto> Contacts { get; set; } = new();
    public BuildingRulesDto? Rules { get; set; }
}

/// <summary>
/// Buildings summary DTO for dashboard
/// </summary>
public class BuildingsSummaryDto
{
    public int TotalBuildings { get; set; }
    public int ActiveBuildings { get; set; }
    public int TotalOrders { get; set; }
    public int OrdersThisMonth { get; set; }
    public int OrdersLastMonth { get; set; }
    public decimal OrdersGrowthPercent { get; set; }
    public List<PropertyTypeSummaryDto> ByPropertyType { get; set; } = new();
    public List<StateSummaryDto> ByState { get; set; } = new();
    public List<InstallationMethodSummaryDto> ByInstallationMethod { get; set; } = new();
    public List<RecentBuildingDto> RecentBuildings { get; set; } = new();
}

/// <summary>
/// Property type summary for dashboard
/// </summary>
public class PropertyTypeSummaryDto
{
    public string PropertyType { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// State summary for dashboard
/// </summary>
public class StateSummaryDto
{
    public string State { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Installation method summary for dashboard
/// </summary>
public class InstallationMethodSummaryDto
{
    public Guid? InstallationMethodId { get; set; }
    public string InstallationMethodName { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Recent building for dashboard
/// </summary>
public class RecentBuildingDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Building lookup result for order parsing
/// </summary>
public class BuildingLookupResult
{
    /// <summary>
    /// Whether a matching building was found
    /// </summary>
    public bool Found { get; set; }
    
    /// <summary>
    /// Matched building (if found)
    /// </summary>
    public BuildingListItemDto? Building { get; set; }
    
    /// <summary>
    /// Detected building name from address parsing
    /// </summary>
    public string? DetectedBuildingName { get; set; }
    
    /// <summary>
    /// Detected address components
    /// </summary>
    public string? DetectedAddress { get; set; }
    public string? DetectedCity { get; set; }
    public string? DetectedState { get; set; }
    public string? DetectedPostcode { get; set; }
    
    /// <summary>
    /// Similar buildings that might match (for user selection)
    /// </summary>
    public List<BuildingListItemDto> SimilarBuildings { get; set; } = new();
}

/// <summary>
/// Preview of merging source building into target: orders (and optionally drafts) to be reassigned.
/// </summary>
public class BuildingMergePreviewDto
{
    public Guid SourceBuildingId { get; set; }
    public Guid TargetBuildingId { get; set; }
    public string SourceBuildingName { get; set; } = string.Empty;
    public string TargetBuildingName { get; set; } = string.Empty;
    public int OrdersToReassignCount { get; set; }
    public int ParsedDraftsToReassignCount { get; set; }
    public List<Guid> OrderIdsToReassign { get; set; } = new();
}

/// <summary>
/// Request to merge source building into target (orders reassigned, source deactivated).
/// </summary>
public class MergeBuildingsRequestDto
{
    public Guid SourceBuildingId { get; set; }
    public Guid TargetBuildingId { get; set; }
}

/// <summary>
/// Result of a building merge (orders moved, source deactivated).
/// </summary>
public class BuildingMergeResultDto
{
    public int OrdersMovedCount { get; set; }
    public int ParsedDraftsReassignedCount { get; set; }
    public bool SourceSoftDeleted { get; set; }
    public string Message { get; set; } = string.Empty;
}