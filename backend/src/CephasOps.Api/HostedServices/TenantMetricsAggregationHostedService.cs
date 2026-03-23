using CephasOps.Application.Platform;
using CephasOps.Application.Platform.TenantHealth;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CephasOps.Api.HostedServices;

/// <summary>SaaS scaling: runs TenantMetricsAggregationJob daily (and monthly at month end). Platform-wide: reads TenantUsageRecords and writes TenantMetricsDaily/Monthly across all tenants.</summary>
public class TenantMetricsAggregationHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TenantMetricsAggregationHostedService> _logger;

    public TenantMetricsAggregationHostedService(IServiceProvider services, ILogger<TenantMetricsAggregationHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
                {
                    var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                    using (var scope = _services.CreateScope())
                    {
                        var job = scope.ServiceProvider.GetRequiredService<TenantMetricsAggregationJob>();
                        await job.AggregateDailyAsync(yesterday, ct);
                        var healthScoring = scope.ServiceProvider.GetRequiredService<ITenantHealthScoringService>();
                        await healthScoring.ComputeAndStoreForAllTenantsAsync(yesterday, ct);
                    }

                    if (DateTime.UtcNow.Day == 1)
                    {
                        var lastMonth = DateTime.UtcNow.AddMonths(-1);
                        using (var scope = _services.CreateScope())
                        {
                            var job = scope.ServiceProvider.GetRequiredService<TenantMetricsAggregationJob>();
                            await job.AggregateMonthlyAsync(lastMonth.Year, lastMonth.Month, ct);
                        }
                    }
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tenant metrics aggregation failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
