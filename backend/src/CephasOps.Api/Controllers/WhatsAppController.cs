using CephasOps.Application.Notifications.Services;
using CephasOps.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// WhatsApp messaging API endpoints
/// </summary>
[ApiController]
[Route("api/whatsapp")]
[Authorize]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppMessagingService _whatsAppMessagingService;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(
        IWhatsAppMessagingService whatsAppMessagingService,
        ILogger<WhatsAppController> logger)
    {
        _whatsAppMessagingService = whatsAppMessagingService;
        _logger = logger;
    }

    /// <summary>
    /// Send WhatsApp template message
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WhatsAppResult>>> SendWhatsApp(
        [FromBody] SendWhatsAppRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.To))
            {
                return this.BadRequest<WhatsAppResult>("Recipient phone number is required");
            }

            if (string.IsNullOrWhiteSpace(request.TemplateName))
            {
                return this.BadRequest<WhatsAppResult>("Template name is required");
            }

            var result = await _whatsAppMessagingService.SendTemplateMessageAsync(
                request.To,
                request.TemplateName,
                request.Parameters,
                request.LanguageCode ?? "en",
                cancellationToken);

            if (result.Success)
            {
                return this.Success(result, "WhatsApp message sent successfully");
            }
            else
            {
                return this.BadRequest<WhatsAppResult>(result.ErrorMessage ?? "Failed to send WhatsApp message");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp to {To}", request.To);
            return this.InternalServerError<WhatsAppResult>($"Error sending WhatsApp: {ex.Message}");
        }
    }

    /// <summary>
    /// Send job update notification via WhatsApp
    /// </summary>
    [HttpPost("send-job-update")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WhatsAppResult>>> SendJobUpdate(
        [FromBody] SendJobUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CustomerPhone))
            {
                return this.BadRequest<WhatsAppResult>("Customer phone number is required");
            }

            if (string.IsNullOrWhiteSpace(request.OrderNumber))
            {
                return this.BadRequest<WhatsAppResult>("Order number is required");
            }

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return this.BadRequest<WhatsAppResult>("Status is required");
            }

            var result = await _whatsAppMessagingService.SendJobUpdateAsync(
                request.CustomerPhone,
                request.OrderNumber,
                request.Status,
                request.AppointmentDate,
                request.InstallerName,
                cancellationToken);

            if (result.Success)
            {
                return this.Success(result, "Job update WhatsApp sent successfully");
            }
            else
            {
                return this.BadRequest<WhatsAppResult>(result.ErrorMessage ?? "Failed to send job update WhatsApp");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending job update WhatsApp to {Phone}", request.CustomerPhone);
            return this.InternalServerError<WhatsAppResult>($"Error sending job update WhatsApp: {ex.Message}");
        }
    }

    /// <summary>
    /// Send SI on-the-way alert via WhatsApp
    /// </summary>
    [HttpPost("send-si-alert")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WhatsAppResult>>> SendSiAlert(
        [FromBody] SendSiAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CustomerPhone))
            {
                return this.BadRequest<WhatsAppResult>("Customer phone number is required");
            }

            if (string.IsNullOrWhiteSpace(request.OrderNumber))
            {
                return this.BadRequest<WhatsAppResult>("Order number is required");
            }

            if (string.IsNullOrWhiteSpace(request.InstallerName))
            {
                return this.BadRequest<WhatsAppResult>("Installer name is required");
            }

            var result = await _whatsAppMessagingService.SendSiOnTheWayAlertAsync(
                request.CustomerPhone,
                request.OrderNumber,
                request.InstallerName,
                request.EstimatedArrival,
                cancellationToken);

            if (result.Success)
            {
                return this.Success(result, "SI alert WhatsApp sent successfully");
            }
            else
            {
                return this.BadRequest<WhatsAppResult>(result.ErrorMessage ?? "Failed to send SI alert WhatsApp");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SI alert WhatsApp to {Phone}", request.CustomerPhone);
            return this.InternalServerError<WhatsAppResult>($"Error sending SI alert WhatsApp: {ex.Message}");
        }
    }
}

/// <summary>
/// Request DTO for sending WhatsApp template message
/// </summary>
public class SendWhatsAppRequest
{
    public string To { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Dictionary<string, string>? Parameters { get; set; }
    public string? LanguageCode { get; set; }
}

/// <summary>
/// Request DTO for sending job update
/// </summary>
public class SendJobUpdateRequest
{
    public string CustomerPhone { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AppointmentDate { get; set; }
    public string? InstallerName { get; set; }
}

/// <summary>
/// Request DTO for sending SI alert
/// </summary>
public class SendSiAlertRequest
{
    public string CustomerPhone { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string InstallerName { get; set; } = string.Empty;
    public string? EstimatedArrival { get; set; }
}

