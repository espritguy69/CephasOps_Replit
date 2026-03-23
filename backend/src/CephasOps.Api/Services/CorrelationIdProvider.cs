using CephasOps.Application.Common.Interfaces;
using CephasOps.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Api.Services;

/// <summary>
/// Provides the current correlation ID from the HTTP request (set by CorrelationIdMiddleware).
/// Enables propagation: HTTP → Workflow → JobRun → Event.
/// </summary>
public class CorrelationIdProvider : ICorrelationIdProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string? GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var value) == true && value is string id)
            return id;
        return null;
    }
}
