using CephasOps.Application.Buildings.DTOs;
using CephasOps.Domain.Buildings.Entities;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// Building infrastructure service interface
/// Manages blocks, splitters, streets, hub boxes, and poles
/// </summary>
public interface IInfrastructureService
{
    // Infrastructure Overview
    Task<BuildingInfrastructureDto> GetBuildingInfrastructureAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default);

    // Building Blocks (MDU)
    Task<List<BuildingBlockDto>> GetBuildingBlocksAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingBlockDto?> GetBuildingBlockByIdAsync(Guid buildingId, Guid blockId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingBlockDto> CreateBuildingBlockAsync(Guid buildingId, SaveBuildingBlockDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingBlockDto> UpdateBuildingBlockAsync(Guid buildingId, Guid blockId, SaveBuildingBlockDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteBuildingBlockAsync(Guid buildingId, Guid blockId, Guid? companyId, CancellationToken cancellationToken = default);

    // Building Splitters
    Task<List<BuildingSplitterDto>> GetBuildingSplittersAsync(Guid buildingId, Guid? companyId, SplitterFilterDto? filter = null, CancellationToken cancellationToken = default);
    Task<List<BuildingSplitterDto>> GetAllSplittersAsync(Guid? companyId, SplitterFilterDto? filter = null, CancellationToken cancellationToken = default);
    Task<BuildingSplitterDto?> GetBuildingSplitterByIdAsync(Guid buildingId, Guid splitterId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingSplitterDto> CreateBuildingSplitterAsync(Guid buildingId, SaveBuildingSplitterDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingSplitterDto> UpdateBuildingSplitterAsync(Guid buildingId, Guid splitterId, SaveBuildingSplitterDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteBuildingSplitterAsync(Guid buildingId, Guid splitterId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<BuildingSplitterDto> UpdateSplitterPortUsageAsync(Guid buildingId, Guid splitterId, int portsUsed, Guid? companyId, CancellationToken cancellationToken = default);

    // Streets (Landed/SDU)
    Task<List<StreetDto>> GetStreetsAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<StreetDto?> GetStreetByIdAsync(Guid buildingId, Guid streetId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<StreetDto> CreateStreetAsync(Guid buildingId, SaveStreetDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<StreetDto> UpdateStreetAsync(Guid buildingId, Guid streetId, SaveStreetDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteStreetAsync(Guid buildingId, Guid streetId, Guid? companyId, CancellationToken cancellationToken = default);

    // Hub Boxes (Landed/SDU)
    Task<List<HubBoxDto>> GetHubBoxesAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<HubBoxDto?> GetHubBoxByIdAsync(Guid buildingId, Guid hubBoxId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<HubBoxDto> CreateHubBoxAsync(Guid buildingId, SaveHubBoxDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<HubBoxDto> UpdateHubBoxAsync(Guid buildingId, Guid hubBoxId, SaveHubBoxDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteHubBoxAsync(Guid buildingId, Guid hubBoxId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<HubBoxDto> UpdateHubBoxPortUsageAsync(Guid buildingId, Guid hubBoxId, int portsUsed, Guid? companyId, CancellationToken cancellationToken = default);

    // Poles (Landed/SDU)
    Task<List<PoleDto>> GetPolesAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<PoleDto?> GetPoleByIdAsync(Guid buildingId, Guid poleId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<PoleDto> CreatePoleAsync(Guid buildingId, SavePoleDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<PoleDto> UpdatePoleAsync(Guid buildingId, Guid poleId, SavePoleDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeletePoleAsync(Guid buildingId, Guid poleId, Guid? companyId, CancellationToken cancellationToken = default);
}

