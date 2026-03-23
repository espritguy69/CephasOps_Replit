using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Services;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Companies.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Domain.Common.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text.Json;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service for sending emails via SMTP
/// </summary>
public class EmailSendingService : IEmailSendingService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkflowEngineService _workflowEngineService;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly IPartnerService _partnerService;
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<EmailSendingService> _logger;
    private readonly IEncryptionService _encryptionService;

    public EmailSendingService(
        ApplicationDbContext context,
        IEmailTemplateService emailTemplateService,
        ICurrentUserService currentUserService,
        IWorkflowEngineService workflowEngineService,
        IApprovalWorkflowService approvalWorkflowService,
        IPartnerService partnerService,
        IDepartmentService departmentService,
        ILogger<EmailSendingService> logger,
        IEncryptionService encryptionService)
    {
        _context = context;
        _emailTemplateService = emailTemplateService;
        _currentUserService = currentUserService;
        _workflowEngineService = workflowEngineService;
        _approvalWorkflowService = approvalWorkflowService;
        _partnerService = partnerService;
        _departmentService = departmentService;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task<EmailSendingResultDto> SendEmailWithTemplateAsync(
        Guid templateId,
        string to,
        Dictionary<string, string> placeholders,
        string? cc = null,
        string? bcc = null,
        List<IFormFile>? attachments = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        Guid? emailAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var template = await _emailTemplateService.GetByIdAsync(templateId, null, cancellationToken);
        if (template == null)
        {
            return new EmailSendingResultDto
            {
                Success = false,
                ErrorMessage = $"Email template {templateId} not found"
            };
        }

        // Render template with placeholders
        var (subject, body) = await _emailTemplateService.RenderTemplateAsync(templateId, placeholders, null, cancellationToken);

        // Use template's email account if not specified
        var accountId = emailAccountId ?? template.EmailAccountId ?? Guid.Empty;

        return await SendEmailAsync(
            accountId,
            to,
            subject,
            body,
            cc,
            bcc,
            attachments,
            relatedEntityId,
            relatedEntityType,
            cancellationToken);
    }

    public async Task<EmailSendingResultDto> SendEmailAsync(
        Guid emailAccountId,
        string to,
        string subject,
        string body,
        string? cc = null,
        string? bcc = null,
        List<IFormFile>? attachments = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default)
    {
        var result = new EmailSendingResultDto { EmailAccountId = emailAccountId };

        try
        {
            var account = await _context.EmailAccounts
                .FirstOrDefaultAsync(ea => ea.Id == emailAccountId, cancellationToken);

            if (account == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Email account {emailAccountId} not found";
                return result;
            }

            if (string.IsNullOrWhiteSpace(account.SmtpHost) || !account.SmtpPort.HasValue)
            {
                result.Success = false;
                result.ErrorMessage = "SMTP configuration not set for this email account";
                return result;
            }

            // Create email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                account.SmtpFromName ?? account.Name,
                account.SmtpFromAddress ?? account.Username));
            message.To.Add(MailboxAddress.Parse(to));

            if (!string.IsNullOrWhiteSpace(cc))
            {
                foreach (var ccAddr in cc.Split(';', ','))
                {
                    if (!string.IsNullOrWhiteSpace(ccAddr.Trim()))
                        message.Cc.Add(MailboxAddress.Parse(ccAddr.Trim()));
                }
            }

            if (!string.IsNullOrWhiteSpace(bcc))
            {
                foreach (var bccAddr in bcc.Split(';', ','))
                {
                    if (!string.IsNullOrWhiteSpace(bccAddr.Trim()))
                        message.Bcc.Add(MailboxAddress.Parse(bccAddr.Trim()));
                }
            }

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };

            // Add attachments
            if (attachments != null && attachments.Count > 0)
            {
                foreach (var attachment in attachments)
                {
                    using var stream = attachment.OpenReadStream();
                    await bodyBuilder.Attachments.AddAsync(
                        attachment.FileName,
                        stream,
                        ContentType.Parse(attachment.ContentType),
                        cancellationToken);
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Send via SMTP
            var smtpSecureSocketOptions = account.SmtpUseSsl
                ? SecureSocketOptions.SslOnConnect
                : account.SmtpUseTls
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(
                account.SmtpHost,
                account.SmtpPort.Value,
                smtpSecureSocketOptions,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(account.SmtpUsername))
            {
                var decryptedSmtpPassword = !string.IsNullOrWhiteSpace(account.SmtpPassword)
                    ? DecryptPassword(account.SmtpPassword)
                    : string.Empty;
                await smtpClient.AuthenticateAsync(
                    account.SmtpUsername,
                    decryptedSmtpPassword,
                    cancellationToken);
            }

            var messageId = await smtpClient.SendAsync(message, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);

            // Save sent email record
            var sentEmail = new EmailMessage
            {
                Id = Guid.NewGuid(),
                EmailAccountId = emailAccountId,
                MessageId = message.MessageId ?? messageId,
                FromAddress = account.SmtpFromAddress ?? account.Username,
                ToAddresses = to,
                CcAddresses = cc,
                Subject = subject,
                BodyPreview = body.Length > 500 ? body.Substring(0, 500) : body,
                ReceivedAt = DateTime.UtcNow, // For sent emails, this is actually sent time
                SentAt = DateTime.UtcNow,
                Direction = "Outbound",
                HasAttachments = attachments?.Count > 0,
                ParserStatus = "Sent",
                CompanyId = account.CompanyId,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailMessages.Add(sentEmail);
            await _context.SaveChangesAsync(cancellationToken);

            // Create workflow job for audit trail
            // Note: WorkflowDefinitionId is required, so we need an Email workflow definition first
            // This will be implemented when Email workflow definitions are created
            // For now, email sending is tracked via EmailMessage entity with Direction="Outbound"
            _logger.LogInformation(
                "Email workflow audit: EmailId={EmailId}, RelatedEntity={EntityType}:{EntityId}",
                sentEmail.Id, relatedEntityType ?? "None", relatedEntityId?.ToString() ?? "None");

            result.Success = true;
            result.EmailMessageId = sentEmail.Id;
            result.MessageId = messageId;

            _logger.LogInformation(
                "Email sent successfully: To={To}, Subject={Subject}, MessageId={MessageId}",
                to, subject, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email: To={To}, Subject={Subject}", to, subject);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<EmailSendingResultDto> SendRescheduleRequestAsync(
        Guid orderId,
        DateTime newDate,
        TimeSpan newWindowFrom,
        TimeSpan newWindowTo,
        string reason,
        string rescheduleType,
        Guid? emailAccountId = null,
        Guid? emailTemplateId = null,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            return new EmailSendingResultDto
            {
                Success = false,
                ErrorMessage = $"Order {orderId} not found"
            };
        }

        // Determine which template to use based on reschedule type
        EmailTemplateDto? template = null;
        var companyId = order.CompanyId;
        if (emailTemplateId.HasValue)
        {
            template = await _emailTemplateService.GetByIdAsync(emailTemplateId.Value, companyId, cancellationToken);
        }
        else
        {
            var templateCode = rescheduleType switch
            {
                "TimeOnly" => "RESCHEDULE_TIME_ONLY",
                "DateAndTime" => "RESCHEDULE_DATE_TIME",
                "Assurance" => "ASSURANCE_CABLE_REPULL",
                _ => "RESCHEDULE_DATE_TIME"
            };

            template = await _emailTemplateService.GetByCodeAsync(templateCode, companyId, cancellationToken);
        }

        // Find email account
        if (!emailAccountId.HasValue)
        {
            if (template?.EmailAccountId.HasValue == true)
            {
                emailAccountId = template.EmailAccountId;
            }
            else
            {
                var defaultAccount = await _context.EmailAccounts
                    .Where(ea => ea.IsActive && !string.IsNullOrWhiteSpace(ea.SmtpHost))
                    .FirstOrDefaultAsync(cancellationToken);

                if (defaultAccount == null)
                {
                    return new EmailSendingResultDto
                    {
                        Success = false,
                        ErrorMessage = "No active email account with SMTP configured"
                    };
                }

                emailAccountId = defaultAccount.Id;
            }
        }

        // Determine recipient based on reschedule type
        string recipientEmail;
        if (rescheduleType == "DateAndTime")
        {
            // Get partner contact email
            if (order.PartnerId != Guid.Empty)
            {
                var partner = await _partnerService.GetPartnerByIdAsync(order.PartnerId, order.CompanyId, cancellationToken);
                recipientEmail = partner?.ContactEmail ?? "time-approval@time.com.my"; // Fallback to default
            }
            else
            {
                recipientEmail = "time-approval@time.com.my"; // Default fallback
            }
        }
        else if (rescheduleType == "Assurance")
        {
            // Get department email (if available) or use default
            if (order.DepartmentId.HasValue)
            {
                var department = await _departmentService.GetDepartmentByIdAsync(order.DepartmentId.Value, order.CompanyId, cancellationToken);
                // Note: Department entity doesn't have email field yet, so we'll use a default
                // In the future, this should be: recipientEmail = department?.Email ?? "fe-team@cephasops.com";
                recipientEmail = "fe-team@cephasops.com"; // Default for now
            }
            else
            {
                recipientEmail = "fe-team@cephasops.com"; // Default fallback
            }
        }
        else
        {
            // TimeOnly - send to customer
            recipientEmail = order.CustomerEmail ?? string.Empty;
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                return new EmailSendingResultDto
                {
                    Success = false,
                    ErrorMessage = "Customer email not found for this order"
                };
            }
        }

        // Create reschedule request
        var reschedule = new OrderReschedule
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            RequestedBySource = "Internal",
            RequestedAt = DateTime.UtcNow,
            OriginalDate = order.AppointmentDate,
            OriginalWindowFrom = order.AppointmentWindowFrom,
            OriginalWindowTo = order.AppointmentWindowTo,
            NewDate = newDate,
            NewWindowFrom = newWindowFrom,
            NewWindowTo = newWindowTo,
            Reason = reason,
            Status = "Pending",
            CompanyId = order.CompanyId,
            CreatedAt = DateTime.UtcNow
        };

        _context.OrderReschedules.Add(reschedule);

        // Prepare placeholders for template
        var placeholders = new Dictionary<string, string>
        {
            { "CustomerName", order.CustomerName ?? "Customer" },
            { "OrderNumber", order.ServiceId ?? order.TicketId ?? orderId.ToString() },
            { "OldDate", order.AppointmentDate.ToString("dd MMM yyyy") },
            { "OldTime", $"{order.AppointmentWindowFrom:hh\\:mm} - {order.AppointmentWindowTo:hh\\:mm}" },
            { "NewDate", newDate.ToString("dd MMM yyyy") },
            { "NewTime", $"{newWindowFrom:hh\\:mm} - {newWindowTo:hh\\:mm}" },
            { "Reason", reason },
            { "Address", $"{order.AddressLine1} {order.AddressLine2}".Trim() }
        };

        EmailSendingResultDto sendResult;

        if (template != null)
        {
            // Use template
            sendResult = await SendEmailWithTemplateAsync(
                template.Id,
                recipientEmail,
                placeholders,
                relatedEntityId: orderId,
                relatedEntityType: "Order",
                emailAccountId: emailAccountId,
                cancellationToken: cancellationToken);
        }
        else
        {
            // Fallback: create email without template
            var subject = $"Reschedule Request - Order {order.ServiceId ?? order.TicketId}";
            var emailBody = $@"
<html>
<body>
    <h2>Appointment Reschedule Request</h2>
    <p>Dear {order.CustomerName},</p>
    <p>We would like to reschedule your appointment for order <strong>{order.ServiceId ?? order.TicketId}</strong>.</p>
    
    <h3>Current Appointment:</h3>
    <ul>
        <li>Date: {placeholders["OldDate"]}</li>
        <li>Time: {placeholders["OldTime"]}</li>
    </ul>

    <h3>Proposed New Appointment:</h3>
    <ul>
        <li>Date: {placeholders["NewDate"]}</li>
        <li>Time: {placeholders["NewTime"]}</li>
    </ul>

    <p><strong>Reason:</strong> {reason}</p>

    <p>Please reply to this email to confirm or suggest an alternative time.</p>
    
    <p>Thank you,<br/>CephasOps Team</p>
</body>
</html>";

            sendResult = await SendEmailAsync(
                emailAccountId.Value,
                recipientEmail,
                subject,
                emailBody,
                relatedEntityId: orderId,
                relatedEntityType: "Order",
                cancellationToken: cancellationToken);
        }

        if (sendResult.Success)
        {
            reschedule.ApprovalEmailId = sendResult.EmailMessageId;
            reschedule.ApprovalSource = "EmailSent";
            await _context.SaveChangesAsync(cancellationToken);
        }

        return sendResult;
    }

    /// <summary>
    /// Decrypts a password when needed for authentication
    /// Handles backward compatibility with plain text passwords
    /// </summary>
    private string DecryptPassword(string storedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword))
            return string.Empty;

        // Check if password is encrypted (base64 format check)
        if (!IsEncrypted(storedPassword))
        {
            _logger.LogDebug("Password appears to be plain text, returning as-is (backward compatibility)");
            return storedPassword; // Plain text password (backward compatibility)
        }

        try
        {
            var decrypted = _encryptionService.Decrypt(storedPassword);
            if (string.IsNullOrEmpty(decrypted))
            {
                _logger.LogWarning("Decryption returned empty string, using stored password as fallback");
                return storedPassword; // Fallback if decryption fails
            }
            return decrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt password, using stored value as fallback");
            return storedPassword; // Fallback to stored value if decryption fails
        }
    }

    /// <summary>
    /// Checks if a password string appears to be encrypted
    /// Encrypted passwords are base64 encoded and typically longer
    /// </summary>
    private static bool IsEncrypted(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        // Base64 strings are typically longer and contain only base64 characters
        // Plain text passwords are usually shorter and may contain spaces/special chars
        if (password.Length < 20)
            return false; // Too short to be encrypted

        // Check if it's valid base64 (encrypted passwords are base64)
        try
        {
            Convert.FromBase64String(password);
            // If it's valid base64 and reasonably long, assume it's encrypted
            return password.Length >= 20;
        }
        catch
        {
            // Not valid base64, likely plain text
            return false;
        }
    }
}

