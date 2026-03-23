using CephasOps.Application.Companies.DTOs;
using CephasOps.Application.Companies.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Partners endpoints
/// </summary>
[ApiController]
[Route("api/partners")]
[Authorize]
public class PartnersController : ControllerBase
{
    private readonly IPartnerService _partnerService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<PartnersController> _logger;

    public PartnersController(
        IPartnerService partnerService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<PartnersController> logger)
    {
        _partnerService = partnerService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all partners for the current company
    /// </summary>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of partners</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PartnerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PartnerDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PartnerDto>>>> GetPartners(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var partners = await _partnerService.GetPartnersAsync(companyId, isActive, cancellationToken);
            return this.Success(partners);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partners");
            return this.Error<List<PartnerDto>>($"Failed to get partners: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get partner by ID
    /// </summary>
    /// <param name="id">Partner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Partner details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PartnerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PartnerDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PartnerDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PartnerDto>>> GetPartner(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var partner = await _partnerService.GetPartnerByIdAsync(id, companyId, cancellationToken);
            if (partner == null)
            {
                return this.NotFound<PartnerDto>($"Partner with ID {id} not found");
            }

            return this.Success(partner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner: {PartnerId}", id);
            return this.Error<PartnerDto>($"Failed to get partner: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new partner
    /// </summary>
    /// <param name="dto">Partner data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created partner</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PartnerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<PartnerDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PartnerDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PartnerDto>>> CreatePartner(
        [FromBody] CreatePartnerDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = dto.CompanyId ?? _tenantProvider.CurrentTenantId;

        try
        {
            var partner = await _partnerService.CreatePartnerAsync(dto, companyId, cancellationToken);
            return this.StatusCode(201, ApiResponse<PartnerDto>.SuccessResponse(partner, "Partner created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating partner");
            return this.Error<PartnerDto>($"Failed to create partner: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update an existing partner
    /// </summary>
    /// <param name="id">Partner ID</param>
    /// <param name="dto">Updated partner data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated partner</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PartnerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PartnerDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PartnerDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PartnerDto>>> UpdatePartner(
        Guid id,
        [FromBody] UpdatePartnerDto dto,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var partner = await _partnerService.UpdatePartnerAsync(id, dto, companyId, cancellationToken);
            return this.Success(partner, "Partner updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<PartnerDto>($"Partner with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating partner: {PartnerId}", id);
            return this.Error<PartnerDto>($"Failed to update partner: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a partner
    /// </summary>
    /// <param name="id">Partner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeletePartner(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Company feature removed - companyId can be null
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _partnerService.DeletePartnerAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Partner deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Partner with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting partner: {PartnerId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete partner: {ex.Message}"));
        }
    }
}

