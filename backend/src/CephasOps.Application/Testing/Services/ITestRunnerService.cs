using CephasOps.Application.Testing.DTOs;

namespace CephasOps.Application.Testing.Services;

/// <summary>
/// Service for running tests and retrieving test results
/// </summary>
public interface ITestRunnerService
{
    /// <summary>
    /// Get test results from the last test run
    /// </summary>
    Task<TestResultsDto> GetTestResultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run all tests
    /// </summary>
    Task<TestRunResultDto> RunTestsAsync(string? suiteName = null, CancellationToken cancellationToken = default);
}

