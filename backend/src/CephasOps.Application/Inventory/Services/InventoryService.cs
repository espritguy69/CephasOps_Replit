using CephasOps.Application.Inventory.DTOs;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Inventory.Services;

/// <summary>
/// Inventory service implementation
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InventoryService> _logger;
    private readonly IMovementValidationService _movementValidationService;
    private readonly IStockLedgerService _stockLedgerService;

    public InventoryService(
        ApplicationDbContext context,
        ILogger<InventoryService> logger,
        IMovementValidationService movementValidationService,
        IStockLedgerService stockLedgerService)
    {
        _context = context;
        _logger = logger;
        _movementValidationService = movementValidationService;
        _stockLedgerService = stockLedgerService;
    }

    public async Task<List<MaterialDto>> GetMaterialsAsync(Guid? companyId, Guid? departmentId = null, string? category = null, string? search = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting materials for company {CompanyId}, department {DepartmentId}", companyId, departmentId);

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<MaterialDto>();

        var query = _context.Materials.Where(m => m.CompanyId == effectiveCompanyId.Value);

        // Filter by department if specified
        if (departmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(m => m.Category == category);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.ItemCode.Contains(search) || m.Description.Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(m => m.IsActive == isActive.Value);
        }

        var materials = await query
            .Include(m => m.MaterialCategory)
            .Include(m => m.MaterialVerticals)
            .Include(m => m.MaterialTags)
            .Include(m => m.MaterialAttributes)
            .OrderBy(m => m.ItemCode)
            .ToListAsync(cancellationToken);

        // Get department names for materials
        var departmentIds = materials.Where(m => m.DepartmentId.HasValue).Select(m => m.DepartmentId!.Value).Distinct().ToList();
        var departments = await _context.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        // Get partner names for materials (from both legacy PartnerId and MaterialPartners)
        var legacyPartnerIds = materials.Where(m => m.PartnerId.HasValue).Select(m => m.PartnerId!.Value).Distinct().ToList();
        var materialIds = materials.Select(m => m.Id).ToList();
        
        // Get MaterialPartners relationships
        var materialPartners = await _context.MaterialPartners
            .Where(mp => materialIds.Contains(mp.MaterialId))
            .Include(mp => mp.Partner)
            .ToListAsync(cancellationToken);
        
        var allPartnerIds = legacyPartnerIds
            .Union(materialPartners.Select(mp => mp.PartnerId))
            .Distinct()
            .ToList();
        
        var partners = allPartnerIds.Any()
            ? await _context.Partners
                .Where(p => allPartnerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        // Get category names
        var categoryIds = materials.Where(m => m.MaterialCategoryId.HasValue).Select(m => m.MaterialCategoryId!.Value).Distinct().ToList();
        var categories = categoryIds.Any()
            ? await _context.Set<Domain.Inventory.Entities.MaterialCategory>()
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        return materials.Select(m => MapToMaterialDto(m, departments, partners, materialPartners.Where(mp => mp.MaterialId == m.Id).ToList(), categories)).ToList();
    }

    public async Task<MaterialDto?> GetMaterialByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting material {MaterialId} for company {CompanyId}", id, companyId);

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var material = await _context.Materials
            .Where(m => m.Id == id && m.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (material == null) return null;

        // Get department name if exists
        Dictionary<Guid, string> departments = new();
        if (material.DepartmentId.HasValue)
        {
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == material.DepartmentId.Value, cancellationToken);
            if (department != null)
            {
                departments[material.DepartmentId.Value] = department.Name;
            }
        }

        // Get partner names (from both legacy PartnerId and MaterialPartners)
        Dictionary<Guid, string> partners = new();
        var materialPartners = await _context.MaterialPartners
            .Where(mp => mp.MaterialId == material.Id)
            .Include(mp => mp.Partner)
            .ToListAsync(cancellationToken);
        
        var allPartnerIds = new List<Guid>();
        if (material.PartnerId.HasValue)
        {
            allPartnerIds.Add(material.PartnerId.Value);
        }
        allPartnerIds.AddRange(materialPartners.Select(mp => mp.PartnerId));
        allPartnerIds = allPartnerIds.Distinct().ToList();
        
        if (allPartnerIds.Any())
        {
            var partnerEntities = await _context.Partners
                .Where(p => allPartnerIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
            
            foreach (var partner in partnerEntities)
            {
                partners[partner.Id] = partner.Name;
            }
        }

        // Get category name
        Dictionary<Guid, string> categories = new();
        if (material.MaterialCategoryId.HasValue)
        {
            var category = await _context.Set<Domain.Inventory.Entities.MaterialCategory>()
                .FirstOrDefaultAsync(c => c.Id == material.MaterialCategoryId.Value, cancellationToken);
            if (category != null)
            {
                categories[material.MaterialCategoryId.Value] = category.Name;
            }
        }

        return MapToMaterialDto(material, departments, partners, materialPartners, categories);
    }

    public async Task<MaterialDto?> GetMaterialByBarcodeAsync(string barcode, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting material by barcode {Barcode} for company {CompanyId}", barcode, companyId);

        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var material = await _context.Materials
            .Where(m => m.Barcode == barcode && m.IsActive && m.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (material == null) return null;

        // Get department name if exists
        Dictionary<Guid, string> departments = new();
        if (material.DepartmentId.HasValue)
        {
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == material.DepartmentId.Value, cancellationToken);
            if (department != null)
            {
                departments[material.DepartmentId.Value] = department.Name;
            }
        }

        // Get MaterialPartners relationships
        var materialPartners = await _context.MaterialPartners
            .Where(mp => mp.MaterialId == material.Id)
            .Include(mp => mp.Partner)
            .ToListAsync(cancellationToken);

        var allPartnerIds = materialPartners.Select(mp => mp.PartnerId).Distinct().ToList();
        
        // If no MaterialPartners but legacy PartnerId exists, include it for backward compatibility
        if (!allPartnerIds.Any() && material.PartnerId.HasValue)
        {
            allPartnerIds.Add(material.PartnerId.Value);
        }

        Dictionary<Guid, string> partners = new();
        if (allPartnerIds.Any())
        {
            var partnerEntities = await _context.Partners
                .Where(p => allPartnerIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
            
            foreach (var partner in partnerEntities)
            {
                partners[partner.Id] = partner.Name;
            }
        }

        // Get category name
        Dictionary<Guid, string> categories = new();
        if (material.MaterialCategoryId.HasValue)
        {
            var category = await _context.Set<Domain.Inventory.Entities.MaterialCategory>()
                .FirstOrDefaultAsync(c => c.Id == material.MaterialCategoryId.Value, cancellationToken);
            if (category != null)
            {
                categories[material.MaterialCategoryId.Value] = category.Name;
            }
        }

        return MapToMaterialDto(material, departments, partners, materialPartners, categories);
    }

    public async Task<MaterialDto> CreateMaterialAsync(CreateMaterialDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating material for company {CompanyId}", companyId);

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a material.");

        // Check if item code already exists within company
        var exists = await _context.Materials
            .AnyAsync(m => m.CompanyId == effectiveCompanyId.Value && m.ItemCode == dto.ItemCode, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Material with item code '{dto.ItemCode}' already exists");
        }

        // Determine partner IDs - use PartnerIds as source of truth, fallback to PartnerId for backward compatibility
        var partnerIds = dto.PartnerIds?.Any() == true 
            ? dto.PartnerIds.Distinct().ToList() 
            : (dto.PartnerId.HasValue ? new List<Guid> { dto.PartnerId.Value } : new List<Guid>());
        
        // Validation: at least one partner is required
        if (!partnerIds.Any())
        {
            throw new InvalidOperationException("At least one partner is required for a material");
        }
        
        // Validate all partner IDs exist
        var existingPartners = await _context.Partners
            .Where(p => partnerIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        
        var missingPartners = partnerIds.Except(existingPartners).ToList();
        if (missingPartners.Any())
        {
            throw new InvalidOperationException($"One or more partner IDs do not exist: {string.Join(", ", missingPartners)}");
        }

        // Validate MaterialCategoryId if provided
        if (dto.MaterialCategoryId.HasValue)
        {
            var categoryExists = await _context.Set<Domain.Inventory.Entities.MaterialCategory>()
                .AnyAsync(c => c.Id == dto.MaterialCategoryId.Value, cancellationToken);
            if (!categoryExists)
            {
                throw new InvalidOperationException($"Material category with ID '{dto.MaterialCategoryId.Value}' does not exist");
            }
        }

        // Validate MaterialVerticalIds if provided
        if (dto.MaterialVerticalIds != null && dto.MaterialVerticalIds.Any())
        {
            var existingVerticals = await _context.Set<Domain.Inventory.Entities.MaterialVertical>()
                .Where(v => dto.MaterialVerticalIds.Contains(v.Id))
                .Select(v => v.Id)
                .ToListAsync(cancellationToken);
            var missingVerticals = dto.MaterialVerticalIds.Except(existingVerticals).ToList();
            if (missingVerticals.Any())
            {
                throw new InvalidOperationException($"One or more vertical IDs do not exist: {string.Join(", ", missingVerticals)}");
            }
        }

        // Validate MaterialTagIds if provided
        if (dto.MaterialTagIds != null && dto.MaterialTagIds.Any())
        {
            var existingTags = await _context.Set<Domain.Inventory.Entities.MaterialTag>()
                .Where(t => dto.MaterialTagIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);
            var missingTags = dto.MaterialTagIds.Except(existingTags).ToList();
            if (missingTags.Any())
            {
                throw new InvalidOperationException($"One or more tag IDs do not exist: {string.Join(", ", missingTags)}");
            }
        }

        var material = new Material
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            ItemCode = dto.ItemCode,
            Description = dto.Description,
            MaterialCategoryId = dto.MaterialCategoryId,
            Category = dto.Category, // Legacy field
            IsSerialised = dto.IsSerialised,
            UnitOfMeasure = dto.UnitOfMeasure,
            DefaultCost = dto.DefaultCost,
            PartnerId = partnerIds.FirstOrDefault(), // Set first partner for backward compatibility
            DepartmentId = dto.DepartmentId,
            Barcode = dto.Barcode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Materials.Add(material);
        
        // Create MaterialPartner relationships
        foreach (var partnerId in partnerIds)
        {
            var materialPartner = new MaterialPartner
            {
                Id = Guid.NewGuid(),
                MaterialId = material.Id,
                PartnerId = partnerId,
                CompanyId = material.CompanyId ?? Guid.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.MaterialPartners.Add(materialPartner);
        }

        // Create MaterialVertical relationships (many-to-many via MaterialMaterialVerticals join table)
        if (dto.MaterialVerticalIds != null && dto.MaterialVerticalIds.Any())
        {
            var verticals = await _context.Set<Domain.Inventory.Entities.MaterialVertical>()
                .Where(v => dto.MaterialVerticalIds.Contains(v.Id))
                .ToListAsync(cancellationToken);
            material.MaterialVerticals = verticals;
        }

        // Create MaterialTag relationships (many-to-many via MaterialMaterialTags join table)
        if (dto.MaterialTagIds != null && dto.MaterialTagIds.Any())
        {
            var tags = await _context.Set<Domain.Inventory.Entities.MaterialTag>()
                .Where(t => dto.MaterialTagIds.Contains(t.Id))
                .ToListAsync(cancellationToken);
            material.MaterialTags = tags;
        }

        // Create MaterialAttribute records
        if (dto.MaterialAttributes != null && dto.MaterialAttributes.Any())
        {
            foreach (var attrDto in dto.MaterialAttributes)
            {
                var attribute = new Domain.Inventory.Entities.MaterialAttribute
                {
                    Id = Guid.NewGuid(),
                    MaterialId = material.Id,
                    Key = attrDto.Key,
                    Value = attrDto.Value,
                    DataType = attrDto.DataType ?? "String",
                    DisplayOrder = attrDto.DisplayOrder,
                    CompanyId = material.CompanyId ?? Guid.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Set<Domain.Inventory.Entities.MaterialAttribute>().Add(attribute);
            }
        }
        
        await _context.SaveChangesAsync(cancellationToken);

        // Get department and partner names for the created material
        Dictionary<Guid, string> departments = new();
        if (material.DepartmentId.HasValue)
        {
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == material.DepartmentId.Value, cancellationToken);
            if (department != null)
            {
                departments[material.DepartmentId.Value] = department.Name;
            }
        }

        var materialPartners = await _context.MaterialPartners
            .Where(mp => mp.MaterialId == material.Id)
            .Include(mp => mp.Partner)
            .ToListAsync(cancellationToken);
        
        var allPartnerIds = materialPartners.Select(mp => mp.PartnerId).Distinct().ToList();
        Dictionary<Guid, string> partners = new();
        if (allPartnerIds.Any())
        {
            var partnerEntities = await _context.Partners
                .Where(p => allPartnerIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
            
            foreach (var partner in partnerEntities)
            {
                partners[partner.Id] = partner.Name;
            }
        }

        // Get category name
        Dictionary<Guid, string> categories = new();
        if (material.MaterialCategoryId.HasValue)
        {
            var category = await _context.Set<Domain.Inventory.Entities.MaterialCategory>()
                .FirstOrDefaultAsync(c => c.Id == material.MaterialCategoryId.Value, cancellationToken);
            if (category != null)
            {
                categories[material.MaterialCategoryId.Value] = category.Name;
            }
        }

        return MapToMaterialDto(material, departments, partners, materialPartners, categories);
    }

    public async Task<MaterialDto> UpdateMaterialAsync(Guid id, UpdateMaterialDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating material {MaterialId} for company {CompanyId}", id, companyId);

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to update a material.");

        var material = await _context.Materials
            .Where(m => m.Id == id && m.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (material == null)
        {
            throw new KeyNotFoundException($"Material with ID {id} not found");
        }

        // Check item code uniqueness if changed
        if (!string.IsNullOrEmpty(dto.ItemCode) && dto.ItemCode != material.ItemCode)
        {
            var materialCompanyId = material.CompanyId; // Use the material's companyId for uniqueness check
            var exists = await _context.Materials
                .AnyAsync(m => m.CompanyId == materialCompanyId && m.ItemCode == dto.ItemCode && m.Id != id, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException($"Material with item code '{dto.ItemCode}' already exists");
            }
        }

        if (!string.IsNullOrEmpty(dto.ItemCode)) material.ItemCode = dto.ItemCode;
        if (!string.IsNullOrEmpty(dto.Description)) material.Description = dto.Description;
        if (dto.MaterialCategoryId.HasValue)
        {
            // Validate category exists
            var categoryExists = await _context.Set<Domain.Inventory.Entities.MaterialCategory>()
                .AnyAsync(c => c.Id == dto.MaterialCategoryId.Value, cancellationToken);
            if (!categoryExists)
            {
                throw new InvalidOperationException($"Material category with ID '{dto.MaterialCategoryId.Value}' does not exist");
            }
            material.MaterialCategoryId = dto.MaterialCategoryId.Value;
        }
        if (dto.Category != null) material.Category = dto.Category; // Legacy field
        if (dto.IsSerialised.HasValue) material.IsSerialised = dto.IsSerialised.Value;
        if (!string.IsNullOrEmpty(dto.UnitOfMeasure)) material.UnitOfMeasure = dto.UnitOfMeasure;
        if (dto.DefaultCost.HasValue) material.DefaultCost = dto.DefaultCost;
        if (dto.DepartmentId.HasValue) material.DepartmentId = dto.DepartmentId;
        if (dto.IsActive.HasValue) material.IsActive = dto.IsActive.Value;
        if (dto.Barcode != null) material.Barcode = dto.Barcode; // Allow setting to null to clear barcode
        material.UpdatedAt = DateTime.UtcNow;

        // Handle partner updates - PartnerIds is source of truth
        if (dto.PartnerIds != null)
        {
            var partnerIds = dto.PartnerIds.Distinct().ToList();
            
            // Validation: at least one partner is required
            if (!partnerIds.Any())
            {
                throw new InvalidOperationException("At least one partner is required for a material");
            }
            
            // Validate all partner IDs exist
            var existingPartners = await _context.Partners
                .Where(p => partnerIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
            
            var missingPartners = partnerIds.Except(existingPartners).ToList();
            if (missingPartners.Any())
            {
                throw new InvalidOperationException($"One or more partner IDs do not exist: {string.Join(", ", missingPartners)}");
            }
            
            // Remove existing MaterialPartner relationships
            var existingMaterialPartners = await _context.MaterialPartners
                .Where(mp => mp.MaterialId == material.Id)
                .ToListAsync(cancellationToken);
            _context.MaterialPartners.RemoveRange(existingMaterialPartners);
            
            // Create new MaterialPartner relationships
            foreach (var partnerId in partnerIds)
            {
                var materialPartner = new MaterialPartner
                {
                    Id = Guid.NewGuid(),
                    MaterialId = material.Id,
                    PartnerId = partnerId,
                    CompanyId = material.CompanyId ?? Guid.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.MaterialPartners.Add(materialPartner);
            }
            
            // Update legacy PartnerId for backward compatibility
            material.PartnerId = partnerIds.FirstOrDefault();
        }
        else if (dto.PartnerId.HasValue)
        {
            // Backward compatibility: if only PartnerId is provided, use it
            var partnerId = dto.PartnerId.Value;
            
            // Validate partner exists
            var partnerExists = await _context.Partners.AnyAsync(p => p.Id == partnerId, cancellationToken);
            if (!partnerExists)
            {
                throw new InvalidOperationException($"Partner with ID {partnerId} does not exist");
            }
            
            // Remove existing MaterialPartner relationships
            var existingMaterialPartners = await _context.MaterialPartners
                .Where(mp => mp.MaterialId == material.Id)
                .ToListAsync(cancellationToken);
            _context.MaterialPartners.RemoveRange(existingMaterialPartners);
            
            // Create new MaterialPartner relationship
            var materialPartner = new MaterialPartner
            {
                Id = Guid.NewGuid(),
                MaterialId = material.Id,
                PartnerId = partnerId,
                CompanyId = material.CompanyId ?? Guid.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.MaterialPartners.Add(materialPartner);
            
            material.PartnerId = partnerId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Get department name if exists
        Dictionary<Guid, string> departments = new();
        if (material.DepartmentId.HasValue)
        {
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == material.DepartmentId.Value, cancellationToken);
            if (department != null)
            {
                departments[material.DepartmentId.Value] = department.Name;
            }
        }

        // Get partner names (from both legacy PartnerId and MaterialPartners)
        var materialPartners = await _context.MaterialPartners
            .Where(mp => mp.MaterialId == material.Id)
            .Include(mp => mp.Partner)
            .ToListAsync(cancellationToken);
        
        var allPartnerIds = new List<Guid>();
        if (material.PartnerId.HasValue)
        {
            allPartnerIds.Add(material.PartnerId.Value);
        }
        allPartnerIds.AddRange(materialPartners.Select(mp => mp.PartnerId));
        allPartnerIds = allPartnerIds.Distinct().ToList();
        
        Dictionary<Guid, string> partners = new();
        if (allPartnerIds.Any())
        {
            var partnerEntities = await _context.Partners
                .Where(p => allPartnerIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
            
            foreach (var partner in partnerEntities)
            {
                partners[partner.Id] = partner.Name;
            }
        }

        // Get category name
        Dictionary<Guid, string> categories = new();
        if (material.MaterialCategoryId.HasValue)
        {
            var category = await _context.Set<Domain.Inventory.Entities.MaterialCategory>()
                .FirstOrDefaultAsync(c => c.Id == material.MaterialCategoryId.Value, cancellationToken);
            if (category != null)
            {
                categories[material.MaterialCategoryId.Value] = category.Name;
            }
        }

        return MapToMaterialDto(material, departments, partners, materialPartners, categories);
    }

    public async Task DeleteMaterialAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting material {MaterialId} for company {CompanyId}", id, companyId);

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete a material.");

        var material = await _context.Materials
            .Where(m => m.Id == id && m.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (material == null)
        {
            throw new KeyNotFoundException($"Material with ID {id} not found");
        }

        _context.Materials.Remove(material);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<StockBalanceDto>> GetStockBalancesAsync(Guid? companyId, Guid? locationId = null, Guid? materialId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting stock balances for company {CompanyId} (ledger-derived)", companyId);

        var ledgerBalances = await _stockLedgerService.GetLedgerDerivedBalancesAsync(companyId, locationId, materialId, cancellationToken);
        if (ledgerBalances.Count == 0)
            return new List<StockBalanceDto>();

        var matIds = ledgerBalances.Select(b => b.MaterialId).Distinct().ToList();
        var locIds = ledgerBalances.Select(b => b.LocationId).Distinct().ToList();
        var materials = await _context.Materials
            .Where(m => matIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m, cancellationToken);
        var locations = await _context.StockLocations
            .Where(l => locIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l, cancellationToken);

        return ledgerBalances
            .Where(b => materials.ContainsKey(b.MaterialId) && locations.ContainsKey(b.LocationId))
            .Select(b =>
            {
                var material = materials[b.MaterialId];
                var location = locations[b.LocationId];
                return new StockBalanceDto
                {
                    Id = Guid.Empty,
                    MaterialId = b.MaterialId,
                    MaterialCode = material?.ItemCode ?? string.Empty,
                    MaterialDescription = material?.Description ?? string.Empty,
                    LocationId = b.LocationId,
                    LocationName = location?.Name ?? string.Empty,
                    Quantity = b.OnHand
                };
            })
            .ToList();
    }

    public async Task<List<StockMovementDto>> GetStockMovementsAsync(Guid? companyId, Guid? materialId = null, Guid? locationId = null, string? movementType = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting stock movements for company {CompanyId}", companyId);

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<StockMovementDto>();

        var query = _context.StockMovements.Include(sm => sm.Material).Where(sm => sm.CompanyId == effectiveCompanyId.Value);

        if (materialId.HasValue)
        {
            query = query.Where(sm => sm.MaterialId == materialId.Value);
        }

        if (locationId.HasValue)
        {
            query = query.Where(sm => sm.FromLocationId == locationId || sm.ToLocationId == locationId);
        }

        if (!string.IsNullOrEmpty(movementType))
        {
            query = query.Where(sm => sm.MovementType == movementType);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(sm => sm.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(sm => sm.CreatedAt <= toDate.Value);
        }

        var movements = await query
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken);

        return movements.Select(sm => new StockMovementDto
        {
            Id = sm.Id,
            CompanyId = sm.CompanyId,
            FromLocationId = sm.FromLocationId,
            ToLocationId = sm.ToLocationId,
            MaterialId = sm.MaterialId,
            MaterialCode = sm.Material?.ItemCode ?? string.Empty,
            Quantity = sm.Quantity,
            MovementType = sm.MovementType,
            OrderId = sm.OrderId,
            ServiceInstallerId = sm.ServiceInstallerId,
            PartnerId = sm.PartnerId,
            Remarks = sm.Remarks,
            CreatedAt = sm.CreatedAt
        }).ToList();
    }

    /// <summary>Legacy write path: records to StockMovement + StockLedgerEntry only. Do NOT write to StockBalance.Quantity; ledger is the single source of truth.</summary>
    [Obsolete("Legacy write path. Prefer IStockLedgerService (ReceiveAsync, TransferAsync, IssueAsync, ReturnAsync). Ledger is the source of truth for quantities.")]
    public async Task<StockMovementDto> CreateStockMovementAsync(CreateStockMovementDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Legacy write path invoked: CreateStockMovementAsync. Quantities are ledger-only; do not reintroduce StockBalance.Quantity writes.");

        // Validate material exists (company feature removed - no company filter)
        var material = await _context.Materials
            .FirstOrDefaultAsync(m => m.Id == dto.MaterialId, cancellationToken);

        if (material == null)
        {
            throw new KeyNotFoundException($"Material with ID {dto.MaterialId} not found");
        }

        // Validate serialised material requirements
        if (material.IsSerialised)
        {
            if (string.IsNullOrWhiteSpace(dto.SerialNumber))
            {
                throw new ArgumentException("Serial number is required for serialised materials");
            }

            // For serialised materials, quantity must be 1
            if (dto.Quantity != 1)
            {
                _logger.LogWarning("Quantity for serialised material was {Quantity}, setting to 1", dto.Quantity);
                dto.Quantity = 1;
            }

            // Check if serial number already exists for this material (prevent duplicates)
            var existingSerialisedItem = await _context.SerialisedItems
                .FirstOrDefaultAsync(si => si.SerialNumber == dto.SerialNumber && si.MaterialId == dto.MaterialId, cancellationToken);

            if (existingSerialisedItem == null && dto.ToLocationId.HasValue)
            {
                // Create new SerialisedItem for incoming movements (GRN, Return, etc.)
                var newSerialisedItem = new SerialisedItem
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId ?? Guid.Empty,
                    MaterialId = dto.MaterialId,
                    SerialNumber = dto.SerialNumber,
                    CurrentLocationId = dto.ToLocationId,
                    Status = dto.ToLocationId.HasValue ? "InWarehouse" : "InTransit",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SerialisedItems.Add(newSerialisedItem);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created new SerialisedItem: {SerialisedItemId} for serial {SerialNumber}", newSerialisedItem.Id, dto.SerialNumber);
            }
        }
        else
        {
            // For non-serialised materials, serial number should be null
            if (!string.IsNullOrWhiteSpace(dto.SerialNumber))
            {
                _logger.LogWarning("Serial number provided for non-serialised material {MaterialId}, ignoring", dto.MaterialId);
                dto.SerialNumber = null;
            }

            if (dto.Quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than 0 for non-serialised materials");
            }
        }

        // Validate movement using MovementValidationService
        var validationResult = await _movementValidationService.ValidateMovementAsync(
            dto,
            null, // MovementTypeId not provided in DTO yet, will be resolved by code
            companyId,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Stock movement validation failed: {string.Join("; ", validationResult.Errors)}");
        }

        // Use MovementType from validation if available
        var movementTypeId = validationResult.MovementType?.Id;
        var movementTypeCode = validationResult.MovementType?.Code ?? dto.MovementType;

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? Guid.Empty, // Company feature removed
            FromLocationId = dto.FromLocationId,
            ToLocationId = dto.ToLocationId,
            MaterialId = dto.MaterialId,
            Quantity = dto.Quantity,
            MovementType = movementTypeCode, // Use validated code
            MovementTypeId = movementTypeId, // Set MovementTypeId if available
            OrderId = dto.OrderId,
            ServiceInstallerId = dto.ServiceInstallerId,
            PartnerId = dto.PartnerId,
            Remarks = dto.Remarks,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StockMovements.Add(movement);

        // Record equivalent ledger entry(ies); do not update StockBalance (Phase 2.1.3A)
        var dtoWithResolvedType = new CreateStockMovementDto
        {
            FromLocationId = dto.FromLocationId,
            ToLocationId = dto.ToLocationId,
            MaterialId = dto.MaterialId,
            Quantity = dto.Quantity,
            MovementType = movementTypeCode,
            OrderId = dto.OrderId,
            ServiceInstallerId = dto.ServiceInstallerId,
            PartnerId = dto.PartnerId,
            Remarks = dto.Remarks,
            SerialNumber = dto.SerialNumber
        };
        await _stockLedgerService.RecordLegacyMovementAsync(dtoWithResolvedType, companyId, userId, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new StockMovementDto
        {
            Id = movement.Id,
            CompanyId = movement.CompanyId,
            FromLocationId = movement.FromLocationId,
            ToLocationId = movement.ToLocationId,
            MaterialId = movement.MaterialId,
            MaterialCode = material.ItemCode,
            Quantity = movement.Quantity,
            MovementType = movement.MovementType,
            OrderId = movement.OrderId,
            ServiceInstallerId = movement.ServiceInstallerId,
            PartnerId = movement.PartnerId,
            Remarks = movement.Remarks,
            CreatedAt = movement.CreatedAt
        };
    }

    private static MaterialDto MapToMaterialDto(
        Material material, 
        Dictionary<Guid, string>? departments = null, 
        Dictionary<Guid, string>? partners = null,
        List<Domain.Inventory.Entities.MaterialPartner>? materialPartners = null,
        Dictionary<Guid, string>? categories = null)
    {
        departments ??= new Dictionary<Guid, string>();
        partners ??= new Dictionary<Guid, string>();
        materialPartners ??= new List<Domain.Inventory.Entities.MaterialPartner>();
        categories ??= new Dictionary<Guid, string>();
        
        // Get partner IDs from MaterialPartners (source of truth)
        var partnerIds = materialPartners.Select(mp => mp.PartnerId).Distinct().ToList();
        
        // If no MaterialPartners but legacy PartnerId exists, include it for backward compatibility
        if (!partnerIds.Any() && material.PartnerId.HasValue)
        {
            partnerIds.Add(material.PartnerId.Value);
        }
        
        var partnerNames = partnerIds
            .Where(id => partners.ContainsKey(id))
            .Select(id => partners[id])
            .ToList();
        
        // Set legacy PartnerId to first partner for backward compatibility
        var firstPartnerId = partnerIds.FirstOrDefault();
        var firstPartnerName = firstPartnerId != Guid.Empty && partners.ContainsKey(firstPartnerId)
            ? partners[firstPartnerId]
            : null;
        
        // Map MaterialCategory
        var categoryName = material.MaterialCategoryId.HasValue && categories.ContainsKey(material.MaterialCategoryId.Value)
            ? categories[material.MaterialCategoryId.Value]
            : null;

        // Map MaterialVerticals (many-to-many - MaterialVertical is the entity itself)
        var verticalIds = material.MaterialVerticals?.Select(mv => mv.Id).Distinct().ToList() ?? new List<Guid>();
        var verticalNames = material.MaterialVerticals?.Select(mv => mv.Name).Distinct().ToList() ?? new List<string>();

        // Map MaterialTags (many-to-many - MaterialTag is the entity itself)
        var tagIds = material.MaterialTags?.Select(mt => mt.Id).Distinct().ToList() ?? new List<Guid>();
        var tagNames = material.MaterialTags?.Select(mt => mt.Name).Distinct().ToList() ?? new List<string>();
        var tagColors = material.MaterialTags?.Select(mt => mt.Color ?? "").Distinct().ToList() ?? new List<string>();

        // Map MaterialAttributes
        var attributes = material.MaterialAttributes?
            .OrderBy(ma => ma.DisplayOrder)
            .Select(ma => new MaterialAttributeDto
            {
                Id = ma.Id,
                Key = ma.Key,
                Value = ma.Value,
                DataType = ma.DataType,
                DisplayOrder = ma.DisplayOrder
            })
            .ToList() ?? new List<MaterialAttributeDto>();

        return new MaterialDto
        {
            Id = material.Id,
            CompanyId = material.CompanyId,
            ItemCode = material.ItemCode,
            Description = material.Description,
            MaterialCategoryId = material.MaterialCategoryId,
            MaterialCategoryName = categoryName,
            Category = material.Category, // Legacy field
            MaterialVerticalIds = verticalIds,
            MaterialVerticalNames = verticalNames, // Empty for now - can be populated if needed
            MaterialTagIds = tagIds,
            MaterialTagNames = tagNames,
            MaterialTagColors = tagColors,
            MaterialAttributes = attributes,
            IsSerialised = material.IsSerialised,
            UnitOfMeasure = material.UnitOfMeasure,
            DefaultCost = material.DefaultCost,
            PartnerId = firstPartnerId != Guid.Empty ? firstPartnerId : material.PartnerId, // Backward compatibility
            PartnerName = firstPartnerName, // Backward compatibility
            PartnerIds = partnerIds,
            PartnerNames = partnerNames,
            DepartmentId = material.DepartmentId,
            DepartmentName = material.DepartmentId.HasValue && departments.ContainsKey(material.DepartmentId.Value) 
                ? departments[material.DepartmentId.Value] 
                : null,
            IsActive = material.IsActive,
            Barcode = material.Barcode,
            CreatedAt = material.CreatedAt
        };
    }

    public async Task<List<StockLocationDto>> GetStockLocationsAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting stock locations for company {CompanyId}", companyId);

        var query = _context.StockLocations.AsQueryable();
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(sl => sl.CompanyId == companyId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(sl => sl.IsActive == isActive.Value);
        }

        var locations = await query
            .OrderBy(sl => sl.Name)
            .ToListAsync(cancellationToken);

        return locations.Select(sl => new StockLocationDto
        {
            Id = sl.Id,
            Name = sl.Name,
            Code = string.Empty, // StockLocation entity doesn't have Code field
            Address = null, // StockLocation entity doesn't have Address field
            IsActive = sl.IsActive
        }).ToList();
    }
}

