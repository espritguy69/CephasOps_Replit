using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Email sending endpoints
/// </summary>
[ApiController]
[Route("api/email-sending")]
[Authorize]
public class EmailSendingController : ControllerBase
{
    private readonly IEmailSendingService _emailSendingService;
    private readonly ILogger<EmailSendingController> _logger;

    public EmailSendingController(
        IEmailSendingService emailSendingService,
        ILogger<EmailSendingController> logger)
    {
        _emailSendingService = emailSendingService;
        _logger = logger;
    }

    /// <summary>
    /// Send an email directly
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse<EmailSendingResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailSendingResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmailSendingResultDto>), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<ActionResult<ApiResponse<EmailSendingResultDto>>> SendEmail(
        [FromForm] SendEmailFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var files = Request.Form.Files?.ToList();

        try
        {
            var result = await _emailSendingService.SendEmailAsync(
                dto.EmailAccountId,
                dto.To,
                dto.Subject,
                dto.Body,
                dto.Cc,
                dto.Bcc,
                files,
                dto.RelatedEntityId,
                dto.RelatedEntityType,
                cancellationToken);

            if (!result.Success)
            {
                return this.BadRequest<EmailSendingResultDto>(result.ErrorMessage ?? "Failed to send email");
            }

            return this.Success(result, "Email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return this.InternalServerError<EmailSendingResultDto>($"Failed to send email: {ex.Message}");
        }
    }

    /// <summary>
    /// Send an email using a template
    /// </summary>
    [HttpPost("send-with-template")]
    [ProducesResponseType(typeof(ApiResponse<EmailSendingResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailSendingResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmailSendingResultDto>), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<ActionResult<ApiResponse<EmailSendingResultDto>>> SendEmailWithTemplate(
        [FromForm] SendEmailWithTemplateFormDto dto,
        CancellationToken cancellationToken = default)
    {
        var files = Request.Form.Files?.ToList();

        // Parse placeholders from JSON string
        Dictionary<string, string>? placeholders = null;
        if (!string.IsNullOrWhiteSpace(dto.PlaceholdersJson))
        {
            try
            {
                placeholders = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(dto.PlaceholdersJson);
            }
            catch
            {
                return this.BadRequest<EmailSendingResultDto>("Invalid placeholders JSON format");
            }
        }

        try
        {
            var result = await _emailSendingService.SendEmailWithTemplateAsync(
                dto.TemplateId,
                dto.To,
                placeholders ?? new Dictionary<string, string>(),
                dto.Cc,
                dto.Bcc,
                files,
                dto.RelatedEntityId,
                dto.RelatedEntityType,
                dto.EmailAccountId,
                cancellationToken);

            if (!result.Success)
            {
                return this.BadRequest<EmailSendingResultDto>(result.ErrorMessage ?? "Failed to send email");
            }

            return this.Success(result, "Email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email with template");
            return this.InternalServerError<EmailSendingResultDto>($"Failed to send email: {ex.Message}");
        }
    }

    /// <summary>
    /// Send reschedule request email
    /// </summary>
    [HttpPost("reschedule-request")]
    [ProducesResponseType(typeof(ApiResponse<EmailSendingResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailSendingResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmailSendingResultDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailSendingResultDto>>> SendRescheduleRequest(
        [FromBody] SendRescheduleRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _emailSendingService.SendRescheduleRequestAsync(
                dto.OrderId,
                dto.NewDate,
                dto.NewWindowFrom,
                dto.NewWindowTo,
                dto.Reason,
                dto.RescheduleType ?? "DateAndTime",
                dto.EmailAccountId,
                dto.EmailTemplateId,
                cancellationToken);

            if (!result.Success)
            {
                return this.BadRequest<EmailSendingResultDto>(result.ErrorMessage ?? "Failed to send reschedule request");
            }

            return this.Success(result, "Reschedule request email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reschedule request");
            return this.InternalServerError<EmailSendingResultDto>($"Failed to send reschedule request: {ex.Message}");
        }
    }
}

/// <summary>
/// Form DTO for sending email (supports file uploads)
/// </summary>
public class SendEmailFormDto
{
    public Guid EmailAccountId { get; set; }
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}

/// <summary>
/// Form DTO for sending email with template (supports file uploads)
/// </summary>
public class SendEmailWithTemplateFormDto
{
    public Guid TemplateId { get; set; }
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string? PlaceholdersJson { get; set; } // JSON string of Dictionary<string, string>
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? EmailAccountId { get; set; }
}

