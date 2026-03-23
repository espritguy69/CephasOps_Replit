using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Notifications.Services;
using CephasOps.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// SMS messaging API endpoints. Template SMS is tenant-scoped (company from ITenantProvider).
/// </summary>
[ApiController]
[Route("api/sms")]
[Authorize]
public class SmsController : ControllerBase
{
    private readonly ISmsMessagingService _smsMessagingService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SmsController> _logger;

    public SmsController(
        ISmsMessagingService smsMessagingService,
        ITenantProvider tenantProvider,
        ILogger<SmsController> logger)
    {
        _smsMessagingService = smsMessagingService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Send SMS message
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse<SmsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SmsResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SmsResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SmsResult>>> SendSms(
        [FromBody] SendSmsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.To))
            {
                return this.BadRequest<SmsResult>("Recipient phone number is required");
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return this.BadRequest<SmsResult>("Message text is required");
            }

            var result = await _smsMessagingService.SendSmsAsync(request.To, request.Message, cancellationToken);

            if (result.Success)
            {
                return this.Success(result, "SMS sent successfully");
            }
            else
            {
                return this.BadRequest<SmsResult>(result.ErrorMessage ?? "Failed to send SMS");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {To}", request.To);
            return this.InternalServerError<SmsResult>($"Error sending SMS: {ex.Message}");
        }
    }

    /// <summary>
    /// Send SMS using template
    /// </summary>
    [HttpPost("send-template")]
    [ProducesResponseType(typeof(ApiResponse<SmsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SmsResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SmsResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SmsResult>>> SendTemplateSms(
        [FromBody] SendTemplateSmsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.To))
            {
                return this.BadRequest<SmsResult>("Recipient phone number is required");
            }

            if (string.IsNullOrWhiteSpace(request.TemplateCode))
            {
                return this.BadRequest<SmsResult>("Template code is required");
            }

            var (companyId, err) = this.RequireCompanyId(_tenantProvider);
            if (err != null) return err;

            var result = await _smsMessagingService.SendTemplateSmsAsync(
                request.To,
                request.TemplateCode,
                request.Placeholders,
                companyId,
                cancellationToken);

            if (result.Success)
            {
                return this.Success(result, "Template SMS sent successfully");
            }
            else
            {
                return this.BadRequest<SmsResult>(result.ErrorMessage ?? "Failed to send template SMS");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending template SMS to {To}", request.To);
            return this.InternalServerError<SmsResult>($"Error sending template SMS: {ex.Message}");
        }
    }

    /// <summary>
    /// Get SMS message status
    /// </summary>
    [HttpGet("status/{messageId}")]
    [ProducesResponseType(typeof(ApiResponse<SmsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SmsResult>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SmsResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SmsResult>>> GetStatus(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(messageId))
            {
                return this.BadRequest<SmsResult>("Message ID is required");
            }

            var result = await _smsMessagingService.GetStatusAsync(messageId, cancellationToken);

            return this.Success(result, "SMS status retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMS status for {MessageId}", messageId);
            return this.InternalServerError<SmsResult>($"Error getting SMS status: {ex.Message}");
        }
    }
}

/// <summary>
/// Request DTO for sending SMS
/// </summary>
public class SendSmsRequest
{
    public string To { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for sending template SMS
/// </summary>
public class SendTemplateSmsRequest
{
    public string To { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public Dictionary<string, string>? Placeholders { get; set; }
}

