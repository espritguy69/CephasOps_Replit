using System.Text.Json;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Subscription;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Api.Middleware;

/// <summary>Phase 3: After authentication, block requests when tenant/subscription state does not allow access.</summary>
public class SubscriptionEnforcementMiddleware
{
    private static readonly PathString AuthBase = new("/api/auth");
    private static readonly string[] AllowedAuthPaths = { "login", "refresh", "forgot-password", "change-password-required", "reset-password-with-token" };
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public SubscriptionEnforcementMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }
        if (_env.EnvironmentName == "Testing")
        {
            await _next(context);
            return;
        }
        if (IsAuthPathThatSkipsEnforcement(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var tenantProvider = context.RequestServices.GetService<ITenantProvider>();
        var accessService = context.RequestServices.GetService<ISubscriptionAccessService>();
        if (tenantProvider == null || accessService == null)
        {
            await _next(context);
            return;
        }
        // Use same effective tenant as TenantGuardMiddleware so SuperAdmin X-Company-Id and JWT company are consistent
        var companyId = tenantProvider.CurrentTenantId;
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
        {
            await _next(context);
            return;
        }
        var access = await accessService.GetAccessForCompanyAsync(companyId, context.RequestAborted);
        if (access.Allowed)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        var body = new
        {
            success = false,
            message = access.DenialReason ?? "Tenant access is not allowed.",
            denialReason = access.DenialReason ?? "tenant_suspended",
            readOnlyMode = access.ReadOnlyMode,
            errors = new[] { access.DenialReason ?? "tenant_suspended" }
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static bool IsAuthPathThatSkipsEnforcement(PathString path)
    {
        if (!path.StartsWithSegments(AuthBase, StringComparison.OrdinalIgnoreCase))
            return false;
        var segment = path.Value?.Replace(AuthBase.Value!, "", StringComparison.OrdinalIgnoreCase).TrimStart('/') ?? "";
        var first = segment.Split('/')[0];
        return AllowedAuthPaths.Contains(first, StringComparer.OrdinalIgnoreCase);
    }
}
