namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Provides the current correlation ID for tracing (e.g. from HTTP request or background job context).
/// Enables propagation: HTTP → Workflow → JobRun → Event.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Gets the current correlation ID, or null if not in a correlated context (e.g. background job without one set).
    /// </summary>
    string? GetCorrelationId();
}
