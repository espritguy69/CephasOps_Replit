using CephasOps.Domain.PlatformSafety;
using CephasOps.Infrastructure.Metrics;
using Microsoft.Extensions.Logging;

namespace CephasOps.Infrastructure;

/// <summary>
/// Structured logging for platform safety guard violations. Use when a guard is about to throw.
/// Category name allows operators to filter logs (e.g. "PlatformGuardViolation").
/// Initialize once at application startup so guards can log without requiring ILogger at every call site.
/// Optionally provide an IGuardViolationBuffer so violations are available for operational observability.
/// </summary>
public static class PlatformGuardLogger
{
    /// <summary>Logging category for guard violation events. Use in log filtering and dashboards.</summary>
    public const string CategoryName = "PlatformGuardViolation";

    private static ILogger? _logger;
    private static IGuardViolationBuffer? _buffer;

    /// <summary>
    /// Initialize the shared logger. Call once at startup with a logger created for category <see cref="CategoryName"/> (e.g. factory.CreateLogger(PlatformGuardLogger.CategoryName)).
    /// Optionally pass a buffer so violations can be read by the operations overview.
    /// </summary>
    public static void Initialize(ILogger? logger, IGuardViolationBuffer? buffer = null)
    {
        _logger = logger;
        _buffer = buffer;
    }

    /// <summary>
    /// Log a guard violation just before the guard throws. Avoid passing sensitive payload data.
    /// </summary>
    /// <param name="guardName">Name of the guard (e.g. TenantSafetyGuard, SiWorkflowGuard).</param>
    /// <param name="operation">Short operation name (e.g. SaveChanges, Append, Order status transition).</param>
    /// <param name="message">Brief details for diagnostics (no sensitive data).</param>
    /// <param name="companyId">Optional; safe identifier.</param>
    /// <param name="entityType">Optional; e.g. Order, EventStoreEntry.</param>
    /// <param name="entityId">Optional; safe identifier.</param>
    /// <param name="orderId">Optional; when relevant.</param>
    /// <param name="eventId">Optional; when relevant.</param>
    public static void LogViolation(
        string guardName,
        string operation,
        string message,
        Guid? companyId = null,
        string? entityType = null,
        Guid? entityId = null,
        Guid? orderId = null,
        Guid? eventId = null)
    {
        if (_logger != null)
        {
            _logger.LogWarning(
                "PlatformGuardViolation: {GuardName} triggered during {Operation}. Details: {Message}. CompanyId={CompanyId}, EntityType={EntityType}, EntityId={EntityId}, OrderId={OrderId}, EventId={EventId}",
                guardName,
                operation,
                message,
                companyId,
                entityType ?? "",
                entityId,
                orderId,
                eventId);
        }

        if (_buffer != null)
        {
            _buffer.Record(new GuardViolationEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                GuardName = guardName ?? "",
                Operation = operation ?? "",
                Message = message ?? "",
                CompanyId = companyId,
                EntityType = entityType,
                EntityId = entityId,
                EventId = eventId
            });
        }

        TenantSafetyMetrics.RecordGuardViolation(guardName ?? "", operation ?? "");
    }
}
