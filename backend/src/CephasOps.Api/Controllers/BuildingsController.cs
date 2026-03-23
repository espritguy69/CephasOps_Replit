using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Common.DTOs;
using CephasOps.Api.Common;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Buildings endpoints
/// </summary>
[ApiController]
[Route("api/buildings")]
[Authorize]
public class BuildingsController : ControllerBase
{
    private readonly IBuildingService _buildingService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICsvService _csvService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BuildingsController> _logger;

    public BuildingsController(
        IBuildingService buildingService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ICsvService csvService,
        ApplicationDbContext context,
        ILogger<BuildingsController> logger)
    {
        _buildingService = buildingService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _csvService = csvService;
        _context = context;
        _logger = logger;
    }

    #region Buildings

    /// <summary>
    /// Get all buildings with optional filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingListItemDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BuildingListItemDto>>>> GetBuildings(
        [FromQuery] string? propertyType = null,
        [FromQuery] Guid? installationMethodId = null,
        [FromQuery] string? state = null,
        [FromQuery] string? city = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var buildings = await _buildingService.GetBuildingsAsync(
                companyId, propertyType, installationMethodId, state, city, isActive, cancellationToken);
            return this.Success(buildings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting buildings");
            return this.Error<List<BuildingListItemDto>>($"Failed to get buildings: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get buildings summary for dashboard
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<BuildingsSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingsSummaryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingsSummaryDto>>> GetBuildingsSummary(
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var summary = await _buildingService.GetBuildingsSummaryAsync(companyId, cancellationToken);
            return this.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting buildings summary");
            return this.Error<BuildingsSummaryDto>($"Failed to get buildings summary: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get similar buildings that could be merge targets (for admin merge tool).
    /// </summary>
    [HttpGet("merge-candidates")]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingListItemDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<BuildingListItemDto>>>> GetMergeCandidates(
        [FromQuery] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        try
        {
            var list = await _buildingService.GetMergeCandidatesAsync(buildingId, companyId, cancellationToken);
            return this.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting merge candidates for building {BuildingId}", buildingId);
            return this.Error<List<BuildingListItemDto>>($"Failed to get merge candidates: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Preview a building merge: how many orders would move from source to target.
    /// </summary>
    [HttpGet("merge-preview")]
    [ProducesResponseType(typeof(ApiResponse<BuildingMergePreviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingMergePreviewDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BuildingMergePreviewDto>>> GetMergePreview(
        [FromQuery] Guid sourceBuildingId,
        [FromQuery] Guid targetBuildingId,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var preview = await _buildingService.GetMergePreviewAsync(sourceBuildingId, targetBuildingId, companyId, cancellationToken);
        if (preview == null)
            return this.NotFound<BuildingMergePreviewDto>("One or both buildings not found, or source equals target.");
        return this.Success(preview);
    }

    /// <summary>
    /// Merge source building into target: reassign all orders to target, deactivate source.
    /// </summary>
    [HttpPost("merge")]
    [ProducesResponseType(typeof(ApiResponse<BuildingMergeResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingMergeResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BuildingMergeResultDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BuildingMergeResultDto>>> MergeBuildings(
        [FromBody] MergeBuildingsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if (userId == null)
            return this.Unauthorized<BuildingMergeResultDto>("User context required");
        try
        {
            var result = await _buildingService.MergeBuildingsAsync(
                request.SourceBuildingId, request.TargetBuildingId, userId.Value, companyId, cancellationToken);
            return this.Success(result, result.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingMergeResultDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<BuildingMergeResultDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging buildings {Source} into {Target}",
                request.SourceBuildingId, request.TargetBuildingId);
            return this.Error<BuildingMergeResultDto>($"Failed to merge buildings: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get building by ID (includes contacts and rules)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> GetBuilding(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var building = await _buildingService.GetBuildingByIdAsync(id, companyId, cancellationToken);
            if (building == null)
            {
                return this.NotFound<BuildingDetailDto>($"Building with ID {id} not found");
            }

            return this.Success(building);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting building: {BuildingId}", id);
            return this.Error<BuildingDetailDto>($"Failed to get building: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new building
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BuildingDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingDto>>> CreateBuilding(
        [FromBody] CreateBuildingDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = dto.CompanyId ?? _tenantProvider.CurrentTenantId;

        try
        {
            var building = await _buildingService.CreateBuildingAsync(dto, companyId, cancellationToken);
            return this.StatusCode(201, ApiResponse<BuildingDto>.SuccessResponse(building, "Building created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating building");
            
            // Extract inner exception message if available (for database constraint violations)
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage = $"{ex.Message} - {ex.InnerException.Message}";
            }
            
            return this.Error<BuildingDto>($"Failed to create building: {errorMessage}", 500);
        }
    }

    /// <summary>
    /// Update an existing building
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingDto>>> UpdateBuilding(
        Guid id,
        [FromBody] UpdateBuildingDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var building = await _buildingService.UpdateBuildingAsync(id, dto, companyId, cancellationToken);
            return this.Success(building, "Building updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<BuildingDto>($"Building with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating building: {BuildingId}", id);
            return this.Error<BuildingDto>($"Failed to update building: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a building
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteBuilding(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _buildingService.DeleteBuildingAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Building deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Building with ID {id} not found"));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(400, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting building: {BuildingId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete building: {ex.Message}"));
        }
    }

    #endregion

    #region Building Contacts

    /// <summary>
    /// Get all contacts for a building
    /// </summary>
    [HttpGet("{buildingId}/contacts")]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingContactDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingContactDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingContactDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BuildingContactDto>>>> GetBuildingContacts(
        Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var contacts = await _buildingService.GetBuildingContactsAsync(buildingId, companyId, cancellationToken);
            return this.Success(contacts);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<List<BuildingContactDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts for building: {BuildingId}", buildingId);
            return this.Error<List<BuildingContactDto>>($"Failed to get contacts: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new contact for a building
    /// </summary>
    [HttpPost("{buildingId}/contacts")]
    [ProducesResponseType(typeof(ApiResponse<BuildingContactDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BuildingContactDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingContactDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingContactDto>>> CreateBuildingContact(
        Guid buildingId,
        [FromBody] SaveBuildingContactDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var contact = await _buildingService.CreateBuildingContactAsync(buildingId, dto, companyId, cancellationToken);
            return this.StatusCode(201, ApiResponse<BuildingContactDto>.SuccessResponse(contact, "Contact created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingContactDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact for building: {BuildingId}", buildingId);
            return this.Error<BuildingContactDto>($"Failed to create contact: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update a building contact
    /// </summary>
    [HttpPut("{buildingId}/contacts/{contactId}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingContactDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingContactDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingContactDto>>> UpdateBuildingContact(
        Guid buildingId,
        Guid contactId,
        [FromBody] SaveBuildingContactDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var contact = await _buildingService.UpdateBuildingContactAsync(buildingId, contactId, dto, companyId, cancellationToken);
            return this.Success(contact, "Contact updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingContactDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact: {ContactId}", contactId);
            return this.Error<BuildingContactDto>($"Failed to update contact: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a building contact
    /// </summary>
    [HttpDelete("{buildingId}/contacts/{contactId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteBuildingContact(
        Guid buildingId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _buildingService.DeleteBuildingContactAsync(buildingId, contactId, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Contact deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact: {ContactId}", contactId);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete contact: {ex.Message}"));
        }
    }

    #endregion

    #region Building Rules

    /// <summary>
    /// Get building rules
    /// </summary>
    [HttpGet("{buildingId:guid}/rules")]
    [ProducesResponseType(typeof(ApiResponse<BuildingRulesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingRulesDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingRulesDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingRulesDto>>> GetBuildingRules(
        [FromRoute] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var rules = await _buildingService.GetBuildingRulesAsync(buildingId, companyId, cancellationToken);
            if (rules == null)
            {
                // Return empty rules object instead of 404
                return this.Success(new BuildingRulesDto { BuildingId = buildingId });
            }
            return this.Success(rules);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingRulesDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rules for building: {BuildingId}", buildingId);
            return this.Error<BuildingRulesDto>($"Failed to get rules: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Save building rules (create or update)
    /// </summary>
    [HttpPut("{buildingId:guid}/rules")]
    [ProducesResponseType(typeof(ApiResponse<BuildingRulesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingRulesDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingRulesDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingRulesDto>>> SaveBuildingRules(
        [FromRoute] Guid buildingId,
        [FromBody] SaveBuildingRulesDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var rules = await _buildingService.SaveBuildingRulesAsync(buildingId, dto, companyId, cancellationToken);
            return this.Success(rules, "Building rules saved successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingRulesDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving rules for building: {BuildingId}", buildingId);
            return this.Error<BuildingRulesDto>($"Failed to save rules: {ex.Message}", 500);
        }
    }

    #endregion

    #region Import/Export

    /// <summary>
    /// Export buildings to CSV
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportBuildings(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var buildings = await _buildingService.GetBuildingsAsync(companyId, null, null, null, null, isActive, cancellationToken);
            
            var csvData = buildings.Select(b => new BuildingCsvDto
            {
                Name = b.Name ?? string.Empty,
                Code = b.Code ?? string.Empty,
                PropertyType = b.PropertyType ?? string.Empty,
                InstallationMethodName = b.InstallationMethodName ?? string.Empty,
                DepartmentName = string.Empty,
                AddressLine1 = string.Empty,
                AddressLine2 = string.Empty,
                City = b.City ?? string.Empty,
                State = b.State ?? string.Empty,
                Postcode = string.Empty,
                Area = b.Area ?? string.Empty,
                Notes = string.Empty,
                IsActive = b.IsActive
            }).ToList();

            var csvBytes = _csvService.ExportToCsvBytes(csvData);
            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(csvBytes, "text/csv", $"buildings-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting buildings");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to export buildings: {ex.Message}"));
        }
    }

    /// <summary>
    /// Download buildings CSV template
    /// </summary>
    [HttpGet("template")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult GetBuildingsTemplate()
    {
        try
        {
            var templateBytes = _csvService.GenerateTemplateBytes<BuildingCsvDto>();
            // File downloads don't use ApiResponse envelope - they return file content directly
            return File(templateBytes, "text/csv", "buildings-template.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating buildings template");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to generate template: {ex.Message}"));
        }
    }

    /// <summary>
    /// Import buildings from CSV
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<BuildingCsvDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<BuildingCsvDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ImportResult<BuildingCsvDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ImportResult<BuildingCsvDto>>>> ImportBuildings(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        if (file == null || file.Length == 0)
        {
            return this.Error<ImportResult<BuildingCsvDto>>("No file uploaded", 400);
        }

        try
        {
            using var stream = file.OpenReadStream();
            var records = _csvService.ImportFromCsv<BuildingCsvDto>(stream);

            var result = new ImportResult<BuildingCsvDto>
            {
                TotalRows = records.Count
            };

            var (effectiveCompanyId, importErr) = this.RequireCompanyId(_tenantProvider);
            if (importErr != null) return importErr;
            var userId = _currentUserService.UserId ?? Guid.Empty;

            // Pre-load reference data for lookups
            var departments = await _context.Departments
                .Where(d => d.CompanyId == effectiveCompanyId)
                .ToListAsync(cancellationToken);
            var departmentLookup = departments.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

            var buildingTypes = await _context.BuildingTypes
                .Where(bt => bt.CompanyId == effectiveCompanyId)
                .ToListAsync(cancellationToken);
            var buildingTypeLookup = buildingTypes.ToDictionary(bt => bt.Name, StringComparer.OrdinalIgnoreCase);

            var installationMethods = await _context.InstallationMethods
                .Where(im => im.CompanyId == effectiveCompanyId)
                .ToListAsync(cancellationToken);
            var installationMethodLookup = installationMethods.ToDictionary(im => im.Name, StringComparer.OrdinalIgnoreCase);
            // Map common CSV/import aliases to seeded InstallationMethod records (seeds use e.g. "Non-prelaid (MDU / old building)", "SDU / RDF Pole")
            foreach (var im in installationMethods)
            {
                if (string.Equals(im.Code, "NON_PRELAID", StringComparison.OrdinalIgnoreCase))
                {
                    installationMethodLookup["Non-Prelaid"] = im;
                    installationMethodLookup["Non Prelaid"] = im;
                }
                else if (string.Equals(im.Code, "SDU_RDF", StringComparison.OrdinalIgnoreCase))
                {
                    installationMethodLookup["SDU"] = im;
                    installationMethodLookup["RDF POLE"] = im;
                    installationMethodLookup["RDF Pole"] = im;
                }
            }

            foreach (var (record, index) in records.Select((r, i) => (r, i + 2)))
            {
                try
                {
                    // Resolve department by name
                    Guid? departmentId = null;
                    if (!string.IsNullOrWhiteSpace(record.DepartmentName))
                    {
                        if (departmentLookup.TryGetValue(record.DepartmentName.Trim(), out var department))
                        {
                            departmentId = department.Id;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Department '{record.DepartmentName}' not found");
                        }
                    }

                    // Resolve building type by name (or use PropertyType as fallback)
                    Guid? buildingTypeId = null;
                    if (!string.IsNullOrWhiteSpace(record.PropertyType))
                    {
                        if (buildingTypeLookup.TryGetValue(record.PropertyType.Trim(), out var buildingType))
                        {
                            buildingTypeId = buildingType.Id;
                        }
                        // If not found by name, try to find by code or create a warning
                        else
                        {
                            _logger.LogWarning("Building type '{PropertyType}' not found for building '{Name}'. Skipping building type assignment.", 
                                record.PropertyType, record.Name);
                        }
                    }

                    // Resolve installation method by name
                    Guid? installationMethodId = null;
                    if (!string.IsNullOrWhiteSpace(record.InstallationMethodName))
                    {
                        if (installationMethodLookup.TryGetValue(record.InstallationMethodName.Trim(), out var installationMethod))
                        {
                            installationMethodId = installationMethod.Id;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Installation method '{record.InstallationMethodName}' not found");
                        }
                    }

                    // Truncate CSV values to Building entity max lengths to avoid "value too long for type character varying(N)" (e.g. column shift in CSV)
                    static string Truncate(string? value, int maxLength)
                    {
                        var s = (value ?? string.Empty).Trim();
                        return s.Length <= maxLength ? s : s[..maxLength];
                    }
                    // Malaysian postcodes are 5 characters (e.g. 47000, 50000)
                    const int postcodeMaxLength = 5;
                    var createDto = new CreateBuildingDto
                    {
                        CompanyId = companyId != Guid.Empty ? companyId : null,
                        DepartmentId = departmentId,
                        Name = Truncate(record.Name, 500),
                        Code = string.IsNullOrWhiteSpace(record.Code) ? null : Truncate(record.Code, 100),
                        AddressLine1 = Truncate(record.AddressLine1, 500),
                        AddressLine2 = string.IsNullOrWhiteSpace(record.AddressLine2) ? null : Truncate(record.AddressLine2, 500),
                        City = Truncate(record.City, 100),
                        State = Truncate(record.State, 100),
                        Postcode = Truncate(record.Postcode, postcodeMaxLength),
                        Area = string.IsNullOrWhiteSpace(record.Area) ? null : Truncate(record.Area, 500),
                        BuildingTypeId = buildingTypeId,
                        InstallationMethodId = installationMethodId,
                        PropertyType = string.IsNullOrWhiteSpace(record.PropertyType) ? null : record.PropertyType.Trim(),
                        Notes = string.IsNullOrWhiteSpace(record.Notes) ? null : record.Notes.Trim(),
                        IsActive = record.IsActive ?? true
                    };

                    // Create building (idempotent: skip duplicates)
                    await _buildingService.CreateBuildingAsync(createDto, companyId != Guid.Empty ? companyId : null, cancellationToken);
                    
                    result.SuccessCount++;
                    result.ImportedRecords.Add(record);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = index,
                        Message = "Skipped (duplicate): " + ex.Message
                    });
                    _logger.LogInformation("Skipped duplicate building at row {RowNumber}: {BuildingName}", index, record.Name);
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = index,
                        Message = GetDiagnosticMessage(ex)
                    });
                    _logger.LogWarning(ex, "Error importing building at row {RowNumber}: {BuildingName}", index, record.Name);
                }
            }

            return this.Success(result, "Buildings imported successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing buildings");
            return this.Error<ImportResult<BuildingCsvDto>>($"Failed to import buildings: {GetDiagnosticMessage(ex)}", 500);
        }
    }

    /// <summary>
    /// Unwraps exception chains (e.g. DbUpdateException -> PostgresException) so the import result
    /// shows the real database or validation error instead of a generic "see inner exception" message.
    /// </summary>
    private static string GetDiagnosticMessage(Exception ex)
    {
        if (ex == null) return string.Empty;
        var current = ex;
        Exception? innermost = ex;
        while (current?.InnerException != null)
        {
            innermost = current.InnerException;
            current = current.InnerException;
        }
        // Prefer innermost message when it is more specific than the outer (e.g. EF generic wrapper)
        if (innermost != null && innermost != ex && !string.IsNullOrWhiteSpace(innermost.Message))
        {
            var outerGeneric = ex.Message.Contains("See the inner exception", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("An error occurred while saving", StringComparison.OrdinalIgnoreCase);
            if (outerGeneric) return innermost.Message;
        }
        return ex.Message;
    }

    #endregion
}
