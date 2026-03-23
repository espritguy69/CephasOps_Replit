using CephasOps.Application.Billing.Usage;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Api.Options;
using CephasOps.Api.Services.RateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CephasOps.Api.Middleware;

/// <summary>SaaS scaling: per-tenant API rate limit. Uses IRateLimitStore (in-memory or Redis). Plan overrides via ITenantRateLimitResolver.</summary>
public class TenantRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantRateLimitOptions _options;
    private readonly ILogger<TenantRateLimitMiddleware> _logger;
    private readonly IRateLimitStore _store;

    public TenantRateLimitMiddleware(
        RequestDelegate next,
        IOptions<TenantRateLimitOptions> options,
        ILogger<TenantRateLimitMiddleware> logger,
        IRateLimitStore store)
    {
        _next = next;
        _options = options?.Value ?? new TenantRateLimitOptions();
        _logger = logger;
        _store = store;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var tenantProvider = context.RequestServices.GetService<ITenantProvider>();
        var companyId = tenantProvider?.CurrentTenantId;
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
        {
            await _next(context);
            return;
        }

        var resolver = context.RequestServices.GetService<ITenantRateLimitResolver>();
        var (perMinute, perHour) = resolver != null
            ? await resolver.GetLimitsAsync(companyId.Value, context.RequestAborted)
            : (_options.RequestsPerMinute, _options.RequestsPerHour);

        var (allowed, limitType) = await _store.TryConsumeAsync(companyId.Value, perMinute, perHour, context.RequestAborted);

        if (!allowed)
        {
            var endpoint = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "Unknown";
            _logger.LogWarning(
                "TenantRateLimitExceeded TenantId={TenantId} Endpoint={Endpoint} LimitType={LimitType}",
                companyId.Value, endpoint, limitType);
            var usageService = context.RequestServices.GetService<ITenantUsageService>();
            if (usageService != null)
            {
                try
                {
                    await usageService.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.RateLimitExceeded, 1, context.RequestAborted);
                }
                catch
                {
                    // Do not fail the 429 response if metrics recording fails
                }
            }
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Tenant request limit exceeded. Please retry later.\"}");
            return;
        }

        await _next(context);
    }
}

/// <summary>Optional: resolve per-tenant rate limits (e.g. from subscription plan). When not registered, default options are used.</summary>
public interface ITenantRateLimitResolver
{
    Task<(int RequestsPerMinute, int RequestsPerHour)> GetLimitsAsync(Guid companyId, CancellationToken cancellationToken = default);
}
