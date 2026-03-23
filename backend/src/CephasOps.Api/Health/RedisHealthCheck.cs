using System.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace CephasOps.Api.Health;

/// <summary>Health check for Redis (rate-limit store / cache). Used when ConnectionStrings:Redis is set.</summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync(CommandFlags.None).WaitAsync(cancellationToken).ConfigureAwait(false);
            var endpoints = _redis.GetEndPoints();
            return HealthCheckResult.Healthy("Redis connection OK", new Dictionary<string, object>
            {
                ["endpoints"] = string.Join(", ", endpoints.Select(e => e.ToString()))
            });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }
}
