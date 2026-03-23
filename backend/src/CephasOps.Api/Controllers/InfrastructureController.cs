using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Building infrastructure endpoints (blocks, splitters, streets, hub boxes, poles)
/// </summary>
[ApiController]
[Route("api/buildings/{buildingId:guid}/infrastructure")]
[Authorize]
public class InfrastructureController : ControllerBase
{
    private readonly IInfrastructureService _infrastructureService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<InfrastructureController> _logger;

    public InfrastructureController(
        IInfrastructureService infrastructureService,
        ITenantProvider tenantProvider,
        ILogger<InfrastructureController> logger)
    {
        _infrastructureService = infrastructureService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    #region Infrastructure Overview

    /// <summary>
    /// Get building infrastructure overview
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<BuildingInfrastructureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingInfrastructureDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingInfrastructureDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingInfrastructureDto>>> GetInfrastructure(
        [FromRoute] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var infrastructure = await _infrastructureService.GetBuildingInfrastructureAsync(buildingId, companyId, cancellationToken);
            return this.Success(infrastructure);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingInfrastructureDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting infrastructure for building {BuildingId}", buildingId);
            return this.InternalServerError<BuildingInfrastructureDto>($"Failed to get infrastructure: {ex.Message}");
        }
    }

    #endregion

    #region Building Blocks

