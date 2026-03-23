using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Integration settings management API
/// </summary>
[ApiController]
[Route("api/integrations")]
[Authorize(Policy = "Settings")]
public class IntegrationSettingsController : ControllerBase
{
    private readonly IIntegrationSettingsService _integrationSettingsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<IntegrationSettingsController> _logger;

    public IntegrationSettingsController(
        IIntegrationSettingsService integrationSettingsService,
        ICurrentUserService currentUserService,
        ILogger<IntegrationSettingsController> logger)
    {
        _integrationSettingsService = integrationSettingsService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all integration settings
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.SettingsView)]
    [ProducesResponseType(typeof(ApiResponse<IntegrationSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IntegrationSettingsDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IntegrationSettingsDto>>> GetSettings(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _integrationSettingsService.GetIntegrationSettingsAsync(cancellationToken);
            return this.Success(settings, "Integration settings retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting integration settings");
            return this.InternalServerError<IntegrationSettingsDto>($"Error retrieving integration settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Update MyInvois settings
    /// </summary>
    [HttpPut("myinvois")]
    [RequirePermission(PermissionCatalog.SettingsEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> UpdateMyInvoisSettings(
        [FromBody] MyInvoisSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId ?? Guid.Empty;
            await _integrationSettingsService.UpdateMyInvoisSettingsAsync(dto, userId, cancellationToken);
            return this.Success("MyInvois settings updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating MyInvois settings");
            return this.InternalServerError($"Error updating MyInvois settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Update SMS settings
    /// </summary>
    [HttpPut("sms")]
    [RequirePermission(PermissionCatalog.SettingsEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> UpdateSmsSettings(
        [FromBody] SmsSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId ?? Guid.Empty;
            await _integrationSettingsService.UpdateSmsSettingsAsync(dto, userId, cancellationToken);
            return this.Success("SMS settings updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SMS settings");
            return this.InternalServerError($"Error updating SMS settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Update WhatsApp settings
    /// </summary>
    [HttpPut("whatsapp")]
    [RequirePermission(PermissionCatalog.SettingsEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> UpdateWhatsAppSettings(
        [FromBody] WhatsAppSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId ?? Guid.Empty;
            await _integrationSettingsService.UpdateWhatsAppSettingsAsync(dto, userId, cancellationToken);
            return this.Success("WhatsApp settings updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating WhatsApp settings");
            return this.InternalServerError($"Error updating WhatsApp settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Test MyInvois connection
    /// </summary>
    [HttpPost("myinvois/test")]
    [RequirePermission(PermissionCatalog.SettingsEdit)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> TestMyInvoisConnection(CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _integrationSettingsService.TestMyInvoisConnectionAsync(cancellationToken);
            return this.Success<object>(
                new { connected = isConnected },
                isConnected ? "MyInvois connection test successful" : "MyInvois connection test failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing MyInvois connection");
            return this.InternalServerError<object>($"Error testing MyInvois connection: {ex.Message}");
        }
    }

    /// <summary>
    /// Test SMS connection
    /// </summary>
    [HttpPost("sms/test")]
    [RequirePermission(PermissionCatalog.SettingsEdit)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> TestSmsConnection(CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _integrationSettingsService.TestSmsConnectionAsync(cancellationToken);
            return this.Success<object>(
                new { connected = isConnected },
                isConnected ? "SMS connection test successful" : "SMS connection test failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMS connection");
            return this.InternalServerError<object>($"Error testing SMS connection: {ex.Message}");
        }
    }

    /// <summary>
    /// Test WhatsApp connection
    /// </summary>
    [HttpPost("whatsapp/test")]
    [RequirePermission(PermissionCatalog.SettingsEdit)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> TestWhatsAppConnection(CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _integrationSettingsService.TestWhatsAppConnectionAsync(cancellationToken);
            return this.Success<object>(
                new { connected = isConnected },
                isConnected ? "WhatsApp connection test successful" : "WhatsApp connection test failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing WhatsApp connection");
            return this.InternalServerError<object>($"Error testing WhatsApp connection: {ex.Message}");
        }
    }
}

