using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if Service Installer (SI) is assigned to an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class SiAssignedValidator : IGuardConditionValidator
{
    public string Key => "siaAssigned";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<SiAssignedValidator> _logger;

    public SiAssignedValidator(
        ApplicationDbContext context,
        ILogger<SiAssignedValidator> logger)
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
            _logger.LogWarning("Order {OrderId} not found for SI assignment validation", entityId);
            return false;
        }

        var result = order.AssignedSiId.HasValue;
        _logger.LogDebug("Order {OrderId} has SI assigned: {Result}", entityId, result);
        return result;
    }
}

