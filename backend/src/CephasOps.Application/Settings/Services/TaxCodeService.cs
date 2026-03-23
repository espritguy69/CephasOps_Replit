using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Settings.Services;

public class TaxCodeService : ITaxCodeService
{
    private readonly ApplicationDbContext _context;

    public TaxCodeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TaxCodeDto>> GetAllAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TaxCode>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    public async Task<TaxCodeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<TaxCode>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        return item == null ? null : MapToDto(item);
    }

    public async Task<TaxCodeDto?> GetByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<TaxCode>()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Code == code && !x.IsDeleted, cancellationToken);

        return item == null ? null : MapToDto(item);
    }

    public async Task<TaxCodeDto> CreateAsync(Guid companyId, CreateTaxCodeDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.IsDefault)
            await ClearDefaultForCompanyAsync(companyId, excludeId: null, cancellationToken);

        var item = new TaxCode
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            TaxRate = dto.TaxRate,
            IsDefault = dto.IsDefault,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<TaxCode>().Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(item);
    }

    public async Task<TaxCodeDto?> UpdateAsync(Guid id, UpdateTaxCodeDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<TaxCode>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (item == null)
            return null;

        if (dto.IsDefault == true)
            await ClearDefaultForCompanyAsync(item.CompanyId, excludeId: id, cancellationToken);

        if (dto.Name != null)
            item.Name = dto.Name;
        if (dto.Description != null)
            item.Description = dto.Description;
        if (dto.TaxRate.HasValue)
            item.TaxRate = dto.TaxRate.Value;
        if (dto.IsDefault.HasValue)
            item.IsDefault = dto.IsDefault.Value;
        if (dto.IsActive.HasValue)
            item.IsActive = dto.IsActive.Value;

        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(item);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<TaxCode>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (item == null)
            return false;

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task ClearDefaultForCompanyAsync(Guid companyId, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = _context.Set<TaxCode>().Where(x => x.CompanyId == companyId && !x.IsDeleted && x.IsDefault);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        var toClear = await query.ToListAsync(cancellationToken);
        foreach (var x in toClear)
            x.IsDefault = false;
    }

    private static TaxCodeDto MapToDto(TaxCode x) => new()
    {
        Id = x.Id,
        CompanyId = x.CompanyId,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        TaxRate = x.TaxRate,
        IsDefault = x.IsDefault,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
