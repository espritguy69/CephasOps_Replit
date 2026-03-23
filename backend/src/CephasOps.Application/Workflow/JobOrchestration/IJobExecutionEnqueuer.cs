namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>
/// Enqueue job execution work (Phase 3). Business modules use this instead of adding BackgroundJob directly.
/// </summary>
public interface IJobExecutionEnqueuer
{
    Task EnqueueAsync(
        string jobType,
        string payloadJson,
        Guid? companyId = null,
        string? correlationId = null,
        Guid? causationId = null,
        int priority = 0,
        DateTime? nextRunAtUtc = null,
        int maxAttempts = 5,
        CancellationToken cancellationToken = default);

    /// <summary>Enqueue and return the created JobExecution.Id for operational visibility (e.g. API returns jobId).</summary>
    Task<Guid> EnqueueWithIdAsync(
        string jobType,
        string payloadJson,
        Guid? companyId = null,
        string? correlationId = null,
        Guid? causationId = null,
        int priority = 0,
        DateTime? nextRunAtUtc = null,
        int maxAttempts = 5,
        CancellationToken cancellationToken = default);
}
