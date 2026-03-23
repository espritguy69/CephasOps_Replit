using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Base work rate CRUD and list (Phase 2 — no impact on payout resolution).
/// </summary>
public class BaseWorkRateService : IBaseWorkRateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BaseWorkRateService> _logger;

    public BaseWorkRateService(ApplicationDbContext context, ILogger<BaseWorkRateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BaseWorkRateDto>> ListAsync(Guid? companyId, BaseWorkRateListFilter? filter, CancellationToken cancellationToken = default)
    {
        var previous = TenantScope.CurrentTenantId;
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            TenantScope.CurrentTenantId = companyId;
        try
        {
        var query = _context.BaseWorkRates
            .Include(x => x.RateGroup)
            .Include(x => x.OrderCategory)
            .Include(x => x.ServiceProfile)
            .Include(x => x.InstallationMethod)
            .Include(x => x.OrderSubtype)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);
        if (filter?.RateGroupId.HasValue == true)
            query = query.Where(x => x.RateGroupId == filter.RateGroupId!.Value);
        if (filter?.OrderCategoryId.HasValue == true)
            query = query.Where(x => x.OrderCategoryId == filter.OrderCategoryId!.Value);
        if (filter?.ServiceProfileId.HasValue == true)
            query = query.Where(x => x.ServiceProfileId == filter.ServiceProfileId!.Value);
        if (filter?.InstallationMethodId.HasValue == true)
            query = query.Where(x => x.InstallationMethodId == filter.InstallationMethodId!.Value);
        if (filter?.OrderSubtypeId.HasValue == true)
            query = query.Where(x => x.OrderSubtypeId == filter.OrderSubtypeId!.Value);
        if (filter?.IsActive.HasValue == true)
            query = query.Where(x => x.IsActive == filter.IsActive!.Value);

        var list = await query
            .OrderBy(x => x.RateGroup!.Code)
            .ThenBy(x => x.OrderCategoryId.HasValue ? 0 : (x.ServiceProfileId.HasValue ? 1 : 2))
            .ThenBy(x => x.OrderCategory != null ? x.OrderCategory!.Code : "")
            .ThenBy(x => x.ServiceProfile != null ? x.ServiceProfile!.Code : "")
            .ThenBy(x => x.InstallationMethodId.HasValue ? 1 : 0)
            .ThenBy(x => x.InstallationMethod!.Code)
            .ThenBy(x => x.OrderSubtypeId.HasValue ? 1 : 0)
            .ThenBy(x => x.OrderSubtype!.Code)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    public async Task<BaseWorkRateDto?> GetByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var previous = TenantScope.CurrentTenantId;
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            TenantScope.CurrentTenantId = companyId;
        try
        {
        var query = _context.BaseWorkRates
            .Include(x => x.RateGroup)
            .Include(x => x.OrderCategory)
            .Include(x => x.ServiceProfile)
            .Include(x => x.InstallationMethod)
            .Include(x => x.OrderSubtype)
            .Where(x => x.Id == id && !x.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        return entity == null ? null : MapToDto(entity);
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    public async Task<BaseWorkRateDto> CreateAsync(CreateBaseWorkRateDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var previous = TenantScope.CurrentTenantId;
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            TenantScope.CurrentTenantId = companyId;
        try
        {
            ValidateCategoryOrProfileOnly(dto.OrderCategoryId, dto.ServiceProfileId);
            await ValidateForeignKeysAsync(dto.RateGroupId, dto.OrderCategoryId, dto.ServiceProfileId, dto.InstallationMethodId, dto.OrderSubtypeId, companyId, cancellationToken);
            ValidateEffectiveDates(dto.EffectiveFrom, dto.EffectiveTo);
            await ValidateNoDuplicateActiveAsync(companyId, dto.RateGroupId, dto.OrderCategoryId, dto.ServiceProfileId, dto.InstallationMethodId, dto.OrderSubtypeId, dto.Priority, dto.EffectiveFrom, dto.EffectiveTo, null, cancellationToken);

            var entity = new BaseWorkRate
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                RateGroupId = dto.RateGroupId,
                OrderCategoryId = dto.OrderCategoryId,
                ServiceProfileId = dto.ServiceProfileId,
                InstallationMethodId = dto.InstallationMethodId,
                OrderSubtypeId = dto.OrderSubtypeId,
                Amount = dto.Amount,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "MYR" : dto.Currency.Trim().ToUpperInvariant(),
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                Priority = dto.Priority,
                IsActive = dto.IsActive,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.BaseWorkRates.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("BaseWorkRate created: {Id}, RateGroupId={RateGroupId}", entity.Id, entity.RateGroupId);
            return (await GetByIdAsync(entity.Id, companyId, cancellationToken))!;
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    public async Task<BaseWorkRateDto> UpdateAsync(Guid id, UpdateBaseWorkRateDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var previous = TenantScope.CurrentTenantId;
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            TenantScope.CurrentTenantId = companyId;
        try
        {
        var query = _context.BaseWorkRates.Where(x => x.Id == id && !x.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"BaseWorkRate with ID {id} not found.");

        var newRateGroupId = dto.RateGroupId ?? entity.RateGroupId;
        var newOrderCategoryId = dto.ClearOrderCategoryId ? null : (dto.OrderCategoryId ?? entity.OrderCategoryId);
        var newServiceProfileId = dto.ClearServiceProfileId ? null : (dto.ServiceProfileId ?? entity.ServiceProfileId);
        var newInstallationMethodId = dto.ClearInstallationMethodId ? null : (dto.InstallationMethodId ?? entity.InstallationMethodId);
        var newOrderSubtypeId = dto.ClearOrderSubtypeId ? null : (dto.OrderSubtypeId ?? entity.OrderSubtypeId);

        ValidateCategoryOrProfileOnly(newOrderCategoryId, newServiceProfileId);
        await ValidateForeignKeysAsync(newRateGroupId, newOrderCategoryId, newServiceProfileId, newInstallationMethodId, newOrderSubtypeId, companyId, cancellationToken);
        ValidateEffectiveDates(dto.EffectiveFrom ?? entity.EffectiveFrom, dto.EffectiveTo ?? entity.EffectiveTo);
        await ValidateNoDuplicateActiveAsync(companyId, newRateGroupId, newOrderCategoryId, newServiceProfileId, newInstallationMethodId, newOrderSubtypeId,
            dto.Priority ?? entity.Priority, dto.EffectiveFrom ?? entity.EffectiveFrom, dto.EffectiveTo ?? entity.EffectiveTo, id, cancellationToken);

        if (dto.RateGroupId.HasValue) entity.RateGroupId = dto.RateGroupId.Value;
        if (dto.ClearOrderCategoryId) entity.OrderCategoryId = null;
        else if (dto.OrderCategoryId.HasValue) entity.OrderCategoryId = dto.OrderCategoryId;
        if (dto.ClearServiceProfileId) entity.ServiceProfileId = null;
        else if (dto.ServiceProfileId.HasValue) entity.ServiceProfileId = dto.ServiceProfileId;
        if (dto.ClearInstallationMethodId) entity.InstallationMethodId = null;
        else if (dto.InstallationMethodId.HasValue) entity.InstallationMethodId = dto.InstallationMethodId;
        if (dto.ClearOrderSubtypeId) entity.OrderSubtypeId = null;
        else if (dto.OrderSubtypeId.HasValue) entity.OrderSubtypeId = dto.OrderSubtypeId;
        if (dto.Amount.HasValue) entity.Amount = dto.Amount.Value;
        if (dto.Currency != null) entity.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "MYR" : dto.Currency.Trim().ToUpperInvariant();
        if (dto.EffectiveFrom.HasValue) entity.EffectiveFrom = dto.EffectiveFrom;
        if (dto.EffectiveTo.HasValue) entity.EffectiveTo = dto.EffectiveTo;
        if (dto.Priority.HasValue) entity.Priority = dto.Priority.Value;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        if (dto.Notes != null) entity.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("BaseWorkRate updated: {Id}", id);
        return (await GetByIdAsync(id, companyId, cancellationToken))!;
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    public async Task DeleteAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var previous = TenantScope.CurrentTenantId;
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            TenantScope.CurrentTenantId = companyId;
        try
        {
        var query = _context.BaseWorkRates.Where(x => x.Id == id && !x.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"BaseWorkRate with ID {id} not found.");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("BaseWorkRate soft-deleted: {Id}", id);
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    /// <summary>At most one of OrderCategoryId or ServiceProfileId may be set (or both null for broad fallback).</summary>
    private static void ValidateCategoryOrProfileOnly(Guid? orderCategoryId, Guid? serviceProfileId)
    {
        if (orderCategoryId.HasValue && serviceProfileId.HasValue)
            throw new ArgumentException("Set either Order Category (exact pricing) or Service Profile (shared pricing), not both. Use one or leave both empty for broad fallback.");
    }

    private async Task ValidateForeignKeysAsync(Guid rateGroupId, Guid? orderCategoryId, Guid? serviceProfileId, Guid? installationMethodId, Guid? orderSubtypeId, Guid? companyId, CancellationToken ct)
    {
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            TenantScope.CurrentTenantId = companyId;
        var rateGroupQuery = _context.RateGroups.Where(r => r.Id == rateGroupId && !r.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            rateGroupQuery = rateGroupQuery.Where(r => r.CompanyId == companyId.Value);
        var rateGroup = await rateGroupQuery.FirstOrDefaultAsync(ct)
            ?? throw new ArgumentException($"RateGroup {rateGroupId} not found.");
        if (companyId.HasValue && companyId.Value != Guid.Empty && rateGroup.CompanyId != companyId)
            throw new UnauthorizedAccessException("RateGroup does not belong to your company.");

        if (orderCategoryId.HasValue)
        {
            var oc = await _context.OrderCategories.FirstOrDefaultAsync(c => c.Id == orderCategoryId.Value && !c.IsDeleted, ct);
            if (oc == null) throw new ArgumentException($"OrderCategory {orderCategoryId} not found.");
            if (companyId.HasValue && companyId.Value != Guid.Empty && oc.CompanyId != companyId)
                throw new UnauthorizedAccessException("OrderCategory does not belong to your company.");
        }
        if (serviceProfileId.HasValue)
        {
            var sp = await _context.ServiceProfiles.FirstOrDefaultAsync(s => s.Id == serviceProfileId.Value && !s.IsDeleted, ct);
            if (sp == null) throw new ArgumentException($"ServiceProfile {serviceProfileId} not found.");
            if (companyId.HasValue && companyId.Value != Guid.Empty && sp.CompanyId != null && sp.CompanyId != companyId)
                throw new UnauthorizedAccessException("ServiceProfile does not belong to your company.");
        }
        if (installationMethodId.HasValue)
        {
            var im = await _context.InstallationMethods.FirstOrDefaultAsync(m => m.Id == installationMethodId.Value && !m.IsDeleted, ct);
            if (im == null) throw new ArgumentException($"InstallationMethod {installationMethodId} not found.");
            if (companyId.HasValue && companyId.Value != Guid.Empty && im.CompanyId != companyId)
                throw new UnauthorizedAccessException("InstallationMethod does not belong to your company.");
        }
        if (orderSubtypeId.HasValue)
        {
            var ot = await _context.OrderTypes.FirstOrDefaultAsync(t => t.Id == orderSubtypeId.Value && !t.IsDeleted, ct);
            if (ot == null) throw new ArgumentException($"OrderSubtype (OrderType) {orderSubtypeId} not found.");
            if (ot.ParentOrderTypeId == null)
                throw new ArgumentException($"OrderType {orderSubtypeId} is not a subtype (it has no parent).");
            if (companyId.HasValue && companyId.Value != Guid.Empty && ot.CompanyId != companyId)
                throw new UnauthorizedAccessException("OrderSubtype does not belong to your company.");
        }
    }

    private static void ValidateEffectiveDates(DateTime? effectiveFrom, DateTime? effectiveTo)
    {
        if (effectiveFrom.HasValue && effectiveTo.HasValue && effectiveTo.Value < effectiveFrom.Value)
            throw new ArgumentException("EffectiveTo must be on or after EffectiveFrom.");
    }

    private async Task ValidateNoDuplicateActiveAsync(Guid? companyId, Guid rateGroupId, Guid? orderCategoryId, Guid? serviceProfileId, Guid? installationMethodId, Guid? orderSubtypeId, int priority, DateTime? effectiveFrom, DateTime? effectiveTo, Guid? excludeId, CancellationToken ct)
    {
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            TenantScope.CurrentTenantId = companyId;
        var query = _context.BaseWorkRates
            .Where(x => !x.IsDeleted && x.IsActive && x.RateGroupId == rateGroupId && x.Priority == priority
                && x.OrderCategoryId == orderCategoryId && x.ServiceProfileId == serviceProfileId
                && x.InstallationMethodId == installationMethodId && x.OrderSubtypeId == orderSubtypeId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        var overlapping = await query.ToListAsync(ct);
        foreach (var other in overlapping)
        {
            var fromA = effectiveFrom ?? DateTime.MinValue;
            var toA = effectiveTo ?? DateTime.MaxValue;
            var fromB = other.EffectiveFrom ?? DateTime.MinValue;
            var toB = other.EffectiveTo ?? DateTime.MaxValue;
            if (fromA <= toB && fromB <= toA)
                throw new InvalidOperationException("Another active Base Work Rate exists with the same Rate Group, Applies To (Order Category or Service Profile), Installation Method, Order Subtype, and Priority with overlapping effective dates.");
        }
    }

    private static BaseWorkRateDto MapToDto(BaseWorkRate x)
    {
        return new BaseWorkRateDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            RateGroupId = x.RateGroupId,
            RateGroupName = x.RateGroup?.Name,
            RateGroupCode = x.RateGroup?.Code,
            OrderCategoryId = x.OrderCategoryId,
            OrderCategoryName = x.OrderCategory?.Name,
            OrderCategoryCode = x.OrderCategory?.Code,
            ServiceProfileId = x.ServiceProfileId,
            ServiceProfileName = x.ServiceProfile?.Name,
            ServiceProfileCode = x.ServiceProfile?.Code,
            InstallationMethodId = x.InstallationMethodId,
            InstallationMethodName = x.InstallationMethod?.Name,
            InstallationMethodCode = x.InstallationMethod?.Code,
            OrderSubtypeId = x.OrderSubtypeId,
            OrderSubtypeName = x.OrderSubtype?.Name,
            OrderSubtypeCode = x.OrderSubtype?.Code,
            Amount = x.Amount,
            Currency = x.Currency,
            EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo,
            Priority = x.Priority,
            IsActive = x.IsActive,
            Notes = x.Notes,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}
