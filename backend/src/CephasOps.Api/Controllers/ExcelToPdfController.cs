using CephasOps.Application.Parser.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Excel to PDF conversion endpoints
/// Uses resilient service with automatic fallback for corrupted files
/// </summary>
[ApiController]
[Route("api/excel-to-pdf")]
[Authorize]
public class ExcelToPdfController : ControllerBase
{
    private readonly IExcelToPdfService _excelToPdfService;
    private readonly ILogger<ExcelToPdfController> _logger;

    // Allowed file extensions
    private static readonly string[] AllowedExtensions = { ".xls", ".xlsx", ".xlsm" };
    private static readonly long MaxFileSize = 50 * 1024 * 1024; // 50MB

    public ExcelToPdfController(
        IExcelToPdfService excelToPdfService,
        ILogger<ExcelToPdfController> logger)
    {
        _excelToPdfService = excelToPdfService;
        _logger = logger;
    }

    /// <summary>
    /// Convert Excel file to PDF
    /// Automatically uses Syncfusion (primary) or ExcelDataReader+QuestPDF (fallback) for corrupted files
    /// </summary>
    /// <param name="file">Excel file to convert (.xls, .xlsx, .xlsm)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file</returns>
    [HttpPost("convert")]
    [ApiExplorerSettings(IgnoreApi = true)] // Exclude from auto-generation, will be added manually
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
    public async Task<ActionResult> ConvertToPdf(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest($"Invalid file format. File must be Excel format (.xls, .xlsx, .xlsm). Received: {extension}");
        }

        // Validate file size
        if (file.Length > MaxFileSize)
        {
            return BadRequest($"File too large. File size ({file.Length / 1024 / 1024} MB) exceeds maximum of {MaxFileSize / 1024 / 1024} MB");
        }

        try
        {
            _logger.LogInformation("Converting Excel to PDF: {FileName}, Size: {Size} KB", 
                file.FileName, file.Length / 1024);

            // Convert to PDF using resilient service
            var pdfBytes = await _excelToPdfService.ConvertToPdfAsync(file, cancellationToken);

            _logger.LogInformation("Excel to PDF conversion successful: {FileName}, PDF Size: {Size} KB",
                file.FileName, pdfBytes.Length / 1024);

            // Return PDF file
            var pdfFileName = Path.ChangeExtension(file.FileName, ".pdf");
            return File(pdfBytes, "application/pdf", pdfFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting Excel to PDF: {FileName}", file.FileName);
            return StatusCode(500, ApiResponse.ErrorResponse(
                $"Conversion failed: {ex.Message}. The Excel file may be corrupted, password-protected, or in an unsupported format. " +
                "If the file opens correctly in Excel, please contact support."));
        }
    }

    /// <summary>
    /// Health check endpoint to verify Excel to PDF service is available
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<object>> GetHealth()
    {
        return this.Success<object>(new 
        { 
            status = "healthy",
            service = "ExcelToPdf",
            converters = new[] { "Syncfusion (Primary)", "ExcelDataReader+QuestPDF (Fallback)" },
            supportedFormats = AllowedExtensions,
            maxFileSize = $"{MaxFileSize / 1024 / 1024} MB"
        });
    }
}

