using System.Text.Json;
using CephasOps.Api.Common.Attributes;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Api.Middleware;

/// <summary>
/// Platform-layer tenant safety: blocks any request from reaching business logic when a valid
/// company/tenant context is missing. Ensures no API endpoint can run without tenant scope
/// even if RequireCompanyId() is not called in the controller.
/// </summary>
public class TenantGuardMiddleware
{
    private const string ErrorMessage = "Company context is required for this operation.";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly PathString ApiAuth = new("/api/auth");
    private static readonly PathString ApiPlatform = new("/api/platform");
    private static readonly PathString Health = new("/health");
    private static readonly PathString Swagger = new("/swagger");

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantGuardMiddleware> _logger;

    public TenantGuardMiddleware(RequestDelegate next, ILogger<TenantGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkipValidation(context))
        {
            await _next(context);
            return;
        }

        var tenantProvider = context.RequestServices.GetService<ITenantProvider>();
        if (tenantProvider == null)
        {
            _logger.LogError("Tenant guard could not resolve ITenantProvider; blocking request for safety.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var body = new { error = ErrorMessage };
            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
            return;
        }

        await tenantProvider.GetEffectiveCompanyIdAsync(context.RequestAborted);
        var tenantId = tenantProvider.CurrentTenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            await _next(context);
            return;
        }

        // SuperAdmin may access platform-level endpoints without a company (e.g. list all users, admin/roles).
        var isSuperAdmin = context.User?.IsInRole("SuperAdmin") ?? false;
        if (isSuperAdmin)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.User?.FindFirst("sub")?.Value
            ?? "(anonymous)";

        _logger.LogWarning(
            "Tenant guard blocked request with missing company context. Endpoint: {Path}, User: {UserId}",
            path,
            userId);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        var responseBody = new { error = ErrorMessage };
        await context.Response.WriteAsync(JsonSerializer.Serialize(responseBody, JsonOptions));
    }

    private static bool ShouldSkipValidation(HttpContext context)
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments(ApiAuth, StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWithSegments(ApiPlatform, StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWithSegments(Health, StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWithSegments(Swagger, StringComparison.OrdinalIgnoreCase))
            return true;

        var endpoint = context.GetEndpoint();
        if (endpoint == null)
            return true;

        if (endpoint.Metadata.GetMetadata<AllowNoTenantAttribute>() != null)
            return true;
        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null)
            return true;

        return false;
    }
}
