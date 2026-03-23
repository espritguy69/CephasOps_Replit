using CephasOps.Application.Parser.DTOs;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service interface for sending emails
/// </summary>
public interface IEmailSendingService
{
    /// <summary>
    /// Send an email using a template
    /// </summary>
    Task<EmailSendingResultDto> SendEmailWithTemplateAsync(
        Guid templateId,
        string to,
        Dictionary<string, string> placeholders,
        string? cc = null,
        string? bcc = null,
        List<IFormFile>? attachments = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        Guid? emailAccountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an email directly (without template)
    /// </summary>
    Task<EmailSendingResultDto> SendEmailAsync(
        Guid emailAccountId,
        string to,
        string subject,
        string body,
        string? cc = null,
        string? bcc = null,
        List<IFormFile>? attachments = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send reschedule request email
    /// </summary>
    Task<EmailSendingResultDto> SendRescheduleRequestAsync(
        Guid orderId,
        DateTime newDate,
        TimeSpan newWindowFrom,
        TimeSpan newWindowTo,
        string reason,
        string rescheduleType, // "TimeOnly", "DateAndTime", "Assurance"
        Guid? emailAccountId = null,
        Guid? emailTemplateId = null,
        CancellationToken cancellationToken = default);
}

