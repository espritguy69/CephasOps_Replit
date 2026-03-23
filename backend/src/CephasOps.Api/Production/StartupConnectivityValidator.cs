using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace CephasOps.Api.Production;

/// <summary>Runs database and Redis connectivity checks at startup when in Production. Fails fast if critical dependencies are unreachable.</summary>
public static class StartupConnectivityValidator
{
    /// <summary>Critical check names that must be Healthy in Production before the app serves traffic.</summary>
    private static readonly HashSet<string> CriticalCheckNames = new(StringComparer.OrdinalIgnoreCase) { "database", "redis" };

    /// <summary>Runs startup connectivity checks (database, and Redis when configured). In Production, throws if any critical check is Unhealthy.</summary>
    public static async Task RunAsync(IHost host, CancellationToken cancellationToken = default)
    {
        var config = host.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var isProduction = string.Equals(config["ASPNETCORE_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase);

        using var scope = host.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();
        var report = await healthCheckService.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

        if (report.Status == HealthStatus.Healthy)
            return;

        if (!isProduction)
            return;

        foreach (var entry in report.Entries)
        {
            if (!CriticalCheckNames.Contains(entry.Key))
                continue;
            if (entry.Value.Status == HealthStatus.Unhealthy)
                throw new InvalidOperationException(
                    $"Production startup connectivity failed: {entry.Key} is unhealthy. {entry.Value.Description}");
        }
    }
}
