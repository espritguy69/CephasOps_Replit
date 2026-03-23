using CephasOps.Api.Common;
using CephasOps.Application.RMA.DTOs;
using CephasOps.Application.RMA.Services;
using CephasOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// RMA management endpoints
/// </summary>
[ApiController]
[Route("api/rma")]
[Authorize]
public class RMAController : ControllerBase
{
    private readonly IRMAService _rmaService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<RMAController> _logger;

    public RMAController(
        IRMAService rmaService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<RMAController> logger)
    {
        _rmaService = rmaService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get RMA requests with filtering
    /// </summary>
    /// <param name="partnerId">Filter by partner</param>
    /// <param name="status">Filter by status</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of RMA requests</returns>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(ApiResponse<List<RmaRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<RmaRequestDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<RmaRequestDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<RmaRequestDto>>>> GetRmaRequests(
        [FromQuery] Guid? partnerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == Guid.Empty && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<List<RmaRequestDto>>("Company context required", 401);
        }

        try
        {
            var requests = await _rmaService.GetRmaRequestsAsync(companyId, partnerId, status, fromDate, toDate, cancellationToken);
            return this.Success(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting RMA requests");
            return this.Error<List<RmaRequestDto>>("Failed to get RMA requests", 500);
        }
    }

    /// <summary>
    /// Get RMA request by ID
    /// </summary>
    /// <param name="id">RMA request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>RMA request details</returns>
    [HttpGet("requests/{id}")]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RmaRequestDto>>> GetRmaRequest(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == Guid.Empty && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<RmaRequestDto>("Company context required", 401);
        }

        try
        {
            var request = await _rmaService.GetRmaRequestByIdAsync(id, companyId, cancellationToken);
            if (request == null)
            {
                return this.NotFound<RmaRequestDto>($"RMA request with ID {id} not found");
            }

            return this.Success(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting RMA request: {RmaId}", id);
            return this.Error<RmaRequestDto>("Failed to get RMA request", 500);
        }
    }

    /// <summary>
    /// Create a new RMA request
    /// </summary>
    /// <param name="dto">RMA request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created RMA request</returns>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RmaRequestDto>>> CreateRmaRequest(
        [FromBody] CreateRmaRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        var userId = _currentUserService.UserId;
        if ((companyId == Guid.Empty && !_currentUserService.IsSuperAdmin) || userId == null)
        {
            return this.Error<RmaRequestDto>("Company and user context required", 401);
        }

        try
        {
            var request = await _rmaService.CreateRmaRequestAsync(dto, companyId, userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetRmaRequest), new { id = request.Id }, ApiResponse<RmaRequestDto>.SuccessResponse(request, "RMA request created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating RMA request");
            return this.Error<RmaRequestDto>("Failed to create RMA request", 500);
        }
    }

    /// <summary>
    /// Update an existing RMA request
    /// </summary>
    /// <param name="id">RMA request ID</param>
    /// <param name="dto">Updated RMA request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated RMA request</returns>
    [HttpPut("requests/{id}")]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<RmaRequestDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RmaRequestDto>>> UpdateRmaRequest(
        Guid id,
        [FromBody] UpdateRmaRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == Guid.Empty && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<RmaRequestDto>("Company context required", 401);
        }

        try
        {
            var request = await _rmaService.UpdateRmaRequestAsync(id, dto, companyId, cancellationToken);
            return this.Success(request);
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<RmaRequestDto>($"RMA request with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RMA request: {RmaId}", id);
            return this.Error<RmaRequestDto>("Failed to update RMA request", 500);
        }
    }

    /// <summary>
    /// Get RMA requests for a specific order
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of RMA requests for the order</returns>
    [HttpGet("orders/{orderId}")]
    [ProducesResponseType(typeof(ApiResponse<List<RmaRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<RmaRequestDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<RmaRequestDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<RmaRequestDto>>>> GetRmaRequestsByOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == Guid.Empty && !_currentUserService.IsSuperAdmin)
        {
            return this.Error<List<RmaRequestDto>>("Company context required", 401);
        }

        try
        {
            var requests = await _rmaService.GetRmaRequestsByOrderAsync(orderId, companyId, cancellationToken);
            return this.Success(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting RMA requests for order {OrderId}", orderId);
            return this.Error<List<RmaRequestDto>>("Failed to get RMA requests", 500);
        }
    }

    /// <summary>
    /// Delete an RMA request
    /// </summary>
    /// <param name="id">RMA request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("requests/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteRmaRequest(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies, regular users need company context
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId == Guid.Empty && !_currentUserService.IsSuperAdmin)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        }

        try
        {
            await _rmaService.DeleteRmaRequestAsync(id, companyId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"RMA request with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting RMA request: {RmaId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse("Failed to delete RMA request"));
        }
    }
}

