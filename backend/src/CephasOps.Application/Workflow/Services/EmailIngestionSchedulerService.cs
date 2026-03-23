using CephasOps.Application.Workflow;
using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Scheduler that enqueues email ingestion work via JobExecution (Phase 8). Uses EmailAccount.PollIntervalSec.
/// </summary>
public class EmailIngestionSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailIngestionSchedulerService> _logger;
    private readonly BackgroundJobStaleOptions _staleOptions;
    private readonly TimeSpan _schedulerInterval = TimeSpan.FromSeconds(30);

    public EmailIngestionSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<EmailIngestionSchedulerService> logger,
        IOptions<BackgroundJobStaleOptions> staleOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _staleOptions = staleOptions?.Value ?? new BackgroundJobStaleOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email ingestion scheduler service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScheduleEmailIngestionJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email ingestion scheduler loop: {ExceptionType} - {Message}\n{StackTrace}", 
                    ex.GetType().Name, ex.Message, ex.StackTrace);
            }

            await Task.Delay(_schedulerInterval, stoppingToken);
        }

        _logger.LogInformation("Email ingestion scheduler service stopped");
    }

    private async Task ScheduleEmailIngestionJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Check database connectivity
        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            _logger.LogWarning("Database not available, skipping email ingestion scheduling cycle");
            return;
        }

        var recorder = scope.ServiceProvider.GetService<IJobRunRecorder>();
        try
        {
            await TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
            {
                Guid? jobRunId = null;
                if (recorder != null)
                {
                    jobRunId = await recorder.StartAsync(new StartJobRunDto
                    {
                        JobName = "Email Ingestion Scheduler",
                        JobType = "EmailIngestionScheduler",
                        TriggerSource = "Scheduler",
                        CorrelationId = Guid.NewGuid().ToString()
                    }, ct);
                }
                try
                {
                    await ScheduleEmailIngestionJobsCoreAsync(scope, context, ct);
                    if (jobRunId.HasValue && recorder != null)
                        await recorder.CompleteAsync(jobRunId.Value, ct);
                }
                catch (Exception ex)
                {
                    if (jobRunId.HasValue && recorder != null)
                        await recorder.FailAsync(jobRunId.Value, new FailJobRunDto { ErrorMessage = ex.Message, ErrorDetails = ex.ToString() }, ct);
                    throw;
                }
            }, cancellationToken);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task ScheduleEmailIngestionJobsCoreAsync(IServiceScope scope, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        // Get all active email accounts
        // Use AsNoTracking() to avoid navigation property loading issues
        var activeAccounts = await context.EmailAccounts
            .AsNoTracking()
            .Where(ea => ea.IsActive && ea.PollIntervalSec > 0)
            .ToListAsync(cancellationToken);

        if (activeAccounts.Count == 0)
        {
            return; // No active accounts to schedule
        }

        var now = DateTime.UtcNow;
        int jobsCreated = 0;

        foreach (var account in activeAccounts)
        {
            try
            {
                if (!account.CompanyId.HasValue || account.CompanyId.Value == Guid.Empty)
                {
                    _logger.LogDebug("Email ingestion: skipping account {AccountId} ({AccountName}) - CompanyId is missing (tenant-boundary).", account.Id, account.Name);
                    continue;
                }

                // Calculate when this account should be polled next
                var lastPolledAt = account.LastPolledAt ?? account.CreatedAt;
                var nextPollTime = lastPolledAt.AddSeconds(account.PollIntervalSec);

                // Check if it's time to poll (with 5 second tolerance to avoid race conditions)
                if (now >= nextPollTime.AddSeconds(-5))
                {
                    var accountIdString = account.Id.ToString();
                    // Skip if there is already a pending or running JobExecution for this account (Phase 8).
                    // Avoid EF translating PayloadJson.Contains to invalid SQL on jsonb; load candidates and filter in memory.
                    var candidateQuery = context.JobExecutions
                        .AsNoTracking()
                        .Where(j => j.JobType == "emailingest" && (j.Status == "Pending" || j.Status == "Running"));
                    if (account.CompanyId.HasValue)
                        candidateQuery = candidateQuery.Where(j => j.CompanyId == account.CompanyId);
                    var payloads = await candidateQuery.Select(j => j.PayloadJson).ToListAsync(cancellationToken);
                    var hasExisting = payloads.Any(p => PayloadContainsEmailAccountId(p, accountIdString));

                    if (!hasExisting)
                    {
                        var enqueuer = scope.ServiceProvider.GetRequiredService<IJobExecutionEnqueuer>();
                        var payloadJson = JsonSerializer.Serialize(new Dictionary<string, object> { ["emailAccountId"] = accountIdString });
                        await enqueuer.EnqueueAsync("emailingest", payloadJson, companyId: account.CompanyId, priority: 1, cancellationToken: cancellationToken);
                        jobsCreated++;

                        _logger.LogDebug(
                            "Enqueued email ingest job for account {AccountName} (Id: {AccountId}, PollInterval: {Interval}s, LastPolled: {LastPolled})",
                            account.Name, account.Id, account.PollIntervalSec, lastPolledAt);
                    }
                    else
                    {
                        _logger.LogDebug("Skipping account {AccountName} - emailingest job already pending/running", account.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling email ingestion for account {AccountId}: {ExceptionType} - {Message}", 
                    account.Id, ex.GetType().Name, ex.Message);
            }
        }

        if (jobsCreated > 0)
        {
            _logger.LogInformation("Enqueued {Count} email ingest job(s) for active accounts", jobsCreated);
        }
    }

    /// <summary>Checks if payload JSON contains emailAccountId equal to the given string (avoids jsonb Contains in SQL).</summary>
    private static bool PayloadContainsEmailAccountId(string? payloadJson, string accountIdString)
    {
        if (string.IsNullOrEmpty(payloadJson)) return false;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("emailAccountId", out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString() == accountIdString;
            return false;
        }
        catch
        {
            return false;
        }
    }
}

