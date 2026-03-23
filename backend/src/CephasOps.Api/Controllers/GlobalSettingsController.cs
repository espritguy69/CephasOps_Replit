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
/// Global settings management endpoints
/// </summary>
[ApiController]
[Route("api/global-settings")]
[Authorize(Policy = "Settings")]
public class GlobalSettingsController : ControllerBase
{
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GlobalSettingsController> _logger;

    public GlobalSettingsController(
        IGlobalSettingsService globalSettingsService,
        ICurrentUserService currentUserService,
        ILogger<GlobalSettingsController> logger)
    {
        _globalSettingsService = globalSettingsService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all global settings
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.SettingsView)]
    [ProducesResponseType(typeof(ApiResponse<List<GlobalSettingDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<GlobalSettingDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<GlobalSettingDto>>>> GetGlobalSettings(
        [FromQuery] string? module = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _globalSettingsService.GetAllAsync(module, cancellationToken);
            return this.Success(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global settings");
            return this.InternalServerError<List<GlobalSettingDto>>($"Failed to get global settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Get global setting by key
    /// </summary>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GlobalSettingDto>>> GetGlobalSetting(
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var setting = await _globalSettingsService.GetByKeyAsync(key, cancellationToken);
            if (setting == null)
            {
                return this.NotFound<GlobalSettingDto>($"Global setting with key '{key}' not found");
            }

            return this.Success(setting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global setting: {Key}", key);
            return this.InternalServerError<GlobalSettingDto>($"Failed to get global setting: {ex.Message}");
        }
    }

    /// <summary>
    /// Create global setting
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCatalog.SettingsEdit)]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GlobalSettingDto>>> CreateGlobalSetting(
        [FromBody] CreateGlobalSettingDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<GlobalSettingDto>("User context required");
        }

        try
        {
            var setting = await _globalSettingsService.CreateAsync(dto, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetGlobalSetting), new { key = setting.Key }, setting, "Global setting created successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(409, this.Error<GlobalSettingDto>(ex.Message, 409));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating global setting");
            return this.InternalServerError<GlobalSettingDto>($"Failed to create global setting: {ex.Message}");
        }
    }

    /// <summary>
    /// Update global setting
    /// </summary>
    [HttpPut("{key}")]
    [RequirePermission(PermissionCatalog.SettingsEdit)]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GlobalSettingDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GlobalSettingDto>>> UpdateGlobalSetting(
        string key,
        [FromBody] UpdateGlobalSettingDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<GlobalSettingDto>("User context required");
        }

        try
        {
            var setting = await _globalSettingsService.UpdateAsync(key, dto, userId.Value, cancellationToken);
            return this.Success(setting, "Global setting updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<GlobalSettingDto>($"Global setting with key '{key}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global setting: {Key}", key);
            return this.InternalServerError<GlobalSettingDto>($"Failed to update global setting: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete global setting
    /// </summary>
    [HttpDelete("{key}")]
    [RequirePermission(PermissionCatalog.SettingsEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteGlobalSetting(
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _globalSettingsService.DeleteAsync(key, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Global setting with key '{key}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting global setting: {Key}", key);
            return this.InternalServerError($"Failed to delete global setting: {ex.Message}");
        }
    }
}
