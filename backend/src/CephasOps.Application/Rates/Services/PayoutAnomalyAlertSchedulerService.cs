using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Domain.Rates;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Background scheduler that runs payout anomaly alerting on a configurable interval.
/// Additive only; does not change payout or anomaly detection. Duplicate prevention unchanged.
/// </summary>
public class PayoutAnomalyAlertSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<PayoutAnomalyAlertOptions> _options;
    private readonly ILogger<PayoutAnomalyAlertSchedulerService> _logger;

    public PayoutAnomalyAlertSchedulerService(
        IServiceProvider serviceProvider,
        IOptions<PayoutAnomalyAlertOptions> options,
        ILogger<PayoutAnomalyAlertSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;
        if (!opts.SchedulerEnabled)
        {
            _logger.LogInformation(
                "Payout anomaly alert scheduler is disabled (PayoutAnomalyAlert:SchedulerEnabled=false). Not starting.");
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            return;
        }

        var intervalHours = Math.Clamp(opts.SchedulerIntervalHours, 1, 168); // 1h–7d
        var interval = TimeSpan.FromHours(intervalHours);

        _logger.LogInformation(
            "Payout anomaly alert scheduler started (interval: {IntervalHours}h, duplicate prevention: {DuplicatePreventionHours}h)",
            intervalHours, opts.DuplicatePreventionHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunScheduledAlertsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payout anomaly alert scheduler error: {Message}", ex.Message);
            }

            await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Payout anomaly alert scheduler stopped");
    }

    private async Task RunScheduledAlertsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("Database not available, skipping scheduled payout anomaly alert run");
            return;
        }

        var opts = _options.Value;
        var request = new RunPayoutAnomalyAlertsRequestDto
        {
            RecipientEmails = string.IsNullOrWhiteSpace(opts.DefaultRecipientEmails)
                ? new List<string>()
                : opts.DefaultRecipientEmails.Split(',', ';').Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e)).ToList(),
            IncludeMediumRepeated = opts.IncludeMediumRepeated
        };

        var alertService = scope.ServiceProvider.GetRequiredService<IPayoutAnomalyAlertService>();
        var startedAt = DateTime.UtcNow;
        var recorder = scope.ServiceProvider.GetService<IJobRunRecorder>();
        Guid? jobRunId = null;

        await TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
        {
            if (recorder != null)
            {
                jobRunId = await recorder.StartAsync(new StartJobRunDto
                {
                    JobName = "Payout Anomaly Alert",
                    JobType = "PayoutAnomalyAlert",
                    TriggerSource = "Scheduler",
                    CorrelationId = Guid.NewGuid().ToString()
                }, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Scheduled payout anomaly alert run starting (trigger: Scheduler)");

            RunPayoutAnomalyAlertsResultDto result;
            try
            {
                result = await alertService.RunAlertsAsync(request, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled payout anomaly alert run failed: {Message}", ex.Message);
                if (jobRunId.HasValue && recorder != null)
                    await recorder.FailAsync(jobRunId.Value, new FailJobRunDto { ErrorMessage = ex.Message, ErrorDetails = ex.ToString() }, ct).ConfigureAwait(false);
                try
                {
                    var history = scope.ServiceProvider.GetService<IAlertRunHistoryService>();
                    if (history != null)
                    {
                        await history.RecordRunAsync(
                            new RunPayoutAnomalyAlertsResultDto(),
                            AlertRunTriggerSource.Scheduler,
                            startedAt,
                            DateTime.UtcNow,
                            ct).ConfigureAwait(false);
                    }
                }
                catch (Exception saveEx)
                {
                    _logger.LogWarning(saveEx, "Could not persist failed alert run history");
                }
                return;
            }

            var completedAt = DateTime.UtcNow;
            if (jobRunId.HasValue && recorder != null)
                await recorder.CompleteAsync(jobRunId.Value, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Scheduled payout anomaly alert run completed: evaluated={Evaluated}, sent={Sent}, skipped={Skipped}, errors={Errors}",
                result.AnomaliesConsidered, result.AnomaliesAlerted, result.SkippedCount, result.AlertsFailed);

            if (result.Errors.Count > 0)
            {
                foreach (var err in result.Errors)
                    _logger.LogWarning("Payout anomaly alert run error: {Error}", err);
            }

            try
            {
                var history = scope.ServiceProvider.GetService<IAlertRunHistoryService>();
                if (history != null)
                {
                    await history.RecordRunAsync(
                        result,
                        AlertRunTriggerSource.Scheduler,
                        startedAt,
                        completedAt,
                        ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not persist payout anomaly alert run history");
            }
        }, cancellationToken);
    }
}
