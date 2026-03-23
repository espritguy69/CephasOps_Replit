using CephasOps.Application.Assets.DTOs;
using CephasOps.Domain.Assets.Entities;
using CephasOps.Domain.Assets.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Assets.Services;

/// <summary>
/// Depreciation calculation and management service implementation
/// </summary>
public class DepreciationService : IDepreciationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DepreciationService> _logger;

    public DepreciationService(ApplicationDbContext context, ILogger<DepreciationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AssetDepreciationDto>> GetDepreciationEntriesAsync(Guid? companyId, string? period = null, Guid? assetId = null, bool? isPosted = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AssetDepreciationEntries
            .AsQueryable();

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(d => d.CompanyId == companyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(period))
        {
            query = query.Where(d => d.Period == period);
        }

        if (assetId.HasValue)
        {
            query = query.Where(d => d.AssetId == assetId.Value);
        }

        if (isPosted.HasValue)
        {
            query = query.Where(d => d.IsPosted == isPosted.Value);
        }

        var entries = await query
            .OrderByDescending(d => d.Period)
            .ThenBy(d => d.AssetId) // Order by AssetId instead of AssetTag to avoid Include
            .ToListAsync(cancellationToken);

        // Load assets separately to avoid query filter issues with Include
        var assetIds = entries.Select(e => e.AssetId).Distinct().ToList();
        var assets = assetIds.Any()
            ? await _context.Assets
                .Where(a => assetIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken)
            : new Dictionary<Guid, Asset>();

        // Attach assets to depreciation entries
        foreach (var entry in entries)
        {
            if (assets.TryGetValue(entry.AssetId, out var asset))
            {
                entry.Asset = asset;
            }
        }

        // Reorder by AssetTag now that assets are loaded
        entries = entries.OrderByDescending(d => d.Period)
            .ThenBy(d => d.Asset?.AssetTag ?? string.Empty)
            .ToList();

        return entries.Select(MapToDto).ToList();
    }

    public async Task<DepreciationScheduleDto> GetDepreciationScheduleAsync(Guid assetId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Assets.Where(a => a.Id == assetId);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(a => a.CompanyId == companyId.Value);
        }

        var asset = await query.FirstOrDefaultAsync(cancellationToken);
        if (asset == null)
        {
            throw new KeyNotFoundException($"Asset with ID {assetId} not found");
        }

        // Get existing depreciation entries
        var existingEntries = await _context.AssetDepreciationEntries
            .Where(d => d.AssetId == assetId)
            .OrderBy(d => d.Period)
            .ToListAsync(cancellationToken);

        var depreciableAmount = asset.PurchaseCost - asset.SalvageValue;
        var monthlyDepreciation = CalculateMonthlyDepreciation(asset.PurchaseCost, asset.SalvageValue, asset.UsefulLifeMonths, asset.DepreciationMethod);

        var scheduleLines = new List<DepreciationScheduleLineDto>();
        var inServiceDate = asset.InServiceDate ?? asset.PurchaseDate;
        var currentBookValue = asset.PurchaseCost;
        var accumulatedDepreciation = 0m;

        // Generate schedule for each month from in-service date
        for (int month = 0; month < asset.UsefulLifeMonths; month++)
        {
            var periodDate = inServiceDate.AddMonths(month);
            var period = periodDate.ToString("yyyy-MM");

            // Check if we have an actual entry for this period
            var existingEntry = existingEntries.FirstOrDefault(e => e.Period == period);

            decimal depAmount;
            if (existingEntry != null)
            {
                depAmount = existingEntry.DepreciationAmount;
                currentBookValue = existingEntry.ClosingBookValue;
                accumulatedDepreciation = existingEntry.AccumulatedDepreciation;

                scheduleLines.Add(new DepreciationScheduleLineDto
                {
                    Period = period,
                    OpeningBookValue = existingEntry.OpeningBookValue,
                    DepreciationAmount = depAmount,
                    ClosingBookValue = currentBookValue,
                    AccumulatedDepreciation = accumulatedDepreciation,
                    IsActual = true
                });
            }
            else
            {
                // Calculate projected depreciation
                var openingValue = currentBookValue;
                depAmount = Math.Min(monthlyDepreciation, currentBookValue - asset.SalvageValue);
                if (depAmount < 0) depAmount = 0;

                currentBookValue -= depAmount;
                accumulatedDepreciation += depAmount;

                scheduleLines.Add(new DepreciationScheduleLineDto
                {
                    Period = period,
                    OpeningBookValue = openingValue,
                    DepreciationAmount = depAmount,
                    ClosingBookValue = currentBookValue,
                    AccumulatedDepreciation = accumulatedDepreciation,
                    IsActual = false
                });
            }
        }

        return new DepreciationScheduleDto
        {
            AssetId = asset.Id,
            AssetName = asset.Name,
            AssetTag = asset.AssetTag,
            PurchaseCost = asset.PurchaseCost,
            SalvageValue = asset.SalvageValue,
            DepreciableAmount = depreciableAmount,
            UsefulLifeMonths = asset.UsefulLifeMonths,
            MonthlyDepreciation = monthlyDepreciation,
            Schedule = scheduleLines
        };
    }

    public async Task<DepreciationRunResultDto> RunDepreciationAsync(RunDepreciationDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var result = new DepreciationRunResultDto
        {
            Period = dto.Period
        };

        // Validate period format (YYYY-MM)
        if (!DateTime.TryParse(dto.Period + "-01", out var periodDate))
        {
            result.Errors.Add($"Invalid period format: {dto.Period}. Expected format: YYYY-MM");
            return result;
        }

        // Get eligible assets
        var assetsQuery = _context.Assets
            .Where(a => a.Status == AssetStatus.Active && !a.IsFullyDepreciated && a.DepreciationMethod != DepreciationMethod.None);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            assetsQuery = assetsQuery.Where(a => a.CompanyId == companyId.Value);
        }

        if (dto.AssetId.HasValue)
        {
            assetsQuery = assetsQuery.Where(a => a.Id == dto.AssetId.Value);
        }

        var assets = await assetsQuery.ToListAsync(cancellationToken);

        foreach (var asset in assets)
        {
            try
            {
                // Check if depreciation already exists for this period
                var existingEntry = await _context.AssetDepreciationEntries
                    .AnyAsync(d => d.AssetId == asset.Id && d.Period == dto.Period, cancellationToken);

                if (existingEntry)
                {
                    result.Errors.Add($"Depreciation entry already exists for asset {asset.AssetTag} in period {dto.Period}");
                    continue;
                }

                // Check if asset was in service during this period
                var inServiceDate = asset.InServiceDate ?? asset.PurchaseDate;
                if (periodDate < inServiceDate.Date.AddDays(-inServiceDate.Day + 1))
                {
                    continue; // Asset wasn't in service yet
                }

                // Calculate depreciation
                var monthlyDepreciation = CalculateMonthlyDepreciation(asset.PurchaseCost, asset.SalvageValue, asset.UsefulLifeMonths, asset.DepreciationMethod);
                var depreciationAmount = Math.Min(monthlyDepreciation, asset.CurrentBookValue - asset.SalvageValue);
                if (depreciationAmount <= 0)
                {
                    // Asset is fully depreciated
                    asset.IsFullyDepreciated = true;
                    asset.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                var openingBookValue = asset.CurrentBookValue;
                var closingBookValue = openingBookValue - depreciationAmount;
                var newAccumulatedDepreciation = asset.AccumulatedDepreciation + depreciationAmount;

                // Create depreciation entry
                var entry = new AssetDepreciation
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    AssetId = asset.Id,
                    Period = dto.Period,
                    DepreciationAmount = depreciationAmount,
                    OpeningBookValue = openingBookValue,
                    ClosingBookValue = closingBookValue,
                    AccumulatedDepreciation = newAccumulatedDepreciation,
                    PnlTypeId = asset.AssetType?.DepreciationPnlTypeId,
                    IsPosted = dto.PostImmediately,
                    CalculatedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.AssetDepreciationEntries.Add(entry);

                // Update asset
                asset.CurrentBookValue = closingBookValue;
                asset.AccumulatedDepreciation = newAccumulatedDepreciation;
                asset.LastDepreciationDate = DateTime.UtcNow;
                if (closingBookValue <= asset.SalvageValue)
                {
                    asset.IsFullyDepreciated = true;
                }
                asset.UpdatedAt = DateTime.UtcNow;

                result.EntriesCreated++;
                result.TotalDepreciation += depreciationAmount;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error processing asset {asset.AssetTag}: {ex.Message}");
                _logger.LogError(ex, "Error running depreciation for asset {AssetId}", asset.Id);
            }
        }

        result.AssetsProcessed = assets.Count;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Depreciation run completed for period {Period}: {EntriesCreated} entries created, {TotalDepreciation:C} total",
            dto.Period, result.EntriesCreated, result.TotalDepreciation);

        return result;
    }

    public async Task<int> PostDepreciationEntriesAsync(Guid? companyId, string period, CancellationToken cancellationToken = default)
    {
        var query = _context.AssetDepreciationEntries
            .Where(d => d.Period == period && !d.IsPosted);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(d => d.CompanyId == companyId.Value);
        }

        var entries = await query.ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            entry.IsPosted = true;
            entry.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Posted {Count} depreciation entries for period {Period}", entries.Count, period);

        return entries.Count;
    }

    public decimal CalculateMonthlyDepreciation(decimal purchaseCost, decimal salvageValue, int usefulLifeMonths, DepreciationMethod method)
    {
        if (usefulLifeMonths <= 0) return 0;

        var depreciableAmount = purchaseCost - salvageValue;
        if (depreciableAmount <= 0) return 0;

        return method switch
        {
            DepreciationMethod.StraightLine => depreciableAmount / usefulLifeMonths,
            DepreciationMethod.DecliningBalance => purchaseCost * (2m / usefulLifeMonths) / 12m,
            DepreciationMethod.DoubleDecliningBalance => purchaseCost * (2m / (usefulLifeMonths / 12m)) / 12m,
            DepreciationMethod.None => 0,
            _ => depreciableAmount / usefulLifeMonths // Default to straight line
        };
    }

    private static AssetDepreciationDto MapToDto(AssetDepreciation entry)
    {
        return new AssetDepreciationDto
        {
            Id = entry.Id,
            CompanyId = entry.CompanyId,
            AssetId = entry.AssetId,
            AssetName = entry.Asset?.Name,
            AssetTag = entry.Asset?.AssetTag,
            Period = entry.Period,
            DepreciationAmount = entry.DepreciationAmount,
            OpeningBookValue = entry.OpeningBookValue,
            ClosingBookValue = entry.ClosingBookValue,
            AccumulatedDepreciation = entry.AccumulatedDepreciation,
            PnlTypeId = entry.PnlTypeId,
            IsPosted = entry.IsPosted,
            CalculatedAt = entry.CalculatedAt,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }
}

