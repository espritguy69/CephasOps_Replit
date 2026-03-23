using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if building is selected for an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class BuildingSelectedValidator : IGuardConditionValidator
{
    public string Key => "buildingSelected";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<BuildingSelectedValidator> _logger;

    public BuildingSelectedValidator(
        ApplicationDbContext context,
        ILogger<BuildingSelectedValidator> logger)
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
            _logger.LogWarning("Order {OrderId} not found for building validation", entityId);
            return false;
        }

        var result = order.BuildingId != Guid.Empty;
        _logger.LogDebug("Order {OrderId} has building selected: {Result}", entityId, result);
        return result;
    }
}

