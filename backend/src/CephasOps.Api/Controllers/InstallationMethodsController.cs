using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// InstallationMethod management endpoints
/// </summary>
[ApiController]
[Route("api/installation-methods")]
[Authorize]
public class InstallationMethodsController : ControllerBase
{
    private readonly IInstallationMethodService _installationMethodService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<InstallationMethodsController> _logger;

    public InstallationMethodsController(
        IInstallationMethodService installationMethodService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<InstallationMethodsController> logger)
    {
        _installationMethodService = installationMethodService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get installation methods list
    /// </summary>
    /// <param name="departmentId">Optional department ID to filter by</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<InstallationMethodDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<InstallationMethodDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<InstallationMethodDto>>>> GetInstallationMethods(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<InstallationMethodDto>>("You do not have access to this department", 403);
        }

        try
        {
            var installationMethods = await _installationMethodService.GetInstallationMethodsAsync(companyId, departmentScope, category, isActive, cancellationToken);
            return this.Success(installationMethods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installation methods");
            return this.InternalServerError<List<InstallationMethodDto>>($"Failed to get installation methods: {ex.Message}");
        }
    }

    /// <summary>
    /// Get installation method by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<InstallationMethodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InstallationMethodDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<InstallationMethodDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<InstallationMethodDto>>> GetInstallationMethod(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var installationMethod = await _installationMethodService.GetInstallationMethodByIdAsync(id, companyId, cancellationToken);
            if (installationMethod == null)
            {
                return this.NotFound<InstallationMethodDto>($"InstallationMethod with ID {id} not found");
            }
            return this.Success(installationMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installation method {InstallationMethodId}", id);
            return this.InternalServerError<InstallationMethodDto>($"Failed to get installation method: {ex.Message}");
        }
    }

    /// <summary>
    /// Create new installation method
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InstallationMethodDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<InstallationMethodDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InstallationMethodDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<InstallationMethodDto>>> CreateInstallationMethod(
        [FromBody] CreateInstallationMethodDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var installationMethod = await _installationMethodService.CreateInstallationMethodAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetInstallationMethod), new { id = installationMethod.Id }, installationMethod, "Installation method created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Duplicate installation method detected");
            return this.BadRequest<InstallationMethodDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating installation method");
            return this.InternalServerError<InstallationMethodDto>($"Failed to create installation method: {ex.Message}");
        }
    }

    /// <summary>
    /// Update installation method
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<InstallationMethodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InstallationMethodDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<InstallationMethodDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InstallationMethodDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<InstallationMethodDto>>> UpdateInstallationMethod(
        Guid id,
        [FromBody] UpdateInstallationMethodDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var installationMethod = await _installationMethodService.UpdateInstallationMethodAsync(id, dto, companyId, cancellationToken);
            return this.Success(installationMethod, "Installation method updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<InstallationMethodDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Duplicate installation method detected during update");
            return this.BadRequest<InstallationMethodDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating installation method {InstallationMethodId}", id);
            return this.InternalServerError<InstallationMethodDto>($"Failed to update installation method: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete installation method
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteInstallationMethod(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _installationMethodService.DeleteInstallationMethodAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting installation method {InstallationMethodId}", id);
            return this.InternalServerError($"Failed to delete installation method: {ex.Message}");
        }
    }
}
