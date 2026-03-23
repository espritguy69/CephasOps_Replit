using CephasOps.Domain.Integration.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Integration;

/// <summary>
/// Persistence for inbound webhook receipts.
/// </summary>
public class InboundWebhookReceiptStore : IInboundWebhookReceiptStore
{
    private readonly ApplicationDbContext _context;

    public InboundWebhookReceiptStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InboundWebhookReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InboundWebhookReceipts
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<InboundWebhookReceipt> Items, int TotalCount)> ListAsync(
        string? connectorKey,
        Guid? companyId,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var q = _context.InboundWebhookReceipts.AsNoTracking();
        if (!string.IsNullOrEmpty(connectorKey))
            q = q.Where(e => e.ConnectorKey == connectorKey);
        if (companyId.HasValue)
            q = q.Where(e => e.CompanyId == companyId.Value);
        if (!string.IsNullOrEmpty(status))
            q = q.Where(e => e.Status == status);
        if (fromUtc.HasValue)
            q = q.Where(e => e.ReceivedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue)
            q = q.Where(e => e.ReceivedAtUtc <= toUtc.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(e => e.ReceivedAtUtc).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task CreateAsync(InboundWebhookReceipt receipt, CancellationToken cancellationToken = default)
    {
        _context.InboundWebhookReceipts.Add(receipt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(InboundWebhookReceipt receipt, CancellationToken cancellationToken = default)
    {
        _context.InboundWebhookReceipts.Update(receipt);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
