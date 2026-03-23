using CephasOps.Application.ServiceInstallers.DTOs;
using CephasOps.Application.ServiceInstallers.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Application.Common.DTOs;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Service Installers endpoints
/// </summary>
[ApiController]
[Route("api/service-installers")]
[Authorize]
public class ServiceInstallersController : ControllerBase
{
    private readonly IServiceInstallerService _serviceInstallerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ICsvService _csvService;
    private readonly ILogger<ServiceInstallersController> _logger;

    public ServiceInstallersController(
        IServiceInstallerService serviceInstallerService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ICsvService csvService,
        ILogger<ServiceInstallersController> logger)
    {
        _serviceInstallerService = serviceInstallerService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _csvService = csvService;
        _logger = logger;
    }

    /// <summary>
    /// Get all service installers for the current company
    /// </summary>
    /// <param name="departmentId">Optional department ID to filter by</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="installerType">Optional installer type filter (InHouse, Subcontractor)</param>
    /// <param name="siLevel">Optional installer level filter (Junior, Senior)</param>
    /// <param name="skillIds">Optional skill IDs filter (comma-separated) - installers must have ALL specified skills</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceInstallerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceInstallerDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ServiceInstallerDto>>>> GetServiceInstallers(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? installerType = null,
        [FromQuery] string? siLevel = null,
        [FromQuery] string? skillIds = null,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            // Parse installer type
            Domain.ServiceInstallers.Enums.InstallerType? typeFilter = null;
            if (!string.IsNullOrWhiteSpace(installerType) && 
                Enum.TryParse<Domain.ServiceInstallers.Enums.InstallerType>(installerType, true, out var parsedType))
            {
                typeFilter = parsedType;
            }

            // Parse installer level
            Domain.ServiceInstallers.Enums.InstallerLevel? levelFilter = null;
            if (!string.IsNullOrWhiteSpace(siLevel) && 
                Enum.TryParse<Domain.ServiceInstallers.Enums.InstallerLevel>(siLevel, true, out var parsedLevel))
            {
                levelFilter = parsedLevel;
            }

            // Parse skill IDs
            List<Guid>? skillIdList = null;
            if (!string.IsNullOrWhiteSpace(skillIds))
            {
                var skillIdStrings = skillIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var parsedSkillIds = new List<Guid>();
                foreach (var skillIdString in skillIdStrings)
                {
                    if (Guid.TryParse(skillIdString.Trim(), out var skillId))
                    {
                        parsedSkillIds.Add(skillId);
                    }
                }
                if (parsedSkillIds.Any())
                {
                    skillIdList = parsedSkillIds;
                }
            }

            Guid? departmentScope = null;
            try
            {
                departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return this.Error<List<ServiceInstallerDto>>("You do not have access to this department", 403);
            }

