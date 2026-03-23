using CephasOps.Application.Inventory.DTOs;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.SIApp.DTOs;
using CephasOps.Application.RMA.Services;
using CephasOps.Application.RMA.DTOs;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Domain.RMA.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CephasOps.Application.SIApp.Services;

/// <summary>
/// Service for SI app material-related operations
/// </summary>
public class SiAppMaterialService
{
    private readonly ApplicationDbContext _context;
    private readonly IRMAService _rmaService;
    private readonly IStockLedgerService _stockLedgerService;
    private readonly ILogger<SiAppMaterialService> _logger;

    public SiAppMaterialService(
        ApplicationDbContext context,
        IRMAService rmaService,
        IStockLedgerService stockLedgerService,
        ILogger<SiAppMaterialService> logger)
    {
        _context = context;
        _rmaService = rmaService;
        _stockLedgerService = stockLedgerService;
        _logger = logger;
    }

    /// <summary>
    /// Mark a device as faulty for an order
    /// </summary>
    public async Task<MarkFaultyResponseDto> MarkDeviceAsFaultyAsync(
        Guid orderId,
        string serialNumber,
        string reason,
        Guid? companyId,
        Guid siId,
        Guid? userId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Marking device as faulty: Order={OrderId}, Serial={SerialNumber}, SI={SiId}",
            orderId, serialNumber, siId);

        // Validate order exists and SI is assigned
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        if (order.CompanyId != companyId.Value)
            throw new UnauthorizedAccessException("Order does not belong to current tenant.");
        if (order.AssignedSiId != siId)
        {
            throw new UnauthorizedAccessException($"Order {orderId} is not assigned to SI {siId}");
        }

        // Find SerialisedItem by serial number
        var serialisedItem = await _context.SerialisedItems
            .Include(si => si.Material)
            .FirstOrDefaultAsync(si => si.SerialNumber == serialNumber, cancellationToken);

        if (serialisedItem == null)
        {
            throw new KeyNotFoundException($"Serial number {serialNumber} not found");
        }

