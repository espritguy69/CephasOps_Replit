using CephasOps.Application.Testing.DTOs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CephasOps.Application.Testing.Services;

/// <summary>
/// Service for running tests and retrieving test results
/// </summary>
public class TestRunnerService : ITestRunnerService
{
    private readonly ILogger<TestRunnerService> _logger;
    private readonly string _testProjectPath;
    private TestResultsDto? _cachedResults;

    public TestRunnerService(ILogger<TestRunnerService> logger)
    {
        _logger = logger;
        
        // Determine test project path relative to solution root
        var currentDir = Directory.GetCurrentDirectory();
        var solutionRoot = FindSolutionRoot(currentDir);
        _testProjectPath = Path.Combine(solutionRoot, "backend", "tests", "CephasOps.Application.Tests", "CephasOps.Application.Tests.csproj");
    }

    private string FindSolutionRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return startPath;
    }

    public async Task<TestResultsDto> GetTestResultsAsync(CancellationToken cancellationToken = default)
    {
        // If we have cached results, return them
        if (_cachedResults != null)
        {
            return _cachedResults;
        }

        // Otherwise, run tests to get fresh results
        await RunTestsAsync(null, cancellationToken);
        return _cachedResults ?? new TestResultsDto();
    }

    public async Task<TestRunResultDto> RunTestsAsync(string? suiteName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Running tests for suite: {SuiteName}", suiteName ?? "All");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"test \"{_testProjectPath}\" --verbosity normal --logger \"trx;LogFileName=test-results.trx\" --no-build",
                WorkingDirectory = Path.GetDirectoryName(_testProjectPath) ?? Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new List<string>();
            var errorBuilder = new List<string>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.Add(e.Data);
                    _logger.LogDebug("Test output: {Output}", e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.Add(e.Data);
                    _logger.LogWarning("Test error: {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var output = string.Join("\n", outputBuilder);
            var errors = string.Join("\n", errorBuilder);

            // Parse test results from output
            var results = ParseTestOutput(output, errors);
            _cachedResults = results;

            var success = process.ExitCode == 0;
            return new TestRunResultDto
            {
                Success = success,
                Message = success 
                    ? $"Tests completed: {results.Suites.Sum(s => s.Passed)} passed, {results.Suites.Sum(s => s.Failed)} failed"
                    : $"Tests failed with exit code {process.ExitCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running tests");
            return new TestRunResultDto
            {
                Success = false,
                Message = $"Failed to run tests: {ex.Message}"
            };
        }
    }

    private TestResultsDto ParseTestOutput(string output, string errors)
    {
        var results = new TestResultsDto();
        var allOutput = output + "\n" + errors;

        // Pattern to match test summary: "Passed!  - Failed:     1, Passed:   250, Skipped:     0, Total:   251"
        var summaryPattern = @"Passed!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)";
        var summaryMatch = Regex.Match(allOutput, summaryPattern, RegexOptions.IgnoreCase);

        if (summaryMatch.Success)
        {
            var suite = new TestSuiteDto
            {
                Name = "Backend Application Tests",
                Total = int.Parse(summaryMatch.Groups[4].Value),
                Passed = int.Parse(summaryMatch.Groups[2].Value),
                Failed = int.Parse(summaryMatch.Groups[1].Value),
                Skipped = int.Parse(summaryMatch.Groups[3].Value),
                Status = int.Parse(summaryMatch.Groups[1].Value) > 0 ? "failed" : "passed"
            };

            // Try to extract duration from output
            var durationPattern = @"Duration:\s*([\d.]+)\s*s";
            var durationMatch = Regex.Match(allOutput, durationPattern);
            if (durationMatch.Success)
            {
                suite.Duration = double.Parse(durationMatch.Groups[1].Value);
            }

            results.Suites.Add(suite);
        }

        // Parse individual test failures
        var failurePattern = @"Failed\s+([^\s]+)\s+\[([\d.]+)\s*s\]";
        var failureMatches = Regex.Matches(allOutput, failurePattern);

        foreach (Match match in failureMatches)
        {
            var testName = match.Groups[1].Value.Trim();
            var duration = double.Parse(match.Groups[2].Value);

            // Extract error message if available
            var errorMessage = ExtractErrorMessage(allOutput, testName);

            results.Results.Add(new TestResultDto
            {
                Suite = "Backend Application Tests",
                Name = testName,
                Status = "failed",
                Duration = duration,
                Error = errorMessage
            });
        }

        return results;
    }

    private string? ExtractErrorMessage(string output, string testName)
    {
        // Look for error details after the test name
        var pattern = $@"{Regex.Escape(testName)}[^\n]*\n(.*?)(?=\n\s*Failed|\n\s*Passed|\Z)";
        var match = Regex.Match(output, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var errorText = match.Groups[1].Value.Trim();
            // Extract exception type and message
            var exceptionPattern = @"(\w+Exception)\s*:\s*(.*?)(?=\n\s*Stack|\n\s*at\s+|\Z)";
            var exceptionMatch = Regex.Match(errorText, exceptionPattern, RegexOptions.Singleline);
            
            if (exceptionMatch.Success)
            {
                return $"{exceptionMatch.Groups[1].Value}: {exceptionMatch.Groups[2].Value.Trim()}";
            }
            
            return errorText.Length > 200 ? errorText.Substring(0, 200) + "..." : errorText;
        }

        return null;
    }
}

