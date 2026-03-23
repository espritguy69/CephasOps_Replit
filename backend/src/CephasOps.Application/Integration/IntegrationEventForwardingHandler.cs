using CephasOps.Application.Events;
using CephasOps.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Integration;

/// <summary>
/// Forwards selected domain events to the outbound integration bus so connectors receive them.
/// Idempotent: outbound delivery is keyed by (EventId, EndpointId); duplicate handler run (e.g. on replay) does not create duplicate deliveries.
/// First-wave: WorkflowTransitionCompleted, OrderStatusChanged, OrderAssigned. Register in DI as handler for each event type.
/// </summary>
public sealed class IntegrationEventForwardingHandler :
    IDomainEventHandler<WorkflowTransitionCompletedEvent>,
    IDomainEventHandler<OrderStatusChangedEvent>,
    IDomainEventHandler<OrderAssignedEvent>,
    IDomainEventHandler<OrderCreatedEvent>,
    IDomainEventHandler<OrderCompletedEvent>,
    IDomainEventHandler<InvoiceGeneratedEvent>,
    IDomainEventHandler<MaterialIssuedEvent>,
    IDomainEventHandler<MaterialReturnedEvent>,
    IDomainEventHandler<PayrollCalculatedEvent>
{
    private readonly IOutboundIntegrationBus _outboundBus;
    private readonly IDomainEventToPlatformEnvelopeBuilder _envelopeBuilder;
    private readonly ILogger<IntegrationEventForwardingHandler> _logger;

    public IntegrationEventForwardingHandler(
        IOutboundIntegrationBus outboundBus,
        IDomainEventToPlatformEnvelopeBuilder envelopeBuilder,
        ILogger<IntegrationEventForwardingHandler> logger)
    {
        _outboundBus = outboundBus;
        _envelopeBuilder = envelopeBuilder;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowTransitionCompletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ForwardAsync(domainEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(OrderStatusChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ForwardAsync(domainEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(OrderAssignedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ForwardAsync(domainEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(OrderCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ForwardAsync(domainEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(OrderCompletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ForwardAsync(domainEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(InvoiceGeneratedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ForwardAsync(domainEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(MaterialIssuedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ForwardAsync(domainEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(MaterialReturnedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ForwardAsync(domainEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(PayrollCalculatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await ForwardAsync(domainEvent, cancellationToken);
    }

    private async Task ForwardAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = _envelopeBuilder.Build(domainEvent);
            await _outboundBus.PublishAsync(envelope, cancellationToken);
            _logger.LogDebug("Forwarded domain event {EventId} ({EventType}) to integration bus", domainEvent.EventId, domainEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to forward domain event {EventId} ({EventType}) to integration bus", domainEvent.EventId, domainEvent.EventType);
            throw;
        }
    }
}
