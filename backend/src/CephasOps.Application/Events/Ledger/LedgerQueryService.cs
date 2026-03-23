using CephasOps.Application.Events.Ledger.DTOs;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Events.Ledger;

public sealed class LedgerQueryService : ILedgerQueryService
{
    private readonly ApplicationDbContext _context;

    public LedgerQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<LedgerEntryDto> Items, int Total)> ListAsync(
        Guid? companyId,
        string? entityType,
        Guid? entityId,
        string? ledgerFamily,
        DateTime? fromOccurredUtc,
        DateTime? toOccurredUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var q = _context.LedgerEntries.AsNoTracking().AsQueryable();
        if (companyId.HasValue)
            q = q.Where(e => e.CompanyId == companyId.Value);
        if (!string.IsNullOrWhiteSpace(entityType))
            q = q.Where(e => e.EntityType == entityType);
        if (entityId.HasValue)
            q = q.Where(e => e.EntityId == entityId.Value);
        if (!string.IsNullOrWhiteSpace(ledgerFamily))
            q = q.Where(e => e.LedgerFamily == ledgerFamily);
        if (fromOccurredUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc >= fromOccurredUtc.Value);
        if (toOccurredUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc <= toOccurredUtc.Value);

        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var size = Math.Clamp(pageSize, 1, 100);
        var safePage = Math.Max(1, page);
        var items = await q
            .OrderBy(e => e.OccurredAtUtc)
            .ThenBy(e => e.Id)
            .Skip((safePage - 1) * size)
            .Take(size)
            .Select(e => new LedgerEntryDto
            {
                Id = e.Id,
                SourceEventId = e.SourceEventId,
                ReplayOperationId = e.ReplayOperationId,
                LedgerFamily = e.LedgerFamily,
                Category = e.Category,
                CompanyId = e.CompanyId,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                EventType = e.EventType,
                OccurredAtUtc = e.OccurredAtUtc,
                RecordedAtUtc = e.RecordedAtUtc,
                PayloadSnapshot = e.PayloadSnapshot,
                CorrelationId = e.CorrelationId,
                TriggeredByUserId = e.TriggeredByUserId,
                OrderingStrategyId = e.OrderingStrategyId
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return (items, total);
    }

    public async Task<LedgerEntryDto?> GetByIdAsync(Guid id, Guid? scopeCompanyId, CancellationToken cancellationToken = default)
    {
        var q = _context.LedgerEntries.AsNoTracking().Where(e => e.Id == id);
        if (scopeCompanyId.HasValue)
            q = q.Where(e => e.CompanyId == scopeCompanyId.Value);
        var e = await q.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return e == null ? null : Map(e);
    }

    private static LedgerEntryDto Map(LedgerEntry e) => new()
    {
        Id = e.Id,
        SourceEventId = e.SourceEventId,
        ReplayOperationId = e.ReplayOperationId,
        LedgerFamily = e.LedgerFamily,
        Category = e.Category,
        CompanyId = e.CompanyId,
        EntityType = e.EntityType,
        EntityId = e.EntityId,
        EventType = e.EventType,
        OccurredAtUtc = e.OccurredAtUtc,
        RecordedAtUtc = e.RecordedAtUtc,
        PayloadSnapshot = e.PayloadSnapshot,
        CorrelationId = e.CorrelationId,
        TriggeredByUserId = e.TriggeredByUserId,
        OrderingStrategyId = e.OrderingStrategyId
    };
}
