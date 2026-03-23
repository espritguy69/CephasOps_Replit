using CephasOps.Application.Events;
using CephasOps.Application.Events.Replay;
using CephasOps.Application.Rebuild;
using CephasOps.Application.Workflow;
using CephasOps.Application.Workers;
using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Domain.Events;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Metrics;
using CephasOps.Infrastructure.Operational;
using CephasOps.Infrastructure.Persistence;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Pnl.Services;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Billing.Usage;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Parser.Services.Converters;
using CephasOps.Application.Sla;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Domain.Users.Entities;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Processes queued BackgroundJob rows (legacy pipeline). Phase 4/5: the following types have been migrated to JobExecution and must not be enqueued here: pnlrebuild, reconcileledgerbalancecache, slaevaluation, populatestockbylocationsnapshots, documentgeneration.
/// Legacy-only types: none (all migrated). eventhandlingasync, operationalreplay, operationalrebuild: deprecated (Phase 11); drained only; real execution via JobExecution.
/// inventoryreportexport: deprecated (Phase 10); drained only; real execution via JobExecution + InventoryReportExportJobExecutor.
/// myinvoisstatuspoll: deprecated (Phase 9); drained only; real execution via JobExecution + MyInvoisStatusPollJobExecutor.
/// emailingest: deprecated (Phase 8); drained only; real execution via JobExecution + EmailIngestJobExecutor.
/// notificationsend: deprecated (Phase 6); drained only; real send via NotificationDispatch.
/// notificationretention: deprecated (Phase 7); drained only; real retention via INotificationRetentionService + NotificationRetentionHostedService.
/// </summary>
public class BackgroundJobProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobProcessorService> _logger;
    private readonly BackgroundJobStaleOptions _staleOptions;
    private readonly BackgroundJobFairnessOptions _fairnessOptions;
    private readonly IWorkerIdentity _workerIdentity;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);

    public BackgroundJobProcessorService(
        IServiceProvider serviceProvider,
        ILogger<BackgroundJobProcessorService> logger,
        IOptions<BackgroundJobStaleOptions> staleOptions,
        IOptions<BackgroundJobFairnessOptions>? fairnessOptions,
        IWorkerIdentity workerIdentity)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _staleOptions = staleOptions?.Value ?? new BackgroundJobStaleOptions();
        _fairnessOptions = fairnessOptions?.Value ?? new BackgroundJobFairnessOptions();
        _workerIdentity = workerIdentity;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background job processor service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown - don't log as error
                _logger.LogInformation("Background job processor service is shutting down");
                break; // Exit the loop gracefully
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background job processor loop");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Background job processor service stopped");
    }

    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        // Check if cancellation is requested before starting
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Check database connectivity before processing jobs
        try
        {
            if (!await context.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogWarning("Database not available, skipping job processing cycle");
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
            return;
        }

        // Reap stale Running jobs (does not block normal processing)
        await ReapStaleRunningJobsAsync(context, cancellationToken);

        var workerId = _workerIdentity.WorkerId;
        var now = DateTime.UtcNow;

        // 1) Jobs already claimed by this worker (scheduler set State=Running, WorkerId=me)
        // 2) Legacy: Queued and unclaimed (claim then process when no scheduler or overflow)
        // Use IgnoreQueryFilters so we see jobs from all tenants; scope is set per job in ProcessJobAsync
        List<BackgroundJob> runningMine;
        List<BackgroundJob> queuedUnclaimed;
        try
        {
            runningMine = await context.BackgroundJobs
                .IgnoreQueryFilters()
                .Where(j => j.State == BackgroundJobState.Running && j.WorkerId == workerId)
                .OrderBy(j => j.Priority)
                .ThenBy(j => j.CreatedAt)
                .Take(10)
                .ToListAsync(cancellationToken);

            queuedUnclaimed = await context.BackgroundJobs
                .IgnoreQueryFilters()
                .Where(j => j.State == BackgroundJobState.Queued
                    && j.WorkerId == null
                    && (j.ScheduledAt == null || j.ScheduledAt <= now))
                .OrderBy(j => j.Priority)
                .ThenBy(j => j.CreatedAt)
                .Take(10)
                .ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var toProcess = new List<BackgroundJob>(runningMine);

        // Tenant fairness: order queued jobs so we take at most MaxJobsPerTenantPerCycle per tenant (round-robin style)
        var toClaim = OrderForFairness(queuedUnclaimed, _fairnessOptions.MaxJobsPerTenantPerCycle);

        if (workerId.HasValue)
        {
            var coordinator = scope.ServiceProvider.GetRequiredService<IWorkerCoordinator>();
            foreach (var job in toClaim)
            {
                if (cancellationToken.IsCancellationRequested) break;
                var claimed = await coordinator.TryClaimBackgroundJobAsync(workerId.Value, job.Id, cancellationToken).ConfigureAwait(false);
                if (claimed)
                {
                    job.State = BackgroundJobState.Running;
                    job.WorkerId = workerId;
                    job.ClaimedAtUtc = now;
                    job.StartedAt = now;
                    job.UpdatedAt = now;
                    toProcess.Add(job);
                }
            }
        }
        else
        {
            // Legacy: no worker registration — process unclaimed Queued directly (still fair-ordered)
            toProcess.AddRange(toClaim);
        }

        if (toProcess.Count > 0)
            _logger.LogInformation("Processing {Count} background jobs", toProcess.Count);

        if (toProcess.Count == 0)
            return;

        var recorder = scope.ServiceProvider.GetService<IJobRunRecorder>();
        var jobDefinitionProvider = scope.ServiceProvider.GetService<IJobDefinitionProvider>();
        foreach (var job in toProcess)
        {
            // Check cancellation before processing each job
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessJobAsync(context, job, recorder, jobDefinitionProvider, cancellationToken);
        }

        // Only save changes if cancellation hasn't been requested
        if (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown - don't log as error
                _logger.LogInformation("Background job processing cancelled during save (shutdown in progress)");
            }
        }
    }

    /// <summary>Mark stale Running jobs as Failed so the scheduler is not blocked. Platform-wide: see all tenants' jobs and update with bypass.</summary>
    private async Task ReapStaleRunningJobsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        List<BackgroundJob> runningJobs;
        try
        {
            runningJobs = await context.BackgroundJobs
                .IgnoreQueryFilters()
                .Where(j => j.State == BackgroundJobState.Running)
                .ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var reaped = 0;
        foreach (var job in runningJobs)
        {
            var effectiveStart = (job.StartedAt ?? (DateTime?)job.UpdatedAt) ?? job.CreatedAt;
            var runningMinutes = (now - effectiveStart).TotalMinutes;
            var thresholdMinutes = string.Equals(job.JobType, "EmailIngest", StringComparison.OrdinalIgnoreCase)
                ? _staleOptions.EmailIngestMinutes
                : _staleOptions.DefaultMinutes;
            if (runningMinutes <= thresholdMinutes)
                continue;

            job.State = BackgroundJobState.Failed;
            job.CompletedAt = now;
            job.LastError = "Recovered: stale Running timeout";
            job.UpdatedAt = now;
            reaped++;
            _logger.LogWarning(
                "Reaped stale Running job {JobId} ({JobType}), runningDuration {RunningDuration:F0}m, threshold {Threshold}m",
                job.Id, job.JobType, runningMinutes, thresholdMinutes);
        }

        if (reaped > 0)
        {
            try
            {
                await TenantScopeExecutor.RunWithPlatformBypassAsync(ct => context.SaveChangesAsync(ct), cancellationToken);
            }
            catch (OperationCanceledException) { }
        }
    }

    /// <summary>Order jobs for tenant fairness: round-robin so no tenant gets more than MaxJobsPerTenantPerCycle per cycle.</summary>
    private static List<BackgroundJob> OrderForFairness(List<BackgroundJob> jobs, int maxPerTenant)
    {
        return TenantFairnessOrdering.OrderByTenantFairness(jobs, j => j.CompanyId, maxPerTenant);
    }

    private async Task ProcessJobAsync(
        ApplicationDbContext context,
        BackgroundJob job,
        IJobRunRecorder? recorder,
        IJobDefinitionProvider? jobDefinitionProvider,
        CancellationToken cancellationToken)
    {
        var startUtc = DateTime.UtcNow;
        Dictionary<string, object>? payload = null;
        if (!string.IsNullOrEmpty(job.PayloadJson))
            payload = JsonSerializer.Deserialize<Dictionary<string, object>>(job.PayloadJson);
        var effectiveCompanyId = job.CompanyId ?? TryGetCompanyIdFromPayload(payload);

        _logger.LogInformation("Processing background job {JobId} of type {JobType} for tenant {TenantId}", job.Id, job.JobType, effectiveCompanyId);

        job.State = BackgroundJobState.Running;
        job.StartedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;

        await TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(effectiveCompanyId, async (ct) =>
        {
            Guid? jobRunId = null;

            // Check cancellation before saving
            if (ct.IsCancellationRequested)
            {
                return; // Exit early if cancellation requested
            }

            try
            {
            await context.SaveChangesAsync(ct);

            // Start observability record
            if (recorder != null)
            {
                var jobName = GetJobDisplayName(job.JobType);
                if (jobDefinitionProvider != null)
                {
                    var def = await jobDefinitionProvider.GetByJobTypeAsync(job.JobType, ct);
                    if (!string.IsNullOrEmpty(def?.DisplayName))
                        jobName = def.DisplayName;
                }

                jobRunId = await recorder.StartAsync(new StartJobRunDto
                {
                    BackgroundJobId = job.Id,
                    CompanyId = TryGetCompanyIdFromPayload(payload),
                    JobName = jobName,
                    JobType = job.JobType,
                    TriggerSource = job.RetriedFromJobRunId.HasValue ? "Retry" : "Scheduler",
                    CorrelationId = job.Id.ToString(),
                    QueueOrChannel = "BackgroundJobs",
                    PayloadSummary = JobRunRecorder.BuildPayloadSummary(job.PayloadJson),
                    RetryCount = job.RetryCount,
                    WorkerNode = null,
                    ParentJobRunId = job.RetriedFromJobRunId
                }, ct);
            }

            // Process based on job type (legacy only; migrated types use JobExecution pipeline)
            bool success = job.JobType.ToLowerInvariant() switch
            {
                "emailingest" => await ProcessEmailIngestJobDeprecatedAsync(payload, ct),
                "notificationsend" => await ProcessNotificationSendJobDeprecatedAsync(payload, ct),
                "notificationretention" => await ProcessNotificationRetentionJobDeprecatedAsync(payload, ct),
                "myinvoisstatuspoll" => await ProcessMyInvoisStatusPollJobDeprecatedAsync(payload, ct),
                "inventoryreportexport" => await ProcessInventoryReportExportJobDeprecatedAsync(payload, ct),
                "eventhandlingasync" => await ProcessEventHandlingAsyncJobDeprecatedAsync(payload, ct),
                "operationalreplay" => await ProcessOperationalReplayJobDeprecatedAsync(payload, ct),
                "operationalrebuild" => await ProcessOperationalRebuildJobDeprecatedAsync(payload, ct),
                "pnlrebuild" => throw new NotSupportedException($"Job type '{job.JobType}' has been migrated to JobExecution; use IJobExecutionEnqueuer."),
                "reconcileledgerbalancecache" => throw new NotSupportedException($"Job type '{job.JobType}' has been migrated to JobExecution; use IJobExecutionEnqueuer."),
                "slaevaluation" => throw new NotSupportedException($"Job type '{job.JobType}' has been migrated to JobExecution; use IJobExecutionEnqueuer."),
                "populatestockbylocationsnapshots" => throw new NotSupportedException($"Job type '{job.JobType}' has been migrated to JobExecution; use IJobExecutionEnqueuer."),
                "documentgeneration" => throw new NotSupportedException($"Job type '{job.JobType}' has been migrated to JobExecution; use IDocumentGenerationJobEnqueuer or IJobExecutionEnqueuer."),
                _ => throw new NotSupportedException($"Job type '{job.JobType}' is not supported (or migrated to JobExecution)")
            };

            if (success)
            {
                job.State = BackgroundJobState.Succeeded;
                job.CompletedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(ct);
                if (jobRunId.HasValue && recorder != null)
                    await recorder.CompleteAsync(jobRunId.Value, ct);
                var usageService = _serviceProvider.GetService<ITenantUsageService>();
                if (usageService != null && job.CompanyId.HasValue)
                    await usageService.RecordUsageAsync(job.CompanyId, TenantUsageService.MetricKeys.BackgroundJobsExecuted, 1, ct);
                var durationMs = (int)(DateTime.UtcNow - startUtc).TotalMilliseconds;
                _logger.LogInformation("Background job {JobId} completed. tenantId={TenantId}, operation=BackgroundJob, durationMs={DurationMs}, success=true", job.Id, effectiveCompanyId, durationMs);
                TenantOperationalMetrics.RecordJobExecuted(effectiveCompanyId);
            }
            else
            {
                throw new InvalidOperationException("Job processing returned false");
            }
            }
        catch (OperationCanceledException)
        {
            if (jobRunId.HasValue && recorder != null)
                await recorder.CancelAsync(jobRunId.Value, ct);
            // Expected during shutdown - reset job state and exit without logging as error
            job.State = BackgroundJobState.Queued; // Reset to queued so it can be retried later
            job.StartedAt = null;
            job.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Background job {JobId} processing cancelled (shutdown in progress), will be retried on next cycle. CompanyId: {CompanyId}", job.Id, job.CompanyId);
        }
        catch (Exception ex)
        {
            var durationMs = (int)(DateTime.UtcNow - startUtc).TotalMilliseconds;
            _logger.LogError(ex, "Background job {JobId} failed. tenantId={TenantId}, operation=BackgroundJob, durationMs={DurationMs}, success=false, errorType={ErrorType}", job.Id, effectiveCompanyId, durationMs, ex.GetType().Name);
            TenantOperationalMetrics.RecordJobFailure(effectiveCompanyId);
            var guard = _serviceProvider.GetService<ITenantOperationsGuard>();
            guard?.RecordJobFailure(effectiveCompanyId);
            if (job.RetryCount > 0)
                guard?.RecordJobRetry(effectiveCompanyId);

            if (jobRunId.HasValue && recorder != null)
            {
                await recorder.FailAsync(jobRunId.Value, new FailJobRunDto
                {
                    ErrorMessage = ex.Message,
                    ErrorDetails = ex.ToString(),
                    Status = job.RetryCount + 1 >= job.MaxRetries ? "DeadLetter" : "Failed"
                }, ct);
            }

            job.State = BackgroundJobState.Failed;
            job.LastError = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.RetryCount++;

            if (string.Equals(job.JobType, ReplayJobEnqueuer.JobType, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var metricsScope = _serviceProvider.CreateScope();
                    var replayMetrics = metricsScope.ServiceProvider.GetService<ReplayMetrics>();
                    replayMetrics?.RecordRunFailed(null, false);
                }
                catch (Exception metricsEx)
                {
                    _logger.LogDebug(metricsEx, "Failed to record replay run failed metric");
                }
            }

            // Schedule retry if max retries not reached
            if (job.RetryCount < job.MaxRetries)
            {
                // Exponential backoff: 2^retryCount minutes
                var retryDelayMinutes = (int)Math.Pow(2, job.RetryCount);
                job.ScheduledAt = DateTime.UtcNow.AddMinutes(retryDelayMinutes);
                job.State = BackgroundJobState.Queued; // Re-queue for retry
                _logger.LogInformation("Background job {JobId} will be retried in {DelayMinutes} minutes (attempt {RetryCount}/{MaxRetries}). CompanyId: {CompanyId}",
                    job.Id, retryDelayMinutes, job.RetryCount, job.MaxRetries, job.CompanyId);
            }
            else
            {
                _logger.LogWarning("Background job {JobId} has exceeded max retries ({MaxRetries}). CompanyId: {CompanyId}", job.Id, job.MaxRetries, job.CompanyId);
            }
            job.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
        }
        finally
        {
            job.UpdatedAt = DateTime.UtcNow;
        }
        }, cancellationToken);
    }

    private static Guid? TryGetCompanyIdFromPayload(Dictionary<string, object>? payload)
    {
        if (payload == null) return null;
        if (payload.TryGetValue("companyId", out var v) && v != null && Guid.TryParse(v.ToString(), out var g))
            return g;
        return null;
    }

    private static string GetJobDisplayName(string jobType)
    {
        return jobType.ToLowerInvariant() switch
        {
            "emailingest" => "Email Ingest",
            "pnlrebuild" => "P&L Rebuild",
            "notificationsend" => "Notification Send",
            "notificationretention" => "Notification Retention",
            "documentgeneration" => "Document Generation",
            "myinvoisstatuspoll" => "MyInvois Status Poll",
            "inventoryreportexport" => "Inventory Report Export",
            "reconcileledgerbalancecache" => "Reconcile Ledger Balance Cache",
            "populatestockbylocationsnapshots" => "Populate Stock by Location Snapshots",
            "eventhandlingasync" => "Event Handling (Async)",
            "slaevaluation" => "SLA Evaluation",
            "operationalreplay" => "Operational Replay",
            "operationalrebuild" => "Operational Rebuild",
            _ => jobType
        };
    }

    /// <summary>Phase 11: operationalrebuild is deprecated. Execution is via JobExecution + OperationalRebuildJobExecutor. Drain only.</summary>
    private Task<bool> ProcessOperationalRebuildJobDeprecatedAsync(Dictionary<string, object>? payload, CancellationToken cancellationToken)
    {
        var opId = payload?.ContainsKey("rebuildOperationId") == true ? payload["rebuildOperationId"]?.ToString() : null;
        _logger.LogWarning("operationalrebuild job is deprecated (Phase 11). Execution is via JobExecution. Skipping legacy job. RebuildOperationId: {OpId}", opId ?? "(none)");
        return Task.FromResult(true);
    }

    /// <summary>Phase 11: eventhandlingasync is deprecated. Execution is via JobExecution + EventHandlingAsyncJobExecutor. Drain only.</summary>
    private Task<bool> ProcessEventHandlingAsyncJobDeprecatedAsync(Dictionary<string, object>? payload, CancellationToken cancellationToken)
    {
        var eventId = payload?.ContainsKey("eventId") == true ? payload["eventId"]?.ToString() : null;
        _logger.LogWarning("eventhandlingasync job is deprecated (Phase 11). Execution is via JobExecution. Skipping legacy job. EventId: {EventId}", eventId ?? "(none)");
        return Task.FromResult(true);
    }

    /// <summary>Phase 11: operationalreplay is deprecated. Execution is via JobExecution + OperationalReplayJobExecutor. Drain only.</summary>
    private Task<bool> ProcessOperationalReplayJobDeprecatedAsync(Dictionary<string, object>? payload, CancellationToken cancellationToken)
    {
        var opId = payload?.ContainsKey("replayOperationId") == true ? payload["replayOperationId"]?.ToString() : null;
        _logger.LogWarning("operationalreplay job is deprecated (Phase 11). Execution is via JobExecution. Skipping legacy job. ReplayOperationId: {OpId}", opId ?? "(none)");
        return Task.FromResult(true);
    }

    private async Task<bool> ProcessSlaEvaluationJobAsync(Dictionary<string, object>? payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running SLA evaluation job");
        Guid? companyId = TryGetCompanyIdFromPayload(payload);
        using var scope = _serviceProvider.CreateScope();
        var slaEvaluation = scope.ServiceProvider.GetRequiredService<ISlaEvaluationService>();
        var result = await slaEvaluation.EvaluateAsync(companyId, cancellationToken);
        _logger.LogInformation("SLA evaluation completed: {RulesEvaluated} rules, {Breaches} breaches, {Warnings} warnings, {Escalations} escalations",
            result.RulesEvaluated, result.BreachesRecorded, result.WarningsRecorded, result.EscalationsRecorded);
        return true;
    }

    /// <summary>Phase 8: emailingest is deprecated. Execution is via JobExecution + EmailIngestJobExecutor. Drain only.</summary>
    private Task<bool> ProcessEmailIngestJobDeprecatedAsync(Dictionary<string, object>? payload, CancellationToken cancellationToken)
    {
        var accountId = payload?.ContainsKey("emailAccountId") == true ? payload["emailAccountId"]?.ToString() : null;
        _logger.LogWarning("emailingest job is deprecated (Phase 8). Execution is via JobExecution. Skipping legacy job. EmailAccountId: {EmailAccountId}", accountId ?? "(none)");
        return Task.FromResult(true);
    }

    private async Task<bool> ProcessPnlRebuildJobAsync(
        ApplicationDbContext context,
        Dictionary<string, object>? payload,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing P&L rebuild job");

        // Extract period information from payload
        var companyId = payload?.ContainsKey("companyId") == true && Guid.TryParse(payload["companyId"]?.ToString(), out var cid) ? cid : Guid.Empty;
        var period = payload?.ContainsKey("period") == true ? payload["period"]?.ToString() : null;

        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("Company ID is required for P&L rebuild job");
        }

        if (string.IsNullOrWhiteSpace(period))
        {
            // Default to current month if period not specified (format: "YYYY-MM")
            period = DateTime.UtcNow.ToString("yyyy-MM");
            _logger.LogInformation("Period not specified, using current month: {Period}", period);
        }

        // Use existing PnlService to rebuild P&L
        using var scope = _serviceProvider.CreateScope();
        var pnlService = scope.ServiceProvider.GetRequiredService<IPnlService>();

        try
        {
            await pnlService.RebuildPnlAsync(companyId, period, cancellationToken);

            _logger.LogInformation(
                "P&L rebuild job completed successfully for company {CompanyId}, period {Period}",
                companyId, period);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing P&L rebuild job for company {CompanyId}, period {Period}", 
                companyId, period);
            throw;
        }
    }

    /// <summary>Phase 10: inventoryreportexport is deprecated. Execution is via JobExecution + InventoryReportExportJobExecutor. Drain only.</summary>
    private Task<bool> ProcessInventoryReportExportJobDeprecatedAsync(Dictionary<string, object>? payload, CancellationToken cancellationToken)
    {
        var reportType = payload?.ContainsKey("reportType") == true ? payload["reportType"]?.ToString() : null;
        _logger.LogWarning("inventoryreportexport job is deprecated (Phase 10). Execution is via JobExecution. Skipping legacy job. ReportType: {ReportType}", reportType ?? "(none)");
        return Task.FromResult(true);
    }

    /// <summary>Reconcile LedgerBalanceCache with ledger + allocations; repair drift. Run periodically (e.g. daily).</summary>
    private async Task<bool> ProcessReconcileLedgerBalanceCacheJobAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing reconcile ledger balance cache job");
        using var scope = _serviceProvider.CreateScope();
        var ledgerService = scope.ServiceProvider.GetRequiredService<IStockLedgerService>();
        await ledgerService.ReconcileBalanceCacheAsync(cancellationToken);
        return true;
    }

    /// <summary>Populate stock-by-location snapshots for a period (Phase 2.2.2). Payload: periodEndDate (optional, ISO date, default yesterday), snapshotType (optional, default Daily), companyId (optional).</summary>
    private async Task<bool> ProcessPopulateStockByLocationSnapshotsJobAsync(ApplicationDbContext context, Dictionary<string, object>? payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing populate stock-by-location snapshots job");
        var periodEndDate = DateTime.UtcNow.Date.AddDays(-1);
        if (payload != null && payload.TryGetValue("periodEndDate", out var pd) && pd != null)
        {
            if (DateTime.TryParse(pd.ToString(), out var parsed))
                periodEndDate = parsed.Date;
        }
        var snapshotType = "Daily";
        if (payload != null && payload.TryGetValue("snapshotType", out var st) && st != null && !string.IsNullOrWhiteSpace(st.ToString()))
            snapshotType = st.ToString()!.Trim();
        Guid? companyId = null;
        if (payload != null && payload.TryGetValue("companyId", out var cid) && cid != null && Guid.TryParse(cid.ToString(), out var g))
            companyId = g;
        using var scope = _serviceProvider.CreateScope();
        var ledgerService = scope.ServiceProvider.GetRequiredService<IStockLedgerService>();
        await ledgerService.EnsureSnapshotsForPeriodAsync(periodEndDate, snapshotType, companyId, cancellationToken);
        return true;
    }

    /// <summary>
    /// Phase 6: notificationsend is deprecated. Real send is via NotificationDispatch (requested when creating notification with DeliveryChannels containing Email).
    /// This no-op drains any stale notificationsend jobs without sending (mark complete to avoid retry storm).
    /// </summary>
    private Task<bool> ProcessNotificationSendJobDeprecatedAsync(
        Dictionary<string, object>? payload,
        CancellationToken cancellationToken)
    {
        var notificationId = payload?.ContainsKey("notificationId") == true ? payload["notificationId"]?.ToString() : null;
        _logger.LogWarning(
            "notificationsend job is deprecated (Phase 6). Notification email is now sent via NotificationDispatch. Skipping legacy job. NotificationId: {NotificationId}",
            notificationId ?? "(none)");
        return Task.FromResult(true);
    }

    /// <summary>
    /// Phase 7: notificationretention is deprecated. Retention runs via INotificationRetentionService and NotificationRetentionHostedService.
    /// This no-op drains any stale jobs without running retention.
    /// </summary>
    private Task<bool> ProcessNotificationRetentionJobDeprecatedAsync(
        Dictionary<string, object>? payload,
        CancellationToken cancellationToken)
    {
        var archiveDays = payload?.ContainsKey("archiveDays") == true ? payload["archiveDays"]?.ToString() : null;
        var deleteDays = payload?.ContainsKey("deleteDays") == true ? payload["deleteDays"]?.ToString() : null;
        _logger.LogWarning(
            "notificationretention job is deprecated (Phase 7). Retention is now run by NotificationRetentionHostedService. Skipping legacy job. Payload archiveDays={ArchiveDays}, deleteDays={DeleteDays}",
            archiveDays ?? "(default)", deleteDays ?? "(default)");
        return Task.FromResult(true);
    }

    /// <summary>Phase 9: myinvoisstatuspoll is deprecated. Execution is via JobExecution + MyInvoisStatusPollJobExecutor. Drain only.</summary>
    private Task<bool> ProcessMyInvoisStatusPollJobDeprecatedAsync(Dictionary<string, object>? payload, CancellationToken cancellationToken)
    {
        var id = payload?.ContainsKey("submissionHistoryId") == true ? payload["submissionHistoryId"]?.ToString() : null;
        _logger.LogWarning("myinvoisstatuspoll job is deprecated (Phase 9). Execution is via JobExecution. Skipping legacy job. SubmissionHistoryId: {Id}", id ?? "(none)");
        return Task.FromResult(true);
    }
}

