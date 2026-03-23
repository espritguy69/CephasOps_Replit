using CephasOps.Application.Assets.DTOs;
using CephasOps.Domain.Assets.Enums;

namespace CephasOps.Application.Assets.Services;

/// <summary>
/// Asset service interface
/// </summary>
public interface IAssetService
{
    // Asset CRUD
    Task<List<AssetDto>> GetAssetsAsync(Guid? companyId, Guid? assetTypeId = null, AssetStatus? status = null, string? search = null, CancellationToken cancellationToken = default);
    Task<AssetDto?> GetAssetByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<AssetDto> CreateAssetAsync(CreateAssetDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<AssetDto> UpdateAssetAsync(Guid id, UpdateAssetDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteAssetAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);

    // Asset Summary
    Task<AssetSummaryDto> GetAssetSummaryAsync(Guid? companyId, CancellationToken cancellationToken = default);

    // Maintenance
    Task<List<AssetMaintenanceDto>> GetMaintenanceRecordsAsync(Guid? companyId, Guid? assetId = null, bool? completed = null, CancellationToken cancellationToken = default);
    Task<AssetMaintenanceDto> CreateMaintenanceRecordAsync(CreateAssetMaintenanceDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<AssetMaintenanceDto> UpdateMaintenanceRecordAsync(Guid id, UpdateAssetMaintenanceDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteMaintenanceRecordAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<List<AssetMaintenanceDto>> GetUpcomingMaintenanceAsync(Guid? companyId, int daysAhead = 30, CancellationToken cancellationToken = default);

    // Disposal
    Task<AssetDisposalDto> CreateDisposalAsync(CreateAssetDisposalDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<AssetDisposalDto> ApproveDisposalAsync(Guid disposalId, ApproveAssetDisposalDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<AssetDisposalDto>> GetDisposalsAsync(Guid? companyId, bool? approved = null, CancellationToken cancellationToken = default);
}

