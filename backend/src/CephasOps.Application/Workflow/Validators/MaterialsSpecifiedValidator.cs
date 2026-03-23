using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if materials are specified for an order
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class MaterialsSpecifiedValidator : IGuardConditionValidator
{
    public string Key => "materialsSpecified";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<MaterialsSpecifiedValidator> _logger;

    public MaterialsSpecifiedValidator(
        ApplicationDbContext context,
        ILogger<MaterialsSpecifiedValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        Guid entityId,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        // Check if OrderMaterialUsage has any items
        var hasMaterials = await _context.OrderMaterialUsage
            .Where(omu => omu.OrderId == entityId)
            .AnyAsync(cancellationToken);

        _logger.LogDebug("Order {OrderId} has materials specified: {Result}", entityId, hasMaterials);
        return hasMaterials;
    }
}

