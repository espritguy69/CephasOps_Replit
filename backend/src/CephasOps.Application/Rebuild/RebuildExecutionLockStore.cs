using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rebuild;

/// <summary>
/// Durable rebuild lock: one active lock per (RebuildTargetId, ScopeKey). Stale locks (past ExpiresAtUtc) are reclaimed on acquire.
/// </summary>
public sealed class RebuildExecutionLockStore : IRebuildExecutionLockStore
{
    public static readonly TimeSpan LockExpiry = TimeSpan.FromHours(2);

    private readonly ApplicationDbContext _context;
    private readonly ILogger<RebuildExecutionLockStore> _logger;

    public RebuildExecutionLockStore(ApplicationDbContext context, ILogger<RebuildExecutionLockStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public string GetScopeKey(Guid? scopeCompanyId) => scopeCompanyId.HasValue ? scopeCompanyId.Value.ToString("N") : "global";

    public async Task<bool> TryAcquireAsync(string rebuildTargetId, string scopeKey, Guid rebuildOperationId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now + LockExpiry;

        var existing = await _context.RebuildExecutionLocks
            .FirstOrDefaultAsync(
                e => e.RebuildTargetId == rebuildTargetId && e.ScopeKey == scopeKey && e.ReleasedAtUtc == null,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
        {
            if (existing.ExpiresAtUtc.HasValue && existing.ExpiresAtUtc.Value <= now)
            {
                existing.RebuildOperationId = rebuildOperationId;
                existing.AcquiredAtUtc = now;
                existing.ExpiresAtUtc = expiresAt;
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "Rebuild lock reclaimed (stale). TargetId={TargetId}, ScopeKey={ScopeKey}, NewOpId={OpId}",
                    rebuildTargetId, scopeKey, rebuildOperationId);
                return true;
            }
            _logger.LogWarning(
                "Rebuild lock acquisition failed: another rebuild is active. TargetId={TargetId}, ScopeKey={ScopeKey}, ActiveOpId={OpId}",
                rebuildTargetId, scopeKey, existing.RebuildOperationId);
            return false;
        }

        try
        {
            _context.RebuildExecutionLocks.Add(new RebuildExecutionLock
            {
                Id = Guid.NewGuid(),
                RebuildTargetId = rebuildTargetId,
                ScopeKey = scopeKey,
                RebuildOperationId = rebuildOperationId,
                AcquiredAtUtc = now,
                ExpiresAtUtc = expiresAt
            });
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Rebuild lock acquired. TargetId={TargetId}, ScopeKey={ScopeKey}, RebuildOperationId={OpId}",
                rebuildTargetId, scopeKey, rebuildOperationId);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogWarning(ex,
                "Rebuild lock acquisition failed (concurrent insert). TargetId={TargetId}, ScopeKey={ScopeKey}",
                rebuildTargetId, scopeKey);
            return false;
        }
    }

    public async Task ReleaseAsync(string rebuildTargetId, string scopeKey, Guid rebuildOperationId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.RebuildExecutionLocks
            .Where(e => e.RebuildTargetId == rebuildTargetId && e.ScopeKey == scopeKey && e.RebuildOperationId == rebuildOperationId && e.ReleasedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ReleasedAtUtc, now), cancellationToken)
            .ConfigureAwait(false);
        if (rows > 0)
            _logger.LogInformation(
                "Rebuild lock released. TargetId={TargetId}, ScopeKey={ScopeKey}, RebuildOperationId={OpId}",
                rebuildTargetId, scopeKey, rebuildOperationId);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        while (inner != null)
        {
            var msg = inner.Message;
            if (msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
                (inner.GetType().Name.Contains("Npgsql") && msg.Contains("23505")))
                return true;
            inner = inner.InnerException;
        }
        return false;
    }
}
