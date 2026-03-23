using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if there are no active blockers for an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class NoActiveBlockersValidator : IGuardConditionValidator
{
    public string Key => "noBlockersActive";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<NoActiveBlockersValidator> _logger;

    public NoActiveBlockersValidator(
        ApplicationDbContext context,
        ILogger<NoActiveBlockersValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        Guid entityId,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        // Check if there are any active blockers
        var hasActiveBlockers = await _context.OrderBlockers
            .Where(ob => ob.OrderId == entityId && !ob.Resolved)
            .AnyAsync(cancellationToken);

        var result = !hasActiveBlockers; // Return true if no active blockers
        _logger.LogDebug("Order {OrderId} has no active blockers: {Result}", entityId, result);
        return result;
    }
}

