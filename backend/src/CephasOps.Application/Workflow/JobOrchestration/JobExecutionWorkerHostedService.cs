using CephasOps.Application.Billing.Usage;
using CephasOps.Application.Events;
using CephasOps.Application.Subscription;
using CephasOps.Domain.Events;
using CephasOps.Domain.Workflow;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>
/// Claims JobExecutions, runs executors, marks result, emits lifecycle events (Phase 3).
/// </summary>
public class JobExecutionWorkerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobExecutionWorkerHostedService> _logger;
    private readonly JobExecutionWorkerOptions _options;

    public JobExecutionWorkerHostedService(
        IServiceProvider serviceProvider,
        ILogger<JobExecutionWorkerHostedService> logger,
        IOptions<JobExecutionWorkerOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new JobExecutionWorkerOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batchSize = Math.Clamp(_options.BatchSize, 1, 50);
        var intervalMs = Math.Max(2000, _options.PollIntervalMs);
        var nodeId = _options.NodeId ?? Environment.MachineName;
        var leaseSeconds = Math.Max(60, _options.LeaseSeconds);

        _logger.LogInformation("Job execution worker started. NodeId={NodeId}, BatchSize={BatchSize}", nodeId, batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetService<IJobExecutionStore>();
                var registry = scope.ServiceProvider.GetService<IJobExecutorRegistry>();
                var eventStore = scope.ServiceProvider.GetService<IEventStore>();

                if (store == null || registry == null)
                {
                    await Task.Delay(intervalMs, stoppingToken);
                    continue;
                }

                // Phase 4: recover stuck Running jobs (lease expired or missing and old)
                var resetCount = await store.ResetStuckRunningAsync(TimeSpan.FromSeconds(leaseSeconds * 2), stoppingToken);
                if (resetCount > 0)
                    _logger.LogWarning("Reset {Count} stuck Running job execution(s) to Pending", resetCount);

                var leaseExpiresAtUtc = DateTime.UtcNow.AddSeconds(leaseSeconds);
                var maxPerTenant = _options.TenantJobFairnessEnabled && _options.MaxJobsPerTenantPerCycle > 0
                    ? (int?)Math.Min(_options.MaxJobsPerTenantPerCycle, batchSize)
                    : null;
                var batch = await store.ClaimNextPendingBatchAsync(batchSize, nodeId, leaseExpiresAtUtc, maxPerTenant, stoppingToken);
                if (batch.Count == 0)
                {
                    await Task.Delay(intervalMs, stoppingToken);
                    continue;
                }

                _logger.LogDebug("Claimed {Count} job execution(s)", batch.Count);

                var accessService = scope.ServiceProvider.GetService<ISubscriptionAccessService>();

                foreach (var job in batch)
                {
                    try
                    {
                        await TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(job.CompanyId, async (ct) =>
                        {
                            // Phase 3: skip execution when tenant/subscription does not allow access
                            if (accessService != null && job.CompanyId.HasValue && job.CompanyId.Value != Guid.Empty)
                            {
                                var access = await accessService.GetAccessForCompanyAsync(job.CompanyId, ct);
                                if (!access.Allowed)
                                {
                                    var reason = access.DenialReason ?? "tenant_access_denied";
                                    await store.MarkFailedAsync(job.Id, $"Subscription/tenant access denied: {reason}", isNonRetryable: true, ct);
                                    await EmitJobFailedAsync(eventStore, job, reason, deadLetter: true, ct);
                                    _logger.LogWarning("Job execution {JobId} skipped: access denied ({Reason}). CompanyId: {CompanyId}", job.Id, reason, job.CompanyId);
                                    return;
                                }
                            }

                            await EmitJobStartedAsync(eventStore, job, ct);
                            var executor = registry.GetExecutor(job.JobType);
                            if (executor == null)
                            {
                                await store.MarkFailedAsync(job.Id, "No executor registered for job type: " + job.JobType, isNonRetryable: true, ct);
                                await EmitJobFailedAsync(eventStore, job, "Unknown job type", deadLetter: true, ct);
                                return;
                            }

                            var startedAt = DateTime.UtcNow;
                            bool success = false;
                            string? errorMessage = null;
                            try
                            {
                                success = await executor.ExecuteAsync(job, ct);
                            }
                            catch (Exception ex)
                            {
                                errorMessage = ex.Message;
                                var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
                                _logger.LogError(ex, "Job execution {JobId} ({JobType}) failed. CompanyId: {CompanyId}, ExecutionTimeMs: {ExecutionTimeMs}", job.Id, job.JobType, job.CompanyId, elapsedMs);
                            }

                            var executionTimeMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
                            if (success)
                            {
                                await store.MarkSucceededAsync(job.Id, ct);
                                await EmitJobCompletedAsync(eventStore, job, ct);
                                var usageService = scope.ServiceProvider.GetService<ITenantUsageService>();
                                if (usageService != null && job.CompanyId.HasValue)
                                    await usageService.RecordUsageAsync(job.CompanyId, TenantUsageService.MetricKeys.BackgroundJobsExecuted, 1, ct);
                                _logger.LogInformation("Job execution {JobId} ({JobType}) completed. CompanyId: {CompanyId}, ExecutionTimeMs: {ExecutionTimeMs}", job.Id, job.JobType, job.CompanyId, executionTimeMs);
                            }
                            else
                            {
                                var isNonRetryable = job.AttemptCount + 1 >= job.MaxAttempts;
                                await store.MarkFailedAsync(job.Id, errorMessage, isNonRetryable, ct);
                                await EmitJobFailedAsync(eventStore, job, errorMessage, deadLetter: isNonRetryable, ct);
                                _logger.LogWarning("Job execution {JobId} ({JobType}) failed (success=false). CompanyId: {CompanyId}, ExecutionTimeMs: {ExecutionTimeMs}", job.Id, job.JobType, job.CompanyId, executionTimeMs);
                            }
                        }, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing job execution {JobId}. CompanyId: {CompanyId}", job.Id, job.CompanyId);
                        try
                        {
                            await store.MarkFailedAsync(job.Id, ex.Message, isNonRetryable: false, stoppingToken);
                        }
                        catch (Exception markEx)
                        {
                            _logger.LogError(markEx, "Failed to mark job {JobId} as failed", job.Id);
                        }
                    }
                }


                if (batch.Count > 0 && _options.BusyDelayMs > 0)
                    await Task.Delay(_options.BusyDelayMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job execution worker loop error");
                await Task.Delay(intervalMs, stoppingToken);
            }
        }

        _logger.LogInformation("Job execution worker stopped");
    }

    private static async Task EmitJobStartedAsync(IEventStore? eventStore, JobExecution job, CancellationToken ct)
    {
        if (eventStore == null) return;
        var evt = new JobStartedEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            CompanyId = job.CompanyId,
            CorrelationId = job.CorrelationId ?? job.Id.ToString(),
            CausationId = job.CausationId,
            Source = "JobOrchestration",
            JobId = job.Id,
            JobType = job.JobType,
            AttemptCount = job.AttemptCount
        };
        await eventStore.AppendAsync(evt, null, ct);
    }

    private static async Task EmitJobCompletedAsync(IEventStore? eventStore, JobExecution job, CancellationToken ct)
    {
        if (eventStore == null) return;
        var evt = new JobCompletedEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            CompanyId = job.CompanyId,
            CorrelationId = job.CorrelationId ?? job.Id.ToString(),
            CausationId = job.CausationId,
            Source = "JobOrchestration",
            JobId = job.Id,
            JobType = job.JobType,
            AttemptCount = job.AttemptCount
        };
        await eventStore.AppendAsync(evt, null, ct);
    }

    private static async Task EmitJobFailedAsync(IEventStore? eventStore, JobExecution job, string? errorMessage, bool deadLetter, CancellationToken ct)
    {
        if (eventStore == null) return;
        var evt = new JobFailedEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            CompanyId = job.CompanyId,
            CorrelationId = job.CorrelationId ?? job.Id.ToString(),
            CausationId = job.CausationId,
            Source = "JobOrchestration",
            JobId = job.Id,
            JobType = job.JobType,
            AttemptCount = job.AttemptCount,
            ErrorMessage = errorMessage,
            DeadLetter = deadLetter
        };
        await eventStore.AppendAsync(evt, null, ct);
    }
}

/// <summary>Options for JobExecutionWorkerHostedService.</summary>
public class JobExecutionWorkerOptions
{
    public const string SectionName = "JobOrchestration:Worker";

    public int BatchSize { get; set; } = 10;
    public int PollIntervalMs { get; set; } = 5000;
    public int BusyDelayMs { get; set; } = 500;
    public string? NodeId { get; set; }
    public int LeaseSeconds { get; set; } = 300;

    /// <summary>SaaS scaling: max jobs claimed per tenant (CompanyId) per cycle when tenant fairness is enabled.</summary>
    public int MaxJobsPerTenantPerCycle { get; set; } = 5;
    /// <summary>SaaS scaling: max concurrent jobs per worker (0 = no limit).</summary>
    public int MaxConcurrentJobs { get; set; }
    /// <summary>SaaS scaling: when true, cap jobs per tenant per cycle to prevent one tenant from starving others.</summary>
    public bool TenantJobFairnessEnabled { get; set; } = true;
}
