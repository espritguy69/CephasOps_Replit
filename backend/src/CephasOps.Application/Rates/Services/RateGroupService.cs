using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Rate group CRUD and order type/subtype mapping (Phase 1 — no impact on payout resolution).
/// </summary>
public class RateGroupService : IRateGroupService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RateGroupService> _logger;

    public RateGroupService(ApplicationDbContext context, ILogger<RateGroupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RateGroupDto>> ListRateGroupsAsync(Guid? companyId, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _context.RateGroups.AsQueryable();
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(r => r.CompanyId == companyId.Value);
        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);
        query = query.Where(r => !r.IsDeleted);
        var list = await query.OrderBy(r => r.DisplayOrder).ThenBy(r => r.Name).ToListAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<RateGroupDto?> GetRateGroupByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.RateGroups.Where(r => r.Id == id && !r.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(r => r.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<RateGroupDto> CreateRateGroupAsync(CreateRateGroupDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var entity = new RateGroup
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.RateGroups.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("RateGroup created: {RateGroupId}, Code: {Code}", entity.Id, entity.Code);
        return MapToDto(entity);
    }

    public async Task<RateGroupDto> UpdateRateGroupAsync(Guid id, UpdateRateGroupDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.RateGroups.Where(r => r.Id == id && !r.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(r => r.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"RateGroup with ID {id} not found.");
        if (dto.Name != null) entity.Name = dto.Name.Trim();
        if (dto.Code != null) entity.Code = dto.Code.Trim();
        if (dto.Description != null) entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) entity.DisplayOrder = dto.DisplayOrder.Value;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("RateGroup updated: {RateGroupId}", id);
        return MapToDto(entity);
    }

    public async Task DeleteRateGroupAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.RateGroups.Where(r => r.Id == id && !r.IsDeleted);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(r => r.CompanyId == companyId.Value);
        var entity = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"RateGroup with ID {id} not found.");
        var hasMappings = await _context.OrderTypeSubtypeRateGroups.AnyAsync(m => m.RateGroupId == id, cancellationToken);
        if (hasMappings)
            throw new InvalidOperationException($"Cannot delete RateGroup {id} because it has order type/subtype mappings. Remove mappings first.");
        _context.RateGroups.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("RateGroup deleted: {RateGroupId}", id);
    }

    public async Task<List<OrderTypeSubtypeRateGroupMappingDto>> ListMappingsAsync(Guid? companyId, Guid? rateGroupId, Guid? orderTypeId, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderTypeSubtypeRateGroups
            .Include(m => m.OrderType)
            .Include(m => m.OrderSubtype)
            .Include(m => m.RateGroup)
            .AsQueryable();
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(m => m.CompanyId == companyId.Value);
        if (rateGroupId.HasValue)
            query = query.Where(m => m.RateGroupId == rateGroupId.Value);
        if (orderTypeId.HasValue)
            query = query.Where(m => m.OrderTypeId == orderTypeId.Value);
        var list = await query.OrderBy(m => m.OrderType!.Name).ThenBy(m => m.OrderSubtype != null ? m.OrderSubtype.Name : "").ToListAsync(cancellationToken);
        return list.Select(MapMappingToDto).ToList();
    }

    public async Task<OrderTypeSubtypeRateGroupMappingDto> AssignRateGroupToOrderTypeSubtypeAsync(AssignRateGroupToOrderTypeSubtypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var tenantId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var orderType = ((tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == dto.OrderTypeId && ot.CompanyId == tenantId.Value, cancellationToken)
            : await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == dto.OrderTypeId, cancellationToken))
            ?? throw new ArgumentException($"OrderType {dto.OrderTypeId} not found.");
        if (dto.OrderSubtypeId.HasValue)
        {
            var subtype = ((tenantId.HasValue && tenantId.Value != Guid.Empty)
                ? await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == dto.OrderSubtypeId.Value && ot.CompanyId == tenantId.Value, cancellationToken)
                : await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == dto.OrderSubtypeId.Value, cancellationToken))
                ?? throw new ArgumentException($"OrderSubtype {dto.OrderSubtypeId} not found.");
            if (subtype.ParentOrderTypeId != dto.OrderTypeId)
                throw new ArgumentException($"OrderType {dto.OrderSubtypeId} is not a subtype of OrderType {dto.OrderTypeId}.");
        }
        var rateGroup = await _context.RateGroups.FirstOrDefaultAsync(r => r.Id == dto.RateGroupId && !r.IsDeleted, cancellationToken)
            ?? throw new ArgumentException($"RateGroup {dto.RateGroupId} not found.");
        if (companyId.HasValue && rateGroup.CompanyId != companyId.Value)
            throw new UnauthorizedAccessException("RateGroup does not belong to your company.");

        var existing = await _context.OrderTypeSubtypeRateGroups
            .FirstOrDefaultAsync(m => m.OrderTypeId == dto.OrderTypeId && m.OrderSubtypeId == dto.OrderSubtypeId && m.CompanyId == companyId, cancellationToken);
        if (existing != null)
        {
            existing.RateGroupId = dto.RateGroupId;
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Entry(existing).Reference(m => m.OrderType).LoadAsync(cancellationToken);
            await _context.Entry(existing).Reference(m => m.OrderSubtype).LoadAsync(cancellationToken);
            await _context.Entry(existing).Reference(m => m.RateGroup).LoadAsync(cancellationToken);
            return MapMappingToDto(existing);
        }

        var mapping = new OrderTypeSubtypeRateGroup
        {
            Id = Guid.NewGuid(),
            OrderTypeId = dto.OrderTypeId,
            OrderSubtypeId = dto.OrderSubtypeId,
            RateGroupId = dto.RateGroupId,
            CompanyId = companyId
        };
        _context.OrderTypeSubtypeRateGroups.Add(mapping);
        await _context.SaveChangesAsync(cancellationToken);
        await _context.Entry(mapping).Reference(m => m.OrderType).LoadAsync(cancellationToken);
        await _context.Entry(mapping).Reference(m => m.OrderSubtype).LoadAsync(cancellationToken);
        await _context.Entry(mapping).Reference(m => m.RateGroup).LoadAsync(cancellationToken);
        _logger.LogInformation("Rate group mapping created: OrderType={OrderTypeId}, OrderSubtype={OrderSubtypeId}, RateGroup={RateGroupId}",
            dto.OrderTypeId, dto.OrderSubtypeId, dto.RateGroupId);
        return MapMappingToDto(mapping);
    }

    public async Task UnassignRateGroupMappingAsync(Guid mappingId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderTypeSubtypeRateGroups.Where(m => m.Id == mappingId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(m => m.CompanyId == companyId.Value);
        var mapping = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Mapping {mappingId} not found.");
        _context.OrderTypeSubtypeRateGroups.Remove(mapping);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Rate group mapping removed: {MappingId}", mappingId);
    }

    private static RateGroupDto MapToDto(RateGroup r)
    {
        return new RateGroupDto
        {
            Id = r.Id,
            CompanyId = r.CompanyId,
            Name = r.Name,
            Code = r.Code,
            Description = r.Description,
            IsActive = r.IsActive,
            DisplayOrder = r.DisplayOrder,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }

    private static OrderTypeSubtypeRateGroupMappingDto MapMappingToDto(OrderTypeSubtypeRateGroup m)
    {
        return new OrderTypeSubtypeRateGroupMappingDto
        {
            Id = m.Id,
            OrderTypeId = m.OrderTypeId,
            OrderTypeName = m.OrderType?.Name,
            OrderTypeCode = m.OrderType?.Code,
            OrderSubtypeId = m.OrderSubtypeId,
            OrderSubtypeName = m.OrderSubtype?.Name,
            OrderSubtypeCode = m.OrderSubtype?.Code,
            RateGroupId = m.RateGroupId,
            RateGroupName = m.RateGroup?.Name,
            RateGroupCode = m.RateGroup?.Code,
            CompanyId = m.CompanyId
        };
    }
}
