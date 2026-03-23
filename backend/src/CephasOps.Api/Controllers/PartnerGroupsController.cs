using CephasOps.Application.Companies.DTOs;
using CephasOps.Application.Companies.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Partner groups endpoints
/// </summary>
[ApiController]
[Route("api/partner-groups")]
[Authorize]
public class PartnerGroupsController : ControllerBase
{
    private readonly IPartnerGroupService _partnerGroupService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<PartnerGroupsController> _logger;

    public PartnerGroupsController(
        IPartnerGroupService partnerGroupService,
        ITenantProvider tenantProvider,
        ILogger<PartnerGroupsController> logger)
    {
        _partnerGroupService = partnerGroupService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all partner groups for the current company
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of partner groups</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PartnerGroupDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PartnerGroupDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PartnerGroupDto>>>> GetPartnerGroups(
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var partnerGroups = await _partnerGroupService.GetPartnerGroupsAsync(companyId, cancellationToken);
            return this.Success(partnerGroups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner groups");
            return this.InternalServerError<List<PartnerGroupDto>>($"Failed to get partner groups: {ex.Message}");
        }
    }

    /// <summary>
    /// Get partner group by ID
    /// </summary>
    /// <param name="id">Partner group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Partner group details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PartnerGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PartnerGroupDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PartnerGroupDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PartnerGroupDto>>> GetPartnerGroup(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var partnerGroup = await _partnerGroupService.GetPartnerGroupByIdAsync(id, companyId, cancellationToken);
            if (partnerGroup == null)
            {
                return this.NotFound<PartnerGroupDto>($"Partner group with ID {id} not found");
            }

            return this.Success(partnerGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting partner group: {PartnerGroupId}", id);
            return this.InternalServerError<PartnerGroupDto>($"Failed to get partner group: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new partner group
    /// </summary>
    /// <param name="dto">Partner group data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created partner group</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PartnerGroupDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<PartnerGroupDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PartnerGroupDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PartnerGroupDto>>> CreatePartnerGroup(
        [FromBody] CreatePartnerGroupDto dto,
        CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = dto.CompanyId ?? _tenantProvider.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return this.Forbidden<PartnerGroupDto>("Company context is required for this operation.");
        var companyId = effectiveCompanyId.Value;

        try
        {
            var partnerGroup = await _partnerGroupService.CreatePartnerGroupAsync(dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetPartnerGroup), new { id = partnerGroup.Id }, partnerGroup, "Partner group created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating partner group");
            return this.InternalServerError<PartnerGroupDto>($"Failed to create partner group: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing partner group
    /// </summary>
    /// <param name="id">Partner group ID</param>
    /// <param name="dto">Updated partner group data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated partner group</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PartnerGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PartnerGroupDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PartnerGroupDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PartnerGroupDto>>> UpdatePartnerGroup(
        Guid id,
        [FromBody] UpdatePartnerGroupDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var partnerGroup = await _partnerGroupService.UpdatePartnerGroupAsync(id, dto, companyId, cancellationToken);
            return this.Success(partnerGroup, "Partner group updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<PartnerGroupDto>($"Partner group with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating partner group: {PartnerGroupId}", id);
            return this.InternalServerError<PartnerGroupDto>($"Failed to update partner group: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a partner group
    /// </summary>
    /// <param name="id">Partner group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeletePartnerGroup(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _partnerGroupService.DeletePartnerGroupAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Partner group with ID {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting partner group: {PartnerGroupId}", id);
            return this.InternalServerError($"Failed to delete partner group: {ex.Message}");
        }
    }
}

