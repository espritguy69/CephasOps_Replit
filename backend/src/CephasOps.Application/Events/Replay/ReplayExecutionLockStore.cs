using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Durable replay lock: one active lock per company. Stale locks (past ExpiresAtUtc) are reclaimed on acquire.
/// </summary>
public class ReplayExecutionLockStore : IReplayExecutionLockStore
{
    /// <summary>Lock is considered stale after this duration if not released (e.g. worker crash).</summary>
    public static readonly TimeSpan LockExpiry = TimeSpan.FromHours(2);

    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReplayExecutionLockStore> _logger;

    public ReplayExecutionLockStore(ApplicationDbContext context, ILogger<ReplayExecutionLockStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireAsync(Guid companyId, Guid replayOperationId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now + LockExpiry;

        var existing = await _context.ReplayExecutionLock
            .FirstOrDefaultAsync(
                e => e.CompanyId == companyId && e.ReleasedAtUtc == null,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
        {
            if (existing.ExpiresAtUtc.HasValue && existing.ExpiresAtUtc.Value <= now)
            {
                var previousOpId = existing.ReplayOperationId;
                existing.ReplayOperationId = replayOperationId;
                existing.AcquiredAtUtc = now;
                existing.ExpiresAtUtc = expiresAt;
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "Replay execution lock reclaimed (stale). CompanyId={CompanyId}, PreviousOpId={PreviousOpId}, NewOpId={NewOpId}",
                    companyId, previousOpId, replayOperationId);
                return true;
            }
            _logger.LogWarning(
                "Replay execution lock acquisition failed: another replay is active. CompanyId={CompanyId}, ActiveReplayOperationId={ActiveOpId}",
                companyId, existing.ReplayOperationId);
            return false;
        }

        try
        {
            _context.ReplayExecutionLock.Add(new ReplayExecutionLock
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                ReplayOperationId = replayOperationId,
                AcquiredAtUtc = now,
                ExpiresAtUtc = expiresAt
            });
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Replay execution lock acquired. CompanyId={CompanyId}, ReplayOperationId={OpId}",
                companyId, replayOperationId);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogWarning(ex,
                "Replay execution lock acquisition failed (concurrent insert). CompanyId={CompanyId}, ReplayOperationId={OpId}",
                companyId, replayOperationId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(Guid companyId, Guid replayOperationId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await _context.ReplayExecutionLock
            .Where(e => e.CompanyId == companyId && e.ReplayOperationId == replayOperationId && e.ReleasedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ReleasedAtUtc, now), cancellationToken)
            .ConfigureAwait(false);
        if (rows > 0)
            _logger.LogInformation(
                "Replay execution lock released. CompanyId={CompanyId}, ReplayOperationId={OpId}",
                companyId, replayOperationId);
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
