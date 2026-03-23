using CephasOps.Api.Options;
using CephasOps.Application.Events;
using CephasOps.Application.Events.DTOs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CephasOps.Api.Health;

/// <summary>
/// Health check for the Event Bus: dispatcher running, pending/failed/dead-letter within thresholds.
/// Healthy: dispatcher running, pending below threshold, dead-letter below degraded.
/// Degraded: pending above threshold or dead-letter above degraded threshold.
/// Unhealthy: dispatcher stopped or dead-letter above unhealthy threshold.
/// </summary>
public sealed class EventBusHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EventBusDispatcherOptions _options;
    private readonly ProductionRolesOptions _productionRoles;

    public EventBusHealthCheck(
        IServiceProvider serviceProvider,
        IOptions<EventBusDispatcherOptions> options,
        IOptions<ProductionRolesOptions> productionRoles)
    {
        _serviceProvider = serviceProvider;
        _options = options?.Value ?? new EventBusDispatcherOptions();
        _productionRoles = productionRoles?.Value ?? new ProductionRolesOptions();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var state = _serviceProvider.GetService<IEventStoreDispatcherState>() as EventStoreDispatcherState;
        var isRunning = state?.IsRunning ?? false;

        EventStoreCountsSnapshot? snapshot = null;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryService = scope.ServiceProvider.GetRequiredService<IEventStoreQueryService>();
            snapshot = await queryService.GetEventStoreCountsAsync(scopeCompanyId: null, cancellationToken);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Event store query failed", ex);
        }

        var pending = snapshot!.PendingCount;
        var deadLetter = snapshot.DeadLetterCount;
        var expiredLeases = snapshot.ExpiredLeasesCount;
        var oldestPendingAgeSeconds = snapshot.OldestPendingEventAgeSeconds;
        var pendingThreshold = Math.Max(0, _options.PendingCountDegradedThreshold);
        var deadLetterUnhealthy = Math.Max(0, _options.DeadLetterUnhealthyThreshold);
        var deadLetterDegraded = Math.Max(0, _options.DeadLetterDegradedThreshold);
        var oldestPendingWarningMinutes = Math.Max(0, _options.OldestPendingEventAgeWarningMinutes);
        var oldestPendingWarningSeconds = oldestPendingWarningMinutes > 0 ? TimeSpan.FromMinutes(oldestPendingWarningMinutes).TotalSeconds : (double?)null;

        // Testing / API-only: dispatcher is not registered when ProductionRoles:RunEventDispatcher=false.
        if (!isRunning && !_productionRoles.RunEventDispatcher)
        {
            return HealthCheckResult.Healthy(
                "Event Store dispatcher not running (ProductionRoles:RunEventDispatcher=false)",
                new Dictionary<string, object>
                {
                    ["dispatcherDisabled"] = true,
                    ["pendingCount"] = pending,
                    ["failedCount"] = snapshot.FailedCount,
                    ["deadLetterCount"] = deadLetter,
                    ["processingCount"] = snapshot.ProcessingCount,
                    ["expiredLeasesCount"] = expiredLeases
                });
        }

        if (!isRunning)
            return HealthCheckResult.Unhealthy("Event Store dispatcher is not running");
        if (deadLetter >= deadLetterUnhealthy)
            return HealthCheckResult.Unhealthy($"Dead-letter count ({deadLetter}) >= unhealthy threshold ({deadLetterUnhealthy})");

        if (expiredLeases > 0)
        {
            var data = new Dictionary<string, object>
            {
                ["pendingCount"] = pending,
                ["deadLetterCount"] = deadLetter,
                ["expiredLeasesCount"] = expiredLeases,
                ["processingCount"] = snapshot.ProcessingCount
            };
            return HealthCheckResult.Degraded($"Event Bus has {expiredLeases} expired lease(s); stuck recovery will reclaim them", data: data);
        }

        if (pending >= pendingThreshold || deadLetter >= deadLetterDegraded)
        {
            var data = new Dictionary<string, object>
            {
                ["pendingCount"] = pending,
                ["deadLetterCount"] = deadLetter,
                ["pendingDegradedThreshold"] = pendingThreshold,
                ["deadLetterDegradedThreshold"] = deadLetterDegraded,
                ["expiredLeasesCount"] = expiredLeases
            };
            return HealthCheckResult.Degraded("Event Bus backlog or dead-letter above degraded threshold", data: data);
        }

        if (oldestPendingWarningSeconds.HasValue && pending > 0 && oldestPendingAgeSeconds > oldestPendingWarningSeconds.Value)
        {
            var data = new Dictionary<string, object>
            {
                ["pendingCount"] = pending,
                ["failedCount"] = snapshot.FailedCount,
                ["deadLetterCount"] = deadLetter,
                ["oldestPendingEventAgeSeconds"] = oldestPendingAgeSeconds,
                ["oldestPendingWarningSeconds"] = oldestPendingWarningSeconds.Value
            };
            return HealthCheckResult.Degraded("Event Bus oldest pending event age above warning threshold", data: data);
        }

        return HealthCheckResult.Healthy("Event Bus healthy", new Dictionary<string, object>
        {
            ["pendingCount"] = pending,
            ["failedCount"] = snapshot.FailedCount,
            ["deadLetterCount"] = deadLetter,
            ["processingCount"] = snapshot.ProcessingCount,
            ["expiredLeasesCount"] = expiredLeases
        });
    }
}
