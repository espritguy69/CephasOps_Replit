using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if customer contact (phone or email) is provided for an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class CustomerContactProvidedValidator : IGuardConditionValidator
{
    public string Key => "customerContactProvided";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerContactProvidedValidator> _logger;

    public CustomerContactProvidedValidator(
        ApplicationDbContext context,
        ILogger<CustomerContactProvidedValidator> logger)
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
            _logger.LogWarning("Order {OrderId} not found for customer contact validation", entityId);
            return false;
        }

        var result = !string.IsNullOrEmpty(order.CustomerPhone) || !string.IsNullOrEmpty(order.CustomerEmail);
        _logger.LogDebug("Order {OrderId} has customer contact provided: {Result}", entityId, result);
        return result;
    }
}

