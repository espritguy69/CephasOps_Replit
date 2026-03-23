using CephasOps.Domain.Workflow;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>
/// Enqueues job execution work (Phase 3).
/// </summary>
public class JobExecutionEnqueuer : IJobExecutionEnqueuer
{
    private readonly IJobExecutionStore _store;
    private readonly ILogger<JobExecutionEnqueuer> _logger;

    public JobExecutionEnqueuer(IJobExecutionStore store, ILogger<JobExecutionEnqueuer> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task EnqueueAsync(
        string jobType,
        string payloadJson,
        Guid? companyId = null,
        string? correlationId = null,
        Guid? causationId = null,
        int priority = 0,
        DateTime? nextRunAtUtc = null,
        int maxAttempts = 5,
        CancellationToken cancellationToken = default)
    {
        var job = new JobExecution
        {
            JobType = jobType,
            PayloadJson = payloadJson ?? "{}",
            Status = JobExecutionStatus.Pending,
            MaxAttempts = Math.Clamp(maxAttempts, 1, 100),
            CompanyId = companyId,
            CorrelationId = correlationId,
            CausationId = causationId,
            Priority = priority,
            NextRunAtUtc = nextRunAtUtc
        };
        await _store.AddAsync(job, cancellationToken);
        _logger.LogInformation("Enqueued job execution {JobId} type {JobType}", job.Id, jobType);
    }

    /// <inheritdoc />
    public async Task<Guid> EnqueueWithIdAsync(
        string jobType,
        string payloadJson,
        Guid? companyId = null,
        string? correlationId = null,
        Guid? causationId = null,
        int priority = 0,
        DateTime? nextRunAtUtc = null,
        int maxAttempts = 5,
        CancellationToken cancellationToken = default)
    {
        var job = new JobExecution
        {
            JobType = jobType,
            PayloadJson = payloadJson ?? "{}",
            Status = JobExecutionStatus.Pending,
            MaxAttempts = Math.Clamp(maxAttempts, 1, 100),
            CompanyId = companyId,
            CorrelationId = correlationId,
            CausationId = causationId,
            Priority = priority,
            NextRunAtUtc = nextRunAtUtc
        };
        await _store.AddAsync(job, cancellationToken);
        _logger.LogInformation("Enqueued job execution {JobId} type {JobType}", job.Id, jobType);
        return job.Id;
    }
}
