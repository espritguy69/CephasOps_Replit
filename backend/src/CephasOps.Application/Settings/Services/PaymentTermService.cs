using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Settings.Services;

public class PaymentTermService : IPaymentTermService
{
    private readonly ApplicationDbContext _context;

    public PaymentTermService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PaymentTermDto>> GetAllAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<PaymentTerm>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    public async Task<PaymentTermDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<PaymentTerm>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        return item == null ? null : MapToDto(item);
    }

    public async Task<PaymentTermDto?> GetByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<PaymentTerm>()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Code == code && !x.IsDeleted, cancellationToken);

        return item == null ? null : MapToDto(item);
    }

    public async Task<PaymentTermDto> CreateAsync(Guid companyId, CreatePaymentTermDto dto, CancellationToken cancellationToken = default)
    {
        var item = new PaymentTerm
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            DueDays = dto.DueDays,
            DiscountPercent = dto.DiscountPercent,
            DiscountDays = dto.DiscountDays,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<PaymentTerm>().Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(item);
    }

    public async Task<PaymentTermDto?> UpdateAsync(Guid id, UpdatePaymentTermDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<PaymentTerm>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (item == null)
            return null;

        if (dto.Name != null)
            item.Name = dto.Name;
        if (dto.Description != null)
            item.Description = dto.Description;
        if (dto.DueDays.HasValue)
            item.DueDays = dto.DueDays.Value;
        if (dto.DiscountPercent.HasValue)
            item.DiscountPercent = dto.DiscountPercent.Value;
        if (dto.DiscountDays.HasValue)
            item.DiscountDays = dto.DiscountDays.Value;
        if (dto.IsActive.HasValue)
            item.IsActive = dto.IsActive.Value;

        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(item);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<PaymentTerm>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (item == null)
            return false;

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static PaymentTermDto MapToDto(PaymentTerm x) => new()
    {
        Id = x.Id,
        CompanyId = x.CompanyId,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        DueDays = x.DueDays,
        DiscountPercent = x.DiscountPercent,
        DiscountDays = x.DiscountDays,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
