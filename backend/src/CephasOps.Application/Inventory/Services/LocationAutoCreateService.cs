using CephasOps.Domain.Inventory.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Inventory.Services;

/// <summary>
/// Service for auto-creating stock locations based on triggers (Service Installer, Building, Warehouse)
/// </summary>
public interface ILocationAutoCreateService
{
    /// <summary>
    /// Auto-create location for a service installer
    /// </summary>
    Task<StockLocation?> CreateLocationForServiceInstallerAsync(
        Guid companyId,
        Guid serviceInstallerId,
        string serviceInstallerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-create location for a building
    /// </summary>
    Task<StockLocation?> CreateLocationForBuildingAsync(
        Guid companyId,
        Guid buildingId,
        string buildingName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-create location for a warehouse
    /// </summary>
    Task<StockLocation?> CreateLocationForWarehouseAsync(
        Guid companyId,
        Guid warehouseId,
        string warehouseName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of location auto-create service
/// </summary>
public class LocationAutoCreateService : ILocationAutoCreateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LocationAutoCreateService> _logger;

    public LocationAutoCreateService(
        ApplicationDbContext context,
        ILogger<LocationAutoCreateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StockLocation?> CreateLocationForServiceInstallerAsync(
        Guid companyId,
        Guid serviceInstallerId,
        string serviceInstallerName,
        CancellationToken cancellationToken = default)
    {
        // Check if location already exists
        var existingLocation = await _context.StockLocations
            .FirstOrDefaultAsync(
                l => l.CompanyId == companyId &&
                     l.LinkedServiceInstallerId == serviceInstallerId &&
                     !l.IsDeleted,
                cancellationToken);

        if (existingLocation != null)
        {
            _logger.LogDebug("Location already exists for SI {ServiceInstallerId}", serviceInstallerId);
            return existingLocation;
        }

        // Find LocationType for SI
        var locationType = await _context.LocationTypes
            .FirstOrDefaultAsync(
                lt => lt.CompanyId == companyId &&
                      lt.Code == "SI" &&
                      lt.IsActive &&
                      lt.AutoCreate &&
                      lt.AutoCreateTrigger == "ServiceInstallerCreated",
                cancellationToken);

        if (locationType == null)
        {
            _logger.LogWarning("No auto-create LocationType found for SI. Code=SI, Trigger=ServiceInstallerCreated");
            return null;
        }

        // Create location
        var location = new StockLocation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = $"SI: {serviceInstallerName}",
            Type = "SI", // Legacy field
            LocationTypeId = locationType.Id,
            LinkedServiceInstallerId = serviceInstallerId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.StockLocations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Auto-created location {LocationId} for SI {ServiceInstallerId}", location.Id, serviceInstallerId);
        return location;
    }

    public async Task<StockLocation?> CreateLocationForBuildingAsync(
        Guid companyId,
        Guid buildingId,
        string buildingName,
        CancellationToken cancellationToken = default)
    {
        // Check if location already exists
        var existingLocation = await _context.StockLocations
            .FirstOrDefaultAsync(
                l => l.CompanyId == companyId &&
                     l.LinkedBuildingId == buildingId &&
                     !l.IsDeleted,
                cancellationToken);

        if (existingLocation != null)
        {
            _logger.LogDebug("Location already exists for Building {BuildingId}", buildingId);
            return existingLocation;
        }

        // Find LocationType for CustomerSite
        var locationType = await _context.LocationTypes
            .FirstOrDefaultAsync(
                lt => lt.CompanyId == companyId &&
                      lt.Code == "CustomerSite" &&
                      lt.IsActive &&
                      lt.AutoCreate &&
                      lt.AutoCreateTrigger == "BuildingCreated",
                cancellationToken);

        if (locationType == null)
        {
            _logger.LogWarning("No auto-create LocationType found for Building. Code=CustomerSite, Trigger=BuildingCreated");
            return null;
        }

        // Create location
        var location = new StockLocation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = $"Customer Site: {buildingName}",
            Type = "CustomerSite", // Legacy field
            LocationTypeId = locationType.Id,
            LinkedBuildingId = buildingId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.StockLocations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Auto-created location {LocationId} for Building {BuildingId}", location.Id, buildingId);
        return location;
    }

    public async Task<StockLocation?> CreateLocationForWarehouseAsync(
        Guid companyId,
        Guid warehouseId,
        string warehouseName,
        CancellationToken cancellationToken = default)
    {
        // Check if location already exists
        var existingLocation = await _context.StockLocations
            .FirstOrDefaultAsync(
                l => l.CompanyId == companyId &&
                     l.WarehouseId == warehouseId &&
                     !l.IsDeleted,
                cancellationToken);

        if (existingLocation != null)
        {
            _logger.LogDebug("Location already exists for Warehouse {WarehouseId}", warehouseId);
            return existingLocation;
        }

        // Find LocationType for Warehouse
        var locationType = await _context.LocationTypes
            .FirstOrDefaultAsync(
                lt => lt.CompanyId == companyId &&
                      lt.Code == "Warehouse" &&
                      lt.IsActive &&
                      lt.AutoCreate &&
                      lt.AutoCreateTrigger == "WarehouseCreated",
                cancellationToken);

        if (locationType == null)
        {
            _logger.LogWarning("No auto-create LocationType found for Warehouse. Code=Warehouse, Trigger=WarehouseCreated");
            return null;
        }

        // Create location
        var location = new StockLocation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = $"Warehouse: {warehouseName}",
            Type = "Warehouse", // Legacy field
            LocationTypeId = locationType.Id,
            WarehouseId = warehouseId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.StockLocations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Auto-created location {LocationId} for Warehouse {WarehouseId}", location.Id, warehouseId);
        return location;
    }
}

