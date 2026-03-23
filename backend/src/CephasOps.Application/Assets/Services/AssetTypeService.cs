using CephasOps.Application.Assets.DTOs;
using CephasOps.Domain.Assets.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Assets.Services;

/// <summary>
/// Asset Type service implementation
/// </summary>
public class AssetTypeService : IAssetTypeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AssetTypeService> _logger;

    public AssetTypeService(ApplicationDbContext context, ILogger<AssetTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AssetTypeDto>> GetAssetTypesAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AssetTypes.AsQueryable();

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CompanyId == companyId.Value || t.CompanyId == null);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var assetTypes = await query
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        // Get asset counts
        var assetCounts = await _context.Assets
            .GroupBy(a => a.AssetTypeId)
            .Select(g => new { AssetTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AssetTypeId, x => x.Count, cancellationToken);

        return assetTypes.Select(t => MapToDto(t, assetCounts.GetValueOrDefault(t.Id, 0))).ToList();
    }

    public async Task<AssetTypeDto?> GetAssetTypeByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.AssetTypes.Where(t => t.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CompanyId == companyId.Value || t.CompanyId == null);
        }

        var assetType = await query.FirstOrDefaultAsync(cancellationToken);
        if (assetType == null) return null;

        var assetCount = await _context.Assets.CountAsync(a => a.AssetTypeId == id, cancellationToken);
        return MapToDto(assetType, assetCount);
    }

    public async Task<AssetTypeDto> CreateAssetTypeAsync(CreateAssetTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Check for duplicate code
        var duplicateCodeQuery = _context.AssetTypes
            .Where(t => EF.Functions.ILike(t.Code, dto.Code.Trim()));

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            duplicateCodeQuery = duplicateCodeQuery.Where(t => t.CompanyId == companyId.Value || t.CompanyId == null);
        }

        var duplicateCode = await duplicateCodeQuery.FirstOrDefaultAsync(cancellationToken);
        if (duplicateCode != null)
        {
            throw new InvalidOperationException($"An asset type with the code '{dto.Code}' already exists.");
        }

        var assetType = new AssetType
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim(),
            Description = dto.Description,
            DefaultDepreciationMethod = dto.DefaultDepreciationMethod,
            DefaultUsefulLifeMonths = dto.DefaultUsefulLifeMonths,
            DefaultSalvageValuePercent = dto.DefaultSalvageValuePercent,
            DepreciationPnlTypeId = dto.DepreciationPnlTypeId,
            IsActive = dto.IsActive,
            SortOrder = dto.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AssetType created: {AssetTypeId}, Code: {Code}", assetType.Id, assetType.Code);

        return MapToDto(assetType, 0);
    }

    public async Task<AssetTypeDto> UpdateAssetTypeAsync(Guid id, UpdateAssetTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.AssetTypes.Where(t => t.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var assetType = await query.FirstOrDefaultAsync(cancellationToken);
        if (assetType == null)
        {
            throw new KeyNotFoundException($"Asset Type with ID {id} not found");
        }

        // Check for duplicate code
        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim() != assetType.Code)
        {
            var duplicateCodeQuery = _context.AssetTypes
                .Where(t => t.Id != id && EF.Functions.ILike(t.Code, dto.Code.Trim()));

            if (companyId.HasValue && companyId.Value != Guid.Empty)
            {
                duplicateCodeQuery = duplicateCodeQuery.Where(t => t.CompanyId == companyId.Value || t.CompanyId == null);
            }

            var duplicateCode = await duplicateCodeQuery.FirstOrDefaultAsync(cancellationToken);
            if (duplicateCode != null)
            {
                throw new InvalidOperationException($"An asset type with the code '{dto.Code}' already exists.");
            }
        }

        if (!string.IsNullOrEmpty(dto.Name)) assetType.Name = dto.Name.Trim();
        if (!string.IsNullOrEmpty(dto.Code)) assetType.Code = dto.Code.Trim();
        if (dto.Description != null) assetType.Description = dto.Description;
        if (dto.DefaultDepreciationMethod.HasValue) assetType.DefaultDepreciationMethod = dto.DefaultDepreciationMethod.Value;
        if (dto.DefaultUsefulLifeMonths.HasValue) assetType.DefaultUsefulLifeMonths = dto.DefaultUsefulLifeMonths.Value;
        if (dto.DefaultSalvageValuePercent.HasValue) assetType.DefaultSalvageValuePercent = dto.DefaultSalvageValuePercent.Value;
        if (dto.DepreciationPnlTypeId.HasValue) assetType.DepreciationPnlTypeId = dto.DepreciationPnlTypeId;
        if (dto.IsActive.HasValue) assetType.IsActive = dto.IsActive.Value;
        if (dto.SortOrder.HasValue) assetType.SortOrder = dto.SortOrder.Value;
        assetType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AssetType updated: {AssetTypeId}", id);

        var assetCount = await _context.Assets.CountAsync(a => a.AssetTypeId == id, cancellationToken);
        return MapToDto(assetType, assetCount);
    }

    public async Task DeleteAssetTypeAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.AssetTypes.Where(t => t.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var assetType = await query.FirstOrDefaultAsync(cancellationToken);
        if (assetType == null)
        {
            throw new KeyNotFoundException($"Asset Type with ID {id} not found");
        }

        // Check if any assets are using this type
        var hasAssets = await _context.Assets.AnyAsync(a => a.AssetTypeId == id, cancellationToken);
        if (hasAssets)
        {
            throw new InvalidOperationException($"Cannot delete Asset Type {id} because it is being used by assets.");
        }

        _context.AssetTypes.Remove(assetType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AssetType deleted: {AssetTypeId}", id);
    }

    private static AssetTypeDto MapToDto(AssetType assetType, int assetCount)
    {
        return new AssetTypeDto
        {
            Id = assetType.Id,
            CompanyId = assetType.CompanyId,
            Name = assetType.Name,
            Code = assetType.Code,
            Description = assetType.Description,
            DefaultDepreciationMethod = assetType.DefaultDepreciationMethod,
            DefaultUsefulLifeMonths = assetType.DefaultUsefulLifeMonths,
            DefaultSalvageValuePercent = assetType.DefaultSalvageValuePercent,
            DepreciationPnlTypeId = assetType.DepreciationPnlTypeId,
            IsActive = assetType.IsActive,
            SortOrder = assetType.SortOrder,
            CreatedAt = assetType.CreatedAt,
            UpdatedAt = assetType.UpdatedAt,
            AssetCount = assetCount
        };
    }
}

