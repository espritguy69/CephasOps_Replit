using CephasOps.Application.Assets.DTOs;
using CephasOps.Application.Assets.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using CephasOps.Api.Common;
using CephasOps.Domain.Assets.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Asset management endpoints
/// </summary>
[ApiController]
[Route("api/assets")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly IDepreciationService _depreciationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        IAssetService assetService,
        IDepreciationService depreciationService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<AssetsController> logger)
    {
        _assetService = assetService;
        _depreciationService = depreciationService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get assets list
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AssetDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AssetDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<AssetDto>>>> GetAssets(
        [FromQuery] Guid? assetTypeId = null,
        [FromQuery] AssetStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var assets = await _assetService.GetAssetsAsync(companyId, assetTypeId, status, search, cancellationToken);
            return this.Success(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assets");
            return this.Error<List<AssetDto>>($"Failed to get assets: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get asset summary for dashboard
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<AssetSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetSummaryDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetSummaryDto>>> GetAssetSummary(CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var summary = await _assetService.GetAssetSummaryAsync(companyId, cancellationToken);
            return this.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset summary");
            return this.Error<AssetSummaryDto>($"Failed to get asset summary: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get asset by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AssetDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetDto>>> GetAsset(Guid id, CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var asset = await _assetService.GetAssetByIdAsync(id, companyId, cancellationToken);
            if (asset == null)
            {
                return this.NotFound<AssetDto>($"Asset with ID {id} not found");
            }
            return this.Success(asset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting asset {AssetId}", id);
            return this.Error<AssetDto>($"Failed to get asset: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create new asset
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AssetDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AssetDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AssetDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetDto>>> CreateAsset(
        [FromBody] CreateAssetDto dto,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var asset = await _assetService.CreateAssetAsync(dto, companyId, cancellationToken);
            return this.StatusCode(201, ApiResponse<AssetDto>.SuccessResponse(asset, "Asset created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<AssetDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset");
            return this.Error<AssetDto>($"Failed to create asset: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update asset
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AssetDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AssetDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetDto>>> UpdateAsset(
        Guid id,
        [FromBody] UpdateAssetDto dto,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var asset = await _assetService.UpdateAssetAsync(id, dto, companyId, cancellationToken);
            return this.Success(asset, "Asset updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<AssetDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<AssetDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset {AssetId}", id);
            return this.Error<AssetDto>($"Failed to update asset: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete asset
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteAsset(Guid id, CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _assetService.DeleteAssetAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Asset deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset {AssetId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete asset: {ex.Message}"));
        }
    }

    // Maintenance endpoints

    /// <summary>
    /// Get maintenance records
    /// </summary>
    [HttpGet("maintenance")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetMaintenanceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AssetMaintenanceDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<AssetMaintenanceDto>>>> GetMaintenanceRecords(
        [FromQuery] Guid? assetId = null,
        [FromQuery] bool? completed = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (departmentId.HasValue || _departmentRequestContext.DepartmentId.HasValue)
        {
            try
            {
                await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return this.Error<List<AssetMaintenanceDto>>("You do not have access to this department", 403);
            }
        }

        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var records = await _assetService.GetMaintenanceRecordsAsync(companyId, assetId, completed, cancellationToken);
            return this.Success(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance records");
            return this.Error<List<AssetMaintenanceDto>>($"Failed to get maintenance records: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get upcoming maintenance
    /// </summary>
    [HttpGet("maintenance/upcoming")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetMaintenanceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AssetMaintenanceDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<AssetMaintenanceDto>>>> GetUpcomingMaintenance(
        [FromQuery] int daysAhead = 30,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (departmentId.HasValue || _departmentRequestContext.DepartmentId.HasValue)
        {
            try
            {
                await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return this.Error<List<AssetMaintenanceDto>>("You do not have access to this department", 403);
            }
        }

        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var records = await _assetService.GetUpcomingMaintenanceAsync(companyId, daysAhead, cancellationToken);
            return this.Success(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming maintenance");
            return this.Error<List<AssetMaintenanceDto>>($"Failed to get upcoming maintenance: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create maintenance record
    /// </summary>
    [HttpPost("maintenance")]
    [ProducesResponseType(typeof(ApiResponse<AssetMaintenanceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AssetMaintenanceDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AssetMaintenanceDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetMaintenanceDto>>> CreateMaintenanceRecord(
        [FromBody] CreateAssetMaintenanceDto dto,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId ?? Guid.Empty;

        try
        {
            var record = await _assetService.CreateMaintenanceRecordAsync(dto, companyId, userId, cancellationToken);
            return this.StatusCode(201, ApiResponse<AssetMaintenanceDto>.SuccessResponse(record, "Maintenance record created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<AssetMaintenanceDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance record");
            return this.Error<AssetMaintenanceDto>($"Failed to create maintenance record: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update maintenance record
    /// </summary>
    [HttpPut("maintenance/{id}")]
    [ProducesResponseType(typeof(ApiResponse<AssetMaintenanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetMaintenanceDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AssetMaintenanceDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetMaintenanceDto>>> UpdateMaintenanceRecord(
        Guid id,
        [FromBody] UpdateAssetMaintenanceDto dto,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var record = await _assetService.UpdateMaintenanceRecordAsync(id, dto, companyId, cancellationToken);
            return this.Success(record, "Maintenance record updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<AssetMaintenanceDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maintenance record {MaintenanceId}", id);
            return this.Error<AssetMaintenanceDto>($"Failed to update maintenance record: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete maintenance record
    /// </summary>
    [HttpDelete("maintenance/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteMaintenanceRecord(Guid id, CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _assetService.DeleteMaintenanceRecordAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Maintenance record deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting maintenance record {MaintenanceId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete maintenance record: {ex.Message}"));
        }
    }

    // Depreciation endpoints

    /// <summary>
    /// Get depreciation entries
    /// </summary>
    [HttpGet("depreciation")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetDepreciationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AssetDepreciationDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<AssetDepreciationDto>>>> GetDepreciationEntries(
        [FromQuery] string? period = null,
        [FromQuery] Guid? assetId = null,
        [FromQuery] bool? isPosted = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (departmentId.HasValue || _departmentRequestContext.DepartmentId.HasValue)
        {
            try
            {
                await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return this.Error<List<AssetDepreciationDto>>("You do not have access to this department", 403);
            }
        }

        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var entries = await _depreciationService.GetDepreciationEntriesAsync(companyId, period, assetId, isPosted, cancellationToken);
            return this.Success(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting depreciation entries");
            return this.Error<List<AssetDepreciationDto>>($"Failed to get depreciation entries: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get depreciation schedule for an asset
    /// </summary>
    [HttpGet("{id}/depreciation-schedule")]
    [ProducesResponseType(typeof(ApiResponse<DepreciationScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DepreciationScheduleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DepreciationScheduleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DepreciationScheduleDto>>> GetDepreciationSchedule(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var schedule = await _depreciationService.GetDepreciationScheduleAsync(id, companyId, cancellationToken);
            return this.Success(schedule);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<DepreciationScheduleDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting depreciation schedule for asset {AssetId}", id);
            return this.Error<DepreciationScheduleDto>($"Failed to get depreciation schedule: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Run depreciation for a period
    /// </summary>
    [HttpPost("depreciation/run")]
    [ProducesResponseType(typeof(ApiResponse<DepreciationRunResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DepreciationRunResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DepreciationRunResultDto>>> RunDepreciation(
        [FromBody] RunDepreciationDto dto,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var result = await _depreciationService.RunDepreciationAsync(dto, companyId, cancellationToken);
            return this.Success(result, "Depreciation run completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running depreciation for period {Period}", dto.Period);
            return this.Error<DepreciationRunResultDto>($"Failed to run depreciation: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Post depreciation entries for a period
    /// </summary>
    [HttpPost("depreciation/post")]
    [ProducesResponseType(typeof(ApiResponse<PostDepreciationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PostDepreciationResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PostDepreciationResultDto>>> PostDepreciation(
        [FromQuery] string period,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var count = await _depreciationService.PostDepreciationEntriesAsync(companyId, period, cancellationToken);
            return this.Success(new PostDepreciationResultDto { Count = count, Message = $"Posted {count} depreciation entries" }, "Depreciation entries posted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting depreciation for period {Period}", period);
            return this.Error<PostDepreciationResultDto>($"Failed to post depreciation: {ex.Message}", 500);
        }
    }

    // Disposal endpoints

    /// <summary>
    /// Get disposals
    /// </summary>
    [HttpGet("disposals")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetDisposalDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AssetDisposalDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<AssetDisposalDto>>>> GetDisposals(
        [FromQuery] bool? approved = null,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var disposals = await _assetService.GetDisposalsAsync(companyId, approved, cancellationToken);
            return this.Success(disposals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting disposals");
            return this.Error<List<AssetDisposalDto>>($"Failed to get disposals: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create disposal
    /// </summary>
    [HttpPost("disposals")]
    [ProducesResponseType(typeof(ApiResponse<AssetDisposalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AssetDisposalDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AssetDisposalDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetDisposalDto>>> CreateDisposal(
        [FromBody] CreateAssetDisposalDto dto,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId ?? Guid.Empty;

        try
        {
            var disposal = await _assetService.CreateDisposalAsync(dto, companyId, userId, cancellationToken);
            return this.StatusCode(201, ApiResponse<AssetDisposalDto>.SuccessResponse(disposal, "Disposal created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<AssetDisposalDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating disposal");
            return this.Error<AssetDisposalDto>($"Failed to create disposal: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Approve/reject disposal
    /// </summary>
    [HttpPost("disposals/{id}/approve")]
    [ProducesResponseType(typeof(ApiResponse<AssetDisposalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AssetDisposalDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AssetDisposalDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AssetDisposalDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AssetDisposalDto>>> ApproveDisposal(
        Guid id,
        [FromBody] ApproveAssetDisposalDto dto,
        CancellationToken cancellationToken = default)
    {
        // Multi-tenant SaaS — CompanyId required; resolved from tenant context.
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId ?? Guid.Empty;

        try
        {
            var disposal = await _assetService.ApproveDisposalAsync(id, dto, companyId, userId, cancellationToken);
            return this.Success(disposal, "Disposal approval processed successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<AssetDisposalDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<AssetDisposalDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving disposal {DisposalId}", id);
            return this.Error<AssetDisposalDto>($"Failed to approve disposal: {ex.Message}", 500);
        }
    }
}

/// <summary>
/// DTO for post depreciation result
/// </summary>
public class PostDepreciationResultDto
{
    public int Count { get; set; }
    public string Message { get; set; } = string.Empty;
}

