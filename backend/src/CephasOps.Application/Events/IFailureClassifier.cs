namespace CephasOps.Application.Events;

/// <summary>
/// Classifies handler failures as retryable (transient) or non-retryable (poison). Phase 7.
/// Non-retryable failures move directly to DeadLetter; retryable use the retry schedule.
/// </summary>
public interface IFailureClassifier
{
    /// <summary>
    /// Returns true if the failure is non-retryable (poison). Examples: validation, missing entity, deserialization, unsupported version.
    /// </summary>
    bool IsNonRetryable(Exception exception);

    /// <summary>
    /// Returns a short error type label for logging and storage (e.g. Validation, Deserialization, Transient).
    /// </summary>
    string GetErrorType(Exception exception);
}
