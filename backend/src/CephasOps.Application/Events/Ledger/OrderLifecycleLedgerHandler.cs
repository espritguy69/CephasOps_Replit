using CephasOps.Application.Events.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Events.Ledger;

/// <summary>
/// Writes OrderStatusChanged events into the operational event ledger (OrderLifecycle family).
/// Replay-safe; idempotent by (SourceEventId, Family). Runs on live and on all replay targets.
/// Category is derived from NewStatus via OrderLifecycleCategoryHelper (Assignment, FieldProgress, Docket, InvoiceReadiness, Completion, or StatusChanged).
/// </summary>
public sealed class OrderLifecycleLedgerHandler : IProjectionEventHandler<OrderStatusChangedEvent>
{
    private readonly ILedgerWriter _ledgerWriter;
    private readonly ILogger<OrderLifecycleLedgerHandler> _logger;

    public OrderLifecycleLedgerHandler(ILedgerWriter ledgerWriter, ILogger<OrderLifecycleLedgerHandler> logger)
    {
        _ledgerWriter = ledgerWriter;
        _logger = logger;
    }

    public async Task HandleAsync(OrderStatusChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            domainEvent.OrderId,
            domainEvent.PreviousStatus,
            domainEvent.NewStatus
        });
        await _ledgerWriter.AppendFromEventAsync(
            domainEvent.EventId,
            LedgerFamilies.OrderLifecycle,
            domainEvent.EventType,
            domainEvent.OccurredAtUtc,
            domainEvent.CompanyId,
            "Order",
            domainEvent.OrderId,
            payload,
            domainEvent.CorrelationId,
            domainEvent.TriggeredByUserId,
            OrderingStrategies.OccurredAtUtcAscendingEventIdAscending,
            category: OrderLifecycleCategoryHelper.GetCategoryForNewStatus(domainEvent.NewStatus),
            cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Ledger appended OrderLifecycle entry for OrderId={OrderId}, EventId={EventId}", domainEvent.OrderId, domainEvent.EventId);
    }
}