    /// <summary>
    /// Get building blocks
    /// </summary>
    [HttpGet("blocks")]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingBlockDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingBlockDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingBlockDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BuildingBlockDto>>>> GetBlocks(
        [FromRoute] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var blocks = await _infrastructureService.GetBuildingBlocksAsync(buildingId, companyId, cancellationToken);
            return this.Success(blocks);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<List<BuildingBlockDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocks for building {BuildingId}", buildingId);
            return this.InternalServerError<List<BuildingBlockDto>>($"Failed to get blocks: {ex.Message}");
        }
    }

    /// <summary>
    /// Create building block
    /// </summary>
    [HttpPost("blocks")]
    [ProducesResponseType(typeof(ApiResponse<BuildingBlockDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BuildingBlockDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingBlockDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingBlockDto>>> CreateBlock(
        [FromRoute] Guid buildingId,
        [FromBody] SaveBuildingBlockDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var block = await _infrastructureService.CreateBuildingBlockAsync(buildingId, dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetBlocks), new { buildingId }, block, "Building block created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingBlockDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating block for building {BuildingId}", buildingId);
            return this.InternalServerError<BuildingBlockDto>($"Failed to create block: {ex.Message}");
        }
    }

    /// <summary>
    /// Update building block
    /// </summary>
    [HttpPut("blocks/{blockId}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingBlockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingBlockDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingBlockDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingBlockDto>>> UpdateBlock(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid blockId,
        [FromBody] SaveBuildingBlockDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var block = await _infrastructureService.UpdateBuildingBlockAsync(buildingId, blockId, dto, companyId, cancellationToken);
            return this.Success(block, "Building block updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingBlockDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating block {BlockId}", blockId);
            return this.InternalServerError<BuildingBlockDto>($"Failed to update block: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete building block
    /// </summary>
    [HttpDelete("blocks/{blockId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteBlock(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid blockId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            await _infrastructureService.DeleteBuildingBlockAsync(buildingId, blockId, companyId, cancellationToken);
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
            _logger.LogError(ex, "Error deleting block {BlockId}", blockId);
            return this.InternalServerError($"Failed to delete block: {ex.Message}");
        }
    }

    #endregion

    #region Building Splitters

    /// <summary>
    /// Get building splitters
    /// </summary>
    [HttpGet("splitters")]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingSplitterDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingSplitterDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingSplitterDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BuildingSplitterDto>>>> GetSplitters(
        [FromRoute] Guid buildingId,
        [FromQuery] SplitterFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var splitters = await _infrastructureService.GetBuildingSplittersAsync(buildingId, companyId, filter, cancellationToken);
            return this.Success(splitters);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<List<BuildingSplitterDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting splitters for building {BuildingId}", buildingId);
            return this.InternalServerError<List<BuildingSplitterDto>>($"Failed to get splitters: {ex.Message}");
        }
    }

    /// <summary>
    /// Create building splitter
    /// </summary>
    [HttpPost("splitters")]
    [ProducesResponseType(typeof(ApiResponse<BuildingSplitterDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BuildingSplitterDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingSplitterDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingSplitterDto>>> CreateSplitter(
        [FromRoute] Guid buildingId,
        [FromBody] SaveBuildingSplitterDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var splitter = await _infrastructureService.CreateBuildingSplitterAsync(buildingId, dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetSplitters), new { buildingId }, splitter, "Building splitter created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingSplitterDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating splitter for building {BuildingId}", buildingId);
            return this.InternalServerError<BuildingSplitterDto>($"Failed to create splitter: {ex.Message}");
        }
    }

    /// <summary>
    /// Update building splitter
    /// </summary>
    [HttpPut("splitters/{splitterId}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingSplitterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingSplitterDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingSplitterDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingSplitterDto>>> UpdateSplitter(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid splitterId,
        [FromBody] SaveBuildingSplitterDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var splitter = await _infrastructureService.UpdateBuildingSplitterAsync(buildingId, splitterId, dto, companyId, cancellationToken);
            return this.Success(splitter, "Building splitter updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingSplitterDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating splitter {SplitterId}", splitterId);
            return this.InternalServerError<BuildingSplitterDto>($"Failed to update splitter: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete building splitter
    /// </summary>
    [HttpDelete("splitters/{splitterId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteSplitter(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid splitterId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            await _infrastructureService.DeleteBuildingSplitterAsync(buildingId, splitterId, companyId, cancellationToken);
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
            _logger.LogError(ex, "Error deleting splitter {SplitterId}", splitterId);
            return this.InternalServerError($"Failed to delete splitter: {ex.Message}");
        }
    }

    /// <summary>
    /// Update splitter port usage
    /// </summary>
    [HttpPatch("splitters/{splitterId}/ports")]
    [ProducesResponseType(typeof(ApiResponse<BuildingSplitterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BuildingSplitterDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BuildingSplitterDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BuildingSplitterDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BuildingSplitterDto>>> UpdateSplitterPorts(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid splitterId,
        [FromBody] int portsUsed,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var splitter = await _infrastructureService.UpdateSplitterPortUsageAsync(buildingId, splitterId, portsUsed, companyId, cancellationToken);
            return this.Success(splitter, "Splitter port usage updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BuildingSplitterDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<BuildingSplitterDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating splitter ports {SplitterId}", splitterId);
            return this.InternalServerError<BuildingSplitterDto>($"Failed to update splitter ports: {ex.Message}");
        }
    }

    #endregion

    #region Streets

    /// <summary>
    /// Get streets
    /// </summary>
    [HttpGet("streets")]
    [ProducesResponseType(typeof(ApiResponse<List<StreetDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<StreetDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<List<StreetDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<StreetDto>>>> GetStreets(
        [FromRoute] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var streets = await _infrastructureService.GetStreetsAsync(buildingId, companyId, cancellationToken);
            return this.Success(streets);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<List<StreetDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting streets for building {BuildingId}", buildingId);
            return this.InternalServerError<List<StreetDto>>($"Failed to get streets: {ex.Message}");
        }
    }

    /// <summary>
    /// Create street
    /// </summary>
    [HttpPost("streets")]
    [ProducesResponseType(typeof(ApiResponse<StreetDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<StreetDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StreetDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<StreetDto>>> CreateStreet(
        [FromRoute] Guid buildingId,
        [FromBody] SaveStreetDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var street = await _infrastructureService.CreateStreetAsync(buildingId, dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetStreets), new { buildingId }, street, "Street created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<StreetDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating street for building {BuildingId}", buildingId);
            return this.InternalServerError<StreetDto>($"Failed to create street: {ex.Message}");
        }
    }

    /// <summary>
    /// Update street
    /// </summary>
    [HttpPut("streets/{streetId}")]
    [ProducesResponseType(typeof(ApiResponse<StreetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StreetDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StreetDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<StreetDto>>> UpdateStreet(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid streetId,
        [FromBody] SaveStreetDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var street = await _infrastructureService.UpdateStreetAsync(buildingId, streetId, dto, companyId, cancellationToken);
            return this.Success(street, "Street updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<StreetDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating street {StreetId}", streetId);
            return this.InternalServerError<StreetDto>($"Failed to update street: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete street
    /// </summary>
    [HttpDelete("streets/{streetId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteStreet(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid streetId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            await _infrastructureService.DeleteStreetAsync(buildingId, streetId, companyId, cancellationToken);
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
            _logger.LogError(ex, "Error deleting street {StreetId}", streetId);
            return this.InternalServerError($"Failed to delete street: {ex.Message}");
        }
    }

    #endregion

    #region Hub Boxes

    /// <summary>
    /// Get hub boxes
    /// </summary>
    [HttpGet("hubboxes")]
    [ProducesResponseType(typeof(ApiResponse<List<HubBoxDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<HubBoxDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<List<HubBoxDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<HubBoxDto>>>> GetHubBoxes(
        [FromRoute] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var hubBoxes = await _infrastructureService.GetHubBoxesAsync(buildingId, companyId, cancellationToken);
            return this.Success(hubBoxes);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<List<HubBoxDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hub boxes for building {BuildingId}", buildingId);
            return this.InternalServerError<List<HubBoxDto>>($"Failed to get hub boxes: {ex.Message}");
        }
    }

    /// <summary>
    /// Create hub box
    /// </summary>
    [HttpPost("hubboxes")]
    [ProducesResponseType(typeof(ApiResponse<HubBoxDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<HubBoxDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<HubBoxDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<HubBoxDto>>> CreateHubBox(
        [FromRoute] Guid buildingId,
        [FromBody] SaveHubBoxDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var hubBox = await _infrastructureService.CreateHubBoxAsync(buildingId, dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetHubBoxes), new { buildingId }, hubBox, "Hub box created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<HubBoxDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hub box for building {BuildingId}", buildingId);
            return this.InternalServerError<HubBoxDto>($"Failed to create hub box: {ex.Message}");
        }
    }

    /// <summary>
    /// Update hub box
    /// </summary>
    [HttpPut("hubboxes/{hubBoxId}")]
    [ProducesResponseType(typeof(ApiResponse<HubBoxDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HubBoxDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<HubBoxDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<HubBoxDto>>> UpdateHubBox(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid hubBoxId,
        [FromBody] SaveHubBoxDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var hubBox = await _infrastructureService.UpdateHubBoxAsync(buildingId, hubBoxId, dto, companyId, cancellationToken);
            return this.Success(hubBox, "Hub box updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<HubBoxDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hub box {HubBoxId}", hubBoxId);
            return this.InternalServerError<HubBoxDto>($"Failed to update hub box: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete hub box
    /// </summary>
    [HttpDelete("hubboxes/{hubBoxId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteHubBox(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid hubBoxId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            await _infrastructureService.DeleteHubBoxAsync(buildingId, hubBoxId, companyId, cancellationToken);
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
            _logger.LogError(ex, "Error deleting hub box {HubBoxId}", hubBoxId);
            return this.InternalServerError($"Failed to delete hub box: {ex.Message}");
        }
    }

    #endregion

    #region Poles

    /// <summary>
    /// Get poles
    /// </summary>
    [HttpGet("poles")]
    [ProducesResponseType(typeof(ApiResponse<List<PoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PoleDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<List<PoleDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PoleDto>>>> GetPoles(
        [FromRoute] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var poles = await _infrastructureService.GetPolesAsync(buildingId, companyId, cancellationToken);
            return this.Success(poles);
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<List<PoleDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting poles for building {BuildingId}", buildingId);
            return this.InternalServerError<List<PoleDto>>($"Failed to get poles: {ex.Message}");
        }
    }

    /// <summary>
    /// Create pole
    /// </summary>
    [HttpPost("poles")]
    [ProducesResponseType(typeof(ApiResponse<PoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<PoleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PoleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PoleDto>>> CreatePole(
        [FromRoute] Guid buildingId,
        [FromBody] SavePoleDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var pole = await _infrastructureService.CreatePoleAsync(buildingId, dto, companyId, cancellationToken);
            return this.CreatedAtAction(nameof(GetPoles), new { buildingId }, pole, "Pole created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<PoleDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pole for building {BuildingId}", buildingId);
            return this.InternalServerError<PoleDto>($"Failed to create pole: {ex.Message}");
        }
    }

    /// <summary>
    /// Update pole
    /// </summary>
    [HttpPut("poles/{poleId}")]
    [ProducesResponseType(typeof(ApiResponse<PoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PoleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PoleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PoleDto>>> UpdatePole(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid poleId,
        [FromBody] SavePoleDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var pole = await _infrastructureService.UpdatePoleAsync(buildingId, poleId, dto, companyId, cancellationToken);
            return this.Success(pole, "Pole updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<PoleDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pole {PoleId}", poleId);
            return this.InternalServerError<PoleDto>($"Failed to update pole: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete pole
    /// </summary>
    [HttpDelete("poles/{poleId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeletePole(
        [FromRoute] Guid buildingId,
        [FromRoute] Guid poleId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            await _infrastructureService.DeletePoleAsync(buildingId, poleId, companyId, cancellationToken);
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
            _logger.LogError(ex, "Error deleting pole {PoleId}", poleId);
            return this.InternalServerError($"Failed to delete pole: {ex.Message}");
        }
    }

    #endregion
}

