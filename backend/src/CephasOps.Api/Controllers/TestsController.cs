using CephasOps.Application.Testing.DTOs;
using CephasOps.Application.Testing.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Test management endpoints
/// </summary>
[ApiController]
[Route("api/tests")]
[Authorize]
public class TestsController : ControllerBase
{
    private readonly ITestRunnerService _testRunnerService;
    private readonly ILogger<TestsController> _logger;

    public TestsController(
        ITestRunnerService testRunnerService,
        ILogger<TestsController> logger)
    {
        _testRunnerService = testRunnerService;
        _logger = logger;
    }

    /// <summary>
    /// Get test results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results</returns>
    [HttpGet("results")]
    [ProducesResponseType(typeof(ApiResponse<TestResultsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TestResultsDto>>> GetTestResults(CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _testRunnerService.GetTestResultsAsync(cancellationToken);
            return this.Success(results, "Test results retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test results");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to retrieve test results: {ex.Message}"));
        }
    }

    /// <summary>
    /// Run tests
    /// </summary>
    /// <param name="suite">Optional test suite name to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test run result</returns>
    [HttpPost("run")]
    [ProducesResponseType(typeof(ApiResponse<TestRunResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TestRunResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TestRunResultDto>>> RunTests(
        [FromQuery] string? suite = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _testRunnerService.RunTestsAsync(suite, cancellationToken);
            
            if (result.Success)
            {
                return this.Success(result, result.Message);
            }
            else
            {
                return StatusCode(500, ApiResponse.ErrorResponse(result.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running tests");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to run tests: {ex.Message}"));
        }
    }
}

