using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Executors;

/// <summary>
/// Executor for triggering invoice eligibility check when workflow transitions occur
/// Configurable via SideEffectDefinition.ExecutorConfigJson
/// </summary>
public class TriggerInvoiceEligibilitySideEffectExecutor : ISideEffectExecutor
{
    public string Key => "triggerInvoiceEligibility";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<TriggerInvoiceEligibilitySideEffectExecutor> _logger;

    public TriggerInvoiceEligibilitySideEffectExecutor(
        ApplicationDbContext context,
        ILogger<TriggerInvoiceEligibilitySideEffectExecutor> logger)
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
        _logger.LogInformation("Executing trigger invoice eligibility side effect for order {OrderId}", entityId);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for invoice eligibility check", entityId);
            return;
        }

        // Get eligibility requirements from config or use defaults
        bool requireDocket = config?.GetValueOrDefault("requireDocket")?.ToString() != "false";
        bool requirePhotos = config?.GetValueOrDefault("requirePhotos")?.ToString() != "false";
        bool requireSerials = config?.GetValueOrDefault("requireSerials")?.ToString() != "false";

        // Check all prerequisites for invoice eligibility
        bool canInvoice = (!requireDocket || order.DocketUploaded)
            && (!requirePhotos || order.PhotosUploaded)
            && (!requireSerials || order.SerialsValidated)
            && !string.IsNullOrEmpty(order.Status) 
            && order.Status != "Cancelled";

        if (canInvoice)
        {
            order.InvoiceEligible = true;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} marked as invoice eligible", entityId);
        }
        else
        {
            _logger.LogInformation("Order {OrderId} does not meet invoice eligibility requirements", entityId);
        }
    }
}

