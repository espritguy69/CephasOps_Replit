namespace CephasOps.Application.Workflow.JobObservability;

/// <summary>
/// Purges old job run records for retention management.
/// </summary>
public interface IJobRunRetentionService
{
    /// <summary>
    /// Deletes completed job runs older than the given UTC time, in batches.
    /// Only runs with CompletedAtUtc set and less than cutoff are deleted.
    /// </summary>
    /// <param name="olderThanUtc">Delete runs completed before this time.</param>
    /// <param name="batchSize">Max records to delete per batch (cap applied).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total number of runs deleted.</returns>
    Task<int> PurgeAsync(DateTime olderThanUtc, int batchSize = 1000, CancellationToken cancellationToken = default);
}
