using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if appointment date is set for an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class AppointmentDateSetValidator : IGuardConditionValidator
{
    public string Key => "appointmentDateSet";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppointmentDateSetValidator> _logger;

    public AppointmentDateSetValidator(
        ApplicationDbContext context,
        ILogger<AppointmentDateSetValidator> logger)
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
            _logger.LogWarning("Order {OrderId} not found for appointment date validation", entityId);
            return false;
        }

        var result = order.AppointmentDate != default;
        _logger.LogDebug("Order {OrderId} has appointment date set: {Result}", entityId, result);
        return result;
    }
}

