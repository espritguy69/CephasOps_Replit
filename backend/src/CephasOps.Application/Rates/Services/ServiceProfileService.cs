using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Rates.Services;

public class ServiceProfileService : IServiceProfileService
{
    private readonly ApplicationDbContext _context;

    public ServiceProfileService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceProfileDto>> ListAsync(Guid? companyId, ServiceProfileListFilter? filter, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceProfiles
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);

        if (filter?.IsActive.HasValue == true)
            query = query.Where(x => x.IsActive == filter.IsActive!.Value);

        if (!string.IsNullOrWhiteSpace(filter?.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                (x.Code != null && x.Code.ToLower().Contains(term)) ||
                (x.Name != null && x.Name.ToLower().Contains(term)));
        }

        var list = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<ServiceProfileDto?> GetByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceProfiles.Where(x => x.Id == id && !x.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<ServiceProfileDto> CreateAsync(CreateServiceProfileDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var code = (dto.Code ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Code is required.");

        var existing = await _context.ServiceProfiles
            .Where(x => !x.IsDeleted && x.Code == code)
            .Where(x => !companyId.HasValue || companyId.Value == Guid.Empty || x.CompanyId == companyId.Value)
            .AnyAsync(cancellationToken);
        if (existing)
            throw new InvalidOperationException($"A Service Profile with code '{code}' already exists for this company.");

        var entity = new ServiceProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code,
            Name = (dto.Name ?? string.Empty).Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ServiceProfiles.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task<ServiceProfileDto> UpdateAsync(Guid id, UpdateServiceProfileDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceProfiles.Where(x => x.Id == id && !x.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Service Profile not found.");

        if (dto.Code != null)
        {
            var code = dto.Code.Trim();
            if (code != entity.Code)
            {
                var duplicate = await _context.ServiceProfiles
                    .Where(x => !x.IsDeleted && x.Code == code && x.Id != id)
                    .Where(x => !companyId.HasValue || companyId.Value == Guid.Empty || x.CompanyId == companyId.Value)
                    .AnyAsync(cancellationToken);
                if (duplicate)
                    throw new InvalidOperationException($"Another Service Profile with code '{code}' already exists.");
                entity.Code = code;
            }
        }
        if (dto.Name != null) entity.Name = dto.Name.Trim();
        if (dto.Description != null) entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) entity.DisplayOrder = dto.DisplayOrder.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceProfiles.Where(x => x.Id == id && !x.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Service Profile not found.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static ServiceProfileDto MapToDto(ServiceProfile x)
    {
        return new ServiceProfileDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            IsActive = x.IsActive,
            DisplayOrder = x.DisplayOrder,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}