            var serviceInstallers = await _serviceInstallerService.GetServiceInstallersAsync(
                companyId, 
                departmentScope, 
                isActive, 
                typeFilter, 
                levelFilter, 
                skillIdList, 
                cancellationToken);
            return this.Success(serviceInstallers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service installers");
            return this.Error<List<ServiceInstallerDto>>($"Failed to get service installers: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get service installer by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ServiceInstallerDto>>> GetServiceInstaller(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var serviceInstaller = await _serviceInstallerService.GetServiceInstallerByIdAsync(id, companyId, cancellationToken);
            if (serviceInstaller == null)
            {
                return this.NotFound<ServiceInstallerDto>($"Service Installer with ID {id} not found");
            }

            return this.Success(serviceInstaller);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service installer: {SiId}", id);
            return this.Error<ServiceInstallerDto>($"Failed to get service installer: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new service installer
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ServiceInstallerDto>>> CreateServiceInstaller(
        [FromBody] CreateServiceInstallerDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var serviceInstaller = await _serviceInstallerService.CreateServiceInstallerAsync(dto, companyId, cancellationToken);
            return this.StatusCode(201, ApiResponse<ServiceInstallerDto>.SuccessResponse(serviceInstaller, "Service installer created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service installer");
            return this.Error<ServiceInstallerDto>($"Failed to create service installer: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update an existing service installer
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ServiceInstallerDto>>> UpdateServiceInstaller(
        Guid id,
        [FromBody] UpdateServiceInstallerDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var serviceInstaller = await _serviceInstallerService.UpdateServiceInstallerAsync(id, dto, companyId, cancellationToken);
            return this.Success(serviceInstaller, "Service installer updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ServiceInstallerDto>($"Service Installer with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service installer: {SiId}", id);
            return this.Error<ServiceInstallerDto>($"Failed to update service installer: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a service installer
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteServiceInstaller(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _serviceInstallerService.DeleteServiceInstallerAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Service installer deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Service Installer with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting service installer: {SiId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete service installer: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get backup / emergency contacts for a service installer.
    /// </summary>
    [HttpGet("{serviceInstallerId:guid}/contacts")]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceInstallerContactDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceInstallerContactDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ServiceInstallerContactDto>>>> GetContacts(
        Guid serviceInstallerId,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var contacts = await _serviceInstallerService.GetContactsAsync(serviceInstallerId, companyId, cancellationToken);
            return this.Success(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts for service installer: {SiId}", serviceInstallerId);
            return this.Error<List<ServiceInstallerContactDto>>($"Failed to get contacts: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new backup / emergency contact for a service installer.
    /// </summary>
    [HttpPost("{serviceInstallerId:guid}/contacts")]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerContactDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerContactDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerContactDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ServiceInstallerContactDto>>> CreateContact(
        Guid serviceInstallerId,
        [FromBody] CreateServiceInstallerContactDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var contact = await _serviceInstallerService.CreateContactAsync(serviceInstallerId, dto, companyId, cancellationToken);
            return this.StatusCode(201, ApiResponse<ServiceInstallerContactDto>.SuccessResponse(contact, "Contact created successfully"));
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ServiceInstallerContactDto>($"Service Installer with ID {serviceInstallerId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact for service installer: {SiId}", serviceInstallerId);
            return this.Error<ServiceInstallerContactDto>($"Failed to create contact: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update an existing contact for a service installer.
    /// </summary>
    [HttpPut("contacts/{contactId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerContactDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ServiceInstallerContactDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ServiceInstallerContactDto>>> UpdateContact(
        Guid contactId,
        [FromBody] UpdateServiceInstallerContactDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var contact = await _serviceInstallerService.UpdateContactAsync(contactId, dto, companyId, cancellationToken);
            return this.Success(contact, "Contact updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ServiceInstallerContactDto>($"Service Installer contact with ID {contactId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact {ContactId}", contactId);
            return this.Error<ServiceInstallerContactDto>($"Failed to update contact: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a contact for a service installer.
    /// </summary>
    [HttpDelete("contacts/{contactId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteContact(
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _serviceInstallerService.DeleteContactAsync(contactId, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Contact deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Service Installer contact with ID {contactId} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact {ContactId}", contactId);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete contact: {ex.Message}"));
        }
    }

    // ============================================
    // Service Installers - Import/Export
    // ============================================

    /// <summary>
    /// Export service installers to CSV
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportServiceInstallers(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("You do not have access to this department"));
        }

        try
        {
            var installers = await _serviceInstallerService.GetServiceInstallersAsync(companyId, departmentScope, isActive, installerType: null, siLevel: null, skillIds: null, cancellationToken);
            
            var csvData = installers.Select(si => new ServiceInstallerCsvDto
            {
                Name = si.Name,
                Code = si.EmployeeId ?? string.Empty,
                DepartmentName = si.DepartmentName ?? string.Empty,
                Phone = si.Phone ?? string.Empty,
                Email = si.Email ?? string.Empty,
                Level = si.SiLevel.ToString(),
                IcNumber = si.IcNumber ?? string.Empty,
                BankName = si.BankName ?? string.Empty,
                BankAccountNumber = si.BankAccountNumber ?? string.Empty,
                Address = si.Address ?? string.Empty,
                EmergencyContact = si.EmergencyContact ?? string.Empty,
                IsActive = si.IsActive
            }).ToList();

            var csvBytes = _csvService.ExportToCsvBytes(csvData);
            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(csvBytes, "text/csv", $"service-installers-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting service installers");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to export service installers: {ex.Message}"));
        }
    }

    /// <summary>
    /// Download service installers CSV template
    /// </summary>
    [HttpGet("template")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult GetServiceInstallersTemplate()
    {
        try
        {
            var templateBytes = _csvService.GenerateTemplateBytes<ServiceInstallerCsvDto>();
            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(templateBytes, "text/csv", "service-installers-template.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating service installers template");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to generate template: {ex.Message}"));
        }
    }

    // CSV Import feature not yet implemented
    // /// <summary>
    // /// Import service installers from CSV
    // /// </summary>
    // [HttpPost("import")]
    // [ProducesResponseType(typeof(ImportResult<ServiceInstallerCsvDto>), StatusCodes.Status200OK)]
    // public async Task<ActionResult<ImportResult<ServiceInstallerCsvDto>>> ImportServiceInstallers(
    //     IFormFile file,
    //     CancellationToken cancellationToken = default)
    // {
    //     ...
    // }
}

