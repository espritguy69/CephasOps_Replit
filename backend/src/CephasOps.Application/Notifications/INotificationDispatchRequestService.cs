namespace CephasOps.Application.Notifications;

/// <summary>
/// Requests notification delivery work for order status changes (Phase 2).
/// Creates NotificationDispatch rows for worker-driven delivery; does not send inline.
/// </summary>
public interface INotificationDispatchRequestService
{
    /// <summary>
    /// Enqueue delivery work for order status change (SMS/WhatsApp per settings).
    /// Idempotent: uses sourceEventId + channel + target as idempotency key.
    /// </summary>
    Task RequestOrderStatusNotificationAsync(
        Guid orderId,
        string newStatus,
        Guid? companyId,
        Guid sourceEventId,
        string? correlationId,
        Guid? causationId,
        CancellationToken cancellationToken = default);
}
