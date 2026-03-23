using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// SMS Gateway registration API controller
/// Allows Android SMS Gateway apps to register themselves
/// </summary>
[ApiController]
[Route("api/sms-gateway")]
// No authentication for now - will be secured later
public class SmsGatewayController : ControllerBase
{
    private readonly ISmsGatewayService _smsGatewayService;
    private readonly ILogger<SmsGatewayController> _logger;

    public SmsGatewayController(
        ISmsGatewayService smsGatewayService,
        ILogger<SmsGatewayController> logger)
    {
        _smsGatewayService = smsGatewayService;
        _logger = logger;
    }

    /// <summary>
    /// Register or update an SMS Gateway
    /// POST /api/sms-gateway/register
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Guid>>> Register([FromBody] RegisterSmsGatewayRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.DeviceName))
            {
                return this.BadRequest<Guid>("DeviceName is required");
            }

            if (string.IsNullOrEmpty(request.BaseUrl))
            {
                return this.BadRequest<Guid>("BaseUrl is required");
            }

            if (string.IsNullOrEmpty(request.ApiKey))
            {
                return this.BadRequest<Guid>("ApiKey is required");
            }

            var gatewayId = await _smsGatewayService.RegisterGatewayAsync(request);

            _logger.LogInformation("SMS Gateway registered successfully: {GatewayId}", gatewayId);

            return this.Success(gatewayId, "SMS Gateway registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register SMS Gateway");
            return this.Error<Guid>($"Failed to register SMS Gateway: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get the currently active SMS Gateway
    /// GET /api/sms-gateway/active
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<SmsGatewayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SmsGatewayDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SmsGatewayDto>>> GetActive()
    {
        try
        {
            var gateway = await _smsGatewayService.GetActiveGatewayAsync();

            if (gateway == null)
            {
                return this.NotFound<SmsGatewayDto>("No active SMS Gateway found");
            }

            return this.Success(gateway);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active SMS Gateway");
            return this.Error<SmsGatewayDto>($"Failed to get active SMS Gateway: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get all SMS Gateways
    /// GET /api/sms-gateway
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SmsGatewayDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SmsGatewayDto>>>> GetAll()
    {
        try
        {
            var gateways = await _smsGatewayService.GetAllGatewaysAsync();
            return this.Success(gateways);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SMS Gateways");
            return this.Error<List<SmsGatewayDto>>($"Failed to get SMS Gateways: {ex.Message}", 500);
        }
    }
}

