using CephasOps.Application.Events;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Notifications.Handlers;

/// <summary>
/// Handles OrderStatusChangedEvent by enqueueing notification delivery work (Phase 2).
/// Does not send inline; creates NotificationDispatch rows for worker-driven delivery.
/// </summary>
public sealed class OrderStatusNotificationDispatchHandler : IDomainEventHandler<OrderStatusChangedEvent>
{
    private readonly INotificationDispatchRequestService _requestService;
    private readonly ILogger<OrderStatusNotificationDispatchHandler> _logger;

    public OrderStatusNotificationDispatchHandler(
        INotificationDispatchRequestService requestService,
        ILogger<OrderStatusNotificationDispatchHandler> logger)
    {
        _requestService = requestService;
        _logger = logger;
    }

    public async Task HandleAsync(OrderStatusChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _requestService.RequestOrderStatusNotificationAsync(
            domainEvent.OrderId,
            domainEvent.NewStatus,
            domainEvent.CompanyId,
            domainEvent.EventId,
            domainEvent.CorrelationId,
            domainEvent.CausationId,
            cancellationToken);
    }
}
