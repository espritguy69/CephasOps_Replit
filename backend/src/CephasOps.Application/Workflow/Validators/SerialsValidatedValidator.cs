using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if serial numbers are validated for an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class SerialsValidatedValidator : IGuardConditionValidator
{
    public string Key => "serialNumbersValidated";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<SerialsValidatedValidator> _logger;

    public SerialsValidatedValidator(
        ApplicationDbContext context,
        ILogger<SerialsValidatedValidator> logger)
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
            _logger.LogWarning("Order {OrderId} not found for serials validation", entityId);
            return false;
        }

        var result = order.SerialsValidated;
        _logger.LogDebug("Order {OrderId} serials validated: {Result}", entityId, result);
        return result;
    }
}

