namespace CephasOps.Api.Services.RateLimit;

/// <summary>Per-tenant rate limit consumption. When not using Redis, use in-memory store; when Redis is configured, use distributed store for shared state across API replicas.</summary>
public interface IRateLimitStore
{
    /// <summary>Try to consume one request for the tenant. Returns (true, null) if allowed, (false, limitType) if rate limited (limitType is "Minute" or "Hour").</summary>
    Task<(bool Allowed, string? LimitType)> TryConsumeAsync(Guid tenantId, int perMinute, int perHour, CancellationToken cancellationToken = default);
}
