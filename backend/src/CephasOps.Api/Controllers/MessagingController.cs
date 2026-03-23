using CephasOps.Application.Notifications.DTOs;
using CephasOps.Application.Notifications.Services;
using CephasOps.Domain.Notifications;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Messaging controller for unified SMS/WhatsApp notifications
/// Handles job updates, SI alerts, and TTKT notifications with smart routing
/// </summary>
[ApiController]
[Route("api/messaging")]
public class MessagingController : ControllerBase
{
    private readonly IUnifiedMessagingService _unifiedMessagingService;
    private readonly IWhatsAppMessagingService _whatsAppMessagingService;
    private readonly ILogger<MessagingController> _logger;

    public MessagingController(
        IUnifiedMessagingService unifiedMessagingService,
        IWhatsAppMessagingService whatsAppMessagingService,
        ILogger<MessagingController> logger)
    {
        _unifiedMessagingService = unifiedMessagingService;
        _whatsAppMessagingService = whatsAppMessagingService;
        _logger = logger;
    }

    /// <summary>
    /// Send job update notification to customer
    /// POST /api/messaging/job-update
    /// Routes to SMS and/or WhatsApp based on customer preference and urgency
    /// </summary>
    [HttpPost("job-update")]
    [ProducesResponseType(typeof(ApiResponse<MessagingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MessagingResult>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MessagingResult>>> SendJobUpdate([FromBody] JobUpdateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CustomerPhone))
            {
                return this.BadRequest<MessagingResult>("CustomerPhone is required");
            }

            if (string.IsNullOrEmpty(request.OrderNumber))
            {
                return this.BadRequest<MessagingResult>("OrderNumber is required");
            }

            if (string.IsNullOrEmpty(request.Status))
            {
                return this.BadRequest<MessagingResult>("Status is required");
            }

            _logger.LogInformation("Sending job update to {Phone} for order {OrderNumber} (Urgent: {IsUrgent})", 
                request.CustomerPhone, request.OrderNumber, request.IsUrgent);

            var result = await _unifiedMessagingService.SendJobUpdateAsync(
                request.CustomerPhone,
                request.OrderNumber,
                request.Status,
                request.AppointmentDate,
                request.InstallerName,
                request.IsUrgent
            );

            if (result.Success)
            {
                var message = $"Job update sent via {(result.SmsSent && result.WhatsAppSent ? "SMS and WhatsApp" : result.WhatsAppSent ? "WhatsApp" : "SMS")}";
                return this.Success(result, message);
            }
            else
            {
                return this.BadRequest<MessagingResult>(result.ErrorMessage ?? "Failed to send job update notification");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send job update notification");
            return this.Error<MessagingResult>($"Failed to send job update: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Send SI (Service Installer) on-the-way alert to customer
    /// POST /api/messaging/si-on-the-way
    /// Routes to SMS and/or WhatsApp based on customer preference and urgency
    /// </summary>
    [HttpPost("si-on-the-way")]
    [ProducesResponseType(typeof(ApiResponse<MessagingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MessagingResult>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MessagingResult>>> SendSiOnTheWay([FromBody] SiOnTheWayRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CustomerPhone))
            {
                return this.BadRequest<MessagingResult>("CustomerPhone is required");
            }

            if (string.IsNullOrEmpty(request.OrderNumber))
            {
                return this.BadRequest<MessagingResult>("OrderNumber is required");
            }

            if (string.IsNullOrEmpty(request.InstallerName))
            {
                return this.BadRequest<MessagingResult>("InstallerName is required");
            }

            _logger.LogInformation("Sending SI on-the-way alert to {Phone} for order {OrderNumber} (Urgent: {IsUrgent})", 
                request.CustomerPhone, request.OrderNumber, request.IsUrgent);

            var result = await _unifiedMessagingService.SendSiOnTheWayAlertAsync(
                request.CustomerPhone,
                request.OrderNumber,
                request.InstallerName,
                request.EstimatedArrival,
                request.IsUrgent
            );

            if (result.Success)
            {
                var message = $"SI alert sent via {(result.SmsSent && result.WhatsAppSent ? "SMS and WhatsApp" : result.WhatsAppSent ? "WhatsApp" : "SMS")}";
                return this.Success(result, message);
            }
            else
            {
                return this.BadRequest<MessagingResult>(result.ErrorMessage ?? "Failed to send SI on-the-way alert");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SI on-the-way alert");
            return this.Error<MessagingResult>($"Failed to send SI on-the-way alert: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Send TTKT (Ticket) notification to customer
    /// POST /api/messaging/ttkt
    /// Routes to SMS and/or WhatsApp based on customer preference and urgency
    /// </summary>
    [HttpPost("ttkt")]
    [ProducesResponseType(typeof(ApiResponse<MessagingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MessagingResult>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MessagingResult>>> SendTtkt([FromBody] TtktRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CustomerPhone))
            {
                return this.BadRequest<MessagingResult>("CustomerPhone is required");
            }

            if (string.IsNullOrEmpty(request.TicketNumber))
            {
                return this.BadRequest<MessagingResult>("TicketNumber is required");
            }

            if (string.IsNullOrEmpty(request.IssueDescription))
            {
                return this.BadRequest<MessagingResult>("IssueDescription is required");
            }

            _logger.LogInformation("Sending TTKT notification to {Phone} for ticket {TicketNumber} (Urgent: {IsUrgent})", 
                request.CustomerPhone, request.TicketNumber, request.IsUrgent);

            var result = await _unifiedMessagingService.SendTtktNotificationAsync(
                request.CustomerPhone,
                request.TicketNumber,
                request.IssueDescription,
                request.Resolution,
                request.IsUrgent
            );

            if (result.Success)
            {
                var message = $"TTKT notification sent via {(result.SmsSent && result.WhatsAppSent ? "SMS and WhatsApp" : result.WhatsAppSent ? "WhatsApp" : "SMS")}";
                return this.Success(result, message);
            }
            else
            {
                return this.BadRequest<MessagingResult>(result.ErrorMessage ?? "Failed to send TTKT notification");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TTKT notification");
            return this.Error<MessagingResult>($"Failed to send TTKT notification: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Send custom template message with dynamic parameters
    /// POST /api/messaging/template
    /// </summary>
    [HttpPost("template")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppResult>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WhatsAppResult>>> SendTemplate([FromBody] TemplateMessageRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.To))
            {
                return this.BadRequest<WhatsAppResult>("To is required");
            }

            if (string.IsNullOrEmpty(request.TemplateName))
            {
                return this.BadRequest<WhatsAppResult>("TemplateName is required");
            }

            _logger.LogInformation("Sending WhatsApp template message '{TemplateName}' to {To}", 
                request.TemplateName, request.To);

            var result = await _whatsAppMessagingService.SendTemplateMessageAsync(
                request.To,
                request.TemplateName,
                request.Parameters,
                request.LanguageCode
            );

            if (result.Success)
            {
                return this.Success(result, "Template message sent successfully");
            }
            else
            {
                return this.BadRequest<WhatsAppResult>(result.ErrorMessage ?? "Failed to send template message");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template message");
            return this.Error<WhatsAppResult>($"Failed to send template message: {ex.Message}", 500);
        }
    }
}

// Request DTOs
public class JobUpdateRequest
{
    public string CustomerPhone { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AppointmentDate { get; set; }
    public string? InstallerName { get; set; }
    public bool IsUrgent { get; set; } = false;
}

public class SiOnTheWayRequest
{
    public string CustomerPhone { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string InstallerName { get; set; } = string.Empty;
    public string? EstimatedArrival { get; set; }
    public bool IsUrgent { get; set; } = false;
}

public class TtktRequest
{
    public string CustomerPhone { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public bool IsUrgent { get; set; } = false;
}

public class TemplateMessageRequest
{
    public string To { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Dictionary<string, string>? Parameters { get; set; }
    public string? LanguageCode { get; set; } = "en";
}

