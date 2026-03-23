using CephasOps.Domain.Workflow.Entities;

namespace CephasOps.Domain.Workflow;

/// <summary>
/// Persistence for job orchestration (Phase 3). Implementation in Infrastructure.
/// </summary>
public interface IJobExecutionStore
{
    Task AddAsync(JobExecution job, CancellationToken cancellationToken = default);
    /// <summary>Claim up to maxCount pending (or due NextRunAtUtc) jobs; set Status=Running and lease. When maxPerTenant is set, caps claimed jobs per CompanyId (tenant fairness). Returns claimed rows.</summary>
    Task<IReadOnlyList<JobExecution>> ClaimNextPendingBatchAsync(int maxCount, string? nodeId, DateTime? leaseExpiresAtUtc, int? maxPerTenant = null, CancellationToken cancellationToken = default);
    /// <summary>Mark job as Succeeded; clear lease.</summary>
    Task MarkSucceededAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Mark job as Failed or DeadLetter; set NextRunAtUtc for retry when AttemptCount &lt; MaxAttempts.</summary>
    Task MarkFailedAsync(Guid id, string? errorMessage, bool isNonRetryable, CancellationToken cancellationToken = default);

    /// <summary>Reset Running jobs whose lease has expired to Pending so they can be re-claimed. Does not increment AttemptCount. Returns count reset.</summary>
    Task<int> ResetStuckRunningAsync(TimeSpan leaseExpiry, CancellationToken cancellationToken = default);
}
