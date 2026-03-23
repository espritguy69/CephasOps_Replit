namespace EmailTester.Models;

/// <summary>
/// Generic result for a connection/authentication test.
/// </summary>
public sealed class TestResult
{
    public bool Success { get; init; }

    /// <summary>
    /// High-level message, e.g. "SMTP connection successful".
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Time taken from start of connect until successful authentication, in milliseconds.
    /// For failures, this may be the elapsed time until the error was thrown.
    /// </summary>
    public long ResponseTimeMs { get; init; }

    /// <summary>
    /// Detailed error message when <see cref="Success"/> is false.
    /// </summary>
    public string? Error { get; init; }

    public static TestResult SuccessResult(string message, long responseTimeMs) => new()
    {
        Success = true,
        Message = message,
        ResponseTimeMs = responseTimeMs,
        Error = null
    };

    public static TestResult FailureResult(string message, string error, long responseTimeMs) => new()
    {
        Success = false,
        Message = message,
        ResponseTimeMs = responseTimeMs,
        Error = error
    };
}


