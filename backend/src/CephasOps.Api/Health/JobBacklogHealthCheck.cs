using CephasOps.Application.Workflow.JobOrchestration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CephasOps.Api.Health;

/// <summary>Health check for job queue backlog: JobExecution pending and dead-letter within thresholds.</summary>
public sealed class JobBacklogHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JobBacklogHealthCheckOptions _options;

    public JobBacklogHealthCheck(IServiceProvider serviceProvider, IOptions<JobBacklogHealthCheckOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _options = options?.Value ?? new JobBacklogHealthCheckOptions();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryService = scope.ServiceProvider.GetRequiredService<IJobExecutionQueryService>();
            var summary = await queryService.GetSummaryAsync(cancellationToken).ConfigureAwait(false);

            var pendingThreshold = Math.Max(0, _options.PendingDegradedThreshold);
            var deadLetterUnhealthy = Math.Max(0, _options.DeadLetterUnhealthyThreshold);
            var deadLetterDegraded = Math.Max(0, _options.DeadLetterDegradedThreshold);

            if (summary.DeadLetterCount >= deadLetterUnhealthy)
                return HealthCheckResult.Unhealthy(
                    $"Job dead-letter count ({summary.DeadLetterCount}) >= unhealthy threshold ({deadLetterUnhealthy})",
                    data: ToData(summary));

            if (summary.PendingCount >= pendingThreshold || summary.DeadLetterCount >= deadLetterDegraded)
                return HealthCheckResult.Degraded(
                    "Job backlog or dead-letter above degraded threshold",
                    data: ToData(summary, pendingThreshold, deadLetterDegraded));

            return HealthCheckResult.Healthy("Job queue within limits", ToData(summary));
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Job backlog check failed", ex);
        }
    }

    private static Dictionary<string, object> ToData(
        JobExecutionSummaryDto summary,
        int? pendingThreshold = null,
        int? deadLetterDegraded = null)
    {
        var data = new Dictionary<string, object>
        {
            ["pendingCount"] = summary.PendingCount,
            ["runningCount"] = summary.RunningCount,
            ["failedRetryScheduledCount"] = summary.FailedRetryScheduledCount,
            ["deadLetterCount"] = summary.DeadLetterCount,
            ["succeededCount"] = summary.SucceededCount
        };
        if (pendingThreshold.HasValue) data["pendingDegradedThreshold"] = pendingThreshold.Value;
        if (deadLetterDegraded.HasValue) data["deadLetterDegradedThreshold"] = deadLetterDegraded.Value;
        return data;
    }
}
