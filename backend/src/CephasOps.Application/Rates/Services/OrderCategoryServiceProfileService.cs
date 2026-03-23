using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Rates.Services;

public class OrderCategoryServiceProfileService : IOrderCategoryServiceProfileService
{
    private readonly ApplicationDbContext _context;

    public OrderCategoryServiceProfileService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderCategoryServiceProfileDto>> ListAsync(Guid? companyId, OrderCategoryServiceProfileListFilter? filter, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderCategoryServiceProfiles
            .Include(x => x.OrderCategory)
            .Include(x => x.ServiceProfile)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);

        if (filter?.ServiceProfileId.HasValue == true)
            query = query.Where(x => x.ServiceProfileId == filter.ServiceProfileId!.Value);
        if (filter?.OrderCategoryId.HasValue == true)
            query = query.Where(x => x.OrderCategoryId == filter.OrderCategoryId!.Value);

        var list = await query
            .OrderBy(x => x.ServiceProfile!.DisplayOrder)
            .ThenBy(x => x.ServiceProfile!.Code)
            .ThenBy(x => x.OrderCategory!.Code)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<OrderCategoryServiceProfileDto?> GetByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderCategoryServiceProfiles
            .Include(x => x.OrderCategory)
            .Include(x => x.ServiceProfile)
            .Where(x => x.Id == id && !x.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<OrderCategoryServiceProfileDto> CreateAsync(CreateOrderCategoryServiceProfileDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        await ValidateForeignKeysAsync(dto.OrderCategoryId, dto.ServiceProfileId, companyId, cancellationToken);

        var duplicate = await _context.OrderCategoryServiceProfiles
            .Where(x => !x.IsDeleted && x.OrderCategoryId == dto.OrderCategoryId)
            .Where(x => !companyId.HasValue || companyId.Value == Guid.Empty || x.CompanyId == companyId.Value)
            .AnyAsync(cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("This Order Category is already mapped to a Service Profile. Remove the existing mapping first.");

        var entity = new OrderCategoryServiceProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            OrderCategoryId = dto.OrderCategoryId,
            ServiceProfileId = dto.ServiceProfileId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrderCategoryServiceProfiles.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(entity)
            .Reference(x => x.OrderCategory).LoadAsync(cancellationToken);
        await _context.Entry(entity)
            .Reference(x => x.ServiceProfile).LoadAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderCategoryServiceProfiles.Where(x => x.Id == id && !x.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(x => x.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Order Category Service Profile mapping not found.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateForeignKeysAsync(Guid orderCategoryId, Guid serviceProfileId, Guid? companyId, CancellationToken ct)
    {
        var oc = await _context.OrderCategories.FirstOrDefaultAsync(c => c.Id == orderCategoryId && !c.IsDeleted, ct)
            ?? throw new ArgumentException("Order Category not found.");
        if (companyId.HasValue && companyId.Value != Guid.Empty && oc.CompanyId != null && oc.CompanyId != companyId)
            throw new UnauthorizedAccessException("Order Category does not belong to your company.");

        var sp = await _context.ServiceProfiles.FirstOrDefaultAsync(s => s.Id == serviceProfileId && !s.IsDeleted, ct)
            ?? throw new ArgumentException("Service Profile not found.");
        if (companyId.HasValue && companyId.Value != Guid.Empty && sp.CompanyId != null && sp.CompanyId != companyId)
            throw new UnauthorizedAccessException("Service Profile does not belong to your company.");
    }

    private static OrderCategoryServiceProfileDto MapToDto(OrderCategoryServiceProfile x)
    {
        return new OrderCategoryServiceProfileDto
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            OrderCategoryId = x.OrderCategoryId,
            OrderCategoryName = x.OrderCategory?.Name,
            OrderCategoryCode = x.OrderCategory?.Code,
            ServiceProfileId = x.ServiceProfileId,
            ServiceProfileName = x.ServiceProfile?.Name,
            ServiceProfileCode = x.ServiceProfile?.Code,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}
