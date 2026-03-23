using CephasOps.Application.Assets.DTOs;
using CephasOps.Domain.Assets.Entities;
using CephasOps.Domain.Assets.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Assets.Services;

/// <summary>
/// Asset service implementation
/// </summary>
public class AssetService : IAssetService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AssetService> _logger;

    public AssetService(ApplicationDbContext context, ILogger<AssetService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AssetDto>> GetAssetsAsync(Guid? companyId, Guid? assetTypeId = null, AssetStatus? status = null, string? search = null, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var query = _context.Assets
            .Include(a => a.AssetType)
            .Where(a => a.CompanyId == companyId.Value);

        if (assetTypeId.HasValue)
        {
            query = query.Where(a => a.AssetTypeId == assetTypeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                EF.Functions.ILike(a.Name, $"%{search}%") ||
                EF.Functions.ILike(a.AssetTag, $"%{search}%") ||
                (a.SerialNumber != null && EF.Functions.ILike(a.SerialNumber, $"%{search}%")));
        }

        var assets = await query
            .OrderBy(a => a.AssetTag)
            .ToListAsync(cancellationToken);

        return assets.Select(MapToDto).ToList();
    }

    public async Task<AssetDto?> GetAssetByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var query = _context.Assets
            .Include(a => a.AssetType)
            .Where(a => a.Id == id && a.CompanyId == companyId.Value);

        var asset = await query.FirstOrDefaultAsync(cancellationToken);
        return asset != null ? MapToDto(asset) : null;
    }

    public async Task<AssetDto> CreateAssetAsync(CreateAssetDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var duplicateTagQuery = _context.Assets
            .Where(a => EF.Functions.ILike(a.AssetTag, dto.AssetTag.Trim()) && a.CompanyId == companyId.Value);

        var duplicateTag = await duplicateTagQuery.FirstOrDefaultAsync(cancellationToken);
        if (duplicateTag != null)
        {
            throw new InvalidOperationException($"An asset with the tag '{dto.AssetTag}' already exists.");
        }

        // Get asset type defaults (tenant-safe: scope by companyId so FindAsync bypass is avoided)
        var assetType = await _context.AssetTypes
            .FirstOrDefaultAsync(at => at.Id == dto.AssetTypeId && at.CompanyId == companyId, cancellationToken);
        if (assetType == null)
        {
            throw new InvalidOperationException($"Asset Type with ID {dto.AssetTypeId} not found.");
        }

        var depreciationMethod = dto.DepreciationMethod ?? assetType.DefaultDepreciationMethod;
        var usefulLifeMonths = dto.UsefulLifeMonths ?? assetType.DefaultUsefulLifeMonths;
        var salvageValue = dto.SalvageValue ?? (dto.PurchaseCost * assetType.DefaultSalvageValuePercent / 100);

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            AssetTypeId = dto.AssetTypeId,
            AssetTag = dto.AssetTag.Trim(),
            Name = dto.Name.Trim(),
            Description = dto.Description,
            SerialNumber = dto.SerialNumber,
            ModelNumber = dto.ModelNumber,
            Manufacturer = dto.Manufacturer,
            Supplier = dto.Supplier,
            SupplierInvoiceId = dto.SupplierInvoiceId,
            PurchaseDate = dto.PurchaseDate,
            InServiceDate = dto.InServiceDate ?? dto.PurchaseDate,
            PurchaseCost = dto.PurchaseCost,
            SalvageValue = salvageValue,
            DepreciationMethod = depreciationMethod,
            UsefulLifeMonths = usefulLifeMonths,
            CurrentBookValue = dto.PurchaseCost,
            AccumulatedDepreciation = 0,
            Status = AssetStatus.Active,
            Location = dto.Location,
            DepartmentId = dto.DepartmentId,
            AssignedToUserId = dto.AssignedToUserId,
            CostCentreId = dto.CostCentreId,
            WarrantyExpiryDate = dto.WarrantyExpiryDate,
            InsurancePolicyNumber = dto.InsurancePolicyNumber,
            InsuranceExpiryDate = dto.InsuranceExpiryDate,
            NextMaintenanceDate = dto.NextMaintenanceDate,
            Notes = dto.Notes,
            IsFullyDepreciated = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Asset created: {AssetId}, Tag: {AssetTag}", asset.Id, asset.AssetTag);

        asset.AssetType = assetType;
        return MapToDto(asset);
    }

    public async Task<AssetDto> UpdateAssetAsync(Guid id, UpdateAssetDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Assets
            .Include(a => a.AssetType)
            .Where(a => a.Id == id);

        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        query = query.Where(a => a.CompanyId == companyId.Value);

        var asset = await query.FirstOrDefaultAsync(cancellationToken);
        if (asset == null)
        {
            throw new KeyNotFoundException($"Asset with ID {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(dto.AssetTag) && dto.AssetTag.Trim() != asset.AssetTag)
        {
            var duplicateTagQuery = _context.Assets
                .Where(a => a.Id != id && EF.Functions.ILike(a.AssetTag, dto.AssetTag.Trim()) && a.CompanyId == companyId.Value);

            var duplicateTag = await duplicateTagQuery.FirstOrDefaultAsync(cancellationToken);
            if (duplicateTag != null)
            {
                throw new InvalidOperationException($"An asset with the tag '{dto.AssetTag}' already exists.");
            }
        }

        if (dto.AssetTypeId.HasValue) asset.AssetTypeId = dto.AssetTypeId.Value;
        if (!string.IsNullOrEmpty(dto.AssetTag)) asset.AssetTag = dto.AssetTag.Trim();
        if (!string.IsNullOrEmpty(dto.Name)) asset.Name = dto.Name.Trim();
        if (dto.Description != null) asset.Description = dto.Description;
        if (dto.SerialNumber != null) asset.SerialNumber = dto.SerialNumber;
        if (dto.ModelNumber != null) asset.ModelNumber = dto.ModelNumber;
        if (dto.Manufacturer != null) asset.Manufacturer = dto.Manufacturer;
        if (dto.Supplier != null) asset.Supplier = dto.Supplier;
        if (dto.InServiceDate.HasValue) asset.InServiceDate = dto.InServiceDate.Value;
        if (dto.SalvageValue.HasValue) asset.SalvageValue = dto.SalvageValue.Value;
        if (dto.DepreciationMethod.HasValue) asset.DepreciationMethod = dto.DepreciationMethod.Value;
        if (dto.UsefulLifeMonths.HasValue) asset.UsefulLifeMonths = dto.UsefulLifeMonths.Value;
        if (dto.Status.HasValue) asset.Status = dto.Status.Value;
        if (dto.Location != null) asset.Location = dto.Location;
        if (dto.DepartmentId.HasValue) asset.DepartmentId = dto.DepartmentId;
        if (dto.AssignedToUserId.HasValue) asset.AssignedToUserId = dto.AssignedToUserId;
        if (dto.CostCentreId.HasValue) asset.CostCentreId = dto.CostCentreId;
        if (dto.WarrantyExpiryDate.HasValue) asset.WarrantyExpiryDate = dto.WarrantyExpiryDate;
        if (dto.InsurancePolicyNumber != null) asset.InsurancePolicyNumber = dto.InsurancePolicyNumber;
        if (dto.InsuranceExpiryDate.HasValue) asset.InsuranceExpiryDate = dto.InsuranceExpiryDate;
        if (dto.NextMaintenanceDate.HasValue) asset.NextMaintenanceDate = dto.NextMaintenanceDate;
        if (dto.Notes != null) asset.Notes = dto.Notes;
        asset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Asset updated: {AssetId}", id);

        return MapToDto(asset);
    }

    public async Task DeleteAssetAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId.Value, cancellationToken);
        if (asset == null)
        {
            throw new KeyNotFoundException($"Asset with ID {id} not found");
        }

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Asset deleted: {AssetId}", id);
    }

    public async Task<AssetSummaryDto> GetAssetSummaryAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        // Multi-tenant SaaS — CompanyId filter required.
        var query = _context.Assets.Where(a => a.CompanyId == companyId.Value);

        // Load assets with asset types
        var assets = await query
            .Include(a => a.AssetType)
            .ToListAsync(cancellationToken);

        // Load asset types separately if needed for grouping (fallback if Include didn't load them)
        var assetTypeIds = assets.Select(a => a.AssetTypeId).Distinct().ToList();
        var assetTypes = assetTypeIds.Any()
            ? await _context.AssetTypes
                .Where(t => assetTypeIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, cancellationToken)
            : new Dictionary<Guid, Domain.Assets.Entities.AssetType>();

        return new AssetSummaryDto
        {
            TotalAssets = assets.Count,
            ActiveAssets = assets.Count(a => a.Status == AssetStatus.Active),
            DisposedAssets = assets.Count(a => a.Status == AssetStatus.Disposed),
            AssetsUnderMaintenance = assets.Count(a => a.Status == AssetStatus.UnderMaintenance),
            TotalPurchaseCost = assets.Sum(a => a.PurchaseCost),
            TotalCurrentBookValue = assets.Sum(a => a.CurrentBookValue),
            TotalAccumulatedDepreciation = assets.Sum(a => a.AccumulatedDepreciation),
            AssetsByType = assets
                .GroupBy(a => new { 
                    AssetTypeId = a.AssetTypeId, 
                    TypeName = a.AssetType?.Name ?? (assetTypes.TryGetValue(a.AssetTypeId, out var type) ? type.Name : "Unknown")
                })
                .Select(g => new AssetsByTypeDto
                {
                    AssetTypeId = g.Key.AssetTypeId,
                    AssetTypeName = g.Key.TypeName,
                    Count = g.Count(),
                    TotalValue = g.Sum(a => a.CurrentBookValue)
                })
                .OrderByDescending(x => x.TotalValue)
                .ToList()
        };
    }

    public async Task<List<AssetMaintenanceDto>> GetMaintenanceRecordsAsync(Guid? companyId, Guid? assetId = null, bool? completed = null, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var query = _context.AssetMaintenanceRecords.Where(m => m.CompanyId == companyId.Value);

        if (assetId.HasValue)
        {
            query = query.Where(m => m.AssetId == assetId.Value);
        }

        if (completed.HasValue)
        {
            query = query.Where(m => m.IsCompleted == completed.Value);
        }

        var records = await query
            .OrderByDescending(m => m.ScheduledDate ?? m.PerformedDate)
            .ToListAsync(cancellationToken);

        // Load assets separately to avoid query filter issues with Include
        var assetIds = records.Select(r => r.AssetId).Distinct().ToList();
        var assets = assetIds.Any()
            ? await _context.Assets
                .Where(a => assetIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken)
            : new Dictionary<Guid, Asset>();

        // Attach assets to maintenance records
        foreach (var record in records)
        {
            if (assets.TryGetValue(record.AssetId, out var asset))
            {
                record.Asset = asset;
            }
        }

        return records.Select(MapMaintenanceToDto).ToList();
    }

    public async Task<AssetMaintenanceDto> CreateMaintenanceRecordAsync(CreateAssetMaintenanceDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var tenantId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create asset maintenance.");
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == dto.AssetId && a.CompanyId == tenantId.Value, cancellationToken);
        if (asset == null)
        {
            throw new InvalidOperationException($"Asset with ID {dto.AssetId} not found.");
        }

        var maintenance = new AssetMaintenance
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            AssetId = dto.AssetId,
            MaintenanceType = dto.MaintenanceType,
            Description = dto.Description,
            ScheduledDate = dto.ScheduledDate,
            PerformedDate = dto.PerformedDate,
            NextScheduledDate = dto.NextScheduledDate,
            Cost = dto.Cost,
            PnlTypeId = dto.PnlTypeId,
            PerformedBy = dto.PerformedBy,
            SupplierInvoiceId = dto.SupplierInvoiceId,
            ReferenceNumber = dto.ReferenceNumber,
            Notes = dto.Notes,
            IsCompleted = dto.IsCompleted,
            RecordedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update asset's next maintenance date if provided
        if (dto.NextScheduledDate.HasValue)
        {
            asset.NextMaintenanceDate = dto.NextScheduledDate;
            asset.UpdatedAt = DateTime.UtcNow;
        }

        _context.AssetMaintenanceRecords.Add(maintenance);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Maintenance record created: {MaintenanceId} for Asset: {AssetId}", maintenance.Id, dto.AssetId);

        maintenance.Asset = asset;
        return MapMaintenanceToDto(maintenance);
    }

    public async Task<AssetMaintenanceDto> UpdateMaintenanceRecordAsync(Guid id, UpdateAssetMaintenanceDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var maintenance = await _context.AssetMaintenanceRecords
            .FirstOrDefaultAsync(m => m.Id == id && m.CompanyId == companyId.Value, cancellationToken);
        if (maintenance == null)
        {
            throw new KeyNotFoundException($"Maintenance record with ID {id} not found");
        }

        // Load asset separately to avoid query filter issues with Include
        if (maintenance.AssetId != Guid.Empty)
        {
            var asset = await _context.Assets
                .Where(a => a.Id == maintenance.AssetId)
                .FirstOrDefaultAsync(cancellationToken);
            if (asset != null)
            {
                maintenance.Asset = asset;
            }
        }

        if (dto.MaintenanceType.HasValue) maintenance.MaintenanceType = dto.MaintenanceType.Value;
        if (!string.IsNullOrEmpty(dto.Description)) maintenance.Description = dto.Description;
        if (dto.ScheduledDate.HasValue) maintenance.ScheduledDate = dto.ScheduledDate;
        if (dto.PerformedDate.HasValue) maintenance.PerformedDate = dto.PerformedDate;
        if (dto.NextScheduledDate.HasValue) maintenance.NextScheduledDate = dto.NextScheduledDate;
        if (dto.Cost.HasValue) maintenance.Cost = dto.Cost.Value;
        if (dto.PnlTypeId.HasValue) maintenance.PnlTypeId = dto.PnlTypeId;
        if (dto.PerformedBy != null) maintenance.PerformedBy = dto.PerformedBy;
        if (dto.SupplierInvoiceId.HasValue) maintenance.SupplierInvoiceId = dto.SupplierInvoiceId;
        if (dto.ReferenceNumber != null) maintenance.ReferenceNumber = dto.ReferenceNumber;
        if (dto.Notes != null) maintenance.Notes = dto.Notes;
        if (dto.IsCompleted.HasValue) maintenance.IsCompleted = dto.IsCompleted.Value;
        maintenance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Maintenance record updated: {MaintenanceId}", id);

        return MapMaintenanceToDto(maintenance);
    }

    public async Task DeleteMaintenanceRecordAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var maintenance = await _context.AssetMaintenanceRecords
            .FirstOrDefaultAsync(m => m.Id == id && m.CompanyId == companyId.Value, cancellationToken);
        if (maintenance == null)
        {
            throw new KeyNotFoundException($"Maintenance record with ID {id} not found");
        }

        _context.AssetMaintenanceRecords.Remove(maintenance);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Maintenance record deleted: {MaintenanceId}", id);
    }

    public async Task<List<AssetMaintenanceDto>> GetUpcomingMaintenanceAsync(Guid? companyId, int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        var records = await _context.AssetMaintenanceRecords
            .Where(m => m.CompanyId == companyId.Value && !m.IsCompleted && m.ScheduledDate.HasValue && m.ScheduledDate <= cutoffDate)
            .OrderBy(m => m.ScheduledDate!)
            .ToListAsync(cancellationToken);

        // Load assets separately to avoid query filter issues with Include
        var assetIds = records.Select(r => r.AssetId).Distinct().ToList();
        var assets = assetIds.Any()
            ? await _context.Assets
                .Where(a => assetIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken)
            : new Dictionary<Guid, Asset>();

        // Attach assets to maintenance records
        foreach (var record in records)
        {
            if (assets.TryGetValue(record.AssetId, out var asset))
            {
                record.Asset = asset;
            }
        }

        return records.Select(MapMaintenanceToDto).ToList();
    }

    public async Task<AssetDisposalDto> CreateDisposalAsync(CreateAssetDisposalDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == dto.AssetId && a.CompanyId == companyId.Value, cancellationToken);

        if (asset == null)
        {
            throw new InvalidOperationException($"Asset with ID {dto.AssetId} not found.");
        }

        if (asset.Status == AssetStatus.Disposed)
        {
            throw new InvalidOperationException("Asset has already been disposed.");
        }

        var existingDisposal = await _context.AssetDisposals.AnyAsync(d => d.AssetId == dto.AssetId, cancellationToken);
        if (existingDisposal)
        {
            throw new InvalidOperationException("A disposal record already exists for this asset.");
        }

        var gainLoss = dto.DisposalProceeds - asset.CurrentBookValue;

        var disposal = new AssetDisposal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            AssetId = dto.AssetId,
            DisposalMethod = dto.DisposalMethod,
            DisposalDate = dto.DisposalDate,
            BookValueAtDisposal = asset.CurrentBookValue,
            DisposalProceeds = dto.DisposalProceeds,
            GainLoss = gainLoss,
            PnlTypeId = dto.PnlTypeId,
            BuyerName = dto.BuyerName,
            ReferenceNumber = dto.ReferenceNumber,
            Reason = dto.Reason,
            Notes = dto.Notes,
            ProcessedByUserId = userId,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update asset status
        asset.Status = AssetStatus.PendingDisposal;
        asset.UpdatedAt = DateTime.UtcNow;

        _context.AssetDisposals.Add(disposal);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Disposal created: {DisposalId} for Asset: {AssetId}", disposal.Id, dto.AssetId);

        return MapDisposalToDto(disposal, asset);
    }

    public async Task<AssetDisposalDto> ApproveDisposalAsync(Guid disposalId, ApproveAssetDisposalDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var disposal = await _context.AssetDisposals
            .FirstOrDefaultAsync(d => d.Id == disposalId && d.CompanyId == companyId.Value, cancellationToken);
        if (disposal == null)
        {
            throw new KeyNotFoundException($"Disposal with ID {disposalId} not found");
        }

        // Load asset separately to avoid query filter issues with Include; explicitly scope to disposal's company.
        // Clear disposal.Asset when we don't load one via this path so EF fixup cannot leave a wrong-company asset attached.
        if (disposal.AssetId != Guid.Empty)
        {
            var asset = await _context.Assets
                .IgnoreQueryFilters() // Bypass global query filter to avoid DeletedAt column issues
                .Where(a => a.Id == disposal.AssetId && a.CompanyId == disposal.CompanyId)
                .FirstOrDefaultAsync(cancellationToken);
            if (asset != null && !asset.IsDeleted)
            {
                disposal.Asset = asset;
            }
            else
            {
                disposal.Asset = null;
            }
        }

        if (disposal.IsApproved)
        {
            throw new InvalidOperationException("Disposal has already been approved.");
        }

        disposal.IsApproved = dto.Approved;
        disposal.ApprovedByUserId = userId;
        disposal.ApprovalDate = DateTime.UtcNow;
        if (dto.Notes != null) disposal.Notes = dto.Notes;
        disposal.UpdatedAt = DateTime.UtcNow;

        if (dto.Approved && disposal.Asset != null)
        {
            disposal.Asset.Status = AssetStatus.Disposed;
            disposal.Asset.UpdatedAt = DateTime.UtcNow;
        }
        else if (!dto.Approved && disposal.Asset != null)
        {
            disposal.Asset.Status = AssetStatus.Active;
            disposal.Asset.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Disposal {Status}: {DisposalId}", dto.Approved ? "approved" : "rejected", disposalId);

        return MapDisposalToDto(disposal, disposal.Asset);
    }

    public async Task<List<AssetDisposalDto>> GetDisposalsAsync(Guid? companyId, bool? approved = null, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var query = _context.AssetDisposals.Where(d => d.CompanyId == companyId.Value);

        if (approved.HasValue)
        {
            query = query.Where(d => d.IsApproved == approved.Value);
        }

        var disposals = await query
            .OrderByDescending(d => d.DisposalDate)
            .ToListAsync(cancellationToken);

        // Load assets separately to avoid query filter issues with Include
        var assetIds = disposals.Select(d => d.AssetId).Distinct().ToList();
        var assets = assetIds.Any()
            ? await _context.Assets
                .Where(a => assetIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken)
            : new Dictionary<Guid, Asset>();

        // Attach assets to disposals
        foreach (var disposal in disposals)
        {
            if (assets.TryGetValue(disposal.AssetId, out var asset))
            {
                disposal.Asset = asset;
            }
        }

        return disposals.Select(d => MapDisposalToDto(d, d.Asset)).ToList();
    }

    private static AssetDto MapToDto(Asset asset)
    {
        var inServiceDate = asset.InServiceDate ?? asset.PurchaseDate;
        var monthsInService = (int)((DateTime.UtcNow - inServiceDate).TotalDays / 30.44);
        var remainingMonths = Math.Max(0, asset.UsefulLifeMonths - monthsInService);
        var depreciationPercent = asset.PurchaseCost > 0 ? (asset.AccumulatedDepreciation / asset.PurchaseCost) * 100 : 0;

        return new AssetDto
        {
            Id = asset.Id,
            CompanyId = asset.CompanyId,
            AssetTypeId = asset.AssetTypeId,
            AssetTypeName = asset.AssetType?.Name,
            AssetTag = asset.AssetTag,
            Name = asset.Name,
            Description = asset.Description,
            SerialNumber = asset.SerialNumber,
            ModelNumber = asset.ModelNumber,
            Manufacturer = asset.Manufacturer,
            Supplier = asset.Supplier,
            SupplierInvoiceId = asset.SupplierInvoiceId,
            PurchaseDate = asset.PurchaseDate,
            InServiceDate = asset.InServiceDate,
            PurchaseCost = asset.PurchaseCost,
            SalvageValue = asset.SalvageValue,
            DepreciationMethod = asset.DepreciationMethod,
            UsefulLifeMonths = asset.UsefulLifeMonths,
            CurrentBookValue = asset.CurrentBookValue,
            AccumulatedDepreciation = asset.AccumulatedDepreciation,
            LastDepreciationDate = asset.LastDepreciationDate,
            Status = asset.Status,
            Location = asset.Location,
            DepartmentId = asset.DepartmentId,
            AssignedToUserId = asset.AssignedToUserId,
            CostCentreId = asset.CostCentreId,
            WarrantyExpiryDate = asset.WarrantyExpiryDate,
            InsurancePolicyNumber = asset.InsurancePolicyNumber,
            InsuranceExpiryDate = asset.InsuranceExpiryDate,
            NextMaintenanceDate = asset.NextMaintenanceDate,
            Notes = asset.Notes,
            IsFullyDepreciated = asset.IsFullyDepreciated,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt,
            RemainingUsefulLifeMonths = remainingMonths,
            DepreciationPercent = depreciationPercent
        };
    }

    private static AssetMaintenanceDto MapMaintenanceToDto(AssetMaintenance maintenance)
    {
        return new AssetMaintenanceDto
        {
            Id = maintenance.Id,
            CompanyId = maintenance.CompanyId,
            AssetId = maintenance.AssetId,
            AssetName = maintenance.Asset?.Name,
            AssetTag = maintenance.Asset?.AssetTag,
            MaintenanceType = maintenance.MaintenanceType,
            Description = maintenance.Description,
            ScheduledDate = maintenance.ScheduledDate,
            PerformedDate = maintenance.PerformedDate,
            NextScheduledDate = maintenance.NextScheduledDate,
            Cost = maintenance.Cost,
            PnlTypeId = maintenance.PnlTypeId,
            PerformedBy = maintenance.PerformedBy,
            SupplierInvoiceId = maintenance.SupplierInvoiceId,
            ReferenceNumber = maintenance.ReferenceNumber,
            Notes = maintenance.Notes,
            IsCompleted = maintenance.IsCompleted,
            RecordedByUserId = maintenance.RecordedByUserId,
            CreatedAt = maintenance.CreatedAt,
            UpdatedAt = maintenance.UpdatedAt
        };
    }

    private static AssetDisposalDto MapDisposalToDto(AssetDisposal disposal, Asset? asset)
    {
        return new AssetDisposalDto
        {
            Id = disposal.Id,
            CompanyId = disposal.CompanyId,
            AssetId = disposal.AssetId,
            AssetName = asset?.Name,
            AssetTag = asset?.AssetTag,
            DisposalMethod = disposal.DisposalMethod,
            DisposalDate = disposal.DisposalDate,
            BookValueAtDisposal = disposal.BookValueAtDisposal,
            DisposalProceeds = disposal.DisposalProceeds,
            GainLoss = disposal.GainLoss,
            PnlTypeId = disposal.PnlTypeId,
            BuyerName = disposal.BuyerName,
            ReferenceNumber = disposal.ReferenceNumber,
            Reason = disposal.Reason,
            Notes = disposal.Notes,
            ProcessedByUserId = disposal.ProcessedByUserId,
            IsApproved = disposal.IsApproved,
            ApprovedByUserId = disposal.ApprovedByUserId,
            ApprovalDate = disposal.ApprovalDate,
            CreatedAt = disposal.CreatedAt,
            UpdatedAt = disposal.UpdatedAt
        };
    }
}

