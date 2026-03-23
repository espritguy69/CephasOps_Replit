using System.Text.RegularExpressions;

namespace CephasOps.Application.Events;

/// <summary>
/// Classifies handler failures as retryable vs non-retryable (poison). Phase 7.
/// </summary>
public sealed class FailureClassifier : IFailureClassifier
{
    private static readonly string[] NonRetryableMessagePatterns =
    {
        "validation", "invalid", "not found", "does not exist", "missing required",
        "deserialization", "could not deserialize", "invalid json", "unable to parse",
        "contract", "version", "unsupported version", "payload version",
        "foreign key", "reference", "required referenced entity",
        "duplicate key", "unique constraint", "already exists"
    };

    private static readonly string[] RetryableMessagePatterns =
    {
        "timeout", "connection", "network", "temporarily unavailable",
        "deadlock", "lock", "contention", "could not obtain",
        "transient", "retry", "busy", "overloaded"
    };

    /// <inheritdoc />
    public bool IsNonRetryable(Exception exception)
    {
        var ex = exception;
        while (ex != null)
        {
            var typeName = ex.GetType().FullName ?? ex.GetType().Name;
            var message = (ex.Message ?? "").ToLowerInvariant();

            if (IsNonRetryableType(typeName))
                return true;
            if (IsRetryableType(typeName))
                return false;
            foreach (var pattern in NonRetryableMessagePatterns)
            {
                if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            foreach (var pattern in RetryableMessagePatterns)
            {
                if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            ex = ex.InnerException;
        }
        return false;
    }

    /// <inheritdoc />
    public string GetErrorType(Exception exception)
    {
        var ex = exception;
        while (ex != null)
        {
            var typeName = ex.GetType().Name;
            var message = (ex.Message ?? "").ToLowerInvariant();

            if (typeName.Contains("Validation", StringComparison.OrdinalIgnoreCase) || message.Contains("validation"))
                return "Validation";
            if (typeName.Contains("Json", StringComparison.OrdinalIgnoreCase) || message.Contains("deserialization") || message.Contains("invalid json"))
                return "Deserialization";
            if (message.Contains("not found") || message.Contains("does not exist") || message.Contains("missing required"))
                return "MissingEntity";
            if (message.Contains("version") || message.Contains("unsupported"))
                return "UnsupportedVersion";
            if (message.Contains("foreign key") || message.Contains("reference"))
                return "ReferenceViolation";
            if (message.Contains("duplicate") || message.Contains("unique constraint"))
                return "Duplicate";
            if (typeName.Contains("Timeout", StringComparison.OrdinalIgnoreCase) || message.Contains("timeout"))
                return "Timeout";
            if (message.Contains("connection") || message.Contains("network"))
                return "Network";
            if (message.Contains("deadlock") || message.Contains("lock"))
                return "LockContention";
            ex = ex.InnerException;
        }
        return exception?.GetType().Name ?? "Unknown";
    }

    private static bool IsNonRetryableType(string typeName)
    {
        return typeName.Contains("ValidationException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("ArgumentException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("KeyNotFoundException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("InvalidOperationException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("JsonException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("NotSupportedException", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRetryableType(string typeName)
    {
        return typeName.Contains("TimeoutException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("IOException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("SqlException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("NpgsqlException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("HttpRequestException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("TaskCanceledException", StringComparison.OrdinalIgnoreCase);
    }
}
