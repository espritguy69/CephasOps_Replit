using CephasOps.Application.Buildings.DTOs;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// Building service interface
/// </summary>
public interface IBuildingService
{
    // Building CRUD
    Task<List<BuildingListItemDto>> GetBuildingsAsync(
        Guid? companyId, 
        string? propertyType = null,
        Guid? installationMethodId = null,
        string? state = null,
        string? city = null,
        bool? isActive = null, 
        CancellationToken cancellationToken = default);
    
    Task<BuildingDetailDto?> GetBuildingByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingDto> CreateBuildingAsync(CreateBuildingDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingDto> UpdateBuildingAsync(Guid id, UpdateBuildingDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteBuildingAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    
    // Building Contacts
    Task<List<BuildingContactDto>> GetBuildingContactsAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingContactDto> CreateBuildingContactAsync(Guid buildingId, SaveBuildingContactDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingContactDto> UpdateBuildingContactAsync(Guid buildingId, Guid contactId, SaveBuildingContactDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteBuildingContactAsync(Guid buildingId, Guid contactId, Guid? companyId, CancellationToken cancellationToken = default);
    
    // Building Rules
    Task<BuildingRulesDto?> GetBuildingRulesAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingRulesDto> SaveBuildingRulesAsync(Guid buildingId, SaveBuildingRulesDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    
    // Dashboard Summary
    Task<BuildingsSummaryDto> GetBuildingsSummaryAsync(Guid? companyId, CancellationToken cancellationToken = default);
    
    // Building Lookup for Order Parsing
    Task<BuildingLookupResult> FindBuildingByAddressAsync(
        string? buildingName,
        string? addressLine1,
        string? city,
        string? state,
        string? postcode,
        Guid? companyId,
        CancellationToken cancellationToken = default);
    
    Task<List<BuildingListItemDto>> FindSimilarBuildingsAsync(
        string? buildingName,
        string? city,
        string? state,
        Guid? companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get similar buildings that could be merge targets (for admin merge tool).
    /// Excludes the building itself; includes order counts.
    /// </summary>
    Task<List<BuildingListItemDto>> GetMergeCandidatesAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview a merge: how many orders would move from source to target.
    /// </summary>
    Task<BuildingMergePreviewDto?> GetMergePreviewAsync(Guid sourceBuildingId, Guid targetBuildingId, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merge source building into target: reassign orders and parsed drafts to target, then soft-delete source.
    /// </summary>
    Task<BuildingMergeResultDto> MergeBuildingsAsync(Guid sourceBuildingId, Guid targetBuildingId, Guid userId, Guid? companyId, CancellationToken cancellationToken = default);
}
