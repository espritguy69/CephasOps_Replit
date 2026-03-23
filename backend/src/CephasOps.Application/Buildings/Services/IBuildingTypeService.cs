using CephasOps.Application.Buildings.DTOs;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// BuildingType service interface
/// </summary>
public interface IBuildingTypeService
{
    Task<List<BuildingTypeDto>> GetBuildingTypesAsync(Guid? companyId, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<BuildingTypeDto?> GetBuildingTypeByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingTypeDto> CreateBuildingTypeAsync(CreateBuildingTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingTypeDto> UpdateBuildingTypeAsync(Guid id, UpdateBuildingTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteBuildingTypeAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}

