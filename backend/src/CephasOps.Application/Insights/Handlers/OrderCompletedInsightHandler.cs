using CephasOps.Application.Events;
using CephasOps.Domain.Events;
using CephasOps.Domain.Insights.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Insights.Handlers;

/// <summary>
/// Writes an OperationalInsight when an order is completed (field ops intelligence seed).
/// Idempotent: can use EventId or (CompanyId, EntityType, EntityId, Type) to avoid duplicates on replay.
/// </summary>
public class OrderCompletedInsightHandler : IDomainEventHandler<OrderCompletedEvent>
{
    public const string InsightTypeOrderCompleted = "OrderCompleted";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderCompletedInsightHandler> _logger;

    public OrderCompletedInsightHandler(ApplicationDbContext context, ILogger<OrderCompletedInsightHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCompletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (!domainEvent.CompanyId.HasValue || domainEvent.CompanyId.Value == Guid.Empty)
            return;

        var companyId = domainEvent.CompanyId.Value;
        var exists = await _context.OperationalInsights
            .AnyAsync(o => o.CompanyId == companyId && o.Type == InsightTypeOrderCompleted && o.EntityType == "Order" && o.EntityId == domainEvent.OrderId, cancellationToken);
        if (exists)
            return;

        var payload = new { domainEvent.OrderId, domainEvent.WorkflowJobId };
        var payloadJson = JsonSerializer.Serialize(payload);

        var insight = new OperationalInsight
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Type = InsightTypeOrderCompleted,
            PayloadJson = payloadJson,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
            EntityType = "Order",
            EntityId = domainEvent.OrderId
        };
        _context.OperationalInsights.Add(insight);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Recorded OperationalInsight OrderCompleted for OrderId={OrderId}, CompanyId={CompanyId}", domainEvent.OrderId, companyId);
    }
}
