using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.Executors;

/// <summary>
/// Executor for creating order status log entries when workflow transitions occur
/// Configurable via SideEffectDefinition.ExecutorConfigJson
/// </summary>
public class CreateOrderStatusLogSideEffectExecutor : ISideEffectExecutor
{
    public string Key => "createOrderStatusLog";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateOrderStatusLogSideEffectExecutor> _logger;

    public CreateOrderStatusLogSideEffectExecutor(
        ApplicationDbContext context,
        ILogger<CreateOrderStatusLogSideEffectExecutor> logger)
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
        _logger.LogInformation("Executing create order status log side effect for order {OrderId}", entityId);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for status log", entityId);
            return;
        }

        // Extract metadata from payload
        string? metadataJson = null;
        if (payload != null && payload.ContainsKey("metadata"))
        {
            metadataJson = JsonSerializer.Serialize(payload["metadata"]);
        }

        var statusLog = new Domain.Orders.Entities.OrderStatusLog
        {
            Id = Guid.NewGuid(),
            CompanyId = order.CompanyId,
            OrderId = order.Id,
            FromStatus = transition.FromStatus,
            ToStatus = transition.ToStatus,
            TransitionReason = payload?.ContainsKey("reason") == true ? payload["reason"]?.ToString() : null,
            TriggeredByUserId = payload?.ContainsKey("userId") == true && Guid.TryParse(payload["userId"]?.ToString(), out var userId) ? userId : null,
            TriggeredBySiId = payload?.ContainsKey("siId") == true && Guid.TryParse(payload["siId"]?.ToString(), out var siId) ? siId : order.AssignedSiId,
            Source = payload?.ContainsKey("source") == true ? payload["source"]?.ToString() ?? "WorkflowEngine" : "WorkflowEngine",
            MetadataJson = metadataJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrderStatusLogs.Add(statusLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order status log created for order {OrderId}", entityId);
    }
}

