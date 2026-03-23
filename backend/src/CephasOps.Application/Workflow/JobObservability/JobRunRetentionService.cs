using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Workflow.JobObservability;

/// <summary>
/// Purges old job run records in batches for retention management.
/// </summary>
public class JobRunRetentionService : IJobRunRetentionService
{
    private readonly ApplicationDbContext _context;

    public const int MaxBatchSize = 10_000;
    public const int DefaultBatchSize = 1000;

    public JobRunRetentionService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<int> PurgeAsync(DateTime olderThanUtc, int batchSize = DefaultBatchSize, CancellationToken cancellationToken = default)
    {
        var batch = Math.Clamp(batchSize, 1, MaxBatchSize);
        var totalDeleted = 0;

        while (true)
        {
            var toDelete = await _context.JobRuns
                .Where(r => r.CompletedAtUtc != null && r.CompletedAtUtc < olderThanUtc)
                .OrderBy(r => r.CompletedAtUtc)
                .Take(batch)
                .ToListAsync(cancellationToken);

            if (toDelete.Count == 0)
                break;

            _context.JobRuns.RemoveRange(toDelete);
            await _context.SaveChangesAsync(cancellationToken);
            totalDeleted += toDelete.Count;

            if (toDelete.Count < batch)
                break;
        }

        return totalDeleted;
    }
}
