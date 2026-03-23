using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if docket is uploaded for an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class DocketUploadedValidator : IGuardConditionValidator
{
    public string Key => "docketUploaded";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocketUploadedValidator> _logger;

    public DocketUploadedValidator(
        ApplicationDbContext context,
        ILogger<DocketUploadedValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        Guid entityId,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for docket validation", entityId);
            return false;
        }

        // Check flag if config says to (default: true)
        bool checkFlag = config?.GetValueOrDefault("checkFlag")?.ToString() != "false";
        if (checkFlag && order.DocketUploaded)
        {
            _logger.LogDebug("Order {OrderId} has DocketUploaded flag set", entityId);
            return true;
        }

        // Check OrderDocket table if config says to (default: true)
        bool checkDockets = config?.GetValueOrDefault("checkDockets")?.ToString() != "false";
        if (checkDockets)
        {
            var hasDocket = await _context.OrderDockets
                .Where(od => od.OrderId == entityId && od.IsFinal)
                .AnyAsync(cancellationToken);

            if (hasDocket)
            {
                _logger.LogDebug("Order {OrderId} has final docket record", entityId);
                return true;
            }
        }

        _logger.LogDebug("Order {OrderId} does not meet docket uploaded condition", entityId);
        return false;
    }
}