        // Validate serial belongs to SI's stock location
        var siLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => sl.LinkedServiceInstallerId == siId && sl.Type == "SI" && sl.IsActive, cancellationToken);

        if (siLocation == null)
        {
            _logger.LogWarning("No stock location found for SI {SiId}, creating one", siId);
            // Optionally create stock location for SI if it doesn't exist
            siLocation = new StockLocation
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = $"SI {siId} Stock",
                Type = "SI",
                LinkedServiceInstallerId = siId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLocations.Add(siLocation);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Check if serial is in SI's location (via stock movements or current location)
        if (serialisedItem.CurrentLocationId != siLocation.Id)
        {
            // Check if there's a recent stock movement showing SI has this item
            // Check if there's a recent stock movement for this material to this SI
            var recentMovement = await _context.StockMovements
                .Where(sm => sm.MaterialId == serialisedItem.MaterialId && sm.ServiceInstallerId == siId)
                .OrderByDescending(sm => sm.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (recentMovement == null)
            {
                _logger.LogWarning("Serial {SerialNumber} is not in SI {SiId}'s stock location", serialNumber, siId);
                // Still allow marking as faulty, but log warning
            }
        }

        // Update SerialisedItem status
        serialisedItem.Status = "FaultyInWarehouse";
        serialisedItem.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            serialisedItem.Notes = notes;
        }

        // Get warehouse RMA location (or create if doesn't exist)
        var rmaLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => sl.Type == "RMA" && sl.IsActive, cancellationToken);

        if (rmaLocation == null)
        {
            _logger.LogWarning("No RMA stock location found, creating one");
            rmaLocation = new StockLocation
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "RMA Warehouse",
                Type = "RMA",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLocations.Add(rmaLocation);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Create StockMovement (ReturnFaulty)
        var stockMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FromLocationId = siLocation.Id,
            ToLocationId = rmaLocation.Id,
            MaterialId = serialisedItem.MaterialId,
            Quantity = 1,
            MovementType = "ReturnFaulty",
            OrderId = orderId,
            ServiceInstallerId = siId,
            Remarks = $"Faulty device: {reason}. Serial: {serialNumber}",
            CreatedByUserId = userId ?? Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StockMovements.Add(stockMovement);

        // Update serialised item location
        serialisedItem.CurrentLocationId = rmaLocation.Id;

        Guid? rmaRequestId = null;

        // Auto-create RMA request if order type is "Assurance"
        var orderType = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == order.OrderTypeId, cancellationToken);
        var orderTypeCode = orderType?.Code?.ToLower() ?? "";
        if (orderTypeCode.Contains("assurance") || orderTypeCode.Contains("assur"))
        {
            _logger.LogInformation("Order type is Assurance, auto-creating RMA request");

            try
            {
                var createRmaDto = new CreateRmaRequestDto
                {
                    PartnerId = order.PartnerId,
                    Reason = $"Faulty device from Assurance job: {reason}",
                    Items = new List<CreateRmaRequestItemDto>
                    {
                        new CreateRmaRequestItemDto
                        {
                            SerialisedItemId = serialisedItem.Id,
                            OriginalOrderId = orderId,
                            Notes = notes
                        }
                    }
                };

                var rmaRequest = await _rmaService.CreateRmaRequestAsync(
                    createRmaDto,
                    companyId,
                    userId ?? Guid.Empty,
                    cancellationToken);

                rmaRequestId = rmaRequest.Id;
                _logger.LogInformation("Created RMA request: {RmaRequestId}", rmaRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-create RMA request for order {OrderId}", orderId);
                // Don't fail the entire operation if RMA creation fails
            }
        }

        // Link to OrderMaterialReplacement if exists
        var replacement = await _context.OrderMaterialReplacements
            .FirstOrDefaultAsync(omr => omr.OrderId == orderId && omr.OldSerialNumber == serialNumber, cancellationToken);

        if (replacement != null && rmaRequestId.HasValue)
        {
            replacement.RmaRequestId = rmaRequestId;
            _logger.LogInformation("Linked RMA request {RmaRequestId} to OrderMaterialReplacement {ReplacementId}",
                rmaRequestId, replacement.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully marked device {SerialNumber} as faulty for order {OrderId}",
            serialNumber, orderId);

        return new MarkFaultyResponseDto
        {
            SerialisedItemId = serialisedItem.Id,
            SerialNumber = serialNumber,
            MaterialName = serialisedItem.Material?.Description ?? "Unknown",
            StockMovementId = stockMovement.Id,
            RmaRequestId = rmaRequestId,
            Message = rmaRequestId.HasValue
                ? $"Device marked as faulty and RMA request created"
                : "Device marked as faulty"
        };
    }

    /// <summary>
    /// Record material replacement for Assurance orders
    /// </summary>
    public async Task<RecordReplacementResponseDto> RecordMaterialReplacementAsync(
        Guid orderId,
        string oldSerialNumber,
        string newSerialNumber,
        string replacementReason,
        Guid? companyId,
        Guid siId,
        Guid? userId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Recording material replacement: Order={OrderId}, OldSerial={OldSerial}, NewSerial={NewSerial}, SI={SiId}",
            orderId, oldSerialNumber, newSerialNumber, siId);

        // Validate order exists and SI is assigned
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        if (order.CompanyId != companyId.Value)
            throw new UnauthorizedAccessException("Order does not belong to current tenant.");
        if (order.AssignedSiId != siId)
        {
            throw new UnauthorizedAccessException($"Order {orderId} is not assigned to SI {siId}");
        }

        // Validate order type is "Assurance"
        var orderType = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == order.OrderTypeId, cancellationToken);
        var orderTypeCode = orderType?.Code?.ToLower() ?? "";
        if (!orderTypeCode.Contains("assurance") && !orderTypeCode.Contains("assur"))
        {
            throw new InvalidOperationException($"Material replacement is only allowed for Assurance orders. Current order type: {orderType?.Code ?? "Unknown"}");
        }

        // Find old (faulty) SerialisedItem
        var oldSerialisedItem = await _context.SerialisedItems
            .Include(si => si.Material)
            .FirstOrDefaultAsync(si => si.SerialNumber == oldSerialNumber, cancellationToken);

        if (oldSerialisedItem == null)
        {
            throw new KeyNotFoundException($"Old serial number {oldSerialNumber} not found");
        }

        // Find new (replacement) SerialisedItem
        var newSerialisedItem = await _context.SerialisedItems
            .Include(si => si.Material)
            .FirstOrDefaultAsync(si => si.SerialNumber == newSerialNumber, cancellationToken);

        if (newSerialisedItem == null)
        {
            throw new KeyNotFoundException($"New serial number {newSerialNumber} not found");
        }

        // Validate both devices belong to SI's stock location
        var siLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => sl.LinkedServiceInstallerId == siId && sl.Type == "SI" && sl.IsActive, cancellationToken);

        if (siLocation == null)
        {
            throw new InvalidOperationException($"No stock location found for SI {siId}");
        }

        // Check if old device is in SI's location or was used on this order
        var oldDeviceInSiLocation = oldSerialisedItem.CurrentLocationId == siLocation.Id;
        var oldDeviceUsedOnOrder = await _context.OrderMaterialUsage
            .AnyAsync(omu => omu.OrderId == orderId && omu.SerialisedItemId == oldSerialisedItem.Id, cancellationToken);

        if (!oldDeviceInSiLocation && !oldDeviceUsedOnOrder)
        {
            _logger.LogWarning("Old device {OldSerial} may not belong to SI {SiId} or order {OrderId}", oldSerialNumber, siId, orderId);
            // Still allow, but log warning
        }

        // Check if new device is in SI's location
        if (newSerialisedItem.CurrentLocationId != siLocation.Id)
        {
            throw new InvalidOperationException($"New device {newSerialNumber} is not in SI {siId}'s stock location");
        }

        // Get warehouse RMA location
        var rmaLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => sl.Type == "RMA" && sl.IsActive, cancellationToken);

        if (rmaLocation == null)
        {
            _logger.LogWarning("No RMA stock location found, creating one");
            rmaLocation = new StockLocation
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "RMA Warehouse",
                Type = "RMA",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLocations.Add(rmaLocation);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Get customer location (or create if needed)
        var customerLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => sl.LinkedBuildingId == order.BuildingId && sl.Type == "CustomerSite", cancellationToken);

        if (customerLocation == null && order.BuildingId != Guid.Empty)
        {
            customerLocation = new StockLocation
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = $"Building {order.BuildingId}",
                Type = "CustomerSite",
                LinkedBuildingId = order.BuildingId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLocations.Add(customerLocation);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Update old device status → FaultyInWarehouse
        oldSerialisedItem.Status = "FaultyInWarehouse";
        oldSerialisedItem.CurrentLocationId = rmaLocation.Id;
        oldSerialisedItem.UpdatedAt = DateTime.UtcNow;

        // Update new device status → InstalledAtCustomer
        newSerialisedItem.Status = "InstalledAtCustomer";
        newSerialisedItem.CurrentLocationId = customerLocation?.Id;
        newSerialisedItem.LastOrderId = orderId;
        newSerialisedItem.UpdatedAt = DateTime.UtcNow;

        // Create stock movement for old device (ReturnFaulty)
        var oldDeviceMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FromLocationId = siLocation.Id,
            ToLocationId = rmaLocation.Id,
            MaterialId = oldSerialisedItem.MaterialId,
            Quantity = 1,
            MovementType = "ReturnFaulty",
            OrderId = orderId,
            ServiceInstallerId = siId,
            Remarks = $"Faulty device replacement: {replacementReason}. Serial: {oldSerialNumber}",
            CreatedByUserId = userId ?? Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(oldDeviceMovement);

        // Create stock movement for new device (InstallAtCustomer)
        var newDeviceMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FromLocationId = siLocation.Id,
            ToLocationId = customerLocation?.Id,
            MaterialId = newSerialisedItem.MaterialId,
            Quantity = 1,
            MovementType = "InstallAtCustomer",
            OrderId = orderId,
            ServiceInstallerId = siId,
            Remarks = $"Replacement device installed. Serial: {newSerialNumber}",
            CreatedByUserId = userId ?? Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(newDeviceMovement);

        // Create OrderMaterialReplacement record
        var replacement = new OrderMaterialReplacement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            OrderId = orderId,
            OldMaterialId = oldSerialisedItem.MaterialId,
            OldSerialNumber = oldSerialNumber,
            OldSerialisedItemId = oldSerialisedItem.Id,
            NewMaterialId = newSerialisedItem.MaterialId,
            NewSerialNumber = newSerialNumber,
            NewSerialisedItemId = newSerialisedItem.Id,
            ReplacementReason = replacementReason,
            ReplacedBySiId = siId,
            RecordedByUserId = userId,
            RecordedAt = DateTime.UtcNow,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrderMaterialReplacements.Add(replacement);

        Guid? rmaRequestId = null;

        // Auto-create RMA request if not exists
        // Check if there's an existing RMA request for this order via RmaRequestItems
        var existingRma = await _context.RmaRequests
            .Include(r => r.Items)
            .Where(r => r.Items.Any(item => item.OriginalOrderId == orderId) && r.Status != "Closed")
            .FirstOrDefaultAsync(cancellationToken);

        if (existingRma == null)
        {
            _logger.LogInformation("No existing RMA request found, creating new one");

            try
            {
                var createRmaDto = new CreateRmaRequestDto
                {
                    PartnerId = order.PartnerId,
                    Reason = $"Faulty device replacement from Assurance job: {replacementReason}",
                    Items = new List<CreateRmaRequestItemDto>
                    {
                        new CreateRmaRequestItemDto
                        {
                            SerialisedItemId = oldSerialisedItem.Id,
                            OriginalOrderId = orderId,
                            Notes = notes
                        }
                    }
                };

                var rmaRequest = await _rmaService.CreateRmaRequestAsync(
                    createRmaDto,
                    companyId,
                    userId ?? Guid.Empty,
                    cancellationToken);

                rmaRequestId = rmaRequest.Id;
                _logger.LogInformation("Created RMA request: {RmaRequestId}", rmaRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-create RMA request for order {OrderId}", orderId);
                // Don't fail the entire operation if RMA creation fails
            }
        }
        else
        {
            rmaRequestId = existingRma.Id;
            _logger.LogInformation("Using existing RMA request: {RmaRequestId}", rmaRequestId);
        }

        // Link RMA request to replacement
        if (rmaRequestId.HasValue)
        {
            replacement.RmaRequestId = rmaRequestId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully recorded material replacement for order {OrderId}: Old={OldSerial}, New={NewSerial}",
            orderId, oldSerialNumber, newSerialNumber);

        return new RecordReplacementResponseDto
        {
            ReplacementId = replacement.Id,
            OldSerialNumber = oldSerialNumber,
            NewSerialNumber = newSerialNumber,
            OldSerialisedItemId = oldSerialisedItem.Id,
            NewSerialisedItemId = newSerialisedItem.Id,
            RmaRequestId = rmaRequestId,
            Message = rmaRequestId.HasValue
                ? $"Replacement recorded and RMA request {(existingRma == null ? "created" : "linked")}"
                : "Replacement recorded"
        };
    }

    /// <summary>
    /// Return faulty material (standalone - not tied to specific order). Legacy write path: records to ledger only; do not write StockBalance.Quantity. Ledger is the single source of truth.
    /// </summary>
    public async Task<MarkFaultyResponseDto> ReturnFaultyMaterialAsync(
        string? serialNumber,
        Guid? materialId,
        decimal? quantity,
        Guid? orderId,
        string reason,
        Guid? companyId,
        Guid siId,
        Guid? userId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogWarning("Legacy write path invoked: ReturnFaultyMaterialAsync. Quantities are ledger-only; do not reintroduce StockBalance.Quantity writes.");
        _logger.LogInformation("Returning faulty material: Serial={SerialNumber}, Material={MaterialId}, Quantity={Quantity}, SI={SiId}",
            serialNumber, materialId, quantity, siId);

        // Get SI's stock location
        var siLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => sl.LinkedServiceInstallerId == siId && sl.Type == "SI" && sl.IsActive, cancellationToken);

        if (siLocation == null)
        {
            throw new InvalidOperationException($"No stock location found for SI {siId}");
        }

        // Get warehouse RMA location
        var rmaLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => sl.Type == "RMA" && sl.IsActive, cancellationToken);

        if (rmaLocation == null)
        {
            rmaLocation = new StockLocation
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "RMA Warehouse",
                Type = "RMA",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLocations.Add(rmaLocation);
            await _context.SaveChangesAsync(cancellationToken);
        }

        Guid? serialisedItemId = null;
        Guid materialIdValue;
        string materialName = "Unknown";

        // Handle serialised materials
        if (!string.IsNullOrWhiteSpace(serialNumber))
        {
            var serialisedItem = await _context.SerialisedItems
                .Include(si => si.Material)
                .FirstOrDefaultAsync(si => si.SerialNumber == serialNumber, cancellationToken);

            if (serialisedItem == null)
            {
                throw new KeyNotFoundException($"Serial number {serialNumber} not found");
            }

            // Validate serial belongs to SI's location
            if (serialisedItem.CurrentLocationId != siLocation.Id)
            {
                throw new InvalidOperationException($"Serial number {serialNumber} is not in SI {siId}'s stock location");
            }

            materialIdValue = serialisedItem.MaterialId;
            materialName = serialisedItem.Material?.Description ?? "Unknown";
            serialisedItemId = serialisedItem.Id;

            // Update SerialisedItem status
            serialisedItem.Status = "FaultyInWarehouse";
            serialisedItem.CurrentLocationId = rmaLocation.Id;
            serialisedItem.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                serialisedItem.Notes = notes;
            }
        }
        // Handle non-serialised materials
        else if (materialId.HasValue && quantity.HasValue)
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == materialId.Value, cancellationToken);

            if (material == null)
            {
                throw new KeyNotFoundException($"Material {materialId.Value} not found");
            }

            if (material.IsSerialised)
            {
                throw new InvalidOperationException("Material is serialised. Please provide serial number.");
            }

            materialIdValue = materialId.Value;
            materialName = material.Description;

            // Check if SI has enough quantity
            var stockBalance = await _context.StockBalances
                .Where(sb => sb.StockLocationId == siLocation.Id && sb.MaterialId == materialId.Value)
                .SumAsync(sb => sb.Quantity, cancellationToken);

            if (stockBalance < quantity.Value)
            {
                throw new InvalidOperationException($"Insufficient stock. Available: {stockBalance}, Requested: {quantity.Value}");
            }

            // Record ledger entries (Phase 2.1.3A); do not update StockBalance
            var legacyDto = new CreateStockMovementDto
            {
                FromLocationId = siLocation.Id,
                ToLocationId = rmaLocation.Id,
                MaterialId = materialId.Value,
                Quantity = quantity.Value,
                MovementType = "ReturnFaulty",
                OrderId = orderId,
                ServiceInstallerId = siId,
                Remarks = $"Faulty material return: {reason}. Quantity: {quantity}",
                SerialNumber = null
            };
            await _stockLedgerService.RecordLegacyMovementAsync(legacyDto, companyId, userId ?? Guid.Empty, cancellationToken);
        }
        else
        {
            throw new ArgumentException("Either serialNumber (for serialised) or materialId+quantity (for non-serialised) must be provided");
        }

        // Create StockMovement (ReturnFaulty)
        var stockMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FromLocationId = siLocation.Id,
            ToLocationId = rmaLocation.Id,
            MaterialId = materialIdValue,
            Quantity = quantity ?? 1,
            MovementType = "ReturnFaulty",
            OrderId = orderId,
            ServiceInstallerId = siId,
            Remarks = $"Faulty material return: {reason}. {(serialNumber != null ? $"Serial: {serialNumber}" : $"Quantity: {quantity}")}",
            CreatedByUserId = userId ?? Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(stockMovement);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully returned faulty material: Material={MaterialId}, Serial={SerialNumber}",
            materialIdValue, serialNumber);

        return new MarkFaultyResponseDto
        {
            SerialisedItemId = serialisedItemId ?? Guid.Empty,
            SerialNumber = serialNumber ?? string.Empty,
            MaterialName = materialName,
            StockMovementId = stockMovement.Id,
            Message = "Faulty material returned successfully"
        };
    }

    /// <summary>
    /// Record non-serialised material replacement. Legacy write path: records to ledger only; do not write StockBalance.Quantity. Ledger is the single source of truth.
    /// </summary>
    public async Task<RecordReplacementResponseDto> RecordNonSerialisedReplacementAsync(
        Guid orderId,
        Guid materialId,
        decimal quantityReplaced,
        string replacementReason,
        Guid? companyId,
        Guid siId,
        Guid? userId,
        string? remark = null,
        CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogWarning("Legacy write path invoked: RecordNonSerialisedReplacementAsync. Quantities are ledger-only; do not reintroduce StockBalance.Quantity writes.");
        _logger.LogInformation("Recording non-serialised replacement: Order={OrderId}, Material={MaterialId}, Quantity={Quantity}, SI={SiId}",
            orderId, materialId, quantityReplaced, siId);

        // Validate order exists and SI is assigned
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        if (order.CompanyId != companyId.Value)
            throw new UnauthorizedAccessException("Order does not belong to current tenant.");
        if (order.AssignedSiId != siId)
        {
            throw new UnauthorizedAccessException($"Order {orderId} is not assigned to SI {siId}");
        }

        // Get material and validate it's non-serialised
        var material = await _context.Materials
            .FirstOrDefaultAsync(m => m.Id == materialId, cancellationToken);

        if (material == null)
        {
            throw new KeyNotFoundException($"Material {materialId} not found");
        }

        if (material.IsSerialised)
        {
            throw new InvalidOperationException($"Material {materialId} is serialised. Use RecordMaterialReplacementAsync for serialised materials.");
        }

        // Get SI's stock location
        var siLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => sl.LinkedServiceInstallerId == siId && sl.Type == "SI" && sl.IsActive, cancellationToken);

        if (siLocation == null)
        {
            throw new InvalidOperationException($"No stock location found for SI {siId}");
        }

        // Check if SI has enough quantity
        var stockBalance = await _context.StockBalances
            .Where(sb => sb.StockLocationId == siLocation.Id && sb.MaterialId == materialId)
            .SumAsync(sb => sb.Quantity, cancellationToken);

        if (stockBalance < quantityReplaced)
        {
            throw new InvalidOperationException($"Insufficient stock. Available: {stockBalance}, Requested: {quantityReplaced}");
        }

        // Get warehouse RMA location
        var rmaLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => sl.Type == "RMA" && sl.IsActive, cancellationToken);

        if (rmaLocation == null)
        {
            rmaLocation = new StockLocation
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "RMA Warehouse",
                Type = "RMA",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.StockLocations.Add(rmaLocation);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Create OrderNonSerialisedReplacement record
        var replacement = new OrderNonSerialisedReplacement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            OrderId = orderId,
            MaterialId = materialId,
            QuantityReplaced = quantityReplaced,
            Unit = material.UnitOfMeasure,
            ReplacementReason = replacementReason,
            Remark = remark,
            ReplacedBySiId = siId,
            RecordedByUserId = userId,
            RecordedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrderNonSerialisedReplacements.Add(replacement);

        // Record ledger entries (Phase 2.1.3A); do not update StockBalance
        var legacyDto = new CreateStockMovementDto
        {
            FromLocationId = siLocation.Id,
            ToLocationId = rmaLocation.Id,
            MaterialId = materialId,
            Quantity = quantityReplaced,
            MovementType = "ReturnFaulty",
            OrderId = orderId,
            ServiceInstallerId = siId,
            Remarks = $"Non-serialised replacement: {replacementReason}. Quantity: {quantityReplaced} {material.UnitOfMeasure}",
            SerialNumber = null
        };
        await _stockLedgerService.RecordLegacyMovementAsync(legacyDto, companyId, userId ?? Guid.Empty, cancellationToken);

        // Create StockMovement (ReturnFaulty)
        var stockMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FromLocationId = siLocation.Id,
            ToLocationId = rmaLocation.Id,
            MaterialId = materialId,
            Quantity = quantityReplaced,
            MovementType = "ReturnFaulty",
            OrderId = orderId,
            ServiceInstallerId = siId,
            Remarks = $"Non-serialised replacement: {replacementReason}. Quantity: {quantityReplaced} {material.UnitOfMeasure}",
            CreatedByUserId = userId ?? Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.StockMovements.Add(stockMovement);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully recorded non-serialised replacement for order {OrderId}: Material={MaterialId}, Quantity={Quantity}",
            orderId, materialId, quantityReplaced);

        return new RecordReplacementResponseDto
        {
            ReplacementId = replacement.Id,
            OldSerialNumber = string.Empty,
            NewSerialNumber = string.Empty,
            Message = $"Non-serialised replacement recorded: {quantityReplaced} {material.UnitOfMeasure}"
        };
    }

    /// <summary>
    /// Get material returns list for SI
    /// </summary>
    public async Task<List<MaterialReturnDto>> GetMaterialReturnsAsync(
        Guid siId,
        Guid? companyId,
        MaterialReturnsQueryDto? filters = null,
        CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("Getting material returns for SI {SiId}", siId);

        var returns = new List<MaterialReturnDto>();

        // Get SI's stock location
        var siLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => sl.LinkedServiceInstallerId == siId && sl.Type == "SI" && sl.IsActive, cancellationToken);

        if (siLocation == null)
        {
            _logger.LogWarning("No stock location found for SI {SiId}", siId);
            return returns;
        }

        // 1. Get StockMovements with ReturnFaulty or ReturnFromSI (multi-tenant — CompanyId required)
        var stockMovementsQuery = _context.StockMovements
            .Include(sm => sm.Material)
            .Where(sm => sm.ServiceInstallerId == siId && sm.CompanyId == companyId.Value &&
                        (sm.MovementType == "ReturnFaulty" || sm.MovementType == "ReturnFromSI"));

        if (filters != null)
        {
            if (filters.DateFrom.HasValue)
            {
                stockMovementsQuery = stockMovementsQuery.Where(sm => sm.CreatedAt >= filters.DateFrom.Value);
            }
            if (filters.DateTo.HasValue)
            {
                stockMovementsQuery = stockMovementsQuery.Where(sm => sm.CreatedAt <= filters.DateTo.Value.AddDays(1));
            }
            if (filters.OrderId.HasValue)
            {
                stockMovementsQuery = stockMovementsQuery.Where(sm => sm.OrderId == filters.OrderId.Value);
            }
            if (filters.MaterialId.HasValue)
            {
                stockMovementsQuery = stockMovementsQuery.Where(sm => sm.MaterialId == filters.MaterialId.Value);
            }
            if (!string.IsNullOrWhiteSpace(filters.Status) && filters.Status != "all")
            {
                if (filters.Status == "faulty")
                {
                    stockMovementsQuery = stockMovementsQuery.Where(sm => sm.MovementType == "ReturnFaulty");
                }
                else if (filters.Status == "returned")
                {
                    stockMovementsQuery = stockMovementsQuery.Where(sm => sm.MovementType == "ReturnFromSI");
                }
            }
        }

        var stockMovements = await stockMovementsQuery
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync(cancellationToken);

        // Get orders for service IDs
        var orderIds = stockMovements.Where(sm => sm.OrderId.HasValue).Select(sm => sm.OrderId!.Value).Distinct().ToList();
        var orders = orderIds.Any() 
            ? await _context.Orders.Where(o => orderIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, cancellationToken)
            : new Dictionary<Guid, Order>();

        // Get RMA requests linked to orders (via RmaRequestItem.OriginalOrderId)
        var rmaRequestItems = orderIds.Any()
            ? await _context.RmaRequestItems
                .Include(item => item.RmaRequest)
                .Where(item => orderIds.Contains(item.OriginalOrderId ?? Guid.Empty) && 
                              item.RmaRequest != null &&
                              item.RmaRequest.Status != "Cancelled" &&
                              item.RmaRequest.CompanyId == companyId)
                .ToListAsync(cancellationToken)
            : new List<RmaRequestItem>();
        var rmaRequestsByOrderId = rmaRequestItems
            .Where(item => item.OriginalOrderId.HasValue && item.RmaRequest != null)
            .GroupBy(item => item.OriginalOrderId!.Value)
            .ToDictionary(g => g.Key, g => g.First().RmaRequest!.Id);

        foreach (var sm in stockMovements)
        {
            // Check if there's an RMA request linked to this movement's order
            Guid? rmaRequestId = null;
            if (sm.OrderId.HasValue && rmaRequestsByOrderId.TryGetValue(sm.OrderId.Value, out var rmaId))
            {
                rmaRequestId = rmaId;
            }

            orders.TryGetValue(sm.OrderId ?? Guid.Empty, out var order);

            returns.Add(new MaterialReturnDto
            {
                Id = sm.Id,
                OrderId = sm.OrderId,
                OrderServiceId = order?.ServiceId,
                SerialNumber = null, // StockMovement doesn't store serial directly
                MaterialId = sm.MaterialId,
                MaterialName = sm.Material?.Description ?? "Unknown",
                Quantity = sm.Quantity,
                ReturnedAt = sm.CreatedAt,
                Reason = sm.Remarks,
                Notes = sm.Remarks,
                Status = rmaRequestId.HasValue ? "RMA Created" : (sm.MovementType == "ReturnFaulty" ? "Faulty" : "Returned"),
                RmaRequestId = rmaRequestId,
                ReturnType = "Faulty"
            });
        }

        // 2. Get OrderMaterialReplacement records (serialised replacements)
        var replacementsQuery = _context.OrderMaterialReplacements
            .Include(omr => omr.Order)
            .Where(omr => omr.ReplacedBySiId == siId);

        if (companyId.HasValue)
        {
            replacementsQuery = replacementsQuery.Where(omr => omr.CompanyId == companyId.Value);
        }

        if (filters != null)
        {
            if (filters.DateFrom.HasValue)
            {
                replacementsQuery = replacementsQuery.Where(omr => omr.RecordedAt >= filters.DateFrom.Value);
            }
            if (filters.DateTo.HasValue)
            {
                replacementsQuery = replacementsQuery.Where(omr => omr.RecordedAt <= filters.DateTo.Value.AddDays(1));
            }
            if (filters.OrderId.HasValue)
            {
                replacementsQuery = replacementsQuery.Where(omr => omr.OrderId == filters.OrderId.Value);
            }
        }

        var replacements = await replacementsQuery
            .OrderByDescending(omr => omr.RecordedAt)
            .ToListAsync(cancellationToken);

        foreach (var rep in replacements)
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == rep.NewMaterialId, cancellationToken);

            returns.Add(new MaterialReturnDto
            {
                Id = rep.Id,
                OrderId = rep.OrderId,
                OrderServiceId = rep.Order?.ServiceId,
                SerialNumber = rep.OldSerialNumber,
                MaterialId = rep.OldMaterialId,
                MaterialName = material?.Description ?? "Unknown",
                Quantity = 1,
                ReturnedAt = rep.RecordedAt,
                Reason = rep.ReplacementReason,
                Notes = rep.Notes,
                Status = rep.RmaRequestId.HasValue ? "RMA Created" : "Faulty",
                RmaRequestId = rep.RmaRequestId,
                ReplacementReason = rep.ReplacementReason,
                ReturnType = "Replacement"
            });
        }

        // 3. Get OrderNonSerialisedReplacement records
        var nonSerialisedReplacementsQuery = _context.OrderNonSerialisedReplacements
            .Include(onsr => onsr.Order)
            .Where(onsr => onsr.ReplacedBySiId == siId);

        if (companyId.HasValue)
        {
            nonSerialisedReplacementsQuery = nonSerialisedReplacementsQuery.Where(onsr => onsr.CompanyId == companyId.Value);
        }

        if (filters != null)
        {
            if (filters.DateFrom.HasValue)
            {
                nonSerialisedReplacementsQuery = nonSerialisedReplacementsQuery.Where(onsr => onsr.RecordedAt >= filters.DateFrom.Value);
            }
            if (filters.DateTo.HasValue)
            {
                nonSerialisedReplacementsQuery = nonSerialisedReplacementsQuery.Where(onsr => onsr.RecordedAt <= filters.DateTo.Value.AddDays(1));
            }
            if (filters.OrderId.HasValue)
            {
                nonSerialisedReplacementsQuery = nonSerialisedReplacementsQuery.Where(onsr => onsr.OrderId == filters.OrderId.Value);
            }
            if (filters.MaterialId.HasValue)
            {
                nonSerialisedReplacementsQuery = nonSerialisedReplacementsQuery.Where(onsr => onsr.MaterialId == filters.MaterialId.Value);
            }
        }

        var nonSerialisedReplacements = await nonSerialisedReplacementsQuery
            .OrderByDescending(onsr => onsr.RecordedAt)
            .ToListAsync(cancellationToken);

        foreach (var nsr in nonSerialisedReplacements)
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.Id == nsr.MaterialId, cancellationToken);

            returns.Add(new MaterialReturnDto
            {
                Id = nsr.Id,
                OrderId = nsr.OrderId,
                OrderServiceId = nsr.Order?.ServiceId,
                SerialNumber = null,
                MaterialId = nsr.MaterialId,
                MaterialName = material?.Description ?? "Unknown",
                Quantity = nsr.QuantityReplaced,
                ReturnedAt = nsr.RecordedAt,
                Reason = nsr.ReplacementReason,
                Notes = nsr.Remark,
                Status = "Returned",
                ReplacementReason = nsr.ReplacementReason,
                ReturnType = "NonSerialisedReplacement"
            });
        }

        // Apply additional filters
        if (filters != null)
        {
            if (!string.IsNullOrWhiteSpace(filters.Status) && filters.Status != "all")
            {
                returns = returns.Where(r => 
                    (filters.Status == "faulty" && r.Status == "Faulty") ||
                    (filters.Status == "returned" && r.Status == "Returned") ||
                    (filters.Status == "rma" && r.Status == "RMA Created")
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filters.ReturnType) && filters.ReturnType != "all")
            {
                returns = returns.Where(r => r.ReturnType == filters.ReturnType).ToList();
            }
        }

        // Sort by date descending
        return returns.OrderByDescending(r => r.ReturnedAt).ToList();
    }
}

