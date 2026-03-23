using CephasOps.Application.Events;
using CephasOps.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Audit;

/// <summary>
/// Records selected domain events to the tenant activity timeline. Runs in-process under the event's tenant scope
/// (EventStoreDispatcherHostedService uses TenantScopeExecutor with entry.CompanyId). Only records when CompanyId is set;
/// skips platform or unset-tenant events to avoid cross-tenant or invalid timeline entries.
/// </summary>
public sealed class TenantActivityTimelineFromEventsHandler :
    IDomainEventHandler<OrderCreatedEvent>,
    IDomainEventHandler<OrderCompletedEvent>,
    IDomainEventHandler<OrderAssignedEvent>,
    IDomainEventHandler<OrderStatusChangedEvent>
{
    private readonly ITenantActivityService _timeline;
    private readonly ILogger<TenantActivityTimelineFromEventsHandler> _logger;

    public TenantActivityTimelineFromEventsHandler(
        ITenantActivityService timeline,
        ILogger<TenantActivityTimelineFromEventsHandler> logger)
    {
        _timeline = timeline;
        _logger = logger;
    }

    public Task HandleAsync(OrderCreatedEvent domainEvent, CancellationToken cancellationToken = default) =>
        RecordIfTenantScopedAsync(domainEvent, "Order", domainEvent.OrderId, "Order created", cancellationToken);

    public Task HandleAsync(OrderCompletedEvent domainEvent, CancellationToken cancellationToken = default) =>
        RecordIfTenantScopedAsync(domainEvent, "Order", domainEvent.OrderId, "Order completed", cancellationToken);

    public Task HandleAsync(OrderAssignedEvent domainEvent, CancellationToken cancellationToken = default) =>
        RecordIfTenantScopedAsync(domainEvent, "Order", domainEvent.OrderId, "Order assigned", cancellationToken);

    public Task HandleAsync(OrderStatusChangedEvent domainEvent, CancellationToken cancellationToken = default) =>
        RecordIfTenantScopedAsync(domainEvent, "Order", domainEvent.OrderId, "Order status changed", cancellationToken);

    private async Task RecordIfTenantScopedAsync(
        IDomainEvent domainEvent,
        string? entityType,
        Guid? entityId,
        string description,
        CancellationToken cancellationToken)
    {
        if (!domainEvent.CompanyId.HasValue || domainEvent.CompanyId.Value == Guid.Empty)
        {
            _logger.LogDebug(
                "TenantActivityTimeline: skipping event {EventId} ({EventType}) — no tenant context; timeline records only tenant-scoped events",
                domainEvent.EventId, domainEvent.EventType);
            return;
        }

        try
        {
            await _timeline.RecordAsync(
                domainEvent.CompanyId.Value,
                domainEvent.EventType,
                entityType,
                entityId,
                description,
                domainEvent.TriggeredByUserId,
                metadataJson: null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "TenantActivityTimeline: failed to record event {EventId} ({EventType}) for tenant {CompanyId}",
                domainEvent.EventId, domainEvent.EventType, domainEvent.CompanyId);
            throw;
        }
    }
}
