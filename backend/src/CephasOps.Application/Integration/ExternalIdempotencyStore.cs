using CephasOps.Domain.Integration.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Integration;

/// <summary>
/// Idempotency for inbound webhooks: one successful processing per external idempotency key.
/// </summary>
public class ExternalIdempotencyStore : IExternalIdempotencyStore
{
    private readonly ApplicationDbContext _context;

    public ExternalIdempotencyStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryClaimAsync(string idempotencyKey, string connectorKey, Guid? companyId, Guid receiptId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.ExternalIdempotencyRecords
            .AnyAsync(e => e.IdempotencyKey == idempotencyKey && e.ConnectorKey == connectorKey && e.CompanyId == companyId, cancellationToken);
        if (exists)
            return false;

        _context.ExternalIdempotencyRecords.Add(new ExternalIdempotencyRecord
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            ConnectorKey = connectorKey,
            CompanyId = companyId,
            InboundWebhookReceiptId = receiptId,
            CreatedAtUtc = DateTime.UtcNow
        });
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Unique constraint - already processed
            return false;
        }
    }

    public async Task MarkCompletedAsync(string idempotencyKey, string connectorKey, Guid? companyId, Guid receiptId, CancellationToken cancellationToken = default)
    {
        var record = await _context.ExternalIdempotencyRecords
            .FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey && e.ConnectorKey == connectorKey && e.CompanyId == companyId, cancellationToken);
        if (record != null)
        {
            record.InboundWebhookReceiptId = receiptId;
            record.CompletedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> IsCompletedAsync(string idempotencyKey, string connectorKey, Guid? companyId, CancellationToken cancellationToken = default)
    {
        return await _context.ExternalIdempotencyRecords
            .AnyAsync(e => e.IdempotencyKey == idempotencyKey && e.ConnectorKey == connectorKey && e.CompanyId == companyId && e.CompletedAtUtc != null, cancellationToken);
    }
}
