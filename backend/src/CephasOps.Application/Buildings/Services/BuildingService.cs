using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Inventory.Services;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// Building service implementation
/// </summary>
public class BuildingService : IBuildingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BuildingService> _logger;
    private readonly ILocationAutoCreateService? _locationAutoCreateService;

    public BuildingService(
        ApplicationDbContext context,
        ILogger<BuildingService> logger,
        ILocationAutoCreateService? locationAutoCreateService = null)
    {
        _context = context;
        _logger = logger;
        _locationAutoCreateService = locationAutoCreateService;
    }

    #region Buildings

    public async Task<List<BuildingListItemDto>> GetBuildingsAsync(
        Guid? companyId, 
        string? propertyType = null,
        Guid? installationMethodId = null,
        string? state = null,
        string? city = null,
        bool? isActive = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Buildings.AsQueryable();
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(b => b.CompanyId == companyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(propertyType))
        {
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            query = query.Where(b => b.PropertyType == propertyType);
#pragma warning restore CS0618
        }

        if (installationMethodId.HasValue)
        {
            query = query.Where(b => b.InstallationMethodId == installationMethodId.Value);
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(b => b.State == state);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(b => b.City == city);
        }

        if (isActive.HasValue)
        {
            query = query.Where(b => b.IsActive == isActive.Value);
        }

        var buildings = await query
            .OrderBy(b => b.Name)
            .Select(b => new BuildingListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                PropertyType = b.PropertyType,
#pragma warning restore CS0618
                InstallationMethodId = b.InstallationMethodId,
                InstallationMethodName = _context.InstallationMethods
                    .Where(im => im.Id == b.InstallationMethodId)
                    .Select(im => im.Name)
                    .FirstOrDefault(),
                BuildingTypeId = b.BuildingTypeId,
                BuildingTypeName = _context.BuildingTypes
                    .Where(bt => bt.Id == b.BuildingTypeId)
                    .Select(bt => bt.Name)
                    .FirstOrDefault(),
                City = b.City,
                State = b.State,
                Area = b.Area,
                RfbAssignedDate = b.RfbAssignedDate,
                FirstOrderDate = b.FirstOrderDate,
                IsActive = b.IsActive,
                OrdersCount = _context.Orders.Count(o => o.BuildingId == b.Id)
            })
            .ToListAsync(cancellationToken);

        return buildings;
    }

    public async Task<BuildingDetailDto?> GetBuildingByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Buildings.Where(b => b.Id == id);
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(b => b.CompanyId == companyId.Value);
        }

        var building = await query.FirstOrDefaultAsync(cancellationToken);

        if (building == null)
        {
            return null;
        }

        // Get related data
        var installationMethod = building.InstallationMethodId.HasValue
            ? await _context.InstallationMethods.FirstOrDefaultAsync(im => im.Id == building.InstallationMethodId.Value, cancellationToken)
            : null;

        var buildingType = building.BuildingTypeId.HasValue
            ? await _context.BuildingTypes.FirstOrDefaultAsync(bt => bt.Id == building.BuildingTypeId.Value, cancellationToken)
            : null;

        var department = building.DepartmentId.HasValue
            ? await _context.Departments.FirstOrDefaultAsync(d => d.Id == building.DepartmentId.Value, cancellationToken)
            : null;

        var contacts = await _context.BuildingContacts
            .Where(c => c.BuildingId == id)
            .OrderBy(c => c.Role)
            .ThenBy(c => c.Name)
            .Select(c => new BuildingContactDto
            {
                Id = c.Id,
                BuildingId = c.BuildingId,
                Role = c.Role,
                Name = c.Name,
                Phone = c.Phone,
                Email = c.Email,
                Remarks = c.Remarks,
                IsPrimary = c.IsPrimary,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var rules = await _context.BuildingRules
            .Where(r => r.BuildingId == id)
            .Select(r => new BuildingRulesDto
            {
                Id = r.Id,
                BuildingId = r.BuildingId,
                AccessRules = r.AccessRules,
                InstallationRules = r.InstallationRules,
                OtherNotes = r.OtherNotes,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        var ordersCount = await _context.Orders.CountAsync(o => o.BuildingId == id, cancellationToken);

        return new BuildingDetailDto
        {
            Id = building.Id,
            CompanyId = building.CompanyId,
            Name = building.Name,
            Code = building.Code,
            AddressLine1 = building.AddressLine1,
            AddressLine2 = building.AddressLine2,
            City = building.City,
            State = building.State,
            Postcode = building.Postcode,
            Area = building.Area,
            Latitude = building.Latitude,
            Longitude = building.Longitude,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            PropertyType = building.PropertyType,
#pragma warning restore CS0618
            BuildingTypeId = building.BuildingTypeId,
            BuildingTypeName = buildingType?.Name,
            InstallationMethodId = building.InstallationMethodId,
            InstallationMethodName = installationMethod?.Name,
            InstallationMethodCode = installationMethod?.Code,
            DepartmentId = building.DepartmentId,
            DepartmentName = department?.Name,
            RfbAssignedDate = building.RfbAssignedDate,
            FirstOrderDate = building.FirstOrderDate,
            Notes = building.Notes,
            IsActive = building.IsActive,
            CreatedAt = building.CreatedAt,
            UpdatedAt = building.UpdatedAt,
            ContactsCount = contacts.Count,
            OrdersCount = ordersCount,
            Contacts = contacts,
            Rules = rules
        };
    }

    public async Task<BuildingDto> CreateBuildingAsync(CreateBuildingDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Check for duplicate building by name (case-insensitive)
        var duplicateByNameQuery = _context.Buildings
            .Where(b => EF.Functions.ILike(b.Name, dto.Name.Trim()));
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            duplicateByNameQuery = duplicateByNameQuery.Where(b => b.CompanyId == companyId.Value);
        }
        
        var duplicateByName = await duplicateByNameQuery.FirstOrDefaultAsync(cancellationToken);
        if (duplicateByName != null)
        {
            _logger.LogWarning("Duplicate building found by name: {BuildingId}, Name: {Name}", duplicateByName.Id, duplicateByName.Name);
            throw new InvalidOperationException($"A building with the name '{dto.Name}' already exists (ID: {duplicateByName.Id}).");
        }
        
        // Check for duplicate building by address (AddressLine1 + City + Postcode)
        var duplicateByAddressQuery = _context.Buildings
            .Where(b => EF.Functions.ILike(b.AddressLine1, dto.AddressLine1.Trim()) 
                     && EF.Functions.ILike(b.City, dto.City.Trim()) 
                     && b.Postcode == dto.Postcode.Trim());
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            duplicateByAddressQuery = duplicateByAddressQuery.Where(b => b.CompanyId == companyId.Value);
        }
        
        var duplicateByAddress = await duplicateByAddressQuery.FirstOrDefaultAsync(cancellationToken);
        if (duplicateByAddress != null)
        {
            static string FormatAddress(string? line1, string? city)
            {
                var parts = new[] { line1?.Trim(), city?.Trim() }.Where(s => !string.IsNullOrEmpty(s));
                return string.Join(", ", parts);
            }
            var addressDisplay = FormatAddress(dto.AddressLine1, dto.City);
            var existingAddressDisplay = FormatAddress(duplicateByAddress.AddressLine1, duplicateByAddress.City);
            _logger.LogWarning("Duplicate building found by address: {BuildingId}, Address: {Address}",
                duplicateByAddress.Id, existingAddressDisplay);
            throw new InvalidOperationException($"A building at '{addressDisplay}' already exists: '{duplicateByAddress.Name}' (ID: {duplicateByAddress.Id}).");
        }

        // Validate BuildingTypeId if provided
        if (dto.BuildingTypeId.HasValue)
        {
            var buildingTypeExists = await _context.BuildingTypes
                .AnyAsync(bt => bt.Id == dto.BuildingTypeId.Value, cancellationToken);
            if (!buildingTypeExists)
            {
                throw new InvalidOperationException($"BuildingType with ID '{dto.BuildingTypeId.Value}' does not exist.");
            }
        }

        if (dto.InstallationMethodId.HasValue)
        {
            var installationMethodExists = await _context.InstallationMethods
                .AnyAsync(im => im.Id == dto.InstallationMethodId.Value, cancellationToken);
            if (!installationMethodExists)
            {
                throw new InvalidOperationException($"InstallationMethod with ID '{dto.InstallationMethodId.Value}' does not exist.");
            }
        }

        if (dto.DepartmentId.HasValue)
        {
            var departmentExists = await _context.Departments
                .AnyAsync(d => d.Id == dto.DepartmentId.Value, cancellationToken);
            if (!departmentExists)
            {
                throw new InvalidOperationException($"Department with ID '{dto.DepartmentId.Value}' does not exist.");
            }
        }

        var building = new Building
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name,
            Code = dto.Code,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            State = dto.State,
            Postcode = dto.Postcode,
            Area = dto.Area,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            PropertyType = dto.PropertyType,
#pragma warning restore CS0618
            BuildingTypeId = dto.BuildingTypeId,
            InstallationMethodId = dto.InstallationMethodId,
            DepartmentId = dto.DepartmentId,
            RfbAssignedDate = dto.RfbAssignedDate,
            FirstOrderDate = dto.FirstOrderDate,
            Notes = dto.Notes,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Buildings.Add(building);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Building created: {BuildingId}, Name: {Name}", building.Id, building.Name);

        // Auto-create stock location if service is available
        if (_locationAutoCreateService != null && companyId.HasValue)
        {
            try
            {
                await _locationAutoCreateService.CreateLocationForBuildingAsync(
                    companyId.Value,
                    building.Id,
                    building.Name,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-create location for Building {BuildingId}", building.Id);
                // Don't fail the building creation if location creation fails
            }
        }

        return MapToDto(building);
    }

    public async Task<BuildingDto> UpdateBuildingAsync(Guid id, UpdateBuildingDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Buildings.Where(b => b.Id == id);
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(b => b.CompanyId == companyId.Value);
        }

        var building = await query.FirstOrDefaultAsync(cancellationToken);

        if (building == null)
        {
            throw new KeyNotFoundException($"Building with ID {id} not found");
        }

        if (dto.Name != null) building.Name = dto.Name;
        if (dto.Code != null) building.Code = dto.Code;
        if (dto.AddressLine1 != null) building.AddressLine1 = dto.AddressLine1;
        if (dto.AddressLine2 != null) building.AddressLine2 = dto.AddressLine2;
        if (dto.City != null) building.City = dto.City;
        if (dto.State != null) building.State = dto.State;
        if (dto.Postcode != null) building.Postcode = dto.Postcode;
        if (dto.Area != null) building.Area = dto.Area;
        if (dto.Latitude.HasValue) building.Latitude = dto.Latitude;
        if (dto.Longitude.HasValue) building.Longitude = dto.Longitude;
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
        if (dto.PropertyType != null) building.PropertyType = dto.PropertyType;
#pragma warning restore CS0618
        if (dto.BuildingTypeId.HasValue)
        {
            var buildingTypeExists = await _context.BuildingTypes
                .AnyAsync(bt => bt.Id == dto.BuildingTypeId.Value, cancellationToken);
            if (!buildingTypeExists)
            {
                throw new InvalidOperationException($"BuildingType with ID '{dto.BuildingTypeId.Value}' does not exist.");
            }
            building.BuildingTypeId = dto.BuildingTypeId;
        }
        if (dto.InstallationMethodId.HasValue) building.InstallationMethodId = dto.InstallationMethodId;
        if (dto.DepartmentId.HasValue) building.DepartmentId = dto.DepartmentId;
        if (dto.RfbAssignedDate.HasValue) building.RfbAssignedDate = dto.RfbAssignedDate;
        if (dto.FirstOrderDate.HasValue) building.FirstOrderDate = dto.FirstOrderDate;
        if (dto.Notes != null) building.Notes = dto.Notes;
        if (dto.IsActive.HasValue) building.IsActive = dto.IsActive.Value;

        building.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Building updated: {BuildingId}", id);

        return MapToDto(building);
    }

    public async Task DeleteBuildingAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.Buildings.Where(b => b.Id == id);
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(b => b.CompanyId == companyId.Value);
        }

        var building = await query.FirstOrDefaultAsync(cancellationToken);

        if (building == null)
        {
            throw new KeyNotFoundException($"Building with ID {id} not found");
        }

        // Check if any orders reference this building
        var hasOrders = await _context.Orders.AnyAsync(o => o.BuildingId == id, cancellationToken);
        if (hasOrders)
        {
            throw new InvalidOperationException("Cannot delete building because it has associated orders. Deactivate instead.");
        }

        // Delete related contacts and rules first
        var contacts = await _context.BuildingContacts.Where(c => c.BuildingId == id).ToListAsync(cancellationToken);
        _context.BuildingContacts.RemoveRange(contacts);

        var rules = await _context.BuildingRules.Where(r => r.BuildingId == id).ToListAsync(cancellationToken);
        _context.BuildingRules.RemoveRange(rules);

        _context.Buildings.Remove(building);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Building deleted: {BuildingId}", id);
    }

    #endregion

    #region Building Contacts

    public async Task<List<BuildingContactDto>> GetBuildingContactsAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Verify building exists
        var buildingQuery = _context.Buildings.Where(b => b.Id == buildingId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            buildingQuery = buildingQuery.Where(b => b.CompanyId == companyId.Value);
        }
        var buildingExists = await buildingQuery.AnyAsync(cancellationToken);
        if (!buildingExists)
        {
            throw new KeyNotFoundException($"Building with ID {buildingId} not found");
        }

        return await _context.BuildingContacts
            .Where(c => c.BuildingId == buildingId)
            .OrderBy(c => c.Role)
            .ThenBy(c => c.Name)
            .Select(c => new BuildingContactDto
            {
                Id = c.Id,
                BuildingId = c.BuildingId,
                Role = c.Role,
                Name = c.Name,
                Phone = c.Phone,
                Email = c.Email,
                Remarks = c.Remarks,
                IsPrimary = c.IsPrimary,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BuildingContactDto> CreateBuildingContactAsync(Guid buildingId, SaveBuildingContactDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Verify building exists
        var buildingQuery = _context.Buildings.Where(b => b.Id == buildingId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            buildingQuery = buildingQuery.Where(b => b.CompanyId == companyId.Value);
        }
        var buildingExists = await buildingQuery.AnyAsync(cancellationToken);
        if (!buildingExists)
        {
            throw new KeyNotFoundException($"Building with ID {buildingId} not found");
        }

        // If this is primary, unset other primaries for same role
        if (dto.IsPrimary)
        {
            var existingPrimaries = await _context.BuildingContacts
                .Where(c => c.BuildingId == buildingId && c.Role == dto.Role && c.IsPrimary)
                .ToListAsync(cancellationToken);
            foreach (var p in existingPrimaries)
            {
                p.IsPrimary = false;
            }
        }

        var contact = new BuildingContact
        {
            Id = Guid.NewGuid(),
            BuildingId = buildingId,
            Role = dto.Role,
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Remarks = dto.Remarks,
            IsPrimary = dto.IsPrimary,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.BuildingContacts.Add(contact);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingContact created: {ContactId} for Building: {BuildingId}", contact.Id, buildingId);

        return new BuildingContactDto
        {
            Id = contact.Id,
            BuildingId = contact.BuildingId,
            Role = contact.Role,
            Name = contact.Name,
            Phone = contact.Phone,
            Email = contact.Email,
            Remarks = contact.Remarks,
            IsPrimary = contact.IsPrimary,
            IsActive = contact.IsActive,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        };
    }

    public async Task<BuildingContactDto> UpdateBuildingContactAsync(Guid buildingId, Guid contactId, SaveBuildingContactDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Verify building exists
        var buildingQuery = _context.Buildings.Where(b => b.Id == buildingId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            buildingQuery = buildingQuery.Where(b => b.CompanyId == companyId.Value);
        }
        var buildingExists = await buildingQuery.AnyAsync(cancellationToken);
        if (!buildingExists)
        {
            throw new KeyNotFoundException($"Building with ID {buildingId} not found");
        }

        var contact = await _context.BuildingContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.BuildingId == buildingId, cancellationToken);

        if (contact == null)
        {
            throw new KeyNotFoundException($"Contact with ID {contactId} not found");
        }

        // If this is primary, unset other primaries for same role
        if (dto.IsPrimary && !contact.IsPrimary)
        {
            var existingPrimaries = await _context.BuildingContacts
                .Where(c => c.BuildingId == buildingId && c.Role == dto.Role && c.IsPrimary && c.Id != contactId)
                .ToListAsync(cancellationToken);
            foreach (var p in existingPrimaries)
            {
                p.IsPrimary = false;
            }
        }

        contact.Role = dto.Role;
        contact.Name = dto.Name;
        contact.Phone = dto.Phone;
        contact.Email = dto.Email;
        contact.Remarks = dto.Remarks;
        contact.IsPrimary = dto.IsPrimary;
        contact.IsActive = dto.IsActive;
        contact.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingContact updated: {ContactId}", contactId);

        return new BuildingContactDto
        {
            Id = contact.Id,
            BuildingId = contact.BuildingId,
            Role = contact.Role,
            Name = contact.Name,
            Phone = contact.Phone,
            Email = contact.Email,
            Remarks = contact.Remarks,
            IsPrimary = contact.IsPrimary,
            IsActive = contact.IsActive,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        };
    }

    public async Task DeleteBuildingContactAsync(Guid buildingId, Guid contactId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Verify building exists
        var buildingQuery = _context.Buildings.Where(b => b.Id == buildingId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            buildingQuery = buildingQuery.Where(b => b.CompanyId == companyId.Value);
        }
        var buildingExists = await buildingQuery.AnyAsync(cancellationToken);
        if (!buildingExists)
        {
            throw new KeyNotFoundException($"Building with ID {buildingId} not found");
        }

        var contact = await _context.BuildingContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.BuildingId == buildingId, cancellationToken);

        if (contact == null)
        {
            throw new KeyNotFoundException($"Contact with ID {contactId} not found");
        }

        _context.BuildingContacts.Remove(contact);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BuildingContact deleted: {ContactId}", contactId);
    }

    #endregion

    #region Building Rules

    public async Task<BuildingRulesDto?> GetBuildingRulesAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Verify building exists
        var buildingQuery = _context.Buildings.Where(b => b.Id == buildingId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            buildingQuery = buildingQuery.Where(b => b.CompanyId == companyId.Value);
        }
        var buildingExists = await buildingQuery.AnyAsync(cancellationToken);
        if (!buildingExists)
        {
            throw new KeyNotFoundException($"Building with ID {buildingId} not found");
        }

        var rules = await _context.BuildingRules
            .FirstOrDefaultAsync(r => r.BuildingId == buildingId, cancellationToken);

        if (rules == null)
        {
            return null;
        }

        return new BuildingRulesDto
        {
            Id = rules.Id,
            BuildingId = rules.BuildingId,
            AccessRules = rules.AccessRules,
            InstallationRules = rules.InstallationRules,
            OtherNotes = rules.OtherNotes,
            CreatedAt = rules.CreatedAt,
            UpdatedAt = rules.UpdatedAt
        };
    }

    public async Task<BuildingRulesDto> SaveBuildingRulesAsync(Guid buildingId, SaveBuildingRulesDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Verify building exists
        var buildingQuery = _context.Buildings.Where(b => b.Id == buildingId);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            buildingQuery = buildingQuery.Where(b => b.CompanyId == companyId.Value);
        }
        var buildingExists = await buildingQuery.AnyAsync(cancellationToken);
        if (!buildingExists)
        {
            throw new KeyNotFoundException($"Building with ID {buildingId} not found");
        }

        var rules = await _context.BuildingRules
            .FirstOrDefaultAsync(r => r.BuildingId == buildingId, cancellationToken);

        if (rules == null)
        {
            // Create new
            rules = new BuildingRules
            {
                Id = Guid.NewGuid(),
                BuildingId = buildingId,
                AccessRules = dto.AccessRules,
                InstallationRules = dto.InstallationRules,
                OtherNotes = dto.OtherNotes,
                CreatedAt = DateTime.UtcNow
            };
            _context.BuildingRules.Add(rules);
            _logger.LogInformation("BuildingRules created for Building: {BuildingId}", buildingId);
        }
        else
        {
            // Update existing
            rules.AccessRules = dto.AccessRules;
            rules.InstallationRules = dto.InstallationRules;
            rules.OtherNotes = dto.OtherNotes;
            rules.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("BuildingRules updated for Building: {BuildingId}", buildingId);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new BuildingRulesDto
        {
            Id = rules.Id,
            BuildingId = rules.BuildingId,
            AccessRules = rules.AccessRules,
            InstallationRules = rules.InstallationRules,
            OtherNotes = rules.OtherNotes,
            CreatedAt = rules.CreatedAt,
            UpdatedAt = rules.UpdatedAt
        };
    }

    #endregion

    #region Dashboard Summary

    public async Task<BuildingsSummaryDto> GetBuildingsSummaryAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting buildings summary for dashboard");

        var buildingsQuery = _context.Buildings.AsQueryable();
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            buildingsQuery = buildingsQuery.Where(b => b.CompanyId == companyId.Value);
        }

        var buildings = await buildingsQuery.ToListAsync(cancellationToken);
        var totalBuildings = buildings.Count;
        var activeBuildings = buildings.Count(b => b.IsActive);

        // Get order counts
        var ordersQuery = _context.Orders.AsQueryable();
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            ordersQuery = ordersQuery.Where(o => o.CompanyId == companyId.Value);
        }

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        var totalOrders = await ordersQuery.CountAsync(cancellationToken);
        var ordersThisMonth = await ordersQuery.CountAsync(o => o.CreatedAt >= startOfMonth, cancellationToken);
        var ordersLastMonth = await ordersQuery.CountAsync(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt < startOfMonth, cancellationToken);
        
        var ordersGrowthPercent = ordersLastMonth > 0 
            ? Math.Round(((decimal)(ordersThisMonth - ordersLastMonth) / ordersLastMonth) * 100, 1)
            : ordersThisMonth > 0 ? 100 : 0;

        // By Property Type
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
        var byPropertyType = buildings
            .GroupBy(b => b.PropertyType ?? "Unknown")
