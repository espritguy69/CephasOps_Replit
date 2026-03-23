using CephasOps.Application.SIApp.DTOs;
using CephasOps.Application.SIApp.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Service Installer App API endpoints
/// </summary>
[ApiController]
[Route("api/si-app")]
[Authorize]
public class SiAppController : ControllerBase
{
    private readonly SiAppMaterialService _siAppMaterialService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SiAppController> _logger;

    public SiAppController(
        SiAppMaterialService siAppMaterialService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<SiAppController> logger)
    {
        _siAppMaterialService = siAppMaterialService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Mark a device as faulty for a job
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="serialNumber">Serial number of the device</param>
    /// <param name="dto">Faulty device information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Mark faulty response</returns>
    [HttpPost("jobs/{orderId}/materials/{serialNumber}/mark-faulty")]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MarkFaultyResponseDto>>> MarkDeviceAsFaulty(
        Guid orderId,
        string serialNumber,
        [FromBody] MarkFaultyDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var siId = _currentUserService.ServiceInstallerId;
        var userId = _currentUserService.UserId;

        if (!siId.HasValue)
        {
            return this.Unauthorized<MarkFaultyResponseDto>("Service Installer context required");
        }

        // Validate serial number matches
        if (!string.Equals(dto.SerialNumber, serialNumber, StringComparison.OrdinalIgnoreCase))
        {
            return this.Error<MarkFaultyResponseDto>("Serial number in URL does not match body", 400);
        }

        try
        {
            var result = await _siAppMaterialService.MarkDeviceAsFaultyAsync(
                orderId,
                dto.SerialNumber,
                dto.Reason,
                companyId,
                siId.Value,
                userId,
                dto.Notes,
                cancellationToken);

            return this.Success(result, "Device marked as faulty successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order or serial not found for mark faulty: Order={OrderId}, Serial={SerialNumber}", orderId, serialNumber);
            return this.NotFound<MarkFaultyResponseDto>($"Order or device not found: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized mark faulty attempt: Order={OrderId}, SI={SiId}", orderId, siId);
            return this.Forbidden<MarkFaultyResponseDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking device as faulty: Order={OrderId}, Serial={SerialNumber}", orderId, serialNumber);
            return this.Error<MarkFaultyResponseDto>($"Failed to mark device as faulty: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Record material replacement for Assurance orders
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="dto">Replacement information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Replacement response</returns>
    [HttpPost("jobs/{orderId}/materials/replace")]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RecordReplacementResponseDto>>> RecordMaterialReplacement(
        Guid orderId,
        [FromBody] RecordReplacementDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var siId = _currentUserService.ServiceInstallerId;
        var userId = _currentUserService.UserId;

        if (!siId.HasValue)
        {
            return this.Unauthorized<RecordReplacementResponseDto>("Service Installer context required");
        }

        if (string.IsNullOrWhiteSpace(dto.OldSerialNumber) || string.IsNullOrWhiteSpace(dto.NewSerialNumber))
        {
            return this.Error<RecordReplacementResponseDto>("Both old and new serial numbers are required", 400);
        }

        if (dto.OldSerialNumber == dto.NewSerialNumber)
        {
            return this.Error<RecordReplacementResponseDto>("Old and new serial numbers must be different", 400);
        }

        try
        {
            var result = await _siAppMaterialService.RecordMaterialReplacementAsync(
                orderId,
                dto.OldSerialNumber,
                dto.NewSerialNumber,
                dto.ReplacementReason,
                companyId,
                siId.Value,
                userId,
                dto.Notes,
                cancellationToken);

            return this.Success(result, "Material replacement recorded successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order or serial not found for replacement: Order={OrderId}", orderId);
            return this.NotFound<RecordReplacementResponseDto>($"Order or device not found: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized replacement attempt: Order={OrderId}, SI={SiId}", orderId, siId);
            return this.Forbidden<RecordReplacementResponseDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for replacement: Order={OrderId}", orderId);
            return this.Error<RecordReplacementResponseDto>($"Invalid operation: {ex.Message}", 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording material replacement: Order={OrderId}", orderId);
            return this.Error<RecordReplacementResponseDto>($"Failed to record material replacement: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Return faulty material (standalone - not tied to specific order)
    /// </summary>
    /// <param name="dto">Faulty material information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Return response</returns>
    [Obsolete("Legacy write path. Quantities are recorded to ledger only (no StockBalance.Quantity). Prefer ledger endpoints for new code. Ledger is the source of truth.")]
    [HttpPost("materials/return-faulty")]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MarkFaultyResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MarkFaultyResponseDto>>> ReturnFaultyMaterial(
        [FromBody] ReturnFaultyDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var siId = _currentUserService.ServiceInstallerId;
        var userId = _currentUserService.UserId;

        if (!siId.HasValue)
        {
            return this.Unauthorized<MarkFaultyResponseDto>("Service Installer context required");
        }

        if (string.IsNullOrWhiteSpace(dto.SerialNumber) && (!dto.MaterialId.HasValue || !dto.Quantity.HasValue))
        {
            return this.Error<MarkFaultyResponseDto>("Either serialNumber (for serialised) or materialId+quantity (for non-serialised) must be provided", 400);
        }

        try
        {
            var result = await _siAppMaterialService.ReturnFaultyMaterialAsync(
                dto.SerialNumber,
                dto.MaterialId,
                dto.Quantity,
                dto.OrderId,
                dto.Reason,
                companyId,
                siId.Value,
                userId,
                dto.Notes,
                cancellationToken);

            return this.Success(result, "Faulty material returned successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Material or serial not found for return faulty");
            return this.NotFound<MarkFaultyResponseDto>($"Material or device not found: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for return faulty");
            return this.Error<MarkFaultyResponseDto>($"Invalid operation: {ex.Message}", 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning faulty material");
            return this.Error<MarkFaultyResponseDto>($"Failed to return faulty material: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Record non-serialised material replacement
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="dto">Replacement information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Replacement response</returns>
    [Obsolete("Legacy write path. Quantities are recorded to ledger only (no StockBalance.Quantity). Ledger is the source of truth.")]
    [HttpPost("jobs/{orderId}/materials/replace-non-serialised")]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<RecordReplacementResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RecordReplacementResponseDto>>> RecordNonSerialisedReplacement(
        Guid orderId,
        [FromBody] RecordNonSerialisedReplacementDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var siId = _currentUserService.ServiceInstallerId;
        var userId = _currentUserService.UserId;

        if (!siId.HasValue)
        {
            return this.Unauthorized<RecordReplacementResponseDto>("Service Installer context required");
        }

        if (dto.QuantityReplaced <= 0)
        {
            return this.Error<RecordReplacementResponseDto>("Quantity must be greater than 0", 400);
        }

        try
        {
            var result = await _siAppMaterialService.RecordNonSerialisedReplacementAsync(
                orderId,
                dto.MaterialId,
                dto.QuantityReplaced,
                dto.ReplacementReason,
                companyId,
                siId.Value,
                userId,
                dto.Remark,
                cancellationToken);

            return this.Success(result, "Non-serialised material replacement recorded successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order or material not found for non-serialised replacement: Order={OrderId}", orderId);
            return this.NotFound<RecordReplacementResponseDto>($"Order or material not found: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized non-serialised replacement attempt: Order={OrderId}, SI={SiId}", orderId, siId);
            return this.Forbidden<RecordReplacementResponseDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for non-serialised replacement: Order={OrderId}", orderId);
            return this.Error<RecordReplacementResponseDto>($"Invalid operation: {ex.Message}", 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording non-serialised replacement: Order={OrderId}", orderId);
            return this.Error<RecordReplacementResponseDto>($"Failed to record non-serialised replacement: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get material returns list for SI
    /// </summary>
    /// <param name="filters">Query filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of material returns</returns>
    [HttpGet("materials/returns")]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialReturnDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialReturnDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<MaterialReturnDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<MaterialReturnDto>>>> GetMaterialReturns(
        [FromQuery] MaterialReturnsQueryDto? filters = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        var siId = _currentUserService.ServiceInstallerId;

        if (!siId.HasValue)
        {
            return this.Unauthorized<List<MaterialReturnDto>>("Service Installer context required");
        }

        try
        {
            var returns = await _siAppMaterialService.GetMaterialReturnsAsync(
                siId.Value,
                companyId,
                filters,
                cancellationToken);

            return this.Success(returns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting material returns for SI {SiId}", siId);
            return this.Error<List<MaterialReturnDto>>($"Failed to get material returns: {ex.Message}", 500);
        }
    }
}

