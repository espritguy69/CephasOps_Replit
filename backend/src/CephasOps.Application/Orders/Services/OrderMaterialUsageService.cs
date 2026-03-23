using CephasOps.Application.Events;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// Service for recording material usage on orders
/// </summary>
public class OrderMaterialUsageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderMaterialUsageService> _logger;
    private readonly IEventBus? _eventBus;

    public OrderMaterialUsageService(
        ApplicationDbContext context,
        ILogger<OrderMaterialUsageService> logger,
        IEventBus? eventBus = null)
    {
        _context = context;
        _logger = logger;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Record material usage for an order
    /// </summary>
    public async Task<MaterialUsageRecordedDto> RecordMaterialUsageAsync(
        Guid orderId,
        Guid materialId,
        string? serialNumber,
        decimal quantity,
        Guid? companyId,
        Guid? siId,
        Guid? userId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording material usage: Order={OrderId}, Material={MaterialId}, Serial={SerialNumber}, Quantity={Quantity}",
            orderId, materialId, serialNumber, quantity);

        // Validate order exists
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        // Get material to check if it's serialised
        var material = await _context.Materials
            .FirstOrDefaultAsync(m => m.Id == materialId, cancellationToken);

        if (material == null)
        {
            throw new KeyNotFoundException($"Material {materialId} not found");
        }

        Guid? serialisedItemId = null;

        // For serialised materials, find or create SerialisedItem
        if (material.IsSerialised)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                throw new ArgumentException("Serial number is required for serialised materials");
            }

            // Check if serial number already exists (global validation - prevent duplicates across all orders)
            var existingSerialisedItem = await _context.SerialisedItems
                .FirstOrDefaultAsync(si => si.SerialNumber == serialNumber && si.MaterialId == materialId, cancellationToken);

            if (existingSerialisedItem != null)
            {
                serialisedItemId = existingSerialisedItem.Id;

                // Check if this serial is already used on this order
                var existingUsageOnOrder = await _context.OrderMaterialUsage
                    .FirstOrDefaultAsync(omu => omu.OrderId == orderId && omu.SerialisedItemId == serialisedItemId, cancellationToken);

                if (existingUsageOnOrder != null)
                {
                    throw new InvalidOperationException($"Serial number {serialNumber} is already recorded for this order");
                }

                // Check if this serial is already used on a different order (global duplicate prevention)
                var existingUsageOnOtherOrder = await _context.OrderMaterialUsage
                    .Where(omu => omu.SerialisedItemId == serialisedItemId && omu.OrderId != orderId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingUsageOnOtherOrder != null)
                {
                    // Get the order to get the order identifier for the error message
                    var existingOrder = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id == existingUsageOnOtherOrder.OrderId, cancellationToken);
                    
                    // Order entity doesn't have OrderNumber - use ServiceId or TicketId or just the ID
                    var orderIdentifier = existingOrder?.ServiceId ?? existingOrder?.TicketId ?? existingUsageOnOtherOrder.OrderId.ToString();
                    throw new InvalidOperationException(
                        $"Serial number {serialNumber} is already recorded for order {orderIdentifier}. " +
                        "Each serial number can only be used once across all orders.");
                }
            }
            else
            {
                // Create new SerialisedItem
                var newSerialisedItem = new SerialisedItem
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    MaterialId = materialId,
                    SerialNumber = serialNumber,
                    Status = "WithSI", // Assuming SI has it
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SerialisedItems.Add(newSerialisedItem);
                await _context.SaveChangesAsync(cancellationToken);

                serialisedItemId = newSerialisedItem.Id;
                _logger.LogInformation("Created new SerialisedItem: {SerialisedItemId} for serial {SerialNumber}", serialisedItemId, serialNumber);
            }

            // For serialised materials, quantity should be 1
            if (quantity != 1)
            {
                _logger.LogWarning("Quantity for serialised material was {Quantity}, setting to 1", quantity);
                quantity = 1;
            }
        }
        else
        {
            // For non-serialised materials, serial number should be null
            if (!string.IsNullOrWhiteSpace(serialNumber))
            {
                _logger.LogWarning("Serial number provided for non-serialised material, ignoring");
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than 0 for non-serialised materials");
            }
        }

        // Create OrderMaterialUsage record
        var usage = new OrderMaterialUsage
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            OrderId = orderId,
            MaterialId = materialId,
            SerialisedItemId = serialisedItemId,
            Quantity = quantity,
            RecordedBySiId = siId,
            RecordedByUserId = userId,
            RecordedAt = DateTime.UtcNow,
            Notes = notes
        };

        _context.OrderMaterialUsage.Add(usage);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recorded material usage: {UsageId} for order {OrderId}", usage.Id, orderId);

        var resolvedCompanyId = companyId ?? order.CompanyId;
        if (_eventBus != null && resolvedCompanyId.HasValue && resolvedCompanyId.Value != Guid.Empty)
        {
            var evt = new MaterialIssuedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAtUtc = DateTime.UtcNow,
                CompanyId = resolvedCompanyId,
                TriggeredByUserId = userId,
                OrderId = orderId,
                MaterialId = materialId,
                UsageId = usage.Id,
                Quantity = quantity,
                SerialNumber = serialNumber
            };
            await _eventBus.PublishAsync(evt, cancellationToken);
        }

        return new MaterialUsageRecordedDto
        {
            Id = usage.Id,
            OrderId = orderId,
            MaterialId = materialId,
            MaterialName = material.Description,
            SerialisedItemId = serialisedItemId,
            SerialNumber = serialNumber,
            Quantity = quantity,
            RecordedAt = usage.RecordedAt
        };
    }

    /// <summary>
    /// Get material usage for an order
    /// </summary>
    public async Task<List<MaterialUsageRecordedDto>> GetMaterialUsageAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
                var usages = await _context.OrderMaterialUsage
            .Include(omu => omu.Material)
            .Include(omu => omu.SerialisedItem)
            .Where(omu => omu.OrderId == orderId)
            .OrderBy(omu => omu.RecordedAt)
            .ToListAsync(cancellationToken);

        return usages.Select(omu => new MaterialUsageRecordedDto
        {
            Id = omu.Id,
            OrderId = omu.OrderId,
            MaterialId = omu.MaterialId,
            MaterialName = omu.Material?.Description ?? "Unknown",
            SerialisedItemId = omu.SerialisedItemId,
            SerialNumber = omu.SerialisedItem?.SerialNumber,
            Quantity = omu.Quantity,
            RecordedAt = omu.RecordedAt
        }).ToList();
    }
}

