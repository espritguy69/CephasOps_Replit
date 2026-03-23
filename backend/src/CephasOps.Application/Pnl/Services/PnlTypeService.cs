using CephasOps.Application.Pnl.DTOs;
using CephasOps.Domain.Pnl.Entities;
using CephasOps.Domain.Pnl.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Pnl.Services;

/// <summary>
/// P&amp;L Type service implementation
/// </summary>
public class PnlTypeService : IPnlTypeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PnlTypeService> _logger;

    public PnlTypeService(ApplicationDbContext context, ILogger<PnlTypeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PnlTypeDto>> GetPnlTypesAsync(Guid? companyId, PnlTypeCategory? category = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PnlTypes
            .Include(t => t.Parent)
            .AsQueryable();

        // Filter by company or get global types (null companyId)
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CompanyId == companyId.Value || t.CompanyId == null);
        }

        if (category.HasValue)
        {
            query = query.Where(t => t.Category == category.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var pnlTypes = await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return pnlTypes.Select(MapToDto).ToList();
    }

    public async Task<List<PnlTypeTreeDto>> GetPnlTypeTreeAsync(Guid? companyId, PnlTypeCategory? category = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PnlTypes.AsQueryable();

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CompanyId == companyId.Value || t.CompanyId == null);
        }

        if (category.HasValue)
        {
            query = query.Where(t => t.Category == category.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var allTypes = await query
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        // Build tree structure
        var rootTypes = allTypes.Where(t => t.ParentId == null).ToList();
        return rootTypes.Select(r => BuildTree(r, allTypes, 0)).ToList();
    }

    public async Task<PnlTypeDto?> GetPnlTypeByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.PnlTypes
            .Include(t => t.Parent)
            .Include(t => t.Children)
            .Where(t => t.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CompanyId == companyId.Value || t.CompanyId == null);
        }

        var pnlType = await query.FirstOrDefaultAsync(cancellationToken);
        return pnlType != null ? MapToDto(pnlType) : null;
    }

    public async Task<PnlTypeDto> CreatePnlTypeAsync(CreatePnlTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Check for duplicate code
        var duplicateCodeQuery = _context.PnlTypes
            .Where(t => EF.Functions.ILike(t.Code, dto.Code.Trim()));

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            duplicateCodeQuery = duplicateCodeQuery.Where(t => t.CompanyId == companyId.Value || t.CompanyId == null);
        }

        var duplicateCode = await duplicateCodeQuery.FirstOrDefaultAsync(cancellationToken);
        if (duplicateCode != null)
        {
            throw new InvalidOperationException($"A P&L type with the code '{dto.Code}' already exists.");
        }

        // Validate parent if specified (tenant-safe: scope by companyId)
        if (dto.ParentId.HasValue)
        {
            var parent = await _context.PnlTypes
                .FirstOrDefaultAsync(t => t.Id == dto.ParentId.Value && t.CompanyId == companyId, cancellationToken);
            if (parent == null)
            {
                throw new InvalidOperationException($"Parent P&L type with ID {dto.ParentId} not found.");
            }
            // Ensure parent has the same category
            if (parent.Category != dto.Category)
            {
                throw new InvalidOperationException("Child P&L type must have the same category as its parent.");
            }
        }

        var pnlType = new PnlType
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim(),
            Description = dto.Description,
            Category = dto.Category,
            ParentId = dto.ParentId,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive,
            IsTransactional = dto.IsTransactional,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PnlTypes.Add(pnlType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("PnlType created: {PnlTypeId}, Code: {Code}, Category: {Category}", pnlType.Id, pnlType.Code, pnlType.Category);

        return MapToDto(pnlType);
    }

    public async Task<PnlTypeDto> UpdatePnlTypeAsync(Guid id, UpdatePnlTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.PnlTypes
            .Include(t => t.Parent)
            .Where(t => t.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var pnlType = await query.FirstOrDefaultAsync(cancellationToken);
        if (pnlType == null)
        {
            throw new KeyNotFoundException($"P&L Type with ID {id} not found");
        }

        // Check for duplicate code
        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim() != pnlType.Code)
        {
            var duplicateCodeQuery = _context.PnlTypes
                .Where(t => t.Id != id && EF.Functions.ILike(t.Code, dto.Code.Trim()));

            if (companyId.HasValue && companyId.Value != Guid.Empty)
            {
                duplicateCodeQuery = duplicateCodeQuery.Where(t => t.CompanyId == companyId.Value || t.CompanyId == null);
            }

            var duplicateCode = await duplicateCodeQuery.FirstOrDefaultAsync(cancellationToken);
            if (duplicateCode != null)
            {
                throw new InvalidOperationException($"A P&L type with the code '{dto.Code}' already exists.");
            }
        }

        // Validate parent change
        if (dto.ParentId.HasValue && dto.ParentId != pnlType.ParentId)
        {
            // Cannot set self as parent
            if (dto.ParentId == id)
            {
                throw new InvalidOperationException("A P&L type cannot be its own parent.");
            }

            var parent = await _context.PnlTypes
                .FirstOrDefaultAsync(t => t.Id == dto.ParentId.Value && t.CompanyId == pnlType.CompanyId, cancellationToken);
            if (parent == null)
            {
                throw new InvalidOperationException($"Parent P&L type with ID {dto.ParentId} not found.");
            }
            if (parent.Category != pnlType.Category)
            {
                throw new InvalidOperationException("Child P&L type must have the same category as its parent.");
            }

            // Check for circular reference
            var descendantIds = await GetDescendantIdsAsync(id, cancellationToken);
            if (descendantIds.Contains(dto.ParentId.Value))
            {
                throw new InvalidOperationException("Cannot set a descendant as parent (circular reference).");
            }
        }

        if (!string.IsNullOrEmpty(dto.Name)) pnlType.Name = dto.Name.Trim();
        if (!string.IsNullOrEmpty(dto.Code)) pnlType.Code = dto.Code.Trim();
        if (dto.Description != null) pnlType.Description = dto.Description;
        if (dto.ParentId.HasValue) pnlType.ParentId = dto.ParentId;
        if (dto.SortOrder.HasValue) pnlType.SortOrder = dto.SortOrder.Value;
        if (dto.IsActive.HasValue) pnlType.IsActive = dto.IsActive.Value;
        if (dto.IsTransactional.HasValue) pnlType.IsTransactional = dto.IsTransactional.Value;
        pnlType.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("PnlType updated: {PnlTypeId}", id);

        return MapToDto(pnlType);
    }

    public async Task DeletePnlTypeAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.PnlTypes.Where(t => t.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CompanyId == companyId.Value);
        }

        var pnlType = await query.FirstOrDefaultAsync(cancellationToken);
        if (pnlType == null)
        {
            throw new KeyNotFoundException($"P&L Type with ID {id} not found");
        }

        // Check if has children
        var hasChildren = await _context.PnlTypes.AnyAsync(t => t.ParentId == id, cancellationToken);
        if (hasChildren)
        {
            throw new InvalidOperationException($"Cannot delete P&L Type {id} because it has child types. Delete children first.");
        }

        // Check if used in transactions (supplier invoice line items, payments, etc.)
        var isUsedInSupplierInvoices = await _context.SupplierInvoiceLineItems.AnyAsync(l => l.PnlTypeId == id, cancellationToken);
        var isUsedInPayments = await _context.Payments.AnyAsync(p => p.PnlTypeId == id, cancellationToken);

        if (isUsedInSupplierInvoices || isUsedInPayments)
        {
            throw new InvalidOperationException($"Cannot delete P&L Type {id} because it is being used in transactions.");
        }

        _context.PnlTypes.Remove(pnlType);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("PnlType deleted: {PnlTypeId}", id);
    }

    public async Task<List<PnlTypeDto>> GetTransactionalPnlTypesAsync(Guid? companyId, PnlTypeCategory? category = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PnlTypes
            .Include(t => t.Parent)
            .Where(t => t.IsTransactional && t.IsActive);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(t => t.CompanyId == companyId.Value || t.CompanyId == null);
        }

        if (category.HasValue)
        {
            query = query.Where(t => t.Category == category.Value);
        }

        var pnlTypes = await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return pnlTypes.Select(MapToDto).ToList();
    }

    private async Task<HashSet<Guid>> GetDescendantIdsAsync(Guid parentId, CancellationToken cancellationToken)
    {
        var descendants = new HashSet<Guid>();
        var toProcess = new Queue<Guid>();
        toProcess.Enqueue(parentId);

        while (toProcess.Count > 0)
        {
            var currentId = toProcess.Dequeue();
            var children = await _context.PnlTypes
                .Where(t => t.ParentId == currentId)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            foreach (var childId in children)
            {
                if (descendants.Add(childId))
                {
                    toProcess.Enqueue(childId);
                }
            }
        }

        return descendants;
    }

    private PnlTypeTreeDto BuildTree(PnlType node, List<PnlType> allTypes, int level)
    {
        var children = allTypes
            .Where(t => t.ParentId == node.Id)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToList();

        return new PnlTypeTreeDto
        {
            Id = node.Id,
            Name = node.Name,
            Code = node.Code,
            Description = node.Description,
            Category = node.Category,
            SortOrder = node.SortOrder,
            IsActive = node.IsActive,
            IsTransactional = node.IsTransactional,
            Level = level,
            Children = children.Select(c => BuildTree(c, allTypes, level + 1)).ToList()
        };
    }

    private static PnlTypeDto MapToDto(PnlType pnlType)
    {
        return new PnlTypeDto
        {
            Id = pnlType.Id,
            CompanyId = pnlType.CompanyId,
            Name = pnlType.Name,
            Code = pnlType.Code,
            Description = pnlType.Description,
            Category = pnlType.Category,
            ParentId = pnlType.ParentId,
            ParentName = pnlType.Parent?.Name,
            SortOrder = pnlType.SortOrder,
            IsActive = pnlType.IsActive,
            IsTransactional = pnlType.IsTransactional,
            CreatedAt = pnlType.CreatedAt,
            UpdatedAt = pnlType.UpdatedAt,
            Children = pnlType.Children?.Select(MapToDto).ToList() ?? new List<PnlTypeDto>()
        };
    }
}

