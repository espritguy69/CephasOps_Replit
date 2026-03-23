using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Executors;

/// <summary>
/// Executor for sending notifications when workflow transitions occur
/// Configurable via SideEffectDefinition.ExecutorConfigJson
/// </summary>
public class NotifySideEffectExecutor : ISideEffectExecutor
{
    public string Key => "notify";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotifySideEffectExecutor> _logger;

    public NotifySideEffectExecutor(
        ApplicationDbContext context,
        ILogger<NotifySideEffectExecutor> logger)
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
        _logger.LogInformation("Executing notify side effect for order {OrderId}", entityId);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for notification", entityId);
            return;
        }

        // Get notification template from config (if specified)
        string? template = config?.GetValueOrDefault("template")?.ToString();
        string? title = config?.GetValueOrDefault("title")?.ToString();
        string? message = config?.GetValueOrDefault("message")?.ToString();

        // Default notification if not configured
        if (string.IsNullOrEmpty(title))
        {
            title = $"Order {order.ServiceId} status changed";
        }

        if (string.IsNullOrEmpty(message))
        {
            message = $"Order status changed from {transition.FromStatus ?? "N/A"} to {transition.ToStatus}";
        }

        // Determine recipient from config or default to assigned SI
        Guid? recipientUserId = null;
        if (config?.ContainsKey("recipientUserId") == true && 
            Guid.TryParse(config["recipientUserId"]?.ToString(), out var userId))
        {
            recipientUserId = userId;
        }
        else if (order.AssignedSiId.HasValue)
        {
            recipientUserId = order.AssignedSiId.Value;
        }

        if (!recipientUserId.HasValue)
        {
            _logger.LogWarning("No recipient found for notification on order {OrderId}", entityId);
            return;
        }

        var notification = new Domain.Notifications.Entities.Notification
        {
            Id = Guid.NewGuid(),
            CompanyId = order.CompanyId,
            UserId = recipientUserId.Value,
            Type = template ?? "OrderStatusChange",
            Title = title,
            Message = message,
            Status = "Unread",
            RelatedEntityType = "Order",
            RelatedEntityId = order.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification created for order {OrderId} status change", entityId);
    }
}

