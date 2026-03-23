using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Settings.Services;

public class VendorService : IVendorService
{
    private readonly ApplicationDbContext _context;

    public VendorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VendorDto>> GetAllAsync(Guid companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Vendor>()
            .Where(x => x.CompanyId == companyId && !x.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var items = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    public async Task<VendorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<Vendor>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        return item == null ? null : MapToDto(item);
    }

    public async Task<VendorDto?> GetByCodeAsync(Guid companyId, string code, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<Vendor>()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Code == code && !x.IsDeleted, cancellationToken);

        return item == null ? null : MapToDto(item);
    }

    public async Task<VendorDto> CreateAsync(Guid companyId, CreateVendorDto dto, CancellationToken cancellationToken = default)
    {
        var item = new Vendor
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            ContactPerson = dto.ContactPerson,
            ContactPhone = dto.ContactPhone,
            ContactEmail = dto.ContactEmail,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            PostCode = dto.PostCode,
            Country = dto.Country,
            PaymentTerms = dto.PaymentTerms,
            PaymentDueDays = dto.PaymentDueDays,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Vendor>().Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(item);
    }

    public async Task<VendorDto?> UpdateAsync(Guid id, UpdateVendorDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<Vendor>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (item == null)
            return null;

        if (dto.Name != null)
            item.Name = dto.Name;
        if (dto.Description != null)
            item.Description = dto.Description;
        if (dto.ContactPerson != null)
            item.ContactPerson = dto.ContactPerson;
        if (dto.ContactPhone != null)
            item.ContactPhone = dto.ContactPhone;
        if (dto.ContactEmail != null)
            item.ContactEmail = dto.ContactEmail;
        if (dto.Address != null)
            item.Address = dto.Address;
        if (dto.City != null)
            item.City = dto.City;
        if (dto.State != null)
            item.State = dto.State;
        if (dto.PostCode != null)
            item.PostCode = dto.PostCode;
        if (dto.Country != null)
            item.Country = dto.Country;
        if (dto.PaymentTerms != null)
            item.PaymentTerms = dto.PaymentTerms;
        if (dto.PaymentDueDays.HasValue)
            item.PaymentDueDays = dto.PaymentDueDays;
        if (dto.IsActive.HasValue)
            item.IsActive = dto.IsActive.Value;

        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(item);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<Vendor>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (item == null)
            return false;

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static VendorDto MapToDto(Vendor x) => new()
    {
        Id = x.Id,
        CompanyId = x.CompanyId,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        ContactPerson = x.ContactPerson,
        ContactPhone = x.ContactPhone,
        ContactEmail = x.ContactEmail,
        Address = x.Address,
        City = x.City,
        State = x.State,
        PostCode = x.PostCode,
        Country = x.Country,
        PaymentTerms = x.PaymentTerms,
        PaymentDueDays = x.PaymentDueDays,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
