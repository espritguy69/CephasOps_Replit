using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if splitter port is assigned to an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class SplitterAssignedValidator : IGuardConditionValidator
{
    public string Key => "splitterAssigned";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<SplitterAssignedValidator> _logger;

    public SplitterAssignedValidator(
        ApplicationDbContext context,
        ILogger<SplitterAssignedValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        Guid entityId,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        // Check if any SplitterPort is assigned to this order
        var splitterPort = await _context.SplitterPorts
            .Where(sp => sp.OrderId == entityId && sp.Status == "Used")
            .AnyAsync(cancellationToken);

        if (splitterPort)
        {
            _logger.LogDebug("Order {OrderId} has splitter port assigned", entityId);
            return true;
        }

        _logger.LogDebug("Order {OrderId} does not have splitter port assigned", entityId);
        return false;
    }
}

