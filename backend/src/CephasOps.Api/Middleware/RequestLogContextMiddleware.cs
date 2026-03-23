using System.Diagnostics;
using CephasOps.Api.Middleware;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Infrastructure.Metrics;
using Serilog.Context;

namespace CephasOps.Api.Middleware;

/// <summary>
/// Enriches Serilog LogContext with request-scoped values: CompanyId (tenant), UserId, DepartmentId, OrderId, ParseSessionId (when available).
/// Logs one structured operational line per request: tenantId, operation=Request, durationMs, success.
/// Must run after UseAuthentication so ICurrentUserService and IDepartmentRequestContext are populated.
/// CorrelationId is set by CorrelationIdMiddleware earlier in the pipeline.
/// </summary>
public class RequestLogContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLogContextMiddleware> _logger;

    public RequestLogContextMiddleware(RequestDelegate next, ILogger<RequestLogContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var currentUser = context.RequestServices.GetService<ICurrentUserService>();
        var departmentContext = context.RequestServices.GetService<IDepartmentRequestContext>();
        var tenantProvider = context.RequestServices.GetService<ITenantProvider>();

        // Use canonical effective company (TenantGuard runs before this and has already resolved when tenant-required)
        var companyId = tenantProvider?.CurrentTenantId;
        var userId = currentUser?.UserId;
        var departmentId = departmentContext?.DepartmentId;
        var roles = currentUser?.Roles != null && currentUser.Roles.Count > 0
            ? string.Join(",", currentUser.Roles)
            : (string?)null;

        Guid? orderId = null;
        Guid? parseSessionId = null;
        var path = context.Request.Path.Value ?? "";
        var query = context.Request.Query;

        if (path.Contains("orders", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Request.RouteValues.TryGetValue("id", out var idVal))
            {
                var s = idVal?.ToString();
                if (!string.IsNullOrEmpty(s) && Guid.TryParse(s, out var oid))
                    orderId = oid;
            }
            else if (query.TryGetValue("orderId", out var q))
            {
                var qs = q.FirstOrDefault();
                if (!string.IsNullOrEmpty(qs) && Guid.TryParse(qs, out var g))
                    orderId = g;
            }
        }
        if (path.Contains("parse-session", StringComparison.OrdinalIgnoreCase) || path.Contains("parser", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Request.RouteValues.TryGetValue("id", out var idVal))
            {
                var s = idVal?.ToString();
                if (!string.IsNullOrEmpty(s) && Guid.TryParse(s, out var sid))
                    parseSessionId = sid;
            }
            else if (query.TryGetValue("parseSessionId", out var q))
            {
                var qs = q.FirstOrDefault();
                if (!string.IsNullOrEmpty(qs) && Guid.TryParse(qs, out var g))
                    parseSessionId = g;
            }
        }

        using (LogContext.PushProperty("CompanyId", companyId.HasValue && companyId.Value != Guid.Empty ? companyId : null))
        using (LogContext.PushProperty("TenantId", companyId.HasValue && companyId.Value != Guid.Empty ? companyId : null))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("DepartmentId", departmentId))
        using (LogContext.PushProperty("Roles", roles))
        using (LogContext.PushProperty("OrderId", orderId != Guid.Empty ? orderId : null))
        using (LogContext.PushProperty("ParseSessionId", parseSessionId != Guid.Empty ? parseSessionId : null))
        {
            var sw = Stopwatch.StartNew();
            await _next(context);
            sw.Stop();
            var statusCode = context.Response.StatusCode;
            var success = statusCode < 400;
            var durationMs = (int)sw.ElapsedMilliseconds;
            _logger.LogInformation("Request completed. tenantId={TenantId}, operation=Request, durationMs={DurationMs}, success={Success}", companyId, durationMs, success);
            TenantOperationalMetrics.RecordRequest(companyId, success);
            if (!success)
            {
                var guard = context.RequestServices.GetService<CephasOps.Infrastructure.Operational.ITenantOperationsGuard>();
                guard?.RecordRequestError(companyId);
            }
        }
    }
}
