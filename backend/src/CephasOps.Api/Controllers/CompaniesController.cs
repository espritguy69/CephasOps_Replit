using CephasOps.Api.Common;
using CephasOps.Application.Companies.DTOs;
using CephasOps.Application.Companies.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Company management endpoints.
/// </summary>
[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly ICompanyDeploymentService _deploymentService;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(
        ICompanyService companyService,
        ICompanyDeploymentService deploymentService,
        ILogger<CompaniesController> logger)
    {
        _companyService = companyService;
        _deploymentService = deploymentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all companies with optional filters.
    /// </summary>
    /// <param name="isActive">Filter by active status.</param>
    /// <param name="search">Case-insensitive search across name, short name, vertical.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CompanyDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<CompanyDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<CompanyDto>>>> GetCompanies(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var companies = await _companyService.GetCompaniesAsync(isActive, search, cancellationToken);
            return this.Success(companies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving companies");
            return this.Error<List<CompanyDto>>("Failed to retrieve companies", 500);
        }
    }

    /// <summary>
    /// Get a single company by ID.
    /// </summary>
    /// <param name="id">Company identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> GetCompany(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var company = await _companyService.GetCompanyByIdAsync(id, cancellationToken);
            if (company == null)
            {
                return this.NotFound<CompanyDto>($"Company with ID {id} not found");
            }

            return this.Success(company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company {CompanyId}", id);
            return this.Error<CompanyDto>("Failed to retrieve company", 500);
        }
    }

    /// <summary>
    /// Create a new company.
    /// </summary>
    /// <param name="dto">Company data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> CreateCompany(
        [FromBody] CreateCompanyDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var company = await _companyService.CreateCompanyAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, ApiResponse<CompanyDto>.SuccessResponse(company, "Company created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Duplicate company detected while creating company");
            return this.Error<CompanyDto>(ex.Message, 409);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid company payload");
            return this.Error<CompanyDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating company");
            return this.Error<CompanyDto>("Failed to create company", 500);
        }
    }

    /// <summary>
    /// Update an existing company.
    /// </summary>
    /// <param name="id">Company identifier.</param>
    /// <param name="dto">Updated data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> UpdateCompany(
        Guid id,
        [FromBody] UpdateCompanyDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var company = await _companyService.UpdateCompanyAsync(id, dto, cancellationToken);
            return this.Success(company);
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<CompanyDto>($"Company with ID {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Duplicate company detected while updating {CompanyId}", id);
            return this.Error<CompanyDto>(ex.Message, 409);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid company update payload for {CompanyId}", id);
            return this.Error<CompanyDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company {CompanyId}", id);
            return this.Error<CompanyDto>("Failed to update company", 500);
        }
    }

    /// <summary>
    /// Delete a company.
    /// </summary>
    /// <param name="id">Company identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteCompany(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _companyService.DeleteCompanyAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Company with ID {id} not found"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Unable to delete company {CompanyId}", id);
            return StatusCode(409, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting company {CompanyId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse("Failed to delete company"));
        }
    }

    #region Company Deployment

    /// <summary>
    /// Download company deployment template (single file or separate files)
    /// </summary>
    [HttpGet("deployment/template")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeploymentTemplate(
        [FromQuery] string format = "single",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (format != "single" && format != "separate")
            {
                return BadRequest(new { error = "Format must be 'single' or 'separate'" });
            }

            var templateBytes = await _deploymentService.GenerateTemplateAsync(format, cancellationToken);
            var fileName = format == "single" 
                ? "company-deployment-template.xlsx" 
                : "company-deployment-instructions.xlsx";

            return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating deployment template");
            return StatusCode(500, new { error = "Failed to generate template", message = ex.Message });
        }
    }

    /// <summary>
    /// Validate company deployment files (dry-run)
    /// </summary>
    [HttpPost("deployment/validate")]
    [ProducesResponseType(typeof(ApiResponse<DeploymentValidationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeploymentValidationResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DeploymentValidationResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DeploymentValidationResult>>> ValidateDeployment(
        IFormFileCollection files,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            return this.Error<DeploymentValidationResult>("No files uploaded", 400);
        }

        try
        {
            var result = await _deploymentService.ValidateDeploymentAsync(files, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating deployment");
            return this.Error<DeploymentValidationResult>("Failed to validate deployment", 500);
        }
    }

    /// <summary>
    /// Import company deployment from Excel files
    /// </summary>
    [HttpPost("deployment/import")]
    [ProducesResponseType(typeof(ApiResponse<DeploymentImportResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeploymentImportResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DeploymentImportResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DeploymentImportResult>>> ImportDeployment(
        IFormFileCollection files,
        [FromQuery] DeploymentImportOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            return this.BadRequest<DeploymentImportResult>("No files uploaded");
        }

        try
        {
            var importOptions = options ?? new DeploymentImportOptions();
            var result = await _deploymentService.ImportDeploymentAsync(files, importOptions, cancellationToken);
            return this.Success(result, "Deployment imported successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing deployment");
            return this.InternalServerError<DeploymentImportResult>($"Failed to import deployment: {ex.Message}");
        }
    }

    /// <summary>
    /// Export existing company data to Excel
    /// </summary>
    [HttpGet("deployment/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCompany(
        [FromQuery] Guid? companyId,
        [FromQuery] string format = "single",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (format != "single" && format != "separate")
            {
                return BadRequest(new { error = "Format must be 'single' or 'separate'" });
            }

            var exportBytes = await _deploymentService.ExportCompanyAsync(companyId, format, cancellationToken);
            var fileName = $"company-export-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";

            return File(exportBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting company");
            return StatusCode(500, new { error = "Failed to export company", message = ex.Message });
        }
    }

    #endregion
}