#pragma warning restore CS0618
            .Select(g => new PropertyTypeSummaryDto
            {
                PropertyType = g.Key,
                Count = g.Count(),
                Percentage = totalBuildings > 0 ? Math.Round((decimal)g.Count() / totalBuildings * 100, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // By State
        var byState = buildings
            .GroupBy(b => b.State ?? "Unknown")
            .Select(g => new StateSummaryDto
            {
                State = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        // By Installation Method
        var installationMethodIds = buildings.Where(b => b.InstallationMethodId.HasValue).Select(b => b.InstallationMethodId!.Value).Distinct().ToList();
        var installationMethods = await _context.InstallationMethods
            .Where(im => installationMethodIds.Contains(im.Id))
            .ToDictionaryAsync(im => im.Id, cancellationToken);

        var byInstallationMethod = buildings
            .GroupBy(b => b.InstallationMethodId)
            .Select(g => new InstallationMethodSummaryDto
            {
                InstallationMethodId = g.Key,
                InstallationMethodName = g.Key.HasValue && installationMethods.TryGetValue(g.Key.Value, out var im) 
                    ? im.Name 
                    : "Not Set",
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Recent buildings
        var recentBuildings = buildings
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .Select(b => new RecentBuildingDto
            {
                Id = b.Id,
                Name = b.Name,
                State = b.State ?? "",
                CreatedAt = b.CreatedAt
            })
            .ToList();

        return new BuildingsSummaryDto
        {
            TotalBuildings = totalBuildings,
            ActiveBuildings = activeBuildings,
            TotalOrders = totalOrders,
            OrdersThisMonth = ordersThisMonth,
            OrdersLastMonth = ordersLastMonth,
            OrdersGrowthPercent = ordersGrowthPercent,
            ByPropertyType = byPropertyType,
            ByState = byState,
            ByInstallationMethod = byInstallationMethod,
            RecentBuildings = recentBuildings
        };
    }

    #endregion

    #region Building Lookup for Order Parsing

    public async Task<BuildingLookupResult> FindBuildingByAddressAsync(
        string? buildingName,
        string? addressLine1,
        string? city,
        string? state,
        string? postcode,
        Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var result = new BuildingLookupResult
        {
            DetectedBuildingName = buildingName,
            DetectedAddress = addressLine1,
            DetectedCity = city,
            DetectedState = state,
            DetectedPostcode = postcode
        };

        if (string.IsNullOrWhiteSpace(buildingName) && string.IsNullOrWhiteSpace(addressLine1))
        {
            return result; // No building info to search
        }

        var query = _context.Buildings.AsQueryable();
        
        // Filter by company if provided
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(b => b.CompanyId == companyId.Value);
        }

        // Try exact match by building name first (case-insensitive)
        if (!string.IsNullOrWhiteSpace(buildingName))
        {
            var exactMatch = await query
                .Where(b => EF.Functions.ILike(b.Name, buildingName.Trim()))
                .FirstOrDefaultAsync(cancellationToken);

            if (exactMatch != null)
            {
                result.Found = true;
                result.Building = new BuildingListItemDto
                {
                    Id = exactMatch.Id,
                    Name = exactMatch.Name,
                    Code = exactMatch.Code,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility in DTO
                    PropertyType = exactMatch.PropertyType,
#pragma warning restore CS0618
                    BuildingTypeId = exactMatch.BuildingTypeId,
                    City = exactMatch.City,
                    State = exactMatch.State,
                    Area = exactMatch.Area,
                    RfbAssignedDate = exactMatch.RfbAssignedDate,
                    FirstOrderDate = exactMatch.FirstOrderDate,
                    IsActive = exactMatch.IsActive,
                    OrdersCount = await _context.Orders.CountAsync(o => o.BuildingId == exactMatch.Id, cancellationToken)
                };
                return result;
            }
        }

        // Try fuzzy match by address components
        var fuzzyQuery = query;
        var hasFilters = false;

        if (!string.IsNullOrWhiteSpace(city))
        {
            fuzzyQuery = fuzzyQuery.Where(b => EF.Functions.ILike(b.City, $"%{city.Trim()}%"));
            hasFilters = true;
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            fuzzyQuery = fuzzyQuery.Where(b => EF.Functions.ILike(b.State, $"%{state.Trim()}%"));
            hasFilters = true;
        }

        if (!string.IsNullOrWhiteSpace(postcode))
        {
            fuzzyQuery = fuzzyQuery.Where(b => b.Postcode == postcode.Trim());
            hasFilters = true;
        }

        // If we have building name, try partial match
        if (!string.IsNullOrWhiteSpace(buildingName))
        {
            var nameParts = buildingName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length > 0)
            {
                // Match if building name contains any significant word from detected name
                var firstSignificantWord = nameParts[0];
                if (firstSignificantWord.Length >= 3)
                {
                    fuzzyQuery = fuzzyQuery.Where(b => EF.Functions.ILike(b.Name, $"%{firstSignificantWord}%"));
                    hasFilters = true;
                }
            }
        }

        if (hasFilters)
        {
            var matches = await fuzzyQuery
                .OrderBy(b => b.Name)
                .Take(10)
                .Select(b => new BuildingListItemDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Code = b.Code,
    #pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                PropertyType = b.PropertyType,
#pragma warning restore CS0618
                    City = b.City,
                    State = b.State,
                    Area = b.Area,
                    RfbAssignedDate = b.RfbAssignedDate,
                    FirstOrderDate = b.FirstOrderDate,
                    IsActive = b.IsActive,
                    OrdersCount = _context.Orders.Count(o => o.BuildingId == b.Id)
                })
                .ToListAsync(cancellationToken);

            if (matches.Count > 0)
            {
                // If only one match, consider it found
                if (matches.Count == 1)
                {
                    result.Found = true;
                    result.Building = matches[0];
                }
                else
                {
                    // Multiple matches - return as similar buildings for user to choose
                    result.SimilarBuildings = matches;
                }
            }
        }

        return result;
    }

    public async Task<List<BuildingListItemDto>> FindSimilarBuildingsAsync(
        string? buildingName,
        string? city,
        string? state,
        Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Buildings.AsQueryable();
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(b => b.CompanyId == companyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(buildingName))
        {
            var nameParts = buildingName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length > 0)
            {
                var firstWord = nameParts[0];
                if (firstWord.Length >= 3)
                {
                    query = query.Where(b => EF.Functions.ILike(b.Name, $"%{firstWord}%"));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(b => EF.Functions.ILike(b.City, $"%{city.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(b => EF.Functions.ILike(b.State, $"%{state.Trim()}%"));
        }

        return await query
            .OrderBy(b => b.Name)
            .Take(20)
            .Select(b => new BuildingListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
                PropertyType = b.PropertyType,
#pragma warning restore CS0618
                City = b.City,
                State = b.State,
                Area = b.Area,
                RfbAssignedDate = b.RfbAssignedDate,
                FirstOrderDate = b.FirstOrderDate,
                IsActive = b.IsActive,
                OrdersCount = _context.Orders.Count(o => o.BuildingId == b.Id)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BuildingListItemDto>> GetMergeCandidatesAsync(Guid buildingId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var building = await _context.Buildings
            .Where(b => b.Id == buildingId && (companyId == null || companyId == Guid.Empty || b.CompanyId == companyId))
            .Select(b => new { b.Name, b.City, b.State })
            .FirstOrDefaultAsync(cancellationToken);
        if (building == null)
            return new List<BuildingListItemDto>();

        var similar = await FindSimilarBuildingsAsync(building.Name, building.City, building.State, companyId, cancellationToken);
        return similar.Where(b => b.Id != buildingId).ToList();
    }

    public async Task<BuildingMergePreviewDto?> GetMergePreviewAsync(Guid sourceBuildingId, Guid targetBuildingId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (sourceBuildingId == targetBuildingId)
            return null;

        var source = await _context.Buildings
            .Where(b => b.Id == sourceBuildingId && (companyId == null || companyId == Guid.Empty || b.CompanyId == companyId))
            .FirstOrDefaultAsync(cancellationToken);
        var target = await _context.Buildings
            .Where(b => b.Id == targetBuildingId && (companyId == null || companyId == Guid.Empty || b.CompanyId == companyId))
            .FirstOrDefaultAsync(cancellationToken);
        if (source == null || target == null)
            return null;

        var orderIdsToMove = await _context.Orders
            .Where(o => o.BuildingId == sourceBuildingId)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);
        var parsedDraftsToReassignCount = await _context.ParsedOrderDrafts
            .CountAsync(d => d.BuildingId == sourceBuildingId, cancellationToken);

        return new BuildingMergePreviewDto
        {
            SourceBuildingId = source.Id,
            SourceBuildingName = source.Name,
            TargetBuildingId = target.Id,
            TargetBuildingName = target.Name,
            OrdersToReassignCount = orderIdsToMove.Count,
            OrderIdsToReassign = orderIdsToMove,
            ParsedDraftsToReassignCount = parsedDraftsToReassignCount
        };
    }

    public async Task<BuildingMergeResultDto> MergeBuildingsAsync(Guid sourceBuildingId, Guid targetBuildingId, Guid userId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (sourceBuildingId == targetBuildingId)
            throw new InvalidOperationException("Source and target building must be different.");

        var source = await _context.Buildings
            .Where(b => b.Id == sourceBuildingId && (companyId == null || companyId == Guid.Empty || b.CompanyId == companyId))
            .FirstOrDefaultAsync(cancellationToken);
        var target = await _context.Buildings
            .Where(b => b.Id == targetBuildingId && (companyId == null || companyId == Guid.Empty || b.CompanyId == companyId))
            .FirstOrDefaultAsync(cancellationToken);

        if (source == null)
            throw new KeyNotFoundException($"Source building {sourceBuildingId} not found.");
        if (target == null)
            throw new KeyNotFoundException($"Target building {targetBuildingId} not found.");

        var ordersToUpdate = await _context.Orders
            .Where(o => o.BuildingId == sourceBuildingId)
            .ToListAsync(cancellationToken);
        var ordersMovedCount = ordersToUpdate.Count;

        foreach (var order in ordersToUpdate)
        {
            order.BuildingId = targetBuildingId;
            if (!string.IsNullOrEmpty(target.Name))
                order.BuildingName = target.Name;
        }

        var draftsToUpdate = await _context.ParsedOrderDrafts
            .Where(d => d.BuildingId == sourceBuildingId)
            .ToListAsync(cancellationToken);
        var draftsReassignedCount = draftsToUpdate.Count;
        foreach (var draft in draftsToUpdate)
            draft.BuildingId = targetBuildingId;

        source.IsActive = false;
        source.IsDeleted = true;
        source.DeletedAt = DateTime.UtcNow;
        source.DeletedByUserId = userId;
        source.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(source.Notes))
            source.Notes = $"[Merged into {target.Name} on {DateTime.UtcNow:yyyy-MM-dd}] " + source.Notes;
        else
            source.Notes = $"[Merged into {target.Name} on {DateTime.UtcNow:yyyy-MM-dd}]";

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Building merge: {SourceId} ({SourceName}) into {TargetId} ({TargetName}), orders moved: {OrdersCount}, drafts reassigned: {DraftsCount}, source soft-deleted",
            sourceBuildingId, source.Name, targetBuildingId, target.Name, ordersMovedCount, draftsReassignedCount);

        return new BuildingMergeResultDto
        {
            OrdersMovedCount = ordersMovedCount,
            ParsedDraftsReassignedCount = draftsReassignedCount,
            SourceSoftDeleted = true,
            Message = $"Merged successfully. {ordersMovedCount} order(s) and {draftsReassignedCount} draft(s) reassigned to {target.Name}. Source building soft-deleted."
        };
    }

    #endregion

    #region Helpers

    private static BuildingDto MapToDto(Building building)
    {
        return new BuildingDto
        {
            Id = building.Id,
            CompanyId = building.CompanyId,
            Name = building.Name,
            Code = building.Code,
            AddressLine1 = building.AddressLine1,
            AddressLine2 = building.AddressLine2,
            City = building.City,
            State = building.State,
            Postcode = building.Postcode,
            Area = building.Area,
            Latitude = building.Latitude,
            Longitude = building.Longitude,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            PropertyType = building.PropertyType,
#pragma warning restore CS0618
            BuildingTypeId = building.BuildingTypeId,
            InstallationMethodId = building.InstallationMethodId,
            DepartmentId = building.DepartmentId,
            RfbAssignedDate = building.RfbAssignedDate,
            FirstOrderDate = building.FirstOrderDate,
            Notes = building.Notes,
            IsActive = building.IsActive,
            CreatedAt = building.CreatedAt,
            UpdatedAt = building.UpdatedAt
        };
    }

    #endregion
}
