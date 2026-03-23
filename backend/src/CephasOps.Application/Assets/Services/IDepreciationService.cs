using CephasOps.Application.Assets.DTOs;

namespace CephasOps.Application.Assets.Services;

/// <summary>
/// Depreciation calculation and management service interface
/// </summary>
public interface IDepreciationService
{
    /// <summary>
    /// Get depreciation entries for a period or asset
    /// </summary>
    Task<List<AssetDepreciationDto>> GetDepreciationEntriesAsync(Guid? companyId, string? period = null, Guid? assetId = null, bool? isPosted = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get depreciation schedule for an asset (actual + projected)
    /// </summary>
    Task<DepreciationScheduleDto> GetDepreciationScheduleAsync(Guid assetId, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run depreciation calculation for a period
    /// </summary>
    Task<DepreciationRunResultDto> RunDepreciationAsync(RunDepreciationDto dto, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Post depreciation entries (mark as finalized)
    /// </summary>
    Task<int> PostDepreciationEntriesAsync(Guid? companyId, string period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate monthly depreciation amount for an asset
    /// </summary>
    decimal CalculateMonthlyDepreciation(decimal purchaseCost, decimal salvageValue, int usefulLifeMonths, Domain.Assets.Enums.DepreciationMethod method);
}

