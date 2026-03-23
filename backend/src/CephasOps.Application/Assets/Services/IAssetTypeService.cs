using CephasOps.Application.Assets.DTOs;

namespace CephasOps.Application.Assets.Services;

/// <summary>
/// Asset Type service interface
/// </summary>
public interface IAssetTypeService
{
    Task<List<AssetTypeDto>> GetAssetTypesAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<AssetTypeDto?> GetAssetTypeByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<AssetTypeDto> CreateAssetTypeAsync(CreateAssetTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<AssetTypeDto> UpdateAssetTypeAsync(Guid id, UpdateAssetTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteAssetTypeAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}

