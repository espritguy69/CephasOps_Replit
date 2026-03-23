using CephasOps.Application.Departments.DTOs;
using CephasOps.Application.Departments.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Department management endpoints
/// </summary>
[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly IDepartmentDeploymentService _deploymentService;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(
        IDepartmentService departmentService,
        IDepartmentDeploymentService deploymentService,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<DepartmentsController> logger)
    {
        _departmentService = departmentService;
        _deploymentService = deploymentService;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get departments list
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetDepartments(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId is always null
        var companyId = (Guid?)null;

        try
        {
            var departments = await _departmentService.GetDepartmentsAsync(companyId, isActive, cancellationToken);
            return this.Success(departments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting departments");
            return this.Error<List<DepartmentDto>>($"Failed to get departments: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get department by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetDepartment(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var department = await _departmentService.GetDepartmentByIdAsync(id, companyId, cancellationToken);
            if (department == null)
            {
                return this.NotFound<DepartmentDto>($"Department with ID {id} not found");
            }

            return this.Success(department);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department: {DepartmentId}", id);
            return this.Error<DepartmentDto>($"Failed to get department: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create department
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment(
        [FromBody] CreateDepartmentDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return this.Error<DepartmentDto>(errors, "Validation failed", 400);
        }

        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var department = await _departmentService.CreateDepartmentAsync(dto, companyId, cancellationToken);
            return this.StatusCode(201, ApiResponse<DepartmentDto>.SuccessResponse(department, "Department created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department");
            return this.Error<DepartmentDto>($"Failed to create department: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update department
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> UpdateDepartment(
        Guid id,
        [FromBody] UpdateDepartmentDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var department = await _departmentService.UpdateDepartmentAsync(id, dto, companyId, cancellationToken);
            return this.Success(department, "Department updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<DepartmentDto>($"Department with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department: {DepartmentId}", id);
            return this.Error<DepartmentDto>($"Failed to update department: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete department
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteDepartment(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _departmentService.DeleteDepartmentAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Department deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Department with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department: {DepartmentId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete department: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get material allocations for a department
    /// </summary>
    [HttpGet("{id}/material-allocations")]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialAllocationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialAllocationDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<MaterialAllocationDto>>>> GetMaterialAllocations(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var allocations = await _departmentService.GetMaterialAllocationsAsync(id, companyId, cancellationToken);
            return this.Success(allocations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting material allocations for department: {DepartmentId}", id);
            return this.Error<List<MaterialAllocationDto>>($"Failed to get material allocations: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create material allocation
    /// </summary>
    [HttpPost("{id}/material-allocations")]
    [ProducesResponseType(typeof(ApiResponse<MaterialAllocationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<MaterialAllocationDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MaterialAllocationDto>>> CreateMaterialAllocation(
        Guid id,
        [FromBody] CreateMaterialAllocationDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var allocation = await _departmentService.CreateMaterialAllocationAsync(id, dto, companyId, cancellationToken);
            return this.StatusCode(201, ApiResponse<MaterialAllocationDto>.SuccessResponse(allocation, "Material allocation created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating material allocation");
            return this.Error<MaterialAllocationDto>($"Failed to create material allocation: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete material allocation
    /// </summary>
    [HttpDelete("material-allocations/{allocationId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteMaterialAllocation(
        Guid allocationId,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _departmentService.DeleteMaterialAllocationAsync(allocationId, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Material allocation deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Material allocation with ID {allocationId} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting material allocation: {AllocationId}", allocationId);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete material allocation: {ex.Message}"));
        }
    }

    #region Department Deployment

    /// <summary>
    /// Get available department deployment configurations
    /// </summary>
    [HttpGet("deployment/configurations")]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentDeploymentConfig>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentDeploymentConfig>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<DepartmentDeploymentConfig>>>> GetDeploymentConfigurations(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var configs = await _deploymentService.GetAvailableConfigurationsAsync(cancellationToken);
            return this.Success(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deployment configurations");
            return this.Error<List<DepartmentDeploymentConfig>>($"Failed to get configurations: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get deployment configuration for a specific department
    /// </summary>
    [HttpGet("deployment/configurations/{departmentCode}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDeploymentConfig>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDeploymentConfig>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDeploymentConfig>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DepartmentDeploymentConfig>>> GetDeploymentConfiguration(
        string departmentCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _deploymentService.GetConfigurationAsync(departmentCode, cancellationToken);
            if (config == null)
            {
                return this.NotFound<DepartmentDeploymentConfig>($"Configuration for department {departmentCode} not found");
            }
            return this.Success(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deployment configuration for {DepartmentCode}", departmentCode);
            return this.Error<DepartmentDeploymentConfig>($"Failed to get configuration: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Download department deployment template
    /// </summary>
    [HttpGet("deployment/template")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeploymentTemplate(
        [FromQuery] string departmentCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
            {
                return StatusCode(400, ApiResponse.ErrorResponse("Department code is required"));
            }

            var templateBytes = await _deploymentService.GenerateTemplateAsync(departmentCode, cancellationToken);
            var fileName = $"{departmentCode.ToLowerInvariant()}-deployment-template.xlsx";

            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (ArgumentException ex)
        {
            return StatusCode(400, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating deployment template for {DepartmentCode}", departmentCode);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to generate template: {ex.Message}"));
        }
    }

    /// <summary>
    /// Validate department deployment files (dry-run)
    /// </summary>
    [HttpPost("deployment/validate")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDeploymentValidationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDeploymentValidationResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDeploymentValidationResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DepartmentDeploymentValidationResult>>> ValidateDeployment(
        IFormFileCollection files,
        [FromQuery] string departmentCode,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            return this.Error<DepartmentDeploymentValidationResult>("No files uploaded", 400);
        }

        if (string.IsNullOrWhiteSpace(departmentCode))
        {
            return this.Error<DepartmentDeploymentValidationResult>("Department code is required", 400);
        }

        try
        {
            var result = await _deploymentService.ValidateDeploymentAsync(files, departmentCode, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating deployment for {DepartmentCode}", departmentCode);
            return this.Error<DepartmentDeploymentValidationResult>($"Failed to validate deployment: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Import department deployment from Excel files
    /// </summary>
    [HttpPost("deployment/import")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDeploymentImportResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDeploymentImportResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDeploymentImportResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DepartmentDeploymentImportResult>>> ImportDeployment(
        IFormFileCollection files,
        [FromQuery] DepartmentDeploymentImportOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            return this.Error<DepartmentDeploymentImportResult>("No files uploaded", 400);
        }

        if (options == null || string.IsNullOrWhiteSpace(options.DepartmentCode))
        {
            return this.Error<DepartmentDeploymentImportResult>("Department code is required", 400);
        }

        try
        {
            var result = await _deploymentService.ImportDeploymentAsync(files, options, cancellationToken);
            return this.Success(result, "Deployment imported successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing deployment for {DepartmentCode}", options.DepartmentCode);
            return this.Error<DepartmentDeploymentImportResult>($"Failed to import deployment: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Export existing department data to Excel
    /// </summary>
    [HttpGet("deployment/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDepartmentData(
        [FromQuery] string departmentCode,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
            {
                return StatusCode(400, ApiResponse.ErrorResponse("Department code is required"));
            }

            Guid? resolvedDepartmentId;
            try
            {
                resolvedDepartmentId = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
            }

            var exportBytes = await _deploymentService.ExportDepartmentDataAsync(departmentCode, resolvedDepartmentId, cancellationToken);
            var fileName = $"{departmentCode.ToLowerInvariant()}-export-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";

            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(exportBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting department data for {DepartmentCode}", departmentCode);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to export department data: {ex.Message}"));
        }
    }

    #endregion
}

