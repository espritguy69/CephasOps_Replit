using CephasOps.Domain.Tenants.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Audit;

/// <summary>Enterprise: tenant activity timeline. All writes must be tenant-scoped; timeline read is platform-only.</summary>
public class TenantActivityService : ITenantActivityService
{
    private readonly ApplicationDbContext _context;

    public TenantActivityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RecordAsync(
        Guid tenantId,
        string eventType,
        string? entityType = null,
        Guid? entityId = null,
        string? description = null,
        Guid? userId = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType)) return;
        if (tenantId == Guid.Empty) return;

        _context.TenantActivityEvents.Add(new TenantActivityEvent
        {
            TenantId = tenantId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            UserId = userId,
            TimestampUtc = DateTime.UtcNow,
            MetadataJson = metadataJson
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantActivityEventDto>> GetTimelineAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default)
    {
        var list = await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
        {
            return await _context.TenantActivityEvents
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId)
                .OrderByDescending(e => e.TimestampUtc)
                .Take(Math.Min(take, 500))
                .Select(e => new TenantActivityEventDto
                {
                    Id = e.Id,
                    EventType = e.EventType,
                    EntityType = e.EntityType,
                    EntityId = e.EntityId,
                    Description = e.Description,
                    UserId = e.UserId,
                    TimestampUtc = e.TimestampUtc,
                    MetadataJson = e.MetadataJson
                })
                .ToListAsync(ct);
        }, cancellationToken);

        return list;
    }
}
