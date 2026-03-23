using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if Assurance orders have recorded replacements when devices were marked as faulty
/// </summary>
public class AssuranceReplacementValidator : IGuardConditionValidator
{
    public string Key => "assuranceReplacementRecorded";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<AssuranceReplacementValidator> _logger;

    public AssuranceReplacementValidator(
        ApplicationDbContext context,
        ILogger<AssuranceReplacementValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        Guid entityId,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        // This validator only applies to Order entity type

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for replacement validation", entityId);
            return false;
        }

        // Get order type
        var orderType = await _context.OrderTypes
            .FirstOrDefaultAsync(ot => ot.Id == order.OrderTypeId, cancellationToken);

        // Only validate Assurance orders
        var orderTypeCode = orderType?.Code?.ToLower() ?? "";
        if (!orderTypeCode.Contains("assurance") && !orderTypeCode.Contains("assur"))
        {
            return true; // Not an Assurance order, validation passes
        }

        // Check if any devices were marked as faulty
        var faultySerials = await _context.SerialisedItems
            .Where(si => si.LastOrderId == entityId && si.Status == "FaultyInWarehouse")
            .ToListAsync(cancellationToken);

        if (faultySerials.Count == 0)
        {
            return true; // No faulty devices, validation passes
        }

        // Check if replacements were recorded for all faulty devices
        var replacements = await _context.OrderMaterialReplacements
            .Where(omr => omr.OrderId == entityId)
            .ToListAsync(cancellationToken);

        var nonSerialisedReplacements = await _context.OrderNonSerialisedReplacements
            .Where(omr => omr.OrderId == entityId)
            .ToListAsync(cancellationToken);

        // For each faulty serial, check if there's a replacement
        foreach (var faultySerial in faultySerials)
        {
            var hasReplacement = replacements.Any(r => r.OldSerialNumber == faultySerial.SerialNumber);
            if (!hasReplacement)
            {
                _logger.LogWarning(
                    "Assurance order {OrderId} has faulty device {SerialNumber} but no replacement recorded",
                    entityId, faultySerial.SerialNumber);
                return false; // Missing replacement
            }
        }

        _logger.LogInformation("All faulty devices for Assurance order {OrderId} have replacements recorded", entityId);
        return true;
    }
}

