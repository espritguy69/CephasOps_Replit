using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Common;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Billing ratecard service implementation
/// Per PARTNER_MODULE.md: Rate lookup uses partnerGroupId first, then partnerId override
/// </summary>
public class BillingRatecardService : IBillingRatecardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BillingRatecardService> _logger;

    public BillingRatecardService(
        ApplicationDbContext context,
        ILogger<BillingRatecardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BillingRatecardDto>> GetBillingRatecardsAsync(
        Guid companyId, 
        Guid? partnerId = null, 
        Guid? orderTypeId = null,
        Guid? departmentId = null,
        string? serviceCategory = null,
        Guid? installationMethodId = null,
        bool? isActive = null, 
        CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<BillingRatecardDto>();

        var query = _context.BillingRatecards.Where(br => br.CompanyId == effectiveCompanyId.Value);

        if (partnerId.HasValue)
        {
            query = query.Where(br => br.PartnerId == partnerId.Value);
        }

        if (orderTypeId.HasValue)
        {
            query = query.Where(br => br.OrderTypeId == orderTypeId.Value);
        }

        // Include rates with null departmentId when filtering by department
        // (null means department-agnostic rates that apply to all departments)
        if (departmentId.HasValue)
        {
            query = query.Where(br => br.DepartmentId == departmentId.Value || br.DepartmentId == null);
        }

        if (!string.IsNullOrEmpty(serviceCategory))
        {
            query = query.Where(br => br.ServiceCategory == serviceCategory);
        }

        if (installationMethodId.HasValue)
        {
            query = query.Where(br => br.InstallationMethodId == installationMethodId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(br => br.IsActive == isActive.Value);
        }

        var ratecards = await query
            .OrderBy(br => br.PartnerGroupId)
            .ThenBy(br => br.PartnerId)
            .ThenBy(br => br.OrderTypeId)
            .ToListAsync(cancellationToken);

        // Load related data
        var partnerIds = ratecards.Where(br => br.PartnerId.HasValue).Select(br => br.PartnerId!.Value).Distinct().ToList();
        var partnerGroupIds = ratecards.Where(br => br.PartnerGroupId.HasValue).Select(br => br.PartnerGroupId!.Value).Distinct().ToList();
        var deptIds = ratecards.Where(br => br.DepartmentId.HasValue).Select(br => br.DepartmentId!.Value).Distinct().ToList();
        var methodIds = ratecards.Where(br => br.InstallationMethodId.HasValue).Select(br => br.InstallationMethodId!.Value).Distinct().ToList();
        var orderTypeIds = ratecards.Where(br => br.OrderTypeId.HasValue).Select(br => br.OrderTypeId!.Value).Distinct().ToList();

        var partners = partnerIds.Any()
            ? await _context.Partners.Where(p => partnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var partnerGroups = partnerGroupIds.Any()
            ? await _context.PartnerGroups.Where(pg => partnerGroupIds.Contains(pg.Id)).ToDictionaryAsync(pg => pg.Id, pg => pg.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var departments = deptIds.Any() 
            ? await _context.Departments.Where(d => deptIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken) 
            : new Dictionary<Guid, string>();

        var methods = methodIds.Any() 
            ? await _context.InstallationMethods.Where(m => methodIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken) 
            : new Dictionary<Guid, string>();

        var orderTypes = orderTypeIds.Any() 
            ? await _context.OrderTypes.Where(ot => orderTypeIds.Contains(ot.Id)).ToDictionaryAsync(ot => ot.Id, ot => ot.Name, cancellationToken) 
            : new Dictionary<Guid, string>();

        return ratecards.Select(br => new BillingRatecardDto
        {
            Id = br.Id,
            CompanyId = br.CompanyId,
            DepartmentId = br.DepartmentId,
            DepartmentName = br.DepartmentId.HasValue && departments.ContainsKey(br.DepartmentId.Value) ? departments[br.DepartmentId.Value] : null,
            PartnerGroupId = br.PartnerGroupId,
            PartnerGroupName = br.PartnerGroupId.HasValue && partnerGroups.ContainsKey(br.PartnerGroupId.Value) ? partnerGroups[br.PartnerGroupId.Value] : null,
            PartnerId = br.PartnerId,
            PartnerName = br.PartnerId.HasValue && partners.ContainsKey(br.PartnerId.Value) ? partners[br.PartnerId.Value] : null,
            OrderTypeId = br.OrderTypeId,
            OrderTypeName = br.OrderTypeId.HasValue && orderTypes.ContainsKey(br.OrderTypeId.Value) ? orderTypes[br.OrderTypeId.Value] : null,
            ServiceCategory = br.ServiceCategory,
            InstallationMethodId = br.InstallationMethodId,
            InstallationMethodName = br.InstallationMethodId.HasValue && methods.ContainsKey(br.InstallationMethodId.Value) ? methods[br.InstallationMethodId.Value] : null,
            BuildingType = br.BuildingType,
            Description = br.Description,
            Amount = br.Amount,
            TaxRate = br.TaxRate,
            IsActive = br.IsActive,
            EffectiveFrom = br.EffectiveFrom,
            EffectiveTo = br.EffectiveTo,
            CreatedAt = br.CreatedAt
        }).ToList();
    }

    public async Task<BillingRatecardDto?> GetBillingRatecardByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var ratecard = await _context.BillingRatecards
            .Where(br => br.Id == id && br.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (ratecard == null)
        {
            return null;
        }

        var cid = ratecard.CompanyId;
        var partner = ratecard.PartnerId.HasValue && cid.HasValue
            ? await _context.Partners.FirstOrDefaultAsync(p => p.Id == ratecard.PartnerId.Value && p.CompanyId == cid.Value, cancellationToken)
            : ratecard.PartnerId.HasValue ? await _context.Partners.FirstOrDefaultAsync(p => p.Id == ratecard.PartnerId.Value, cancellationToken) : null;
        var partnerGroup = ratecard.PartnerGroupId.HasValue && cid.HasValue
            ? await _context.PartnerGroups.FirstOrDefaultAsync(pg => pg.Id == ratecard.PartnerGroupId.Value && pg.CompanyId == cid.Value, cancellationToken)
            : ratecard.PartnerGroupId.HasValue ? await _context.PartnerGroups.FirstOrDefaultAsync(pg => pg.Id == ratecard.PartnerGroupId.Value, cancellationToken) : null;
        var department = ratecard.DepartmentId.HasValue && cid.HasValue
            ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == ratecard.DepartmentId.Value && d.CompanyId == cid.Value, cancellationToken)
            : ratecard.DepartmentId.HasValue ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == ratecard.DepartmentId.Value, cancellationToken) : null;
        var method = ratecard.InstallationMethodId.HasValue && cid.HasValue
            ? await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == ratecard.InstallationMethodId.Value && m.CompanyId == cid.Value, cancellationToken)
            : ratecard.InstallationMethodId.HasValue ? await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == ratecard.InstallationMethodId.Value, cancellationToken) : null;
        var orderType = ratecard.OrderTypeId.HasValue && cid.HasValue
            ? await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == ratecard.OrderTypeId.Value && ot.CompanyId == cid.Value, cancellationToken)
            : ratecard.OrderTypeId.HasValue ? await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == ratecard.OrderTypeId.Value, cancellationToken) : null;

        return new BillingRatecardDto
        {
            Id = ratecard.Id,
            CompanyId = ratecard.CompanyId,
            DepartmentId = ratecard.DepartmentId,
            DepartmentName = department?.Name,
            PartnerGroupId = ratecard.PartnerGroupId,
            PartnerGroupName = partnerGroup?.Name,
            PartnerId = ratecard.PartnerId,
            PartnerName = partner?.Name,
            OrderTypeId = ratecard.OrderTypeId,
            OrderTypeName = orderType?.Name,
            ServiceCategory = ratecard.ServiceCategory,
            InstallationMethodId = ratecard.InstallationMethodId,
            InstallationMethodName = method?.Name,
            BuildingType = ratecard.BuildingType,
            Description = ratecard.Description,
            Amount = ratecard.Amount,
            TaxRate = ratecard.TaxRate,
            IsActive = ratecard.IsActive,
            EffectiveFrom = ratecard.EffectiveFrom,
            EffectiveTo = ratecard.EffectiveTo,
            CreatedAt = ratecard.CreatedAt
        };
    }

    public async Task<BillingRatecardDto> CreateBillingRatecardAsync(CreateBillingRatecardDto dto, Guid companyId, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("CreateBillingRatecard");
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a billing ratecard.");
        FinancialIsolationGuard.RequireCompany(effectiveCompanyId, "CreateBillingRatecard");

        Domain.Companies.Entities.Partner? partner = null;
        if (dto.PartnerId.HasValue)
        {
            partner = await _context.Partners
                .Where(p => p.Id == dto.PartnerId.Value && p.CompanyId == effectiveCompanyId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (partner == null)
            {
                throw new KeyNotFoundException($"Partner with ID {dto.PartnerId.Value} not found");
            }
        }

        Domain.Companies.Entities.PartnerGroup? partnerGroup = null;
        if (dto.PartnerGroupId.HasValue)
        {
            partnerGroup = await _context.PartnerGroups
                .Where(pg => pg.Id == dto.PartnerGroupId.Value && pg.CompanyId == effectiveCompanyId.Value)
                .FirstOrDefaultAsync(cancellationToken);

        if (partnerGroup == null)
        {
            throw new KeyNotFoundException($"Partner Group with ID {dto.PartnerGroupId.Value} not found");
        }
    }

        if (!string.IsNullOrWhiteSpace(dto.ServiceCategory))
        {
            if (!await _context.OrderCategories
                .AnyAsync(oc => oc.CompanyId == effectiveCompanyId.Value && oc.Code == dto.ServiceCategory.Trim() && oc.IsActive, cancellationToken))
            {
                throw new ArgumentException(
                    $"ServiceCategory '{dto.ServiceCategory}' does not match any active Order Category code. ServiceCategory must equal an existing OrderCategory.Code (e.g. FTTH, FTTO, FTTR, FTTC).");
            }
        }

        var ratecard = new BillingRatecard
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            DepartmentId = dto.DepartmentId,
            PartnerGroupId = dto.PartnerGroupId,
            PartnerId = dto.PartnerId,
            OrderTypeId = dto.OrderTypeId,
            ServiceCategory = dto.ServiceCategory,
            InstallationMethodId = dto.InstallationMethodId,
            BuildingType = dto.BuildingType,
            Description = dto.Description,
            Amount = dto.Amount,
            TaxRate = dto.TaxRate,
            IsActive = dto.IsActive,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            CreatedAt = DateTime.UtcNow
        };

        _context.BillingRatecards.Add(ratecard);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Billing ratecard created: {RatecardId}, PartnerGroup: {PartnerGroupId}, Partner: {PartnerId}, Department: {DepartmentId}", 
            ratecard.Id, ratecard.PartnerGroupId, ratecard.PartnerId, ratecard.DepartmentId);

        // Load related data for response (tenant-safe: scope by ratecard.CompanyId)
        var cid = ratecard.CompanyId;
        var department = ratecard.DepartmentId.HasValue && cid.HasValue
            ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == ratecard.DepartmentId.Value && d.CompanyId == cid.Value, cancellationToken)
            : ratecard.DepartmentId.HasValue ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == ratecard.DepartmentId.Value, cancellationToken) : null;
        var method = ratecard.InstallationMethodId.HasValue && cid.HasValue
            ? await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == ratecard.InstallationMethodId.Value && m.CompanyId == cid.Value, cancellationToken)
            : ratecard.InstallationMethodId.HasValue ? await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == ratecard.InstallationMethodId.Value, cancellationToken) : null;
        var orderType = ratecard.OrderTypeId.HasValue && cid.HasValue
            ? await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == ratecard.OrderTypeId.Value && ot.CompanyId == cid.Value, cancellationToken)
            : ratecard.OrderTypeId.HasValue ? await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == ratecard.OrderTypeId.Value, cancellationToken) : null;

        return new BillingRatecardDto
        {
            Id = ratecard.Id,
            CompanyId = ratecard.CompanyId,
            DepartmentId = ratecard.DepartmentId,
            DepartmentName = department?.Name,
            PartnerGroupId = ratecard.PartnerGroupId,
            PartnerGroupName = partnerGroup?.Name,
            PartnerId = ratecard.PartnerId,
            PartnerName = partner?.Name,
            OrderTypeId = ratecard.OrderTypeId,
            OrderTypeName = orderType?.Name,
            ServiceCategory = ratecard.ServiceCategory,
            InstallationMethodId = ratecard.InstallationMethodId,
            InstallationMethodName = method?.Name,
            BuildingType = ratecard.BuildingType,
            Description = ratecard.Description,
            Amount = ratecard.Amount,
            TaxRate = ratecard.TaxRate,
            IsActive = ratecard.IsActive,
            EffectiveFrom = ratecard.EffectiveFrom,
            EffectiveTo = ratecard.EffectiveTo,
            CreatedAt = ratecard.CreatedAt
        };
    }

    public async Task<BillingRatecardDto> UpdateBillingRatecardAsync(Guid id, UpdateBillingRatecardDto dto, Guid companyId, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("UpdateBillingRatecard");
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to update a billing ratecard.");
        FinancialIsolationGuard.RequireCompany(effectiveCompanyId, "UpdateBillingRatecard");

        var ratecard = await _context.BillingRatecards
            .Where(br => br.Id == id && br.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (ratecard == null)
        {
            throw new KeyNotFoundException($"Billing ratecard with ID {id} not found");
        }

        if (dto.PartnerId.HasValue)
        {
            var partner = await _context.Partners
                .Where(p => p.Id == dto.PartnerId.Value && p.CompanyId == effectiveCompanyId.Value)
                .FirstOrDefaultAsync(cancellationToken);
            if (partner == null)
            {
                throw new KeyNotFoundException($"Partner with ID {dto.PartnerId.Value} not found");
            }
            ratecard.PartnerId = dto.PartnerId.Value;
        }

        if (dto.PartnerGroupId.HasValue)
        {
            var partnerGroup = await _context.PartnerGroups
                .Where(pg => pg.Id == dto.PartnerGroupId.Value && pg.CompanyId == effectiveCompanyId.Value)
                .FirstOrDefaultAsync(cancellationToken);
            if (partnerGroup == null)
            {
                throw new KeyNotFoundException($"Partner Group with ID {dto.PartnerGroupId.Value} not found");
            }
            ratecard.PartnerGroupId = dto.PartnerGroupId.Value;
        }

        // Update fields
        if (dto.DepartmentId != null) ratecard.DepartmentId = dto.DepartmentId;
        if (dto.OrderTypeId != null) ratecard.OrderTypeId = dto.OrderTypeId;
        if (dto.ServiceCategory != null)
        {
            var newValue = dto.ServiceCategory.Trim();
            if (!string.IsNullOrEmpty(newValue))
            {
                if (!await _context.OrderCategories
                    .AnyAsync(oc => oc.CompanyId == effectiveCompanyId.Value && oc.Code == newValue && oc.IsActive, cancellationToken))
                {
                    throw new ArgumentException(
                        $"ServiceCategory '{newValue}' does not match any active Order Category code. ServiceCategory must equal an existing OrderCategory.Code (e.g. FTTH, FTTO, FTTR, FTTC).");
                }
            }
            ratecard.ServiceCategory = newValue;
        }
        if (dto.InstallationMethodId != null) ratecard.InstallationMethodId = dto.InstallationMethodId;
        if (dto.BuildingType != null) ratecard.BuildingType = dto.BuildingType;
        if (dto.Description != null) ratecard.Description = dto.Description;
        if (dto.Amount.HasValue) ratecard.Amount = dto.Amount.Value;
        if (dto.TaxRate.HasValue) ratecard.TaxRate = dto.TaxRate.Value;
        if (dto.IsActive.HasValue) ratecard.IsActive = dto.IsActive.Value;
        if (dto.EffectiveFrom.HasValue) ratecard.EffectiveFrom = dto.EffectiveFrom;
        if (dto.EffectiveTo.HasValue) ratecard.EffectiveTo = dto.EffectiveTo;

        ratecard.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Billing ratecard updated: {RatecardId}", id);

        return await GetBillingRatecardByIdAsync(id, effectiveCompanyId.Value, cancellationToken) 
            ?? throw new InvalidOperationException($"Failed to retrieve updated ratecard {id}");
    }

    public async Task DeleteBillingRatecardAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("DeleteBillingRatecard");
        var effectiveCompanyId = (companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete a billing ratecard.");
        FinancialIsolationGuard.RequireCompany(effectiveCompanyId, "DeleteBillingRatecard");

        var ratecard = await _context.BillingRatecards
            .Where(br => br.Id == id && br.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (ratecard == null)
        {
            throw new KeyNotFoundException($"Billing ratecard with ID {id} not found");
        }

        _context.BillingRatecards.Remove(ratecard);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Billing ratecard deleted: {RatecardId}", id);
    }
}
