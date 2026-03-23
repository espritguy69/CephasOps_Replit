using CephasOps.Application.Tenants.DTOs;
using CephasOps.Domain.Tenants.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Tenants.Services;

/// <summary>
/// Tenant CRUD service (Phase 11).
/// </summary>
public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _context;

    public TenantService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantDto>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Tenants.AsNoTracking();
        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);
        var list = await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return t == null ? null : Map(t);
    }

    public async Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var t = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
        return t == null ? null : Map(t);
    }

    public async Task<TenantDto> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var exists = await _context.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"A tenant with slug '{slug}' already exists.");
        var entity = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _context.Tenants.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<TenantDto?> UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return null;

        if (request.Name != null) entity.Name = request.Name.Trim();
        if (request.Slug != null) entity.Slug = request.Slug.Trim().ToLowerInvariant();
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private static TenantDto Map(Tenant t)
    {
        return new TenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            IsActive = t.IsActive,
            CreatedAtUtc = t.CreatedAtUtc,
            UpdatedAtUtc = t.UpdatedAtUtc
        };
    }
}
