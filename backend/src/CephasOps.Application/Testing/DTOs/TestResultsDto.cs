namespace CephasOps.Application.Testing.DTOs;

/// <summary>
/// Test results summary
/// </summary>
public class TestResultsDto
{
    public List<TestSuiteDto> Suites { get; set; } = new();
    public List<TestResultDto> Results { get; set; } = new();
}

/// <summary>
/// Test suite summary
/// </summary>
public class TestSuiteDto
{
    public string Name { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public double Duration { get; set; }
    public string Status { get; set; } = "unknown"; // passed, failed, running
}

/// <summary>
/// Individual test result
/// </summary>
public class TestResultDto
{
    public string Suite { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "unknown"; // passed, failed, skipped
    public double Duration { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Test run result
/// </summary>
public class TestRunResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? JobId { get; set; }
}

