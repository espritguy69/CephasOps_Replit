using System.Net;
using System.Text.Json;
using CephasOps.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.ExceptionHandling;

/// <summary>
/// Global exception handler that returns RFC 7807 Problem Details with correlation ID.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
            ?? httpContext.TraceIdentifier;
        var path = httpContext.Request.Path.ToString();

        // Tenant guard failures: log with request context, return 403 with safe message (no internal detail)
        var isTenantGuardFailure = exception is InvalidOperationException io
            && io.Message.Contains("TenantSafetyGuard", StringComparison.OrdinalIgnoreCase);
        if (isTenantGuardFailure)
        {
            _logger.LogWarning(
                "TenantSafetyGuard violation. Path={RequestPath}, CorrelationId={CorrelationId}, TraceId={TraceId}. Details in PlatformGuardViolation log.",
                path, correlationId, httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception. CorrelationId: {CorrelationId}, Path: {RequestPath}", correlationId, path);
        }

        var statusCode = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => isTenantGuardFailure ? HttpStatusCode.Forbidden : HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        var title = isTenantGuardFailure ? "Forbidden" :
            exception is UnauthorizedAccessException ? "Unauthorized" :
            exception is KeyNotFoundException ? "Not Found" :
            (int)statusCode >= 500 ? "Internal Server Error" : "Bad Request";
        var detail = isTenantGuardFailure ? "Invalid request context." : exception.Message;

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["correlationId"] = correlationId,
                ["traceId"] = httpContext.TraceIdentifier
            }
        };

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            cancellationToken);

        return true;
    }
}
