using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Executors;

/// <summary>
/// Executor for creating stock movements when workflow transitions occur
/// Configurable via SideEffectDefinition.ExecutorConfigJson
/// </summary>
public class CreateStockMovementSideEffectExecutor : ISideEffectExecutor
{
    public string Key => "createStockMovement";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateStockMovementSideEffectExecutor> _logger;

    public CreateStockMovementSideEffectExecutor(
        ApplicationDbContext context,
        ILogger<CreateStockMovementSideEffectExecutor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Guid entityId,
        WorkflowTransitionDto transition,
        Dictionary<string, object>? payload,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing create stock movement side effect for order {OrderId}", entityId);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for stock movement", entityId);
            return;
        }

        var materialUsages = await _context.OrderMaterialUsage
            .Where(omu => omu.OrderId == order.Id)
            .ToListAsync(cancellationToken);

        if (materialUsages.Count == 0)
        {
            _logger.LogWarning("No material usage found for order {OrderId}, skipping stock movement creation", entityId);
            return;
        }

        // Get movement type from config or determine from transition
        string movementType = config?.GetValueOrDefault("movementType")?.ToString() 
            ?? transition.ToStatus.ToLowerInvariant() switch
            {
                "assigned" => "IssueToSI",
                "ordercompleted" => "UseForOrder",
                "metcustomer" => "ConsumeForOrder",
                _ => "Unknown"
            };

        foreach (var materialUsage in materialUsages)
        {
            var stockMovement = new Domain.Inventory.Entities.StockMovement
            {
                Id = Guid.NewGuid(),
                CompanyId = order.CompanyId,
                MaterialId = materialUsage.MaterialId,
                Quantity = materialUsage.Quantity,
                MovementType = movementType,
                OrderId = order.Id,
                ServiceInstallerId = order.AssignedSiId,
                Remarks = $"Automatic stock movement for order {order.ServiceId} status change to {transition.ToStatus}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(stockMovement);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stock movements created for order {OrderId}", entityId);
    }
}

