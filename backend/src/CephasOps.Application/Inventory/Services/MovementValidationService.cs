using CephasOps.Application.Inventory.DTOs;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Inventory.Services;

/// <summary>
/// Service for validating stock movement rules based on MovementType configuration
/// Validates that stock movements comply with the configured MovementType requirements
/// </summary>
public class MovementValidationService : IMovementValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MovementValidationService> _logger;

    public MovementValidationService(
        ApplicationDbContext context,
        ILogger<MovementValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MovementValidationResult> ValidateMovementAsync(
        CreateStockMovementDto dto,
        Guid? movementTypeId,
        Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        MovementType? movementType = null;

        // If MovementTypeId is provided, load it
        if (movementTypeId.HasValue)
        {
            movementType = await _context.MovementTypes
                .FirstOrDefaultAsync(
                    mt => mt.Id == movementTypeId.Value &&
                          (companyId == null || companyId == Guid.Empty || mt.CompanyId == companyId || mt.CompanyId == Guid.Empty) &&
                          mt.IsActive,
                    cancellationToken);

            if (movementType == null)
            {
                return MovementValidationResult.Failure(
                    $"MovementType with ID {movementTypeId.Value} not found or inactive",
                    null);
            }
        }
        // Otherwise, try to find by code (from legacy MovementType string field)
        else if (!string.IsNullOrEmpty(dto.MovementType))
        {
            movementType = await GetMovementTypeByCodeAsync(dto.MovementType, companyId, cancellationToken);
        }

        // If we have a MovementType, validate against its rules
        if (movementType != null)
        {
            // Validate required FromLocation
            if (movementType.RequiresFromLocation && !dto.FromLocationId.HasValue)
            {
                errors.Add($"MovementType '{movementType.Name}' requires a source location (FromLocationId)");
            }

            // Validate required ToLocation
            if (movementType.RequiresToLocation && !dto.ToLocationId.HasValue)
            {
                errors.Add($"MovementType '{movementType.Name}' requires a destination location (ToLocationId)");
            }

            // Validate required OrderId
            if (movementType.RequiresOrderId && !dto.OrderId.HasValue)
            {
                errors.Add($"MovementType '{movementType.Name}' requires an Order ID");
            }

            // Validate required ServiceInstallerId
            if (movementType.RequiresServiceInstallerId && !dto.ServiceInstallerId.HasValue)
            {
                errors.Add($"MovementType '{movementType.Name}' requires a Service Installer ID");
            }

            // Validate required PartnerId
            if (movementType.RequiresPartnerId && !dto.PartnerId.HasValue)
            {
                errors.Add($"MovementType '{movementType.Name}' requires a Partner ID");
            }

            // Validate direction constraints
            if (movementType.Direction == "In" && dto.FromLocationId.HasValue)
            {
                errors.Add($"MovementType '{movementType.Name}' (Direction=In) should not have a source location");
            }

            if (movementType.Direction == "Out" && dto.ToLocationId.HasValue)
            {
                errors.Add($"MovementType '{movementType.Name}' (Direction=Out) should not have a destination location");
            }

            // Validate quantity is positive
            if (dto.Quantity <= 0)
            {
                errors.Add("Quantity must be greater than zero");
            }

            // Validate locations exist and are active
            if (dto.FromLocationId.HasValue)
            {
                var fromLocation = await _context.StockLocations
                    .FirstOrDefaultAsync(
                        sl => sl.Id == dto.FromLocationId.Value &&
                              !sl.IsDeleted &&
                              sl.IsActive,
                        cancellationToken);

                if (fromLocation == null)
                {
                    errors.Add($"Source location (ID: {dto.FromLocationId.Value}) not found or inactive");
                }
            }

            if (dto.ToLocationId.HasValue)
            {
                var toLocation = await _context.StockLocations
                    .FirstOrDefaultAsync(
                        sl => sl.Id == dto.ToLocationId.Value &&
                              !sl.IsDeleted &&
                              sl.IsActive,
                        cancellationToken);

                if (toLocation == null)
                {
                    errors.Add($"Destination location (ID: {dto.ToLocationId.Value}) not found or inactive");
                }
            }

            // Validate stock availability for outgoing movements
            if (movementType.StockImpact == "Negative" && dto.FromLocationId.HasValue)
            {
                var stockBalance = await _context.StockBalances
                    .FirstOrDefaultAsync(
                        sb => sb.MaterialId == dto.MaterialId &&
                              sb.StockLocationId == dto.FromLocationId.Value,
                        cancellationToken);

                var availableQuantity = stockBalance?.Quantity ?? 0;
                if (availableQuantity < dto.Quantity)
                {
                    errors.Add(
                        $"Insufficient stock. Available: {availableQuantity}, Requested: {dto.Quantity}");
                }
            }
        }
        else
        {
            // No MovementType found - allow legacy movements but log warning
            _logger.LogWarning(
                "Stock movement created without MovementType validation. MovementType: {MovementType}",
                dto.MovementType);

            // Basic validation even without MovementType
            if (dto.Quantity <= 0)
            {
                errors.Add("Quantity must be greater than zero");
            }
        }

        if (errors.Any())
        {
            _logger.LogWarning(
                "Stock movement validation failed. Errors: {Errors}",
                string.Join("; ", errors));
            return MovementValidationResult.Failure(errors, movementType);
        }

        _logger.LogDebug(
            "Stock movement validation passed for MovementType: {MovementTypeName}",
            movementType?.Name ?? dto.MovementType);

        return MovementValidationResult.Success(movementType);
    }

    public async Task<MovementType?> GetMovementTypeByCodeAsync(
        string code,
        Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MovementTypes
            .FirstOrDefaultAsync(
                mt => mt.Code == code &&
                      (companyId == null || companyId == Guid.Empty || mt.CompanyId == companyId || mt.CompanyId == Guid.Empty) &&
                      mt.IsActive,
                cancellationToken);
    }
}
