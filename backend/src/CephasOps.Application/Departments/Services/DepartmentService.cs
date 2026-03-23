using CephasOps.Application.Departments.DTOs;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Departments.Services;

/// <summary>
/// Department service implementation
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DepartmentService> _logger;
    private readonly IDepartmentAccessService _departmentAccessService;

    public DepartmentService(
        ApplicationDbContext context,
        ILogger<DepartmentService> logger,
        IDepartmentAccessService departmentAccessService)
    {
        _context = context;
        _logger = logger;
        _departmentAccessService = departmentAccessService;
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting departments for company {CompanyId}", companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = companyId.HasValue 
            ? _context.Departments.Include(d => d.MaterialAllocations).Where(d => d.CompanyId == companyId.Value)
            : _context.Departments.Include(d => d.MaterialAllocations).AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(d => d.IsActive == isActive.Value);
        }

        var access = await _departmentAccessService.GetAccessAsync(cancellationToken);
        if (!access.HasGlobalAccess)
        {
            if (access.DepartmentIds.Count == 0)
            {
                return new List<DepartmentDto>();
            }

            query = query.Where(d => access.DepartmentIds.Contains(d.Id));
        }

        var departments = await query
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        // Load cost centres and materials in bulk
        var costCentreIds = departments.Where(d => d.CostCentreId.HasValue).Select(d => d.CostCentreId!.Value).Distinct().ToList();
        var costCentres = await _context.CostCentres
            .Where(cc => costCentreIds.Contains(cc.Id))
            .ToDictionaryAsync(cc => cc.Id, cancellationToken);

        var materialIds = departments
            .SelectMany(d => d.MaterialAllocations)
            .Select(a => a.MaterialId)
            .Distinct()
            .ToList();
        var materials = await _context.Materials
            .Where(m => materialIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        return departments.Select(d => MapToDepartmentDto(d, costCentres, materials)).ToList();
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting department {DepartmentId} for company {CompanyId}", id, companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = _context.Departments.Include(d => d.MaterialAllocations).Where(d => d.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(d => d.CompanyId == companyId.Value);
        }

        var access = await _departmentAccessService.GetAccessAsync(cancellationToken);
        if (!access.HasGlobalAccess)
        {
            if (!access.DepartmentIds.Contains(id))
            {
                return null;
            }
        }

        var department = await query.FirstOrDefaultAsync(cancellationToken);

        if (department == null) return null;

        // Load cost centre and materials
        CostCentre? costCentre = null;
        if (department.CostCentreId.HasValue)
        {
            costCentre = await _context.CostCentres
                .FirstOrDefaultAsync(cc => cc.Id == department.CostCentreId.Value, cancellationToken);
        }

        var costCentres = costCentre != null
            ? new Dictionary<Guid, CostCentre> { [costCentre.Id] = costCentre }
            : new Dictionary<Guid, CostCentre>();

        var materialIds = department.MaterialAllocations.Select(a => a.MaterialId).Distinct().ToList();
        var materials = await _context.Materials
            .Where(m => materialIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        return MapToDepartmentDto(department, costCentres, materials);
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        _logger.LogInformation("Creating department (company feature disabled)");

        var department = new Department
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId, // Can be null now
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            CostCentreId = dto.CostCentreId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with allocations for DTO mapping
        var reloaded = await _context.Departments
            .Include(d => d.MaterialAllocations)
            .FirstOrDefaultAsync(d => d.Id == department.Id, cancellationToken);

        if (reloaded == null)
        {
            throw new InvalidOperationException("Failed to reload department after creation");
        }

        // Load cost centre and materials
        CostCentre? costCentre = null;
        if (reloaded.CostCentreId.HasValue)
        {
            costCentre = await _context.CostCentres
                .FirstOrDefaultAsync(cc => cc.Id == reloaded.CostCentreId.Value, cancellationToken);
        }

        var costCentres = costCentre != null
            ? new Dictionary<Guid, CostCentre> { [costCentre.Id] = costCentre }
            : new Dictionary<Guid, CostCentre>();

        var materialIds = reloaded.MaterialAllocations.Select(a => a.MaterialId).Distinct().ToList();
        var materials = materialIds.Any()
            ? await _context.Materials
                .Where(m => materialIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, cancellationToken)
            : new Dictionary<Guid, Material>();

        return MapToDepartmentDto(reloaded, costCentres, materials);
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(Guid id, UpdateDepartmentDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating department {DepartmentId} for company {CompanyId}", id, companyId);

        // For update operations, filter by companyId if provided
        var query = _context.Departments.Include(d => d.MaterialAllocations).Where(d => d.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(d => d.CompanyId == companyId.Value);
        }
        var department = await query.FirstOrDefaultAsync(cancellationToken);

        if (department == null)
        {
            throw new KeyNotFoundException($"Department with ID {id} not found");
        }

        await _departmentAccessService.EnsureAccessAsync(department.Id, cancellationToken);

        if (!string.IsNullOrEmpty(dto.Name)) department.Name = dto.Name;
        if (dto.Code != null) department.Code = dto.Code;
        if (dto.Description != null) department.Description = dto.Description;
        if (dto.CostCentreId.HasValue) department.CostCentreId = dto.CostCentreId;
        if (dto.IsActive.HasValue) department.IsActive = dto.IsActive.Value;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Reload with allocations for DTO mapping
        var reloaded = await _context.Departments
            .Include(d => d.MaterialAllocations)
            .FirstOrDefaultAsync(d => d.Id == department.Id, cancellationToken);

        if (reloaded == null)
        {
            throw new InvalidOperationException("Failed to reload department after update");
        }

        // Load cost centre and materials
        CostCentre? costCentre = null;
        if (reloaded.CostCentreId.HasValue)
        {
            costCentre = await _context.CostCentres
                .FirstOrDefaultAsync(cc => cc.Id == reloaded.CostCentreId.Value, cancellationToken);
        }

        var costCentres = costCentre != null
            ? new Dictionary<Guid, CostCentre> { [costCentre.Id] = costCentre }
            : new Dictionary<Guid, CostCentre>();

        var materialIds = reloaded.MaterialAllocations.Select(a => a.MaterialId).Distinct().ToList();
        var materials = materialIds.Any()
            ? await _context.Materials
                .Where(m => materialIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, cancellationToken)
            : new Dictionary<Guid, Material>();

        return MapToDepartmentDto(reloaded, costCentres, materials);
    }

    public async Task DeleteDepartmentAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting department {DepartmentId} for company {CompanyId}", id, companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = _context.Departments.Where(d => d.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(d => d.CompanyId == companyId.Value);
        }

        var department = await query.FirstOrDefaultAsync(cancellationToken);

        if (department == null)
        {
            throw new KeyNotFoundException($"Department with ID {id} not found");
        }

        await _departmentAccessService.EnsureAccessAsync(department.Id, cancellationToken);

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<MaterialAllocationDto>> GetMaterialAllocationsAsync(Guid departmentId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting material allocations for department {DepartmentId}", departmentId);

        await _departmentAccessService.EnsureAccessAsync(departmentId, cancellationToken);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = _context.MaterialAllocations.Include(a => a.Department).Where(a => a.DepartmentId == departmentId);
        if (companyId.HasValue)
        {
            query = query.Where(a => a.CompanyId == companyId.Value);
        }

        var allocations = await query.ToListAsync(cancellationToken);

        // Load materials in bulk
        var materialIds = allocations.Select(a => a.MaterialId).Distinct().ToList();
        var materials = await _context.Materials
            .Where(m => materialIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        return allocations.Select(a => new MaterialAllocationDto
        {
            Id = a.Id,
            MaterialId = a.MaterialId,
            MaterialCode = materials.TryGetValue(a.MaterialId, out var material) ? material.ItemCode : string.Empty,
            MaterialDescription = materials.TryGetValue(a.MaterialId, out var material2) ? material2.Description : string.Empty,
            Quantity = a.Quantity,
            Notes = a.Notes
        }).ToList();
    }

    public async Task<MaterialAllocationDto> CreateMaterialAllocationAsync(Guid departmentId, CreateMaterialAllocationDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating material allocation for department {DepartmentId}", departmentId);

        var query = _context.Departments.Where(d => d.Id == departmentId);
        if (companyId.HasValue)
        {
            query = query.Where(d => d.CompanyId == companyId.Value);
        }
        var department = await query.FirstOrDefaultAsync(cancellationToken);

        if (department == null)
        {
            throw new KeyNotFoundException($"Department with ID {departmentId} not found");
        }

        await _departmentAccessService.EnsureAccessAsync(departmentId, cancellationToken);

        var allocation = new MaterialAllocation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DepartmentId = departmentId,
            MaterialId = dto.MaterialId,
            Quantity = dto.Quantity,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MaterialAllocations.Add(allocation);
        await _context.SaveChangesAsync(cancellationToken);

        // Load material for DTO
        var material = await _context.Materials
            .FirstOrDefaultAsync(m => m.Id == allocation.MaterialId, cancellationToken);

        return new MaterialAllocationDto
        {
            Id = allocation.Id,
            MaterialId = allocation.MaterialId,
            MaterialCode = material?.ItemCode ?? string.Empty,
            MaterialDescription = material?.Description ?? string.Empty,
            Quantity = allocation.Quantity,
            Notes = allocation.Notes
        };
    }

    public async Task DeleteMaterialAllocationAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting material allocation {AllocationId}", id);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = _context.MaterialAllocations.Where(a => a.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(a => a.CompanyId == companyId.Value);
        }

        var allocation = await query.FirstOrDefaultAsync(cancellationToken);

        if (allocation == null)
        {
            throw new KeyNotFoundException($"Material allocation with ID {id} not found");
        }

        await _departmentAccessService.EnsureAccessAsync(allocation.DepartmentId, cancellationToken);

        _context.MaterialAllocations.Remove(allocation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static DepartmentDto MapToDepartmentDto(
        Department department, 
        Dictionary<Guid, CostCentre>? costCentres = null,
        Dictionary<Guid, Material>? materials = null)
    {
        costCentres ??= new Dictionary<Guid, CostCentre>();
        materials ??= new Dictionary<Guid, Material>();

        return new DepartmentDto
        {
            Id = department.Id,
            CompanyId = department.CompanyId,
            Name = department.Name,
            Code = department.Code,
            Description = department.Description,
            CostCentreId = department.CostCentreId,
            CostCentreName = department.CostCentreId.HasValue && costCentres.TryGetValue(department.CostCentreId.Value, out var costCentre)
                ? costCentre.Name
                : string.Empty,
            IsActive = department.IsActive,
            MaterialAllocations = department.MaterialAllocations.Select(a => new MaterialAllocationDto
            {
                Id = a.Id,
                MaterialId = a.MaterialId,
                MaterialCode = materials.TryGetValue(a.MaterialId, out var material) ? material.ItemCode : string.Empty,
                MaterialDescription = materials.TryGetValue(a.MaterialId, out var material2) ? material2.Description : string.Empty,
                Quantity = a.Quantity,
                Notes = a.Notes
            }).ToList(),
            CreatedAt = department.CreatedAt
        };
    }
}

