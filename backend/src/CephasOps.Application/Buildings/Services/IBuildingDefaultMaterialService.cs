using CephasOps.Application.Buildings.DTOs;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// Service interface for building default materials
/// </summary>
public interface IBuildingDefaultMaterialService
{
    /// <summary>
    /// Get all default materials for a building
    /// </summary>
    Task<List<BuildingDefaultMaterialDto>> GetBuildingDefaultMaterialsAsync(
        Guid buildingId,
        Guid? orderTypeId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific default material by ID
    /// </summary>
    Task<BuildingDefaultMaterialDto?> GetBuildingDefaultMaterialByIdAsync(
        Guid buildingId,
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new default material for a building
    /// </summary>
    Task<BuildingDefaultMaterialDto> CreateBuildingDefaultMaterialAsync(
        Guid buildingId,
        CreateBuildingDefaultMaterialDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing default material
    /// </summary>
    Task<BuildingDefaultMaterialDto> UpdateBuildingDefaultMaterialAsync(
        Guid buildingId,
        Guid id,
        UpdateBuildingDefaultMaterialDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a default material
    /// </summary>
    Task DeleteBuildingDefaultMaterialAsync(
        Guid buildingId,
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get summary of default materials for dashboard
    /// </summary>
    Task<BuildingDefaultMaterialsSummaryDto> GetDefaultMaterialsSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get default materials for order creation (by building + order type)
    /// </summary>
    Task<List<BuildingDefaultMaterialDto>> GetMaterialsForOrderAsync(
        Guid buildingId,
        Guid orderTypeId,
        CancellationToken cancellationToken = default);
}

