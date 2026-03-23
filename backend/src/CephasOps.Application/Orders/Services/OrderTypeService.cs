using CephasOps.Application.Orders.DTOs;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// OrderType service implementation
/// </summary>
public class OrderTypeService : IOrderTypeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderTypeService> _logger;

    public OrderTypeService(ApplicationDbContext context, ILogger<OrderTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get order types. When parentsOnly=true returns only top-level parents (ParentOrderTypeId == null),
    /// deduped by (CompanyId, Code) so at most one row per logical parent even if data has duplicates.
    /// </summary>
    public async Task<List<OrderTypeDto>> GetOrderTypesAsync(Guid? companyId, Guid? departmentId = null, bool? isActive = null, bool parentsOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderTypes.AsNoTracking().AsQueryable();
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(ot => ot.CompanyId == companyId.Value);
        if (departmentId.HasValue)
            query = query.Where(ot => ot.DepartmentId == departmentId.Value);
        if (isActive.HasValue)
            query = query.Where(ot => ot.IsActive == isActive.Value);
        if (parentsOnly)
        {
            query = query.Where(ot => ot.ParentOrderTypeId == null);
            var canonicalParentCodes = new[] { "ACTIVATION", "MODIFICATION", "ASSURANCE", "VALUE_ADDED_SERVICE" };
            query = query.Where(ot => canonicalParentCodes.Contains(ot.Code));
        }

        var orderTypes = await query
            .OrderBy(ot => ot.DisplayOrder)
            .ThenBy(ot => ot.Name)
            .ThenBy(ot => ot.CreatedAt)
            .ToListAsync(cancellationToken);

        // Dedupe parents by (CompanyId, Code): keep one with most children, then lowest DisplayOrder, then oldest CreatedAt.
        List<OrderType> list = orderTypes;
        if (parentsOnly && orderTypes.Count > 0)
        {
            var childCounts = await _context.OrderTypes
                .AsNoTracking()
                .Where(ot => ot.ParentOrderTypeId != null)
                .GroupBy(ot => ot.ParentOrderTypeId!.Value)
                .Select(g => new { ParentId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var countMap = childCounts.ToDictionary(c => c.ParentId, c => c.Count);

            list = orderTypes
                .GroupBy(ot => new { ot.CompanyId, Code = ot.Code.ToUpperInvariant() })
                .Select(g =>
                {
                    var candidates = g.OrderByDescending(ot => countMap.GetValueOrDefault(ot.Id, 0))
                        .ThenBy(ot => ot.DisplayOrder)
                        .ThenBy(ot => ot.CreatedAt)
                        .ToList();
                    return candidates[0];
                })
                .OrderBy(ot => ot.DisplayOrder)
                .ThenBy(ot => ot.Name)
                .ToList();
        }

        var dtos = list.Select(MapToDto).ToList();
        if (parentsOnly && dtos.Count > 0)
        {
            var parentIds = dtos.Select(d => d.Id).ToList();
            var counts = await _context.OrderTypes
                .AsNoTracking()
                .Where(ot => ot.ParentOrderTypeId != null && parentIds.Contains(ot.ParentOrderTypeId.Value))
                .GroupBy(ot => ot.ParentOrderTypeId!.Value)
                .Select(g => new { ParentId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var countMap = counts.ToDictionary(c => c.ParentId, c => c.Count);
            foreach (var d in dtos)
                d.ChildCount = countMap.GetValueOrDefault(d.Id, 0);
        }
        return dtos;
    }

    /// <summary>Get subtypes for a parent. Returns only direct children (ParentOrderTypeId == parentId). If parentId is not a top-level parent, returns empty list.</summary>
    public async Task<List<OrderTypeDto>> GetSubtypesAsync(Guid parentId, Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var parentExists = await _context.OrderTypes
            .AsNoTracking()
            .AnyAsync(ot => ot.Id == parentId && ot.ParentOrderTypeId == null, cancellationToken);
        if (!parentExists)
            return new List<OrderTypeDto>();

        var query = _context.OrderTypes
            .AsNoTracking()
            .Where(ot => ot.ParentOrderTypeId == parentId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(ot => ot.CompanyId == companyId.Value);
        if (isActive.HasValue)
            query = query.Where(ot => ot.IsActive == isActive.Value);

        var list = await query
            .OrderBy(ot => ot.DisplayOrder)
            .ThenBy(ot => ot.Name)
            .ToListAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<OrderTypeDto?> GetOrderTypeByCodeAsync(Guid? companyId, Guid? departmentId, string code, Guid? parentOrderTypeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var query = _context.OrderTypes.Where(ot => ot.Code == code.Trim());
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(ot => ot.CompanyId == companyId.Value);
        if (departmentId.HasValue)
            query = query.Where(ot => ot.DepartmentId == departmentId.Value);
        if (parentOrderTypeId.HasValue)
            query = query.Where(ot => ot.ParentOrderTypeId == parentOrderTypeId.Value);
        else
            query = query.Where(ot => ot.ParentOrderTypeId == null);
        var orderType = await query.FirstOrDefaultAsync(cancellationToken);
        return orderType != null ? MapToDto(orderType) : null;
    }

    public async Task<OrderTypeDto?> GetOrderTypeByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderTypes.Where(ot => ot.Id == id);
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(ot => ot.CompanyId == companyId.Value);
        }
        
        var orderType = await query.FirstOrDefaultAsync(cancellationToken);

        return orderType != null ? MapToDto(orderType) : null;
    }

    public async Task<OrderTypeDto> CreateOrderTypeAsync(CreateOrderTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var code = (dto.Code ?? "").Trim();
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Code is required.", nameof(dto));

        // Subtype creation requires parentOrderTypeId; parent creation has ParentOrderTypeId null.
        if (dto.ParentOrderTypeId.HasValue)
        {
            var parent = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == dto.ParentOrderTypeId.Value && ot.ParentOrderTypeId == null, cancellationToken);
            if (parent == null)
                throw new InvalidOperationException("Parent order type not found or is not a parent.");
            // Duplicate subtype code under same parent (same company)
            var duplicateSubtype = await _context.OrderTypes
                .AnyAsync(ot => ot.CompanyId == companyId && ot.ParentOrderTypeId == dto.ParentOrderTypeId && ot.Code == code, cancellationToken);
            if (duplicateSubtype)
                throw new InvalidOperationException($"A subtype with code \"{code}\" already exists under this parent.");
        }
        else
        {
            // Duplicate parent code (same company)
            var duplicateParent = await _context.OrderTypes
                .AnyAsync(ot => ot.CompanyId == companyId && ot.ParentOrderTypeId == null && ot.Code == code, cancellationToken);
            if (duplicateParent)
                throw new InvalidOperationException($"A parent order type with code \"{code}\" already exists.");
        }

        var orderType = new OrderType
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DepartmentId = dto.DepartmentId,
            ParentOrderTypeId = dto.ParentOrderTypeId,
            Name = dto.Name ?? "",
            Code = code,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrderTypes.Add(orderType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OrderType created: {OrderTypeId}, Name: {Name}", orderType.Id, orderType.Name);

        return MapToDto(orderType);
    }

    public async Task<OrderTypeDto> UpdateOrderTypeAsync(Guid id, UpdateOrderTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderTypes.Where(ot => ot.Id == id);
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(ot => ot.CompanyId == companyId.Value);
        }
        
        var orderType = await query.FirstOrDefaultAsync(cancellationToken);

        if (orderType == null)
        {
            throw new KeyNotFoundException("Order type not found.");
        }

        // Prevent self-parent
        if (dto.ParentOrderTypeId.HasValue && dto.ParentOrderTypeId.Value == id)
            throw new InvalidOperationException("An order type cannot be its own parent.");

        // Prevent circular parent: new parent must not be this type or any of its descendants
        if (dto.ParentOrderTypeId.HasValue)
        {
            var descendantIds = await GetDescendantIdsAsync(id, cancellationToken);
            if (descendantIds.Contains(dto.ParentOrderTypeId.Value))
                throw new InvalidOperationException("Cannot set parent: would create a circular relationship.");
            var parent = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == dto.ParentOrderTypeId.Value && ot.ParentOrderTypeId == null, cancellationToken);
            if (parent == null)
                throw new InvalidOperationException("Parent order type not found or is not a parent.");
        }

        // Duplicate code check when code is being changed
        if (!string.IsNullOrEmpty(dto.Code))
        {
            var newCode = dto.Code.Trim();
            var sameScope = orderType.ParentOrderTypeId.HasValue
                ? _context.OrderTypes.Where(ot => ot.CompanyId == orderType.CompanyId && ot.ParentOrderTypeId == orderType.ParentOrderTypeId && ot.Code == newCode && ot.Id != id)
                : _context.OrderTypes.Where(ot => ot.CompanyId == orderType.CompanyId && ot.ParentOrderTypeId == null && ot.Code == newCode && ot.Id != id);
            if (await sameScope.AnyAsync(cancellationToken))
                throw new InvalidOperationException($"Another order type with code \"{newCode}\" already exists in this scope.");
        }

        if (dto.DepartmentId.HasValue) orderType.DepartmentId = dto.DepartmentId;
        // Only update ParentOrderTypeId when explicitly provided; never clear it when editing a subtype (prevents accidental detach).
        if (dto.ParentOrderTypeId.HasValue)
            orderType.ParentOrderTypeId = dto.ParentOrderTypeId;
        if (!string.IsNullOrEmpty(dto.Name)) orderType.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Code)) orderType.Code = dto.Code;
        if (dto.Description != null) orderType.Description = dto.Description;
        if (dto.IsActive.HasValue) orderType.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) orderType.DisplayOrder = dto.DisplayOrder.Value;
        orderType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OrderType updated: {OrderTypeId}", id);

        return MapToDto(orderType);
    }

    public async Task DeleteOrderTypeAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.OrderTypes.Where(ot => ot.Id == id);
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(ot => ot.CompanyId == companyId.Value);
        }
        
        var orderType = await query.FirstOrDefaultAsync(cancellationToken);

        if (orderType == null)
        {
            throw new KeyNotFoundException($"OrderType with ID {id} not found");
        }

        // Check all tables that reference OrderType so we can report what is blocking delete.
        var ordersCount = await _context.Orders.CountAsync(o => o.OrderTypeId == id, cancellationToken);
        var jobEarningCount = await _context.JobEarningRecords.CountAsync(j => j.OrderTypeId == id, cancellationToken);
        var buildingDefaultMaterialCount = await _context.BuildingDefaultMaterials.CountAsync(b => b.OrderTypeId == id, cancellationToken);
        var billingRatecardCount = await _context.BillingRatecards.CountAsync(b => b.OrderTypeId == id, cancellationToken);
        var gponPartnerJobRateCount = await _context.GponPartnerJobRates.CountAsync(g => g.OrderTypeId == id, cancellationToken);
        var gponSiJobRateCount = await _context.GponSiJobRates.CountAsync(g => g.OrderTypeId == id, cancellationToken);
        var gponSiCustomRateCount = await _context.GponSiCustomRates.CountAsync(g => g.OrderTypeId == id, cancellationToken);
        var parserTemplateCount = await _context.ParserTemplates.CountAsync(pt => pt.OrderTypeId == id, cancellationToken);

        var refs = new List<string>();
        if (ordersCount > 0) refs.Add($"{ordersCount} order(s)");
        if (jobEarningCount > 0) refs.Add($"{jobEarningCount} job earning record(s)");
        if (buildingDefaultMaterialCount > 0) refs.Add($"{buildingDefaultMaterialCount} building default material(s)");
        if (billingRatecardCount > 0) refs.Add($"{billingRatecardCount} billing ratecard(s)");
        if (gponPartnerJobRateCount > 0) refs.Add($"{gponPartnerJobRateCount} partner job rate(s)");
        if (gponSiJobRateCount > 0) refs.Add($"{gponSiJobRateCount} SI job rate(s)");
        if (gponSiCustomRateCount > 0) refs.Add($"{gponSiCustomRateCount} SI custom rate(s)");
        if (parserTemplateCount > 0) refs.Add($"{parserTemplateCount} parser template(s)");

        if (refs.Count > 0)
            throw new InvalidOperationException($"Order type cannot be deleted because it is used by: {string.Join(", ", refs)}. Reassign those records to the correct order type, or run the repair script (see backend/scripts/repair-duplicate-assurance-parent.sql) to merge duplicate parents.");

        if (orderType.ParentOrderTypeId == null)
        {
            var hasChildren = await _context.OrderTypes.AnyAsync(ot => ot.ParentOrderTypeId == id, cancellationToken);
            if (hasChildren)
                throw new InvalidOperationException("Cannot delete a parent that has subtypes. Delete or reassign the subtypes first.");
        }

        _context.OrderTypes.Remove(orderType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OrderType deleted: {OrderTypeId}", id);
    }

    /// <summary>Returns all descendant IDs (children, grandchildren, etc.) of the given order type.</summary>
    private async Task<HashSet<Guid>> GetDescendantIdsAsync(Guid parentId, CancellationToken cancellationToken)
    {
        var result = new HashSet<Guid>();
        var current = new List<Guid> { parentId };
        while (current.Count > 0)
        {
            var children = await _context.OrderTypes
                .Where(ot => ot.ParentOrderTypeId != null && current.Contains(ot.ParentOrderTypeId!.Value))
                .Select(ot => ot.Id)
                .ToListAsync(cancellationToken);
            foreach (var c in children)
                result.Add(c);
            current = children;
        }
        return result;
    }

    private static OrderTypeDto MapToDto(OrderType orderType)
    {
        return new OrderTypeDto
        {
            Id = orderType.Id,
            CompanyId = orderType.CompanyId,
            DepartmentId = orderType.DepartmentId,
            ParentOrderTypeId = orderType.ParentOrderTypeId,
            Name = orderType.Name,
            Code = orderType.Code,
            Description = orderType.Description,
            IsActive = orderType.IsActive,
            DisplayOrder = orderType.DisplayOrder,
            CreatedAt = orderType.CreatedAt,
            UpdatedAt = orderType.UpdatedAt,
            ChildCount = 0
        };
    }
}

