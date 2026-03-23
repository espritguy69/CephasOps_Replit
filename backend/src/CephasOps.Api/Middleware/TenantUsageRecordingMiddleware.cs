using CephasOps.Application.Billing.Usage;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Api.Middleware;

/// <summary>SaaS scaling: records API call usage per tenant for metering. Runs after request; skips when no tenant.</summary>
public class TenantUsageRecordingMiddleware
{
    private readonly RequestDelegate _next;

    public TenantUsageRecordingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var tenantProvider = context.RequestServices.GetService<ITenantProvider>();
        var usageService = context.RequestServices.GetService<ITenantUsageService>();
        if (tenantProvider == null || usageService == null)
            return;

        var companyId = tenantProvider.CurrentTenantId;
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            return;

        try
        {
            await usageService.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.ApiCalls, 1, context.RequestAborted);
        }
        catch
        {
            // Do not fail the request if usage recording fails
        }
    }
}
