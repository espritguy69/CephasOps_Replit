using CephasOps.Domain.Integration.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Integration;

/// <summary>
/// Persistence for outbound integration deliveries and attempts.
/// </summary>
public class OutboundDeliveryStore : IOutboundDeliveryStore
{
    private readonly ApplicationDbContext _context;

    public OutboundDeliveryStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OutboundIntegrationDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundIntegrationDeliveries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboundIntegrationDelivery>> GetPendingOrRetryAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.OutboundIntegrationDeliveries
            .AsNoTracking()
            .Where(e => (e.Status == OutboundIntegrationDelivery.Statuses.Pending || e.Status == OutboundIntegrationDelivery.Statuses.Failed) &&
                        (e.NextRetryAtUtc == null || e.NextRetryAtUtc <= now) &&
                        e.AttemptCount < e.MaxAttempts)
            .OrderBy(e => e.CreatedAtUtc)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<OutboundIntegrationDelivery> Items, int TotalCount)> ListAsync(
        Guid? connectorEndpointId,
        Guid? companyId,
        string? eventType,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var q = _context.OutboundIntegrationDeliveries.AsNoTracking();
        if (connectorEndpointId.HasValue)
            q = q.Where(e => e.ConnectorEndpointId == connectorEndpointId.Value);
        if (companyId.HasValue)
            q = q.Where(e => e.CompanyId == companyId.Value);
        if (!string.IsNullOrEmpty(eventType))
            q = q.Where(e => e.EventType == eventType);
        if (!string.IsNullOrEmpty(status))
            q = q.Where(e => e.Status == status);
        if (fromUtc.HasValue)
            q = q.Where(e => e.CreatedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue)
            q = q.Where(e => e.CreatedAtUtc <= toUtc.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(e => e.CreatedAtUtc).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<OutboundIntegrationDelivery?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.OutboundIntegrationDeliveries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task CreateDeliveryAsync(OutboundIntegrationDelivery delivery, CancellationToken cancellationToken = default)
    {
        _context.OutboundIntegrationDeliveries.Add(delivery);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDeliveryAsync(OutboundIntegrationDelivery delivery, CancellationToken cancellationToken = default)
    {
        _context.OutboundIntegrationDeliveries.Update(delivery);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAttemptAsync(OutboundIntegrationAttempt attempt, CancellationToken cancellationToken = default)
    {
        _context.OutboundIntegrationAttempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
