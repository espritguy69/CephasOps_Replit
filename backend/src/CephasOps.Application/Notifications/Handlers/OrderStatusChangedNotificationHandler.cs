using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Settings.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Notifications.Handlers;

/// <summary>
/// Handler for order status change events - sends SMS/WhatsApp notifications to customers
/// </summary>
public class OrderStatusChangedNotificationHandler
{
    private readonly CustomerNotificationService _customerNotificationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderStatusChangedNotificationHandler> _logger;

    public OrderStatusChangedNotificationHandler(
        CustomerNotificationService customerNotificationService,
        IServiceProvider serviceProvider,
        ILogger<OrderStatusChangedNotificationHandler> logger)
    {
        _customerNotificationService = customerNotificationService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Handle order status change - send notifications if enabled
    /// This is called asynchronously (fire-and-forget) to avoid blocking workflow
    /// </summary>
    public async Task HandleAsync(Guid orderId, string newStatus, Guid? companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Lazily resolve services to break circular dependency
            var globalSettingsService = _serviceProvider.GetRequiredService<IGlobalSettingsService>();
            var orderService = _serviceProvider.GetRequiredService<IOrderService>();

            // Check if auto-send is enabled
            var smsAutoSend = await globalSettingsService.GetValueAsync<bool>("SMS_AutoSendOnStatusChange", cancellationToken);
            var whatsAppAutoSend = await globalSettingsService.GetValueAsync<bool>("WhatsApp_AutoSendOnStatusChange", cancellationToken);

            if (!smsAutoSend && !whatsAppAutoSend)
            {
                _logger.LogDebug("Auto-send notifications are disabled. Skipping notification for order {OrderId}", orderId);
                return;
            }

            // Get order details
            OrderDto? order = null;
            if (companyId.HasValue)
            {
                order = await orderService.GetOrderByIdAsync(orderId, companyId.Value, null, cancellationToken);
            }
            else
            {
                // Try to get order without company filter (for super admin)
                order = await orderService.GetOrderByIdAsync(orderId, null, null, cancellationToken);
            }

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found. Cannot send notification.", orderId);
                return;
            }

            // Use order's company ID if available
            var effectiveCompanyId = order.CompanyId ?? companyId ?? Guid.Empty;

            // Send notification
            await _customerNotificationService.SendOrderStatusNotificationAsync(
                order,
                newStatus,
                effectiveCompanyId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - notifications should never block workflow
            _logger.LogError(ex, "Failed to handle order status change notification for order {OrderId}. Error: {ErrorMessage}",
                orderId, ex.Message);
        }
    }
}

