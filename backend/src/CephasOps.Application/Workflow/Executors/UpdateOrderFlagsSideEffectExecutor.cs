using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Executors;

/// <summary>
/// Executor for updating order flags when workflow transitions occur
/// Configurable via SideEffectDefinition.ExecutorConfigJson
/// </summary>
public class UpdateOrderFlagsSideEffectExecutor : ISideEffectExecutor
{
    public string Key => "updateOrderFlags";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<UpdateOrderFlagsSideEffectExecutor> _logger;

    public UpdateOrderFlagsSideEffectExecutor(
        ApplicationDbContext context,
        ILogger<UpdateOrderFlagsSideEffectExecutor> logger)
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
        _logger.LogInformation("Executing update order flags side effect for order {OrderId}", entityId);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for flag update", entityId);
            return;
        }

        // Get flag updates from config or use defaults based on status
        if (config != null && config.ContainsKey("flags"))
        {
            // Config-driven flag updates
            var flags = config["flags"] as Dictionary<string, object>;
            if (flags != null)
            {
                foreach (var (flagName, flagValue) in flags)
                {
                    switch (flagName.ToLowerInvariant())
                    {
                        case "docketuploaded":
                            if (flagValue is bool docketValue)
                                order.DocketUploaded = docketValue;
                            break;
                        case "photosuploaded":
                            if (flagValue is bool photosValue)
                                order.PhotosUploaded = photosValue;
                            break;
                        case "invoiceeligible":
                            if (flagValue is bool invoiceValue)
                                order.InvoiceEligible = invoiceValue;
                            break;
                        case "serialsvalidated":
                            if (flagValue is bool serialsValue)
                                order.SerialsValidated = serialsValue;
                            break;
                    }
                }
            }
        }
        else
        {
            // Default behavior based on transition target status
            switch (transition.ToStatus.ToLowerInvariant())
            {
                case "docketsreceived":
                case "docketsuploaded":
                    order.DocketUploaded = true;
                    break;

                case "ordercompleted":
                case "metcustomer":
                    order.PhotosUploaded = true;
                    break;

                case "readyforinvoice":
                    order.InvoiceEligible = true;
                    break;
            }
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order flags updated for order {OrderId}", entityId);
    }
}

