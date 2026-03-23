using CephasOps.Api.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CephasOps.Api.Health;

/// <summary>Health check for Platform Guardian: config present and enabled when expected in production.</summary>
public sealed class GuardianHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<ProductionRolesOptions> _rolesOptions;
    private readonly IOptions<CephasOps.Application.Platform.Guardian.PlatformGuardianOptions> _guardianOptions;

    public GuardianHealthCheck(
        IConfiguration configuration,
        IOptions<ProductionRolesOptions> rolesOptions,
        IOptions<CephasOps.Application.Platform.Guardian.PlatformGuardianOptions> guardianOptions)
    {
        _configuration = configuration;
        _rolesOptions = rolesOptions;
        _guardianOptions = guardianOptions;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var isProduction = string.Equals(_configuration["ASPNETCORE_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase);
        var runGuardian = _rolesOptions.Value.RunGuardian;
        var guardianEnabled = _guardianOptions.Value.Enabled;

        if (!runGuardian)
        {
            var data = new Dictionary<string, object>
            {
                ["productionRolesRunGuardian"] = false,
                ["platformGuardianEnabled"] = guardianEnabled
            };
            return Task.FromResult(
                isProduction
                    ? HealthCheckResult.Degraded("Guardian is disabled (ProductionRoles:RunGuardian=false) in Production", data: data)
                    : HealthCheckResult.Healthy("Guardian disabled by role (non-production)", data: data));
        }

        if (!guardianEnabled)
        {
            var data = new Dictionary<string, object>
            {
                ["productionRolesRunGuardian"] = true,
                ["platformGuardianEnabled"] = false
            };
            return Task.FromResult(
                isProduction
                    ? HealthCheckResult.Degraded("Platform Guardian is disabled (PlatformGuardian:Enabled=false) in Production", data: data)
                    : HealthCheckResult.Healthy("Guardian disabled by config", data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Guardian enabled and configured", new Dictionary<string, object>
        {
            ["runIntervalMinutes"] = _guardianOptions.Value.RunIntervalMinutes,
            ["runAnomalyDetection"] = _guardianOptions.Value.RunAnomalyDetection,
            ["runDriftDetection"] = _guardianOptions.Value.RunDriftDetection,
            ["runPerformanceWatchdog"] = _guardianOptions.Value.RunPerformanceWatchdog
        }));
    }
}
