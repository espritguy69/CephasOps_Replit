using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace CephasOps.Api.Middleware;

/// <summary>
/// Ensures every request has a correlation ID for tracing.
/// Reads X-Correlation-Id from request or generates one; sets it in HttpContext.Items and Serilog LogContext.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? context.Request.Headers["Request-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items[CorrelationIdItemKey] = correlationId;

        // Add to response so clients can correlate
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            }
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
