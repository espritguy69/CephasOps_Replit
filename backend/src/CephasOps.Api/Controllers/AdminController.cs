using CephasOps.Api.Common;
using CephasOps.Api.Models;
using CephasOps.Api.Services;
using CephasOps.Application.Admin.DTOs;
using CephasOps.Application.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Administrative endpoints for system maintenance and monitoring
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ExcelParseReportService _excelParseReportService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminService adminService,
        ExcelParseReportService excelParseReportService,
        ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _excelParseReportService = excelParseReportService;
        _logger = logger;
    }

    /// <summary>
    /// Rebuild search indexes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("reindex")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> Reindex(CancellationToken cancellationToken = default)
    {
        try
        {
            await _adminService.ReindexAsync(cancellationToken);
            return this.Success<object>(new { message = "Search indexes rebuilt successfully" }, "Search indexes rebuilt successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding search indexes");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to rebuild search indexes: {ex.Message}"));
        }
    }

    /// <summary>
    /// Flush settings cache
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("cache/flush")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> FlushCache(CancellationToken cancellationToken = default)
    {
        try
        {
            await _adminService.FlushCacheAsync(cancellationToken);
            return this.Success<object>(new { message = "Settings cache flushed successfully" }, "Settings cache flushed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing cache");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to flush cache: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get system health status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System health information</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<SystemHealthDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SystemHealthDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<SystemHealthDto>>> GetHealth(CancellationToken cancellationToken = default)
    {
        var health = await _adminService.GetHealthAsync(cancellationToken);

        if (!health.IsHealthy)
        {
            return StatusCode(503, ApiResponse<SystemHealthDto>.SuccessResponse(health, "System is unhealthy"));
        }

        return this.Success(health, "System is healthy.");
    }

    /// <summary>
    /// Get read-only email ingestion diagnostics (last successful job, job counts by state, account last poll, drafts today). No mutations, no secrets.
    /// </summary>
    [HttpGet("diagnostics/email-ingestion")]
    [ProducesResponseType(typeof(ApiResponse<EmailIngestionDiagnosticsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EmailIngestionDiagnosticsDto>>> GetEmailIngestionDiagnostics(CancellationToken cancellationToken = default)
    {
        var dto = await _adminService.GetEmailIngestionDiagnosticsAsync(cancellationToken);
        return this.Success(dto, "Email ingestion diagnostics.");
    }

    /// <summary>
    /// Parse backend/test-data/A1862604.xls with ExcelDataReader (no Syncfusion) and return a read-only JSON report.
    /// No DB writes.
    /// </summary>
    [HttpGet("test/parse-excel")]
    [ProducesResponseType(typeof(ApiResponse<ParseExcelReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public ActionResult<ApiResponse<ParseExcelReportDto>> GetParseExcelReport()
    {
        try
        {
            var report = _excelParseReportService.GetReport();
            return this.Success(report, "Excel parse report.");
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Test Excel file not found");
            return this.NotFound<ParseExcelReportDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing test Excel file");
            return this.InternalServerError<ParseExcelReportDto>(ex.Message);
        }
    }
}
