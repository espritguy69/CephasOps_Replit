using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Files.DTOs;
using CephasOps.Application.Files.Services;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using FileEntity = CephasOps.Domain.Files.Entities.File;
using CephasOps.Application.Notifications.DTOs;
using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services.Converters;
using CephasOps.Application.Parser.Settings;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Domain.Common.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Search;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Security.Cryptography;
using System.Text;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service for ingesting emails from configured mailboxes and creating parse sessions
/// </summary>
public class EmailIngestionService : IEmailIngestionService
{
    private readonly ApplicationDbContext _context;
    private readonly IParserTemplateService _parserTemplateService;
    private readonly ITimeExcelParserService _timeExcelParser;
    private readonly IPdfTextExtractionService _pdfTextExtractionService;
    private readonly IPdfOrderParserService _pdfOrderParserService;
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IBuildingService _buildingService;
    private readonly IPaymentService _paymentService;
    private readonly IParserService _parserService;
    private readonly ExcelFormatConverter _excelFormatConverter;
    private readonly IParsedOrderDraftEnrichmentService _enrichmentService;
    private readonly MailSettings _mailSettings;
    private readonly ILogger<EmailIngestionService> _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly IGlobalSettingsService? _globalSettingsService;
    private readonly IOrderService _orderService;
    private readonly IWorkflowEngineService? _workflowEngineService;

    public EmailIngestionService(
        ApplicationDbContext context,
        IParserTemplateService parserTemplateService,
        ITimeExcelParserService timeExcelParser,
        IPdfTextExtractionService pdfTextExtractionService,
        IPdfOrderParserService pdfOrderParserService,
        IFileService fileService,
        INotificationService notificationService,
        IBuildingService buildingService,
        IPaymentService paymentService,
        IParserService parserService,
        ExcelFormatConverter excelFormatConverter,
        IParsedOrderDraftEnrichmentService enrichmentService,
        Microsoft.Extensions.Options.IOptions<MailSettings> mailSettings,
        ILogger<EmailIngestionService> logger,
        IEncryptionService encryptionService,
        IOrderService orderService,
        IGlobalSettingsService? globalSettingsService = null,
        IWorkflowEngineService? workflowEngineService = null)
    {
        _context = context;
        _parserTemplateService = parserTemplateService;
        _timeExcelParser = timeExcelParser;
        _pdfTextExtractionService = pdfTextExtractionService;
        _pdfOrderParserService = pdfOrderParserService;
        _fileService = fileService;
        _notificationService = notificationService;
        _buildingService = buildingService;
        _paymentService = paymentService;
        _parserService = parserService;
        _excelFormatConverter = excelFormatConverter;
        _enrichmentService = enrichmentService;
        _mailSettings = mailSettings.Value;
        _logger = logger;
        _encryptionService = encryptionService;
        _orderService = orderService;
        _globalSettingsService = globalSettingsService;
        _workflowEngineService = workflowEngineService;
    }

    /// <summary>
    /// Get system user ID from GlobalSettings or find first admin user
    /// </summary>
    private async Task<Guid> GetSystemUserIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // First, try to get SystemUserId from GlobalSettings
            if (_globalSettingsService != null)
            {
                var systemUserIdValue = await _globalSettingsService.GetValueAsync<string>("SystemUserId", cancellationToken);
                if (!string.IsNullOrWhiteSpace(systemUserIdValue) && Guid.TryParse(systemUserIdValue, out var systemUserId))
                {
                    _logger.LogInformation("Using system user ID from GlobalSettings: {SystemUserId}", systemUserId);
                    return systemUserId;
                }
            }

            // If not found in settings, query for first admin user
            var adminRole = await _context.Set<CephasOps.Domain.Users.Entities.Role>()
                .FirstOrDefaultAsync(r => r.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase), cancellationToken);

            if (adminRole != null)
            {
                var adminUser = await _context.Set<CephasOps.Domain.Users.Entities.UserRole>()
                    .Where(ur => ur.RoleId == adminRole.Id)
                    .Join(_context.Set<CephasOps.Domain.Users.Entities.User>(),
                        ur => ur.UserId,
                        u => u.Id,
                        (ur, u) => u)
                    .Where(u => u.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);

                if (adminUser != null)
                {
                    _logger.LogInformation("Using first active admin user as system user: {SystemUserId} ({Email})", adminUser.Id, adminUser.Email);
                    return adminUser.Id;
                }
            }

            // Fallback to Guid.Empty with warning
            _logger.LogWarning("No system user ID found in GlobalSettings and no admin user found. Using Guid.Empty as fallback.");
            return Guid.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system user ID. Using Guid.Empty as fallback.");
            return Guid.Empty;
        }
    }

    public async Task<EmailIngestionResultDto> IngestEmailsAsync(Guid emailAccountId, CancellationToken cancellationToken = default)
    {
        var result = new EmailIngestionResultDto { EmailAccountId = emailAccountId };

        try
        {
            var account = await _context.EmailAccounts
                .FirstOrDefaultAsync(ea => ea.Id == emailAccountId && ea.IsActive, cancellationToken);

            if (account == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Email account {emailAccountId} not found or inactive";
                return result;
            }

            result.EmailAccountName = account.Name;

            _logger.LogInformation("Starting email ingestion for account {AccountName} ({AccountId})", 
                account.Name, account.Id);

            var provider = account.Provider?.ToUpperInvariant() ?? "POP3";
            var secureSocketOptions = account.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            List<MimeMessage> messages;

            if (provider == "IMAP")
            {
                messages = await FetchEmailsViaImapAsync(account, secureSocketOptions, cancellationToken);
            }
            else
            {
                messages = await FetchEmailsViaPop3Async(account, secureSocketOptions, cancellationToken);
            }

            result.EmailsFetched = messages.Count;
            _logger.LogInformation("Fetched {Count} emails from {AccountName}", messages.Count, account.Name);

            // Process each email
            foreach (var message in messages)
            {
                try
                {
                    var processed = await ProcessEmailAsync(account, message, cancellationToken);
                    if (processed)
                    {
                        result.ParseSessionsCreated++;
                        result.ProcessedEmails.Add(message.Subject ?? "(no subject)");
                    }
                }
                catch (Exception ex)
                {
                    var subject = message.Subject ?? "(no subject)";
                    var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? "unknown";
                    
                    _logger.LogError(ex, 
                        "Error processing email: {Subject} from {From}. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                        subject, fromAddress, ex.GetType().Name, ex.Message, ex.StackTrace);
                    
                    if (ex.InnerException != null)
                    {
                        _logger.LogError(ex.InnerException, 
                            "Inner exception for email {Subject}: {InnerExceptionType}, {InnerMessage}",
                            subject, ex.InnerException.GetType().Name, ex.InnerException.Message);
                    }
                    
                    result.Errors++;
                    
                    // Clear the change tracker to prevent failed entities from being saved later
                    // This ensures the duplicate/failed email doesn't get re-attempted on SaveChanges
                    foreach (var entry in _context.ChangeTracker.Entries().ToList())
                    {
                        if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                        {
                            entry.State = EntityState.Detached;
                        }
                    }
                    
                    // Re-attach the account so we can update LastPolledAt
                    _context.Attach(account);
                }
            }

            // Update last polled timestamp
            account.LastPolledAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            result.Success = true;
            _logger.LogInformation("Email ingestion completed for {AccountName}: {Fetched} fetched, {Sessions} sessions created, {Errors} errors",
                account.Name, result.EmailsFetched, result.ParseSessionsCreated, result.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email ingestion failed for account {AccountId}", emailAccountId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<List<EmailIngestionResultDto>> IngestAllEmailsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<EmailIngestionResultDto>();

        var activeAccounts = await _context.EmailAccounts
            .Where(ea => ea.IsActive)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Starting email ingestion for {Count} active accounts", activeAccounts.Count);

        foreach (var account in activeAccounts)
        {
            var result = await IngestEmailsAsync(account.Id, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public async Task<EmailIngestionResultDto> TriggerPollAsync(Guid emailAccountId, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("TriggerPollAsync called: EmailAccountId={EmailAccountId}, CompanyId={CompanyId}", 
            emailAccountId, companyId);

        // Multi-tenant SaaS — CompanyId filter required.
        var account = await _context.EmailAccounts
            .FirstOrDefaultAsync(ea => ea.Id == emailAccountId && !ea.IsDeleted && ea.CompanyId == companyId, cancellationToken);

        if (account == null)
        {
            _logger.LogWarning("Email account not found: EmailAccountId={EmailAccountId}, CompanyId={CompanyId}", 
                emailAccountId, companyId);
            return new EmailIngestionResultDto
            {
                EmailAccountId = emailAccountId,
                Success = false,
                ErrorMessage = "Email account not found or access denied"
            };
        }

        if (!account.IsActive)
        {
            _logger.LogWarning("Email account is inactive: EmailAccountId={EmailAccountId}, Name={Name}", 
                emailAccountId, account.Name);
            return new EmailIngestionResultDto
            {
                EmailAccountId = emailAccountId,
                EmailAccountName = account.Name,
                Success = false,
                ErrorMessage = $"Email account '{account.Name}' is inactive"
            };
        }

        _logger.LogInformation("Email account found and active: EmailAccountId={EmailAccountId}, Name={Name}, CompanyId={CompanyId}", 
            emailAccountId, account.Name, account.CompanyId);

        return await IngestEmailsAsync(emailAccountId, cancellationToken);
    }

    private async Task<List<MimeMessage>> FetchEmailsViaPop3Async(
        EmailAccount account, 
        SecureSocketOptions secureSocketOptions,
        CancellationToken cancellationToken)
    {
        var messages = new List<MimeMessage>();

        using var client = new Pop3Client();
        await client.ConnectAsync(account.Host!, account.Port!.Value, secureSocketOptions, cancellationToken);
        var decryptedPassword = DecryptPassword(account.Password);
        await client.AuthenticateAsync(account.Username, decryptedPassword, cancellationToken);

        var count = client.Count;
        _logger.LogDebug("POP3 mailbox has {Count} messages", count);

        // Fetch last 50 messages (or all if less)
        var startIndex = Math.Max(0, count - 50);
        var companyId = account.CompanyId;
        
        for (int i = startIndex; i < count; i++)
        {
            try
            {
                var message = await client.GetMessageAsync(i, cancellationToken);
                
                // Check if we've already processed this message (by CompanyId + MessageId)
                // Handle nullable CompanyId properly - NULL = NULL is false in SQL
                var messageId = message.MessageId ?? $"{message.Date:yyyyMMddHHmmss}-{message.Subject}";
                var exists = await _context.EmailMessages
                    .AnyAsync(em => (companyId == null ? em.CompanyId == null : em.CompanyId == companyId) 
                                    && em.MessageId == messageId, cancellationToken);

                if (!exists)
                {
                    messages.Add(message);
                }
                else
                {
                    _logger.LogDebug("Skipping already processed email: {MessageId}", messageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch message at index {Index}", i);
            }
        }

        await client.DisconnectAsync(true, cancellationToken);
        return messages;
    }

    private async Task<List<MimeMessage>> FetchEmailsViaImapAsync(
        EmailAccount account,
        SecureSocketOptions secureSocketOptions,
        CancellationToken cancellationToken)
    {
        var messages = new List<MimeMessage>();

        using var client = new ImapClient();
        await client.ConnectAsync(account.Host!, account.Port!.Value, secureSocketOptions, cancellationToken);
        var decryptedPassword = DecryptPassword(account.Password);
        await client.AuthenticateAsync(account.Username, decryptedPassword, cancellationToken);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        _logger.LogDebug("IMAP inbox has {Count} messages", inbox.Count);

        // Search for unseen messages or messages from the last 7 days
        var query = SearchQuery.DeliveredAfter(DateTime.UtcNow.AddDays(-7));
        var uids = await inbox.SearchAsync(query, cancellationToken);
        var companyId = account.CompanyId;

        foreach (var uid in uids.Take(50))
        {
            try
            {
                var message = await inbox.GetMessageAsync(uid, cancellationToken);
                
                // Check if we've already processed this message (by CompanyId + MessageId)
                // Handle nullable CompanyId properly - NULL = NULL is false in SQL
                var messageId = message.MessageId ?? $"{message.Date:yyyyMMddHHmmss}-{message.Subject}";
                var exists = await _context.EmailMessages
                    .AnyAsync(em => (companyId == null ? em.CompanyId == null : em.CompanyId == companyId) 
                                    && em.MessageId == messageId, cancellationToken);

                if (!exists)
                {
                    messages.Add(message);
                }
                else
                {
                    _logger.LogDebug("Skipping already processed email: {MessageId}", messageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch message with UID {Uid}", uid);
            }
        }

        await client.DisconnectAsync(true, cancellationToken);
        return messages;
    }

    private async Task<bool> ProcessEmailAsync(EmailAccount account, MimeMessage message, CancellationToken cancellationToken)
    {
        var messageId = message.MessageId ?? $"{message.Date:yyyyMMddHHmmss}-{message.Subject?.GetHashCode()}";
        var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? "unknown";
        var subject = message.Subject ?? "(no subject)";
        var companyId = account.CompanyId;

        _logger.LogInformation("Processing email: {Subject} from {From}", subject, fromAddress);

        // Check if this email was already processed (by CompanyId + MessageId)
        // Handle nullable CompanyId properly - NULL = NULL is false in SQL
        var alreadyExists = await _context.EmailMessages
            .AnyAsync(em => (companyId == null ? em.CompanyId == null : em.CompanyId == companyId) 
                            && em.MessageId == messageId, cancellationToken);
        
        if (alreadyExists)
        {
            _logger.LogDebug("Email already processed, skipping: {MessageId}", messageId);
            return false;
        }

        // Create EmailMessage record
        // Ensure ReceivedAt is always UTC (PostgreSQL requires this for timestamp with time zone)
        var receivedAt = message.Date.UtcDateTime;
        if (receivedAt.Kind == DateTimeKind.Unspecified)
        {
            receivedAt = DateTime.SpecifyKind(receivedAt, DateTimeKind.Utc);
        }
        
        // Extract full body text and HTML
        var textBody = message.TextBody ?? "";
        var htmlBody = message.HtmlBody ?? "";
        
        // If only HTML is available, strip tags to create text version
        if (string.IsNullOrEmpty(textBody) && !string.IsNullOrEmpty(htmlBody))
        {
            textBody = StripHtmlTags(htmlBody);
        }
        
        // Create preview (first 500 chars)
        var fullBody = textBody.Length > 0 ? textBody : htmlBody;
        var bodyPreview = fullBody.Length > 500 ? fullBody.Substring(0, 500) : fullBody;
        
        // Set expiry based on configured retention hours
        var retentionHours = _mailSettings.RetentionHours;
        var expiresAt = receivedAt.AddHours(retentionHours);
        
        var emailMessage = new EmailMessage
        {
            CompanyId = companyId,
            EmailAccountId = account.Id,
            MessageId = messageId,
            FromAddress = fromAddress,
            ToAddresses = string.Join(";", message.To.Mailboxes.Select(m => m.Address)),
            CcAddresses = string.Join(";", message.Cc.Mailboxes.Select(m => m.Address)),
            Subject = subject,
            BodyPreview = bodyPreview,
            BodyText = textBody,
            BodyHtml = htmlBody,
            ReceivedAt = receivedAt,
            HasAttachments = message.Attachments.Any(),
            ParserStatus = "Pending",
            ExpiresAt = expiresAt
        };

        // Check for VIP email
        var vipEmail = await _context.VipEmails
            .FirstOrDefaultAsync(v => v.EmailAddress.ToLower() == fromAddress.ToLower() && v.IsActive, cancellationToken);
        
        if (vipEmail != null)
        {
            emailMessage.IsVip = true;
            emailMessage.MatchedVipEmailId = vipEmail.Id;
            _logger.LogInformation("Email from VIP sender: {From}", fromAddress);
        }

        _context.EmailMessages.Add(emailMessage);
        await _context.SaveChangesAsync(cancellationToken);

        // Store all attachments for mail viewer (after email is saved)
        await StoreEmailAttachmentsAsync(emailMessage, message, companyId, expiresAt, cancellationToken);

        // Send VIP notifications if applicable
        if (vipEmail != null)
        {
            await SendVipEmailNotificationsAsync(vipEmail, emailMessage, account.CompanyId, cancellationToken);
        }

        // Extract attachments first to determine processing strategy
        var attachmentFiles = await ExtractAttachmentsAsync(message, cancellationToken);
        
        // ✅ FIX: If no attachments found from MIME message, check stored files (fallback for emails where MIME parsing missed attachments)
        if (!attachmentFiles.Any() && emailMessage.HasAttachments)
        {
            _logger.LogInformation("No attachments extracted from MIME message, checking stored files for email {EmailId}", emailMessage.Id);
            var storedFiles = await GetStoredExcelPdfFilesAsync(emailMessage.Id, companyId, cancellationToken);
            attachmentFiles.AddRange(storedFiles);
            if (storedFiles.Any())
            {
                _logger.LogInformation("Found {Count} Excel/PDF file(s) from stored files for email {EmailId}", storedFiles.Count, emailMessage.Id);
            }
        }
        
        // Determine attachment types
        var hasExcelAttachment = attachmentFiles.Any(f => 
            Path.GetExtension(f.FileName).ToLowerInvariant() is ".xlsx" or ".xls");
        var hasPdfAttachment = attachmentFiles.Any(f => 
            Path.GetExtension(f.FileName).ToLowerInvariant() == ".pdf");
        
        // ✅ FIX: Try to match a parser template FIRST (this is the source of truth)
        // Template matching determines what type of email this is and what attachments it expects
        var hasAttachments = message.Attachments.Any();
        var matchedTemplate = await _parserTemplateService.FindMatchingTemplateAsync(
            fromAddress, subject, account.CompanyId, account.Id, hasAttachments, cancellationToken);

        if (matchedTemplate == null && account.DefaultParserTemplateId.HasValue)
        {
            matchedTemplate = await _parserTemplateService.GetByIdAsync(
                account.DefaultParserTemplateId.Value, cancellationToken);

            if (matchedTemplate != null && matchedTemplate.IsActive == false)
            {
                matchedTemplate = null;
            }
        }
        
        // ✅ FIX: Check if matched template expects attachments and what types
        var templateExpectsAttachments = !string.IsNullOrEmpty(matchedTemplate?.ExpectedAttachmentTypes);
        var templateExpectsExcel = templateExpectsAttachments && matchedTemplate != null && (
            matchedTemplate.ExpectedAttachmentTypes!.Contains(".xls", StringComparison.OrdinalIgnoreCase) ||
            matchedTemplate.ExpectedAttachmentTypes!.Contains(".xlsx", StringComparison.OrdinalIgnoreCase)
        );
        var templateExpectsPdf = templateExpectsAttachments && matchedTemplate != null && 
            matchedTemplate.ExpectedAttachmentTypes!.Contains(".pdf", StringComparison.OrdinalIgnoreCase);
        
        // Check if this is an ASSURANCE email (subject contains "APPMT" or matches TIME_ASSURANCE template)
        var isAssuranceEmail = subject.Contains("APPMT", StringComparison.OrdinalIgnoreCase) ||
                               matchedTemplate?.Code == "TIME_ASSURANCE";

        // Create ParseSession
        var parseSession = new ParseSession
        {
            CompanyId = account.CompanyId,
            EmailMessageId = emailMessage.Id,
            Status = "Pending",
            ParserTemplateId = matchedTemplate?.Id,
            SourceType = "Email",
            SourceDescription = $"Email: {subject}",
            CreatedAt = DateTime.UtcNow
        };

        _context.ParseSessions.Add(parseSession);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Wrap main processing in try-catch to capture detailed errors
        try
        {

        // ✅ EARLY EXIT: Skip emails based on TEMPLATE requirements, not hardcoded logic
        // If a template matches, trust the template configuration
        // If template expects Excel/PDF and we don't have them, skip
        // If template is ASSURANCE (no attachments expected), process it
        // If no template matches and no attachments, skip
        if (matchedTemplate != null)
        {
            // Template matched - check if it expects attachments
            if (templateExpectsAttachments)
            {
                // Template expects attachments - check if we have the right type
                var hasRequiredAttachment = (templateExpectsExcel && hasExcelAttachment) || 
                                           (templateExpectsPdf && hasPdfAttachment);
                
                if (!hasRequiredAttachment)
                {
                    emailMessage.ParserStatus = "Skipped";
                    parseSession.Status = "Skipped";
                    parseSession.SourceDescription = $"Email: {subject} | Template {matchedTemplate.Code} expects {matchedTemplate.ExpectedAttachmentTypes} but none found";
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Skipping email {Subject} - Template {TemplateCode} expects {ExpectedTypes} but attachments not found", 
                        subject, matchedTemplate.Code, matchedTemplate.ExpectedAttachmentTypes);
                    return false;
                }
            }
            // If template doesn't expect attachments (like ASSURANCE), continue processing
            _logger.LogInformation("Template {TemplateCode} matched for email {Subject}. Expected attachments: {ExpectedTypes}, Found: Excel={HasExcel}, PDF={HasPdf}", 
                matchedTemplate.Code, subject, matchedTemplate.ExpectedAttachmentTypes ?? "None", hasExcelAttachment, hasPdfAttachment);
        }
        else
        {
            // No template matched - only process if we have Excel/PDF attachments or it's ASSURANCE
            if (!hasExcelAttachment && !hasPdfAttachment && !isAssuranceEmail)
            {
                emailMessage.ParserStatus = "Skipped";
                parseSession.Status = "Skipped";
                parseSession.SourceDescription = $"Email: {subject} | No template matched and no actionable attachments";
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Skipping email {Subject} - no template matched and no Excel/PDF attachments", subject);
                return false;
            }
        }
        
        // ✅ Route to special processing based on matched template Code (template-based routing)
        if (matchedTemplate != null)
        {
            var templateCode = matchedTemplate.Code.ToUpperInvariant();
            var processed = false;

            switch (templateCode)
            {
                case "TIME_RESCHEDULE":
                    // Reschedule emails with Excel attachments
                    if (attachmentFiles.Any(f => Path.GetExtension(f.FileName).ToLowerInvariant() == ".xlsx" || Path.GetExtension(f.FileName).ToLowerInvariant() == ".xls"))
                    {
                        _logger.LogInformation("Reschedule email with Excel attachment detected via template {TemplateCode}", templateCode);
                        processed = await ProcessRescheduleEmailAsync(emailMessage, message.Subject ?? "", attachmentFiles, account.CompanyId, cancellationToken);
                    }
                    break;

                case "TIME_WITHDRAWAL":
                    // Withdrawal notification emails
                    _logger.LogInformation("Withdrawal email detected via template {TemplateCode}", templateCode);
                    await ProcessWithdrawalEmailAsync(emailMessage, textBody ?? htmlBody ?? "", account.CompanyId, cancellationToken);
                    processed = true;
                    break;

                case "TIME_CUSTOMER_UNCONTACTABLE":
                    // Customer Uncontactable notification emails
                    _logger.LogInformation("Customer Uncontactable email detected via template {TemplateCode}", templateCode);
                    await ProcessCustomerUncontactableEmailAsync(emailMessage, message.Subject ?? "", textBody ?? htmlBody ?? "", account.CompanyId, cancellationToken);
                    processed = true;
                    break;

                case "TIME_RFB":
                    // RFB meeting notification emails
                    _logger.LogInformation("RFB email detected via template {TemplateCode}", templateCode);
                    await ProcessRfbEmailAsync(emailMessage, message.Subject ?? "", textBody ?? htmlBody ?? "", account.CompanyId, cancellationToken);
                    processed = true;
                    break;

                case "TIME_PAYMENT_ADVICE":
                    // Payment advice emails with PDF attachments
                    if (attachmentFiles.Any(f => Path.GetExtension(f.FileName).ToLowerInvariant() == ".pdf"))
                    {
                        _logger.LogInformation("Payment Advice email with PDF attachment detected via template {TemplateCode}", templateCode);
                        processed = await ProcessPaymentAdviceEmailAsync(emailMessage, message.Subject ?? "", attachmentFiles, account.CompanyId, cancellationToken);
                    }
                    break;
            }

            if (processed)
            {
                // Mark email as processed
                emailMessage.ParserStatus = "Processed";
                parseSession.Status = "Completed";
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
        }

        // ✅ UNIFIED PARSING STRATEGY:
        // 1. If Excel attachments exist: Use SyncfusionExcelParserService (100% accuracy)
        // 2. If PDF attachments exist: Use PdfOrderParserService (extract text from PDF, then parse)
        // 3. Email body: Always parse using PdfOrderParserService (works for all emails, not just assurance)
        // 4. If both attachments and body exist, parse both and create separate drafts
        
        var draftsFromAttachments = 0;
        Guid? snapshotFileId = null;
        
        // Process attachments (Excel and PDF)
        if (attachmentFiles.Any())
        {
            _logger.LogInformation("Found {Count} attachments in email {Subject}, processing with unified parser...", 
                attachmentFiles.Count, subject);
            
            foreach (var file in attachmentFiles)
            {
                try
                {
                        // Compute file hash for duplicate detection
                        var fileHash = await ComputeFileHashAsync(file, cancellationToken);
                        
                        // Check for duplicate file (same hash, same company)
                        var duplicateDraft = await _context.ParsedOrderDrafts
                            .Where(d => d.CompanyId == account.CompanyId 
                                     && d.FileHash == fileHash 
                                     && d.FileHash != null
                                     && d.CreatedAt >= DateTime.UtcNow.AddDays(-30)) // Only check last 30 days
                            .OrderByDescending(d => d.CreatedAt)
                            .FirstOrDefaultAsync(cancellationToken);
                        
                        if (duplicateDraft != null)
                        {
                            _logger.LogWarning(
                                "⚠️ DUPLICATE FILE DETECTED: File {FileName} (Hash: {Hash}) was already processed in draft {DraftId} on {ProcessedDate}. " +
                                "Skipping to prevent duplicate order creation. Original draft ServiceId: {ServiceId}",
                                file.FileName, fileHash, duplicateDraft.Id, duplicateDraft.CreatedAt, duplicateDraft.ServiceId);
                            
                            // Create a placeholder draft with duplicate flag for visibility
                            var duplicatePlaceholder = new ParsedOrderDraft
                            {
                                Id = Guid.NewGuid(),
                                CompanyId = account.CompanyId ?? Guid.Empty,
                                ParseSessionId = parseSession.Id,
                                SourceFileName = file.FileName,
                                FileHash = fileHash,
                                ValidationStatus = "Rejected",
                                ValidationNotes = $"Duplicate file detected. Original draft: {duplicateDraft.Id} (processed {duplicateDraft.CreatedAt:yyyy-MM-dd HH:mm:ss}). ServiceId: {duplicateDraft.ServiceId}",
                                ConfidenceScore = 0.0m,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.ParsedOrderDrafts.Add(duplicatePlaceholder);
                            continue; // Skip processing this file
                        }
                        
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        ParsedOrderDraft? attachmentDraft = null;
                        
                        if (extension == ".xlsx" || extension == ".xls")
                        {
                            // Parse Excel file directly using TIME Excel parser (no PDF conversion needed)
                            attachmentDraft = await ParseExcelAttachmentAsync(
                                file, parseSession.Id, account.CompanyId ?? Guid.Empty, DateTime.UtcNow, fileHash, matchedTemplate?.AutoApprove ?? false, cancellationToken);
                            
                            // Save first Excel file as snapshot (original file, not converted to PDF)
                            if (snapshotFileId == null && attachmentDraft != null)
                            {
                                try
                                {
                                    using var stream = new MemoryStream();
                                    await file.CopyToAsync(stream, cancellationToken);
                                    var excelFile = new InMemoryFormFile(
                                        stream.ToArray(), file.FileName, file.ContentType);
                                    
                                    var uploadDto = new FileUploadDto
                                    {
                                        File = excelFile,
                                        Module = "Parser",
                                        EntityId = parseSession.Id,
                                        EntityType = "ParseSession"
                                    };
                                    var savedFile = await _fileService.UploadFileAsync(
                                        uploadDto, account.CompanyId ?? Guid.Empty, Guid.Empty, cancellationToken);
                                    snapshotFileId = savedFile.Id;
                                    parseSession.SnapshotFileId = snapshotFileId;
                                    
                                    _logger.LogInformation("Saved Excel file as snapshot: {FileName}", file.FileName);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to save Excel snapshot for attachment {FileName}", file.FileName);
                                }
                            }
                        }
                        else if (extension == ".pdf")
                        {
                            // ✅ OPTIMIZATION: For assurance emails (APPMT in subject), skip PDF parsing
                            // Email body contains all necessary information and provides better confidence scores
                            if (isAssuranceEmail)
                            {
                                _logger.LogInformation("ASSURANCE email detected with PDF attachment. Skipping PDF parsing - will parse from email body only for better accuracy. File: {FileName}", file.FileName);
                                
                                // Save PDF as snapshot for reference only (don't parse it)
                                if (snapshotFileId == null)
                                {
                                    try
                                    {
                                        using var stream = new MemoryStream();
                                        await file.CopyToAsync(stream, cancellationToken);
                                        var pdfFile = new InMemoryFormFile(
                                            stream.ToArray(), file.FileName, "application/pdf");
                                        
                                        var uploadDto = new FileUploadDto
                                        {
                                            File = pdfFile,
                                            Module = "Parser",
                                            EntityId = parseSession.Id,
                                            EntityType = "ParseSession"
                                        };
                                        var savedFile = await _fileService.UploadFileAsync(
                                            uploadDto, account.CompanyId ?? Guid.Empty, Guid.Empty, cancellationToken);
                                        snapshotFileId = savedFile.Id;
                                        parseSession.SnapshotFileId = snapshotFileId;
                                        
                                        _logger.LogInformation("Saved PDF as snapshot for reference (not parsed): {FileName}", file.FileName);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to save PDF snapshot for attachment {FileName}", file.FileName);
                                    }
                                }
                                
                                // Skip PDF parsing - will be handled by body parsing logic below
                                attachmentDraft = null;
                            }
                            else
                            {
                                // ✅ Parse PDF attachment using PDF parser service (non-assurance emails)
                                attachmentDraft = await ParsePdfAttachmentAsync(
                                    file, parseSession.Id, account.CompanyId ?? Guid.Empty, DateTime.UtcNow, fileHash, matchedTemplate?.AutoApprove ?? false, cancellationToken);
                                
                                // Save first PDF as snapshot
                                if (snapshotFileId == null)
                                {
                                    try
                                    {
                                        using var stream = new MemoryStream();
                                        await file.CopyToAsync(stream, cancellationToken);
                                        var pdfFile = new InMemoryFormFile(
                                            stream.ToArray(), file.FileName, "application/pdf");
                                        
                                        var uploadDto = new FileUploadDto
                                        {
                                            File = pdfFile,
                                            Module = "Parser",
                                            EntityId = parseSession.Id,
                                            EntityType = "ParseSession"
                                        };
                                        var savedFile = await _fileService.UploadFileAsync(
                                            uploadDto, account.CompanyId ?? Guid.Empty, Guid.Empty, cancellationToken);
                                        snapshotFileId = savedFile.Id;
                                        parseSession.SnapshotFileId = snapshotFileId;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to save PDF snapshot for attachment {FileName}", file.FileName);
                                    }
                                }
                            }
                        }
                        
                    if (attachmentDraft != null)
                    {
                        _context.ParsedOrderDrafts.Add(attachmentDraft);
                        draftsFromAttachments++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing attachment {FileName}: {Error}", 
                        file.FileName, ex.Message);
                }
            }
            
            await _context.SaveChangesAsync(cancellationToken);
            
            // ✅ AUTO-APPROVAL: If template has AutoApprove enabled, automatically approve drafts with ValidationStatus = "Valid"
            if (matchedTemplate?.AutoApprove == true && draftsFromAttachments > 0)
            {
                _logger.LogInformation("Template {TemplateCode} has AutoApprove enabled. Checking drafts for auto-approval...", matchedTemplate.Code);
                
                // Get all drafts for this session that are ready for auto-approval
                var draftsToApprove = await _context.ParsedOrderDrafts
                    .Where(d => d.ParseSessionId == parseSession.Id && d.ValidationStatus == "Valid")
                    .ToListAsync(cancellationToken);
                
                if (draftsToApprove.Any())
                {
                    _logger.LogInformation("Found {Count} draft(s) ready for auto-approval", draftsToApprove.Count);
                    
                    // Get system user ID from settings or admin user
                    var systemUserId = await GetSystemUserIdAsync(cancellationToken);
                    
                    foreach (var draft in draftsToApprove)
                    {
                        try
                        {
                            _logger.LogInformation("Auto-approving draft {DraftId} (ServiceId: {ServiceId})", draft.Id, draft.ServiceId);
                            
                            // Auto-approve the draft (creates the order)
                            var approveDto = new ApproveParsedOrderDto
                            {
                                ValidationNotes = "Auto-approved via email parser template"
                            };
                            
                            await _parserService.ApproveParsedOrderAsync(
                                draft.Id, 
                                approveDto, 
                                draft.CompanyId ?? Guid.Empty, 
                                systemUserId, 
                                cancellationToken);
                            
                            _logger.LogInformation("✅ Successfully auto-approved draft {DraftId}, Order created", draft.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to auto-approve draft {DraftId}: {Error}", draft.Id, ex.Message);
                            // Update draft to NeedsReview if auto-approval fails
                            draft.ValidationStatus = "NeedsReview";
                            draft.ValidationNotes = $"Auto-approval failed: {ex.Message}. Please review manually.";
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                    }
                }
            }
            
            _logger.LogInformation("Processed {Count} attachments, created {DraftCount} draft(s)", 
                attachmentFiles?.Count ?? 0, draftsFromAttachments);
        }
        
        // ✅ Parse email body for all emails using PdfOrderParserService
        // This extracts order information from email body text (subject + body content)
        // If attachments were already parsed, body parsing provides additional data
        // If no attachments, body parsing is the primary source
        var bodyText = !string.IsNullOrEmpty(textBody) ? textBody : StripHtmlTags(htmlBody ?? string.Empty);
        var hasBodyContent = !string.IsNullOrWhiteSpace(bodyText) && bodyText.Length > 50; // Minimum meaningful content
        var bodyDraftCreated = false;
        
        if (hasBodyContent)
        {
            // Only parse body if no attachments were found, or if this is an assurance email
            // For assurance emails, body parsing is preferred even if PDF attachments exist
            var shouldParseBody = draftsFromAttachments == 0 || isAssuranceEmail;
            
            if (shouldParseBody)
            {
                _logger.LogInformation("Parsing email body for {EmailType}: {Subject}", 
                    isAssuranceEmail ? "ASSURANCE email" : "email", subject);
                
                // Combine subject and body for parsing (subject often contains TTKT/AWO info)
                var fullText = $"{message.Subject} {bodyText}";
                
                // Parse using PDF parser service (it can parse any text, not just PDFs)
                var parsedData = _pdfOrderParserService.ParseFromText(fullText, message.Subject ?? "Email");
                
                // Only create body draft if we got meaningful data (ServiceId or other key fields)
                var hasMeaningfulData = !string.IsNullOrEmpty(parsedData.ServiceId) || 
                                       !string.IsNullOrEmpty(parsedData.CustomerName) ||
                                       !string.IsNullOrEmpty(parsedData.CustomerPhone) ||
                                       !string.IsNullOrEmpty(parsedData.ServiceAddress) ||
                                       !string.IsNullOrEmpty(parsedData.TicketId);
                
                if (hasMeaningfulData)
                {
                    // Create draft from parsed body data
                    var bodyDraft = new ParsedOrderDraft
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = account.CompanyId ?? Guid.Empty,
                        ParseSessionId = parseSession.Id,
                        SourceFileName = $"Email Body: {subject}",
                        ServiceId = parsedData.ServiceId,
                        TicketId = parsedData.TicketId,
                        AwoNumber = parsedData.AwoNumber,
                        CustomerName = parsedData.CustomerName ?? parsedData.ContactPerson,
                        CustomerPhone = parsedData.CustomerPhone,
                        CustomerEmail = parsedData.CustomerEmail,
                        AdditionalContactNumber = parsedData.AdditionalContactNumber, // ✅ Map Additional Contact Number
                        Issue = parsedData.Issue, // ✅ Map Issue for Assurance orders
                        AddressText = parsedData.ServiceAddress,
                        OldAddress = parsedData.OldAddress,
                        AppointmentDate = parsedData.AppointmentDateTime, // Will be normalized by enrichment service
                        AppointmentWindow = parsedData.AppointmentWindow,
                        OrderTypeHint = parsedData.OrderTypeHint ?? (isAssuranceEmail ? "ASSURANCE" : "UNKNOWN"),
                        OrderTypeCode = parsedData.OrderTypeCode ?? (isAssuranceEmail ? "ASSURANCE" : "UNKNOWN"),
                        PackageName = parsedData.PackageName,
                        Bandwidth = parsedData.Bandwidth,
                        OnuSerialNumber = parsedData.OnuSerialNumber,
                        OnuPassword = parsedData.OnuPassword,
                        Username = parsedData.Username, // ✅ Extract Username from email body
                        Password = parsedData.Password, // ✅ Extract Password from email body
                        VoipServiceId = parsedData.VoipServiceId,
                        ConfidenceScore = parsedData.ConfidenceScore,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    // Build remarks with PartnerCode and any parsed remarks
                    var remarksParts = new List<string>();
                    if (!string.IsNullOrEmpty(parsedData.PartnerCode) && parsedData.PartnerCode != "TIME")
                    {
                        remarksParts.Add($"[PartnerCode: {parsedData.PartnerCode}]");
                    }
                    if (!string.IsNullOrEmpty(parsedData.Remarks))
                    {
                        remarksParts.Add(parsedData.Remarks);
                    }
                    if (remarksParts.Any())
                    {
                        bodyDraft.Remarks = string.Join(" | ", remarksParts);
                    }
                    
                    // Set validation status based on parsed data and template's AutoApprove setting
                    if (parsedData.ConfidenceScore >= 0.7m && !string.IsNullOrEmpty(parsedData.ServiceId))
                    {
                        // If template has AutoApprove enabled and parsing succeeded, mark as Valid (ready for auto-approval)
                        // Otherwise, mark as Pending (requires manual review)
                        bodyDraft.ValidationStatus = (matchedTemplate?.AutoApprove ?? false) ? "Valid" : "Pending";
                        bodyDraft.ValidationNotes = $"Successfully parsed from email body. Order Type: {parsedData.OrderTypeCode}";
                    }
                    else if (parsedData.ConfidenceScore >= 0.5m)
                    {
                        bodyDraft.ValidationStatus = "NeedsReview";
                        bodyDraft.ValidationNotes = "Moderate confidence parsing from email body - please verify all fields";
                    }
                    else
                    {
                        bodyDraft.ValidationStatus = "NeedsReview";
                        bodyDraft.ValidationNotes = "Low confidence parsing from email body - please verify all fields";
                    }
                    
                    _context.ParsedOrderDrafts.Add(bodyDraft);
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    draftsFromAttachments++;
                    parseSession.ParsedOrdersCount = draftsFromAttachments;
                    bodyDraftCreated = true;
                    
                    _logger.LogInformation("Created draft from email body: ServiceId={ServiceId}, TicketId={TicketId}, Confidence={Confidence}, OrderType={OrderType}",
                        bodyDraft.ServiceId, bodyDraft.TicketId, bodyDraft.ConfidenceScore, bodyDraft.OrderTypeCode);
                    
                    // ✅ AUTO-APPROVAL: If template has AutoApprove enabled, automatically approve the draft
                    if (matchedTemplate?.AutoApprove == true && bodyDraft.ValidationStatus == "Valid")
                    {
                        try
                        {
                            _logger.LogInformation("Template {TemplateCode} has AutoApprove enabled. Auto-approving body draft {DraftId}...", 
                                matchedTemplate.Code, bodyDraft.Id);
                            
                            var approveDto = new ApproveParsedOrderDto
                            {
                                ValidationNotes = "Auto-approved via email parser template"
                            };
                            
                            var systemUserId = await GetSystemUserIdAsync(cancellationToken);
                            await _parserService.ApproveParsedOrderAsync(
                                bodyDraft.Id, 
                                approveDto, 
                                bodyDraft.CompanyId ?? Guid.Empty, 
                                systemUserId, 
                                cancellationToken);
                            
                            _logger.LogInformation("✅ Successfully auto-approved body draft {DraftId}, Order created", bodyDraft.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to auto-approve body draft {DraftId}: {Error}", bodyDraft.Id, ex.Message);
                            // Update draft to NeedsReview if auto-approval fails
                            bodyDraft.ValidationStatus = "NeedsReview";
                            bodyDraft.ValidationNotes = $"Auto-approval failed: {ex.Message}. Please review manually.";
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Email body parsed but no meaningful data extracted. Confidence: {Confidence}", parsedData.ConfidenceScore);
                }
            }
        }

            // Update parse session with final draft count and description
            parseSession.ParsedOrdersCount = draftsFromAttachments;
            var attachmentCount = attachmentFiles?.Count ?? 0;
            var sourceParts = new List<string>();
            if (attachmentCount > 0)
            {
                sourceParts.Add($"{attachmentCount} attachment(s) processed");
            }
            if (bodyDraftCreated)
            {
                sourceParts.Add("email body parsed");
            }
            parseSession.SourceDescription = $"Email: {subject} | {string.Join(", ", sourceParts)}";
            parseSession.ParserTemplateId = matchedTemplate?.Id ?? account.DefaultParserTemplateId;
            parseSession.Status = matchedTemplate?.AutoApprove == true ? "AutoApproved" : "Pending";
            
            // ✅ Update email message parser status to "Parsed" after successful parsing
            emailMessage.ParserStatus = "Parsed";
            
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created parse session {SessionId} with {DraftCount} draft(s) for email {Subject}",
                parseSession.Id, parseSession.ParsedOrdersCount, subject);

            return true;
        }
        catch (Exception ex)
        {
            // Capture detailed error information
            var errorDetails = new StringBuilder();
            errorDetails.AppendLine($"Error processing email: {subject}");
            errorDetails.AppendLine($"From: {fromAddress}");
            errorDetails.AppendLine($"MessageId: {messageId}");
            errorDetails.AppendLine($"Exception: {ex.GetType().Name}");
            errorDetails.AppendLine($"Message: {ex.Message}");
            errorDetails.AppendLine($"Stack Trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                errorDetails.AppendLine($"Inner Exception: {ex.InnerException.GetType().Name}");
                errorDetails.AppendLine($"Inner Message: {ex.InnerException.Message}");
            }
            
            // Store error in parse session
            parseSession.Status = "Failed";
            parseSession.ErrorMessage = errorDetails.ToString();
            parseSession.SourceDescription = $"Email: {subject} | Processing failed";
            
            // Update email message status
            emailMessage.ParserStatus = "Failed";
            
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save error details to parse session {SessionId}", parseSession.Id);
            }
            
            _logger.LogError(ex, 
                "Error processing email {Subject} from {From}. ParseSession: {SessionId}, Error: {Error}",
                subject, fromAddress, parseSession.Id, ex.Message);
            
            return false;
        }
    }

    /// <summary>
    /// Compute SHA256 hash of file content for duplicate detection
    /// </summary>
    private async Task<string> ComputeFileHashAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var stream = file.OpenReadStream();
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Parse Excel attachment and create draft
    /// </summary>
    private async Task<ParsedOrderDraft> ParseExcelAttachmentAsync(
        IFormFile file, Guid sessionId, Guid companyId, DateTime now, string fileHash, bool autoApprove, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parsing Excel attachment: {FileName} (Hash: {Hash})", file.FileName, fileHash);
        var result = await _timeExcelParser.ParseAsync(file, null, cancellationToken);
        
        _logger.LogInformation("Parse result for {FileName}: Success={Success}, HasOrderData={HasOrderData}, ErrorMessage={ErrorMessage}, ValidationErrors={ErrorCount}",
            file.FileName, result.Success, result.OrderData != null, result.ErrorMessage, result.ValidationErrors?.Count ?? 0);
        if (result.ParseReport != null)
        {
            _logger.LogInformation("ParseReport for {FileName}: Status={ParseStatus}, Confidence={Confidence}, MissingRequired=[{Missing}], Sheet={Sheet}, HeaderRow={HeaderRow}",
                file.FileName, result.ParseReport.ParseStatus, result.ParseReport.FinalConfidenceScore,
                string.Join(", ", result.ParseReport.MissingRequiredFields ?? new List<string>()),
                result.ParseReport.SelectedSheetName, result.ParseReport.DetectedHeaderRow);
        }

        var draft = new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ParseSessionId = sessionId,
            SourceFileName = file.FileName,
            FileHash = fileHash,
            CreatedAt = now
        };

        // Check if we have parsed order data (even with validation errors)
        if (result.OrderData != null)
        {
            var data = result.OrderData;

            // Map parsed data to draft - ensure ALL fields from Excel parser are mapped
            draft.ServiceId = data.ServiceId;
            draft.TicketId = data.TicketId;
            draft.AwoNumber = data.AwoNumber; // ✅ Map AWO Number for Assurance orders
            draft.CustomerName = data.CustomerName ?? data.ContactPerson;
            draft.CustomerPhone = data.CustomerPhone;
            draft.CustomerEmail = data.CustomerEmail;
            draft.AdditionalContactNumber = data.AdditionalContactNumber; // ✅ Map Additional Contact Number
            draft.Issue = data.Issue; // ✅ Map Issue for Assurance orders
            draft.AddressText = data.ServiceAddress;
            draft.OldAddress = data.OldAddress;
            draft.AppointmentWindow = data.AppointmentWindow;
            draft.OrderTypeHint = data.OrderTypeHint;
            draft.OrderTypeCode = data.OrderTypeCode;
            draft.PackageName = data.PackageName;
            draft.Bandwidth = data.Bandwidth;
            draft.OnuSerialNumber = data.OnuSerialNumber;
            draft.OnuPassword = data.OnuPassword; // ✅ Ensure ONU Password is mapped
            draft.Username = data.Username;
            draft.Password = data.Password;
            draft.InternetWanIp = data.InternetWanIp;
            draft.InternetLanIp = data.InternetLanIp;
            draft.InternetGateway = data.InternetGateway;
            draft.InternetSubnetMask = data.InternetSubnetMask;
            draft.VoipServiceId = data.VoipServiceId;
            draft.ConfidenceScore = data.ConfidenceScore;
            
            // ✅ Store PartnerCode in Remarks if available (for partner resolution during order creation)
            var remarksParts = new List<string>();
            if (!string.IsNullOrEmpty(data.PartnerCode) && data.PartnerCode != "TIME")
            {
                remarksParts.Add($"[PartnerCode: {data.PartnerCode}]");
            }
            if (!string.IsNullOrEmpty(data.Remarks))
            {
                remarksParts.Add(data.Remarks);
            }
            draft.Remarks = remarksParts.Any() ? string.Join(" | ", remarksParts) : null;
            
            // ✅ Map Materials (if any) to JSON for storage (same as file upload parser)
            if (data.Materials != null && data.Materials.Any())
            {
                var materialDtos = data.Materials
                    .Select(m => new ParsedDraftMaterialDto
                    {
                        Id = Guid.NewGuid(),
                        Name = m.Name,
                        ActionTag = m.ActionTag,
                        Quantity = m.Quantity,
                        UnitOfMeasure = m.UnitOfMeasure,
                        Notes = m.Notes
                        // Note: IsRequired is not in ParsedDraftMaterialDto, but materials from Excel will have it in the source data
                    })
                    .ToList();

                draft.ParsedMaterialsJson = ParsedMaterialsSerializer.Serialize(materialDtos);
            }
            
            // Enrich draft with building matching, PDF fallback, date normalization, and validation status
            await _enrichmentService.EnrichDraftAsync(draft, result, file, companyId, cancellationToken);
            _enrichmentService.SetValidationStatus(draft, result, file.FileName, autoApprove);
        }
        else
        {
            draft.ValidationStatus = "NeedsReview";
            draft.ValidationNotes = result.ErrorMessage ?? "Failed to parse Excel file";
            draft.ConfidenceScore = 0.3m;
        }

        return draft;
    }

    /// <summary>
    /// Parse PDF attachment and create draft
    /// ✅ REFACTORED: Now uses enrichment service (same as Excel parser) for consistency
    /// </summary>
    private async Task<ParsedOrderDraft> ParsePdfAttachmentAsync(
        IFormFile file, Guid sessionId, Guid companyId, DateTime now, string fileHash, bool autoApprove, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parsing PDF attachment: {FileName} (Hash: {Hash})", file.FileName, fileHash);
        
        try
        {
            // Extract text from PDF
            var pdfText = await _pdfTextExtractionService.ExtractTextAsync(file, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(pdfText))
            {
                _logger.LogWarning("PDF text extraction returned empty text for {FileName}", file.FileName);
                return CreatePlaceholderDraft(
                    sessionId, companyId, now, file.FileName,
                    "PDF text extraction returned empty content. Please review manually.", 0.3m);
            }
            
            // Parse order data from PDF text
            var parsedData = _pdfOrderParserService.ParseFromText(pdfText, file.FileName);
            
            _logger.LogInformation("PDF parse result for {FileName}: ServiceId={ServiceId}, OrderType={OrderType}, Confidence={Confidence}",
                file.FileName, parsedData.ServiceId, parsedData.OrderTypeCode, parsedData.ConfidenceScore);
            
            var draft = new ParsedOrderDraft
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                ParseSessionId = sessionId,
                SourceFileName = file.FileName,
                FileHash = fileHash,
                CreatedAt = now
            };
            
            // Map parsed data to draft (same mapping as Excel parser)
            draft.ServiceId = parsedData.ServiceId;
            draft.TicketId = parsedData.TicketId;
            draft.AwoNumber = parsedData.AwoNumber; // ✅ Map AWO Number for Assurance orders
            draft.CustomerName = parsedData.CustomerName ?? parsedData.ContactPerson;
            draft.CustomerPhone = parsedData.CustomerPhone;
            draft.CustomerEmail = parsedData.CustomerEmail;
            draft.AdditionalContactNumber = parsedData.AdditionalContactNumber; // ✅ Map Additional Contact Number
            draft.Issue = parsedData.Issue; // ✅ Map Issue for Assurance orders
            draft.AddressText = parsedData.ServiceAddress;
            draft.OldAddress = parsedData.OldAddress;
            // Don't set AppointmentDate here - let enrichment service normalize it properly
            // The enrichment service will handle timezone conversion correctly
            draft.AppointmentDate = parsedData.AppointmentDateTime;
            draft.AppointmentWindow = parsedData.AppointmentWindow;
            draft.OrderTypeHint = parsedData.OrderTypeHint;
            draft.OrderTypeCode = parsedData.OrderTypeCode;
            draft.PackageName = parsedData.PackageName;
            draft.Bandwidth = parsedData.Bandwidth;
            draft.OnuSerialNumber = parsedData.OnuSerialNumber;
            draft.OnuPassword = parsedData.OnuPassword;
            draft.Username = parsedData.Username;
            draft.Password = parsedData.Password;
            draft.InternetWanIp = parsedData.InternetWanIp;
            draft.InternetLanIp = parsedData.InternetLanIp;
            draft.InternetGateway = parsedData.InternetGateway;
            draft.InternetSubnetMask = parsedData.InternetSubnetMask;
            draft.VoipServiceId = parsedData.VoipServiceId;
            draft.ConfidenceScore = parsedData.ConfidenceScore;
            
            // Store PartnerCode in Remarks if available
            var remarksParts = new List<string>();
            if (!string.IsNullOrEmpty(parsedData.PartnerCode) && parsedData.PartnerCode != "TIME")
            {
                remarksParts.Add($"[PartnerCode: {parsedData.PartnerCode}]");
            }
            if (!string.IsNullOrEmpty(parsedData.Remarks))
            {
                remarksParts.Add(parsedData.Remarks);
            }
            draft.Remarks = remarksParts.Any() ? string.Join(" | ", remarksParts) : null;
            
            // ✅ REFACTORED: Convert PDF parse result to TimeExcelParseResult format for enrichment service
            var parseResult = ConvertPdfParseResultToTimeExcelResult(parsedData);
            
            // ✅ REFACTORED: Use enrichment service (same as Excel parser) - provides building matching, date normalization, PDF fallback
            await _enrichmentService.EnrichDraftAsync(draft, parseResult, file, companyId, cancellationToken);
            
            // ✅ REFACTORED: Use enrichment service for validation status (same as Excel parser) - ensures consistent logic
            _enrichmentService.SetValidationStatus(draft, parseResult, file.FileName, autoApprove);
            
            _logger.LogInformation("PDF attachment parsed and enriched: {FileName}, ServiceId: {ServiceId}, OrderType: {OrderType}, Confidence: {Confidence}, BuildingStatus: {BuildingStatus}, ValidationStatus: {ValidationStatus}",
                file.FileName, draft.ServiceId, draft.OrderTypeCode, draft.ConfidenceScore, draft.BuildingStatus, draft.ValidationStatus);
            
            return draft;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing PDF attachment {FileName}: {Error}", file.FileName, ex.Message);
            return CreatePlaceholderDraft(
                sessionId, companyId, now, file.FileName,
                $"PDF parsing error: {ex.Message}. Please review manually.", 0.3m);
        }
    }
    
    /// <summary>
    /// Convert PDF parse result to TimeExcelParseResult format for use with enrichment service
    /// This allows PDF parsing to use the same enrichment logic as Excel parsing
    /// </summary>
    private static TimeExcelParseResult ConvertPdfParseResultToTimeExcelResult(ParsedOrderData parsedData)
    {
        // Determine success based on whether we got meaningful data
        var hasMeaningfulData = !string.IsNullOrEmpty(parsedData.ServiceId) ||
                               !string.IsNullOrEmpty(parsedData.CustomerName) ||
                               !string.IsNullOrEmpty(parsedData.CustomerPhone) ||
                               !string.IsNullOrEmpty(parsedData.ServiceAddress) ||
                               !string.IsNullOrEmpty(parsedData.TicketId);
        
        return new TimeExcelParseResult
        {
            Success = hasMeaningfulData,
            OrderData = parsedData,
            ValidationErrors = new List<string>(), // PDF parser doesn't return validation errors
            ErrorMessage = hasMeaningfulData ? null : "PDF parsing did not extract meaningful order data",
            // ConfidenceScore is stored in OrderData.ConfidenceScore
        };
    }

    /// <summary>
    /// Create placeholder draft for unsupported file types
    /// </summary>
    private ParsedOrderDraft CreatePlaceholderDraft(
        Guid sessionId, Guid companyId, DateTime now, string fileName, string notes, decimal confidenceScore)
    {
        return new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ParseSessionId = sessionId,
            SourceFileName = fileName,
            ValidationStatus = "NeedsReview",
            ValidationNotes = notes,
            ConfidenceScore = confidenceScore,
            CreatedAt = now
        };
    }

    /// <summary>
    /// Store all email attachments as EmailAttachment entities for mail viewer
    /// </summary>
    private async Task StoreEmailAttachmentsAsync(
        EmailMessage emailMessage,
        MimeMessage message,
        Guid? companyId,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        if (!message.Attachments.Any())
            return;

        foreach (var attachment in message.Attachments)
        {
            try
            {
                if (attachment is MimePart mimePart)
                {
                    var fileName = mimePart.FileName ?? $"attachment_{Guid.NewGuid()}";
                    var contentType = mimePart.ContentType?.MimeType ?? "application/octet-stream";
                    
                    // Check if inline/CID image
                    var isInline = mimePart.ContentDisposition?.Disposition == "inline";
                    var contentId = mimePart.ContentId;
                    
                    // Read attachment content
                    if (mimePart.Content == null)
                    {
                        _logger.LogWarning("Attachment {FileName} has no content, skipping storage", fileName);
                        continue;
                    }
                    using var memoryStream = new MemoryStream();
                    await mimePart.Content.DecodeToAsync(memoryStream, cancellationToken);
                    var bytes = memoryStream.ToArray();
                    
                    if (bytes.Length == 0)
                    {
                        _logger.LogWarning("Attachment {FileName} is empty, skipping storage", fileName);
                        continue;
                    }
                    
                    // Store attachment via FileService
                    var uploadDto = new FileUploadDto
                    {
                        File = new InMemoryFormFile(bytes, fileName, contentType),
                        Module = "Email",
                        EntityId = emailMessage.Id,
                        EntityType = "EmailMessage"
                    };
                    
                    var savedFile = await _fileService.UploadFileAsync(
                        uploadDto, 
                        companyId ?? Guid.Empty, 
                        Guid.Empty, // System user for email ingestion
                        cancellationToken);
                    
                    // Create EmailAttachment entity
                    var emailAttachment = new EmailAttachment
                    {
                        CompanyId = companyId,
                        EmailMessageId = emailMessage.Id,
                        FileName = fileName,
                        ContentType = contentType,
                        SizeBytes = bytes.Length,
                        StoragePath = savedFile.FileName, // FileService handles storage path
                        FileId = savedFile.Id,
                        IsInline = isInline,
                        ContentId = contentId,
                        ExpiresAt = expiresAt,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    _context.Set<EmailAttachment>().Add(emailAttachment);
                    
                    _logger.LogInformation("Stored email attachment: {FileName} ({Size} bytes, Inline: {IsInline})", 
                        fileName, bytes.Length, isInline);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing email attachment: {Error}", ex.Message);
            }
        }
        
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Extract attachments from email message and convert to IFormFile list (for parsing only)
    /// </summary>
    private async Task<List<IFormFile>> ExtractAttachmentsAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        var files = new List<IFormFile>();
        
        foreach (var attachment in message.Attachments)
        {
            try
            {
                if (attachment is MimePart mimePart)
                {
                    var fileName = mimePart.FileName ?? $"attachment_{Guid.NewGuid()}";
                    var contentType = mimePart.ContentType?.MimeType ?? "application/octet-stream";
                    
                    // Only process Excel and PDF files
                    var extension = Path.GetExtension(fileName).ToLowerInvariant();
                    if (extension != ".xls" && extension != ".xlsx" && extension != ".pdf")
                    {
                        _logger.LogDebug("Skipping attachment {FileName} - not Excel or PDF", fileName);
                        continue;
                    }
                    
                    // Read attachment content
                    if (mimePart.Content == null)
                    {
                        _logger.LogWarning("Attachment {FileName} has no content, skipping", fileName);
                        continue;
                    }
                    using var memoryStream = new MemoryStream();
                    await mimePart.Content.DecodeToAsync(memoryStream, cancellationToken);
                    var bytes = memoryStream.ToArray();
                    
                    if (bytes.Length == 0)
                    {
                        _logger.LogWarning("Attachment {FileName} is empty, skipping", fileName);
                        continue;
                    }
                    
                    // Create IFormFile wrapper
                    var formFile = new InMemoryFormFile(bytes, fileName, contentType);
                    files.Add(formFile);
                    
                    _logger.LogInformation("Extracted attachment: {FileName} ({Size} bytes)", 
                        fileName, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting attachment: {Error}", ex.Message);
            }
        }
        
        return files;
    }

    /// <summary>
    /// Get stored Excel/PDF files for an email message (fallback when MIME extraction fails)
    /// </summary>
    private async Task<List<IFormFile>> GetStoredExcelPdfFilesAsync(
        Guid emailMessageId,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        var files = new List<IFormFile>();
        
        try
        {
            // Query stored files for this email message
            var storedFiles = await _context.Set<FileEntity>()
                .Where(f => f.EntityId == emailMessageId 
                         && f.EntityType == "EmailMessage"
                         && (companyId == null || f.CompanyId == companyId))
                .ToListAsync(cancellationToken);
            
            foreach (var storedFile in storedFiles)
            {
                try
                {
                    var extension = Path.GetExtension(storedFile.FileName).ToLowerInvariant();
                    
                    // Only process Excel and PDF files
                    if (extension != ".xls" && extension != ".xlsx" && extension != ".pdf")
                    {
                        _logger.LogDebug("Skipping stored file {FileName} - not Excel or PDF", storedFile.FileName);
                        continue;
                    }
                    
                    // Load file content
                    var fileContent = await _fileService.GetFileContentAsync(storedFile.Id, storedFile.CompanyId, cancellationToken);
                    if (fileContent == null || fileContent.Length == 0)
                    {
                        _logger.LogWarning("Stored file {FileName} (Id: {FileId}) has no content, skipping", 
                            storedFile.FileName, storedFile.Id);
                        continue;
                    }
                    
                    // Create IFormFile wrapper
                    var formFile = new InMemoryFormFile(fileContent, storedFile.FileName, storedFile.ContentType);
                    files.Add(formFile);
                    
                    _logger.LogInformation("Loaded stored file for parsing: {FileName} ({Size} bytes)", 
                        storedFile.FileName, fileContent.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading stored file {FileName} (Id: {FileId})", 
                        storedFile.FileName, storedFile.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stored files for email message {EmailMessageId}", emailMessageId);
        }
        
        return files;
    }

    // ============================================================================
    // REMOVED: Weak regex-based extraction methods (ExtractCustomerName, 
    // ExtractPhoneNumber, ExtractAddress, ExtractOrderDetails)
    // 
    // These methods produced unreliable data. All email parsing now uses:
    // 1. SyncfusionExcelParserService for Excel attachments (100% accuracy)
    // 2. PdfOrderParserService for PDF attachments and ASSURANCE email bodies
    // 3. Body-only emails (except ASSURANCE) are skipped as non-actionable
    // ============================================================================

    /// <summary>
    /// Strip HTML tags from email body for better text extraction
    /// </summary>
    private string StripHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        // Remove script and style tags with content
        html = System.Text.RegularExpressions.Regex.Replace(html,
            @"<(script|style)[^>]*>.*?</\1>", "",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // Convert common HTML elements to newlines
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<br\s*/?>", "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<p[^>]*>", "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"</p>", "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<div[^>]*>", "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"</div>", "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // Remove all remaining HTML tags
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", "");
        
        // Decode HTML entities
        html = System.Net.WebUtility.HtmlDecode(html);
        
        // Clean up multiple whitespace and newlines
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\s+", " ");
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\n\s*\n", "\n");
        
        return html.Trim();
    }


    /// <summary>
    /// Process withdrawal email: Extract Service ID and update order status to Cancelled
    /// </summary>
    private async Task ProcessWithdrawalEmailAsync(
        EmailMessage emailMessage,
        string bodyText,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract Service ID from body
            // Pattern: "Service ID : TBBNB087846G" or "Service ID: TBBNB087846G"
            var serviceIdPattern = @"Service\s+ID\s*:?\s*([A-Z0-9]+)";
            var match = System.Text.RegularExpressions.Regex.Match(bodyText, serviceIdPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (!match.Success || match.Groups.Count < 2)
            {
                _logger.LogWarning("Could not extract Service ID from withdrawal email: {EmailId}", emailMessage.Id);
                return;
            }

            var serviceId = match.Groups[1].Value.Trim().ToUpperInvariant();
            _logger.LogInformation("Extracted Service ID from withdrawal email: {ServiceId}", serviceId);

            // Find order by Service ID
            var order = await _context.Orders
                .Where(o => o.ServiceId == serviceId && 
                           (companyId == null || o.CompanyId == companyId))
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found for Service ID {ServiceId} in withdrawal email", serviceId);
                return;
            }

            // Check if order is already cancelled
            if (order.Status == "Cancelled")
            {
                _logger.LogInformation("Order {OrderId} (Service ID: {ServiceId}) is already cancelled", order.Id, serviceId);
                return;
            }

            var oldStatus = order.Status;

            // Add note about withdrawal
            var withdrawalNote = $"Order withdrawn via email notification (Email ID: {emailMessage.Id}, Received: {emailMessage.ReceivedAt:yyyy-MM-dd HH:mm:ss})";
            if (!string.IsNullOrEmpty(order.OrderNotesInternal))
            {
                order.OrderNotesInternal = $"{order.OrderNotesInternal}\n\n{withdrawalNote}";
            }
            else
            {
                order.OrderNotesInternal = withdrawalNote;
            }

            // Transition to Cancelled via workflow (no direct status writes per RBAC rules)
            if (_workflowEngineService == null)
            {
                _logger.LogWarning("WorkflowEngineService not available. Cannot transition Order {OrderId} to Cancelled. Note added only.", order.Id);
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            try
            {
                var orderCompanyId = order.CompanyId ?? Guid.Empty;
                var executeDto = new ExecuteTransitionDto
                {
                    EntityType = "Order",
                    EntityId = order.Id,
                    TargetStatus = "Cancelled",
                    PartnerId = order.PartnerId,
                    DepartmentId = order.DepartmentId,
                    Payload = new Dictionary<string, object>
                    {
                        ["reason"] = "Withdrawal notification from TIME",
                        ["source"] = "Parser",
                        ["emailId"] = emailMessage.Id.ToString(),
                        ["notes"] = "Auto-cancelled via email"
                    }
                };
                var job = await _workflowEngineService.ExecuteTransitionAsync(orderCompanyId, executeDto, null, cancellationToken);
                if (job.State != "Succeeded")
                {
                    _logger.LogWarning("Workflow transition to Cancelled failed for Order {OrderId}: {Error}. Note added only.", order.Id, job.LastError);
                }
                else
                {
                    _logger.LogInformation(
                        "✅ Order {OrderId} (Service ID: {ServiceId}) status updated from {OldStatus} to Cancelled via workflow (withdrawal email)",
                        order.Id, serviceId, oldStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow transition to Cancelled failed for Order {OrderId}. Note added only.", order.Id);
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing withdrawal email {EmailId}", emailMessage.Id);
        }
    }


    /// <summary>
    /// Process Customer Uncontactable email: Extract Service ID and set order to Blocker status
    /// </summary>
    private async Task ProcessCustomerUncontactableEmailAsync(
        EmailMessage emailMessage,
        string subject,
        string bodyText,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract Service ID from subject or body: "Customer Uncontactable - TBBNB092724G - ..."
            var serviceIdPattern = @"Service\s+ID\s*[:\-]?\s*([A-Z0-9]+)";
            var serviceIdMatch = System.Text.RegularExpressions.Regex.Match(bodyText, serviceIdPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (!serviceIdMatch.Success || serviceIdMatch.Groups.Count < 2)
            {
                // Try from subject: "Customer Uncontactable - TBBNB092724G - ..."
                var subjectPattern = @"Customer\s+Uncontactable\s*[-\s]+\s*([A-Z0-9]+)";
                serviceIdMatch = System.Text.RegularExpressions.Regex.Match(subject, subjectPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            
            if (!serviceIdMatch.Success || serviceIdMatch.Groups.Count < 2)
            {
                _logger.LogWarning("Could not extract Service ID from Customer Uncontactable email: {EmailId}", emailMessage.Id);
                return;
            }

            var serviceId = serviceIdMatch.Groups[1].Value.Trim().ToUpperInvariant();
            _logger.LogInformation("Extracted Service ID from Customer Uncontactable email: {ServiceId}", serviceId);

            // Extract customer name: "Customer name - SUN,TINGBAO"
            var customerNamePattern = @"Customer\s+name\s*[:\-]?\s*([^\n\r]+)";
            var customerNameMatch = System.Text.RegularExpressions.Regex.Match(bodyText, customerNamePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var customerName = customerNameMatch.Success && customerNameMatch.Groups.Count > 1
                ? customerNameMatch.Groups[1].Value.Trim()
                : null;

            // Extract date and time
            var datePattern = @"Date\s*[:\-]?\s*(\d+)\s*/\s*(\d+)\s*/\s*(\d+)";
            var dateMatch = System.Text.RegularExpressions.Regex.Match(bodyText, datePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            DateTime? reportedDate = null;
            if (dateMatch.Success && dateMatch.Groups.Count > 3)
            {
                try
                {
                    var day = int.Parse(dateMatch.Groups[1].Value);
                    var month = int.Parse(dateMatch.Groups[2].Value);
                    var year = int.Parse(dateMatch.Groups[3].Value);
                    // Handle 2-digit year
                    if (year < 100) year += 2000;
                    reportedDate = new DateTime(year, month, day);
                }
                catch { }
            }

            var timePattern = @"Time\s*[:\-]?\s*([\d.]+)\s*(am|pm)";
            var timeMatch = System.Text.RegularExpressions.Regex.Match(bodyText, timePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var reportedTime = timeMatch.Success ? timeMatch.Value.Trim() : null;

            // Find order by Service ID
            var order = await _context.Orders
                .Where(o => o.ServiceId == serviceId && 
                           (companyId == null || o.CompanyId == companyId))
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found for Service ID {ServiceId} in Customer Uncontactable email", serviceId);
                return;
            }

            // Check if order status allows blocker
            var canSetBlocker = OrderStatus.CanSetBlocker(order.Status);
            if (!canSetBlocker)
            {
                _logger.LogInformation(
                    "Order {OrderId} (Service ID: {ServiceId}) is in status {Status}, cannot set blocker. Adding note only.",
                    order.Id, serviceId, order.Status);
                
                // Add note even if blocker cannot be set
                var note = $"Customer Uncontactable notification received (Email ID: {emailMessage.Id}, Received: {emailMessage.ReceivedAt:yyyy-MM-dd HH:mm:ss})\n" +
                          $"Customer: {customerName ?? "N/A"}\n" +
                          $"Reported Date: {reportedDate?.ToString("dd/MM/yyyy") ?? "N/A"}\n" +
                          $"Reported Time: {reportedTime ?? "N/A"}\n" +
                          $"Action Required: Please provide alternate contact number";
                
                if (!string.IsNullOrEmpty(order.OrderNotesInternal))
                {
                    order.OrderNotesInternal = $"{order.OrderNotesInternal}\n\n{note}";
                }
                else
                {
                    order.OrderNotesInternal = note;
                }
                
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            var oldStatus = order.Status;

            // Add note about uncontactable
            var blockerNote = $"Customer Uncontactable - Alternate contact number required\n" +
                             $"Email ID: {emailMessage.Id}\n" +
                             $"Received: {emailMessage.ReceivedAt:yyyy-MM-dd HH:mm:ss}\n" +
                             $"Customer: {customerName ?? "N/A"}\n" +
                             $"Reported Date: {reportedDate?.ToString("dd/MM/yyyy") ?? "N/A"}\n" +
                             $"Reported Time: {reportedTime ?? "N/A"}";
            
            if (!string.IsNullOrEmpty(order.OrderNotesInternal))
            {
                order.OrderNotesInternal = $"{order.OrderNotesInternal}\n\n{blockerNote}";
            }
            else
            {
                order.OrderNotesInternal = blockerNote;
            }

            // Create OrderBlocker record (documents the blocker reason)
            var blocker = new OrderBlocker
            {
                Id = Guid.NewGuid(),
                CompanyId = order.CompanyId,
                OrderId = order.Id,
                BlockerType = "CustomerUncontactable",
                BlockerCategory = "Customer",
                Description = $"Customer is uncontactable. Alternate contact number required. Reported: {reportedDate?.ToString("dd/MM/yyyy") ?? "N/A"} {reportedTime ?? ""}",
                RaisedByUserId = Guid.Empty, // System action
                RaisedAt = DateTime.UtcNow,
                Resolved = false,
                EvidenceRequired = false, // No evidence needed for uncontactable
                EvidenceNotes = $"Email notification from TIME: {emailMessage.Subject}"
            };
            _context.OrderBlockers.Add(blocker);

            // Transition to Blocker via workflow (no direct status writes per RBAC rules)
            if (_workflowEngineService == null)
            {
                _logger.LogWarning("WorkflowEngineService not available. Cannot transition Order {OrderId} to Blocker. Note and OrderBlocker added only.", order.Id);
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            try
            {
                var orderCompanyId = order.CompanyId ?? Guid.Empty;
                var executeDto = new ExecuteTransitionDto
                {
                    EntityType = "Order",
                    EntityId = order.Id,
                    TargetStatus = "Blocker",
                    PartnerId = order.PartnerId,
                    DepartmentId = order.DepartmentId,
                    Payload = new Dictionary<string, object>
                    {
                        ["reason"] = "Customer Uncontactable - Alternate contact number required",
                        ["source"] = "Parser",
                        ["emailId"] = emailMessage.Id.ToString(),
                        ["customerName"] = customerName ?? "N/A",
                        ["notes"] = "Auto-blocked via email"
                    }
                };
                var job = await _workflowEngineService.ExecuteTransitionAsync(orderCompanyId, executeDto, null, cancellationToken);
                if (job.State != "Succeeded")
                {
                    _logger.LogWarning("Workflow transition to Blocker failed for Order {OrderId}: {Error}. Note and OrderBlocker added only.", order.Id, job.LastError);
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    _logger.LogInformation(
                        "✅ Order {OrderId} (Service ID: {ServiceId}) status updated from {OldStatus} to Blocker via workflow (Customer Uncontactable email). Customer: {CustomerName}",
                        order.Id, serviceId, oldStatus, customerName ?? "N/A");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow transition to Blocker failed for Order {OrderId}. Note and OrderBlocker added only.", order.Id);
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Customer Uncontactable email {EmailId}", emailMessage.Id);
        }
    }

    /// <summary>
    /// Process Reschedule email: Parse Excel attachment to extract new appointment date/time and update order
    /// </summary>
    private async Task<bool> ProcessRescheduleEmailAsync(
        EmailMessage emailMessage,
        string subject,
        List<IFormFile> attachmentFiles,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find Excel attachment
            var excelFile = attachmentFiles.FirstOrDefault(f => 
                Path.GetExtension(f.FileName).ToLowerInvariant() == ".xlsx" || 
                Path.GetExtension(f.FileName).ToLowerInvariant() == ".xls");
            
            if (excelFile == null)
            {
                _logger.LogWarning("No Excel attachment found in reschedule email: {EmailId}", emailMessage.Id);
                return false;
            }

            // Parse Excel file to extract appointment date/time and Service ID (preserves original file extension for correct parser routing)
            var parseResult = await _timeExcelParser.ParseAsync(excelFile, null, cancellationToken);
            
            if (!parseResult.Success || parseResult.OrderData == null)
            {
                _logger.LogWarning("Failed to parse Excel attachment in reschedule email: {EmailId}, Errors: {Errors}", 
                    emailMessage.Id, string.Join(", ", parseResult.ValidationErrors ?? new List<string>()));
                return false;
            }

            var parsedData = parseResult.OrderData;
            
            if (string.IsNullOrEmpty(parsedData.ServiceId))
            {
                _logger.LogWarning("Service ID not found in parsed Excel from reschedule email: {EmailId}", emailMessage.Id);
                return false;
            }

            if (!parsedData.AppointmentDateTime.HasValue)
            {
                _logger.LogWarning("Appointment date/time not found in parsed Excel from reschedule email: {EmailId}", emailMessage.Id);
                return false;
            }

            var serviceId = parsedData.ServiceId.Trim().ToUpperInvariant();
            var newAppointmentDate = parsedData.AppointmentDateTime.Value;
            
            // Parse appointment window if available
            TimeSpan? newWindowFrom = null;
            TimeSpan? newWindowTo = null;
            if (!string.IsNullOrEmpty(parsedData.AppointmentWindow))
            {
                try
                {
                    var (from, to) = AppointmentWindowParser.ParseAppointmentWindow(parsedData.AppointmentWindow);
                    newWindowFrom = from;
                    newWindowTo = to;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse appointment window '{Window}' from reschedule Excel", parsedData.AppointmentWindow);
                }
            }

            _logger.LogInformation(
                "Extracted reschedule info from Excel: ServiceId={ServiceId}, NewDate={NewDate}, Window={Window}",
                serviceId, newAppointmentDate, parsedData.AppointmentWindow);

            // Find order by Service ID
            var order = await _context.Orders
                .Where(o => o.ServiceId == serviceId && 
                           (companyId == null || o.CompanyId == companyId))
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found for Service ID {ServiceId} in reschedule email", serviceId);
                return false;
            }

            // Store original appointment for reschedule record
            var originalDate = order.AppointmentDate;
            var originalWindowFrom = order.AppointmentWindowFrom;
            var originalWindowTo = order.AppointmentWindowTo;

            // Create OrderReschedule record
            var reschedule = new OrderReschedule
            {
                Id = Guid.NewGuid(),
                CompanyId = order.CompanyId,
                OrderId = order.Id,
                RequestedBySource = "Partner",
                RequestedAt = DateTime.UtcNow,
                OriginalDate = originalDate,
                OriginalWindowFrom = originalWindowFrom,
                OriginalWindowTo = originalWindowTo,
                NewDate = newAppointmentDate.Date,
                NewWindowFrom = newWindowFrom.GetValueOrDefault(),
                NewWindowTo = newWindowTo.GetValueOrDefault(),
                Reason = "Reschedule request from TIME via email",
                ApprovalSource = "EmailParser",
                ApprovalEmailId = emailMessage.Id,
                Status = "Approved", // Auto-approve reschedules from TIME
                StatusChangedAt = DateTime.UtcNow,
                IsSameDayReschedule = originalDate.Date == newAppointmentDate.Date
            };
            _context.OrderReschedules.Add(reschedule);

            // Update order with new appointment
            order.AppointmentDate = newAppointmentDate.Date;
            if (newWindowFrom.HasValue)
            {
                order.AppointmentWindowFrom = newWindowFrom.Value;
            }
            if (newWindowTo.HasValue)
            {
                order.AppointmentWindowTo = newWindowTo.Value;
            }
            order.HasReschedules = true;
            order.RescheduleCount++;
            order.UpdatedAt = DateTime.UtcNow;

            // Add note about reschedule
            var rescheduleNote = $"Order rescheduled via email notification (Email ID: {emailMessage.Id})\n" +
                                $"Original: {originalDate:yyyy-MM-dd} {originalWindowFrom:hh\\:mm}-{originalWindowTo:hh\\:mm}\n" +
                                $"New: {newAppointmentDate:yyyy-MM-dd} {(newWindowFrom.HasValue && newWindowTo.HasValue ? $"{newWindowFrom.Value:hh\\:mm}-{newWindowTo.Value:hh\\:mm}" : "N/A")}";
            
            if (!string.IsNullOrEmpty(order.OrderNotesInternal))
            {
                order.OrderNotesInternal = $"{order.OrderNotesInternal}\n\n{rescheduleNote}";
            }
            else
            {
                order.OrderNotesInternal = rescheduleNote;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // If order was in ReschedulePendingApproval, transition back to Assigned via workflow engine
            if (order.Status == OrderStatus.ReschedulePendingApproval)
            {
                var systemUserId = await GetSystemUserIdAsync(cancellationToken);
                await _orderService.ChangeOrderStatusAsync(
                    order.Id,
                    new ChangeOrderStatusDto { Status = OrderStatus.Assigned, Reason = "Reschedule approved via email" },
                    order.CompanyId,
                    order.DepartmentId,
                    systemUserId,
                    cancellationToken);
            }

            _logger.LogInformation(
                "✅ Order {OrderId} (Service ID: {ServiceId}) rescheduled via email. New appointment: {NewDate} {Window}",
                order.Id, serviceId, newAppointmentDate, parsedData.AppointmentWindow ?? "N/A");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reschedule email {EmailId}", emailMessage.Id);
            return false;
        }
    }

    /// <summary>
    /// Process RFB email: Extract building information, meeting details, and BM contact information
    /// </summary>
    private async Task ProcessRfbEmailAsync(
        EmailMessage emailMessage,
        string subject,
        string bodyText,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract building name from subject: "RFB MEETING Pangsapuri Vista Mahogani | ..."
            var buildingNameMatch = System.Text.RegularExpressions.Regex.Match(
                subject, @"RFB\s+MEETING\s+(.+?)(?:\s*\||$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var buildingName = buildingNameMatch.Success && buildingNameMatch.Groups.Count > 1
                ? buildingNameMatch.Groups[1].Value.Trim()
                : null;

            // Extract building code from body: "Pangsapuri Vista Mahogani | 11303"
            var buildingCodeMatch = System.Text.RegularExpressions.Regex.Match(
                bodyText, @"([A-Za-z\s]+)\s*\|\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var buildingCode = buildingCodeMatch.Success && buildingCodeMatch.Groups.Count > 2
                ? buildingCodeMatch.Groups[2].Value.Trim()
                : null;

            // If building name not found in subject, try from body
            if (string.IsNullOrEmpty(buildingName) && buildingCodeMatch.Success)
            {
                buildingName = buildingCodeMatch.Groups[1].Value.Trim();
            }

            // Extract meeting date: "Date : 24 / 4 / 2025"
            var dateMatch = System.Text.RegularExpressions.Regex.Match(
                bodyText, @"Date\s*:?\s*(\d+)\s*/\s*(\d+)\s*/\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            DateTime? meetingDate = null;
            if (dateMatch.Success && dateMatch.Groups.Count > 3)
            {
                try
                {
                    var day = int.Parse(dateMatch.Groups[1].Value);
                    var month = int.Parse(dateMatch.Groups[2].Value);
                    var year = int.Parse(dateMatch.Groups[3].Value);
                    meetingDate = new DateTime(year, month, day);
                }
                catch { }
            }

            // Extract meeting time: "Time: 3.00 pm – 4.00 pm"
            var timeMatch = System.Text.RegularExpressions.Regex.Match(
                bodyText, @"Time\s*:?\s*([\d.]+)\s*(am|pm)\s*[–-]\s*([\d.]+)\s*(am|pm)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var meetingTime = timeMatch.Success ? timeMatch.Value.Trim() : null;

            // Extract location: "Location : Pangsapuri Vista Mahogani"
            var locationMatch = System.Text.RegularExpressions.Regex.Match(
                bodyText, @"Location\s*:?\s*([^\n\r]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var location = locationMatch.Success && locationMatch.Groups.Count > 1
                ? locationMatch.Groups[1].Value.Trim()
                : null;

            // Extract BM PIC name: "BM PIC:  Mr Syammir"
            var picNameMatch = System.Text.RegularExpressions.Regex.Match(
                bodyText, @"BM\s+PIC\s*:?\s*(?:Mr|Ms|Mrs)?\.?\s*([A-Za-z\s]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var picName = picNameMatch.Success && picNameMatch.Groups.Count > 1
                ? picNameMatch.Groups[1].Value.Trim()
                : null;

            // Extract BM phone: "Phone: 03 8210 9497"
            var phoneMatch = System.Text.RegularExpressions.Regex.Match(
                bodyText, @"Phone\s*:?\s*([\d\s\-+()]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var phone = phoneMatch.Success && phoneMatch.Groups.Count > 1
                ? phoneMatch.Groups[1].Value.Trim()
                : null;

            // Extract BM email: "email: vistamahogani.pmo@gmail.com"
            var emailMatch = System.Text.RegularExpressions.Regex.Match(
                bodyText, @"email\s*:?\s*([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var email = emailMatch.Success && emailMatch.Groups.Count > 1
                ? emailMatch.Groups[1].Value.Trim()
                : null;

            if (string.IsNullOrEmpty(buildingName))
            {
                _logger.LogWarning("Could not extract building name from RFB email: {EmailId}", emailMessage.Id);
                return;
            }

            _logger.LogInformation(
                "Extracted RFB info: Building={BuildingName}, Code={Code}, Date={Date}, PIC={PIC}, Phone={Phone}, Email={Email}",
                buildingName, buildingCode, meetingDate, picName, phone, email);

            // Find or create building
            var building = await _context.Buildings
                .Where(b => b.CompanyId == companyId && 
                           (b.Name.ToUpper() == buildingName.ToUpper() || 
                            (buildingCode != null && b.Code == buildingCode)))
                .FirstOrDefaultAsync(cancellationToken);

            if (building == null)
            {
                // Create new building
                var createDto = new CreateBuildingDto
                {
                    CompanyId = companyId,
                    Name = buildingName,
                    Code = buildingCode,
                    AddressLine1 = location ?? buildingName,
                    City = "", // Will need to be filled manually
                    State = "",
                    Postcode = "",
                    RfbAssignedDate = meetingDate ?? emailMessage.ReceivedAt.Date,
                    Notes = $"RFB meeting email received: {emailMessage.Subject}\nMeeting Date: {meetingDate}\nMeeting Time: {meetingTime}\nLocation: {location}"
                };

                var newBuilding = await _buildingService.CreateBuildingAsync(createDto, companyId, cancellationToken);
                building = await _context.Buildings
                    .FirstOrDefaultAsync(b => b.Id == newBuilding.Id && b.CompanyId == companyId, cancellationToken);
                _logger.LogInformation("Created new building: {BuildingId} ({BuildingName}) from RFB email", building!.Id, buildingName);
            }
            else
            {
                // Update existing building
                var updateDto = new UpdateBuildingDto
                {
                    Code = buildingCode ?? building.Code,
                    RfbAssignedDate = meetingDate ?? building.RfbAssignedDate ?? emailMessage.ReceivedAt.Date,
                    Notes = $"{building.Notes}\n\nRFB meeting email received: {emailMessage.Subject}\nMeeting Date: {meetingDate}\nMeeting Time: {meetingTime}\nLocation: {location}"
                };

                await _buildingService.UpdateBuildingAsync(building.Id, updateDto, companyId, cancellationToken);
                _logger.LogInformation("Updated building: {BuildingId} ({BuildingName}) from RFB email", building.Id, buildingName);
            }

            // Create or update BM contact
            if (!string.IsNullOrEmpty(picName))
            {
                var existingContact = await _context.BuildingContacts
                    .Where(c => c.BuildingId == building.Id && 
                               c.Role == "Building Manager" && 
                               c.Name.ToUpper() == picName.ToUpper())
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingContact == null)
                {
                    // Create new BM contact
                    var contactDto = new SaveBuildingContactDto
                    {
                        Role = "Building Manager",
                        Name = picName,
                        Phone = phone,
                        Email = email,
                        IsPrimary = true,
                        IsActive = true,
                        Remarks = $"Added from RFB email: {emailMessage.Subject}"
                    };

                    await _buildingService.CreateBuildingContactAsync(building.Id, contactDto, companyId, cancellationToken);
                    _logger.LogInformation("Created BM contact: {Name} for building {BuildingId}", picName, building.Id);
                }
                else
                {
                    // Update existing contact
                    var contactDto = new SaveBuildingContactDto
                    {
                        Role = "Building Manager",
                        Name = picName,
                        Phone = phone ?? existingContact.Phone,
                        Email = email ?? existingContact.Email,
                        IsPrimary = true,
                        IsActive = true,
                        Remarks = $"{existingContact.Remarks}\nUpdated from RFB email: {emailMessage.Subject}"
                    };

                    await _buildingService.UpdateBuildingContactAsync(building.Id, existingContact.Id, contactDto, companyId, cancellationToken);
                    _logger.LogInformation("Updated BM contact: {Name} for building {BuildingId}", picName, building.Id);
                }
            }

            _logger.LogInformation(
                "✅ RFB email processed: Building {BuildingId} ({BuildingName}) updated with meeting details and BM contact",
                building.Id, buildingName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RFB email {EmailId}", emailMessage.Id);
        }
    }

    /// <summary>
    /// Process Payment Advice email: Extract payment details from PDF and create Payment record
    /// </summary>
    private async Task<bool> ProcessPaymentAdviceEmailAsync(
        EmailMessage emailMessage,
        string subject,
        List<IFormFile> attachmentFiles,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find PDF attachment
            var pdfFile = attachmentFiles.FirstOrDefault(f => Path.GetExtension(f.FileName).ToLowerInvariant() == ".pdf");
            
            if (pdfFile == null)
            {
                _logger.LogWarning("No PDF attachment found in payment advice email: {EmailId}", emailMessage.Id);
                return false;
            }

            // Extract payment references from subject
            // Pattern: "Payment Advice - Advice Ref:[A2nMLXnGfnSa] / ACH credits / Customer Ref:[20251215HSM04] / Second Party Ref:[1366833W] / Second Party ID:[0000311582]"
            var adviceRefMatch = System.Text.RegularExpressions.Regex.Match(
                subject, @"Advice\s+Ref\s*:\s*\[([^\]]+)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var adviceRef = adviceRefMatch.Success && adviceRefMatch.Groups.Count > 1
                ? adviceRefMatch.Groups[1].Value.Trim()
                : null;

            var customerRefMatch = System.Text.RegularExpressions.Regex.Match(
                subject, @"Customer\s+Ref\s*:\s*\[([^\]]+)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var customerRef = customerRefMatch.Success && customerRefMatch.Groups.Count > 1
                ? customerRefMatch.Groups[1].Value.Trim()
                : null;

            var secondPartyRefMatch = System.Text.RegularExpressions.Regex.Match(
                subject, @"Second\s+Party\s+Ref\s*:\s*\[([^\]]+)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var secondPartyRef = secondPartyRefMatch.Success && secondPartyRefMatch.Groups.Count > 1
                ? secondPartyRefMatch.Groups[1].Value.Trim()
                : null;

            var secondPartyIdMatch = System.Text.RegularExpressions.Regex.Match(
                subject, @"Second\s+Party\s+ID\s*:\s*\[([^\]]+)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var secondPartyId = secondPartyIdMatch.Success && secondPartyIdMatch.Groups.Count > 1
                ? secondPartyIdMatch.Groups[1].Value.Trim()
                : null;

            // Extract payment method from subject
            var paymentMethod = PaymentMethod.BankTransfer; // Default to BankTransfer for ACH credits
            if (subject.Contains("ACH", StringComparison.OrdinalIgnoreCase) || subject.Contains("credits", StringComparison.OrdinalIgnoreCase))
            {
                paymentMethod = PaymentMethod.BankTransfer;
            }
            else if (subject.Contains("cheque", StringComparison.OrdinalIgnoreCase))
            {
                paymentMethod = PaymentMethod.Cheque;
            }
            else if (subject.Contains("cash", StringComparison.OrdinalIgnoreCase))
            {
                paymentMethod = PaymentMethod.Cash;
            }

            // Extract text from PDF
            var pdfText = await _pdfTextExtractionService.ExtractTextAsync(pdfFile, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(pdfText))
            {
                _logger.LogWarning("Could not extract text from PDF in payment advice email: {EmailId}", emailMessage.Id);
                return false;
            }

            // Extract payment amount from PDF
            // Common patterns: "Amount: RM 1,234.56", "Total: 1234.56", "Payment Amount: 1,234.56"
            var amountPatterns = new[]
            {
                @"(?:Amount|Total|Payment\s+Amount)\s*:?\s*(?:RM\s*)?([\d,]+\.?\d*)",
                @"RM\s*([\d,]+\.?\d*)",
                @"([\d,]+\.?\d*)\s*(?:RM|MYR)"
            };
            
            decimal? paymentAmount = null;
            foreach (var pattern in amountPatterns)
            {
                var amountMatch = System.Text.RegularExpressions.Regex.Match(
                    pdfText, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (amountMatch.Success && amountMatch.Groups.Count > 1)
                {
                    var amountStr = amountMatch.Groups[1].Value.Replace(",", "");
                    if (decimal.TryParse(amountStr, out var amount))
                    {
                        paymentAmount = amount;
                        break;
                    }
                }
            }

            // Extract payment date from PDF
            // Common patterns: "Date: 15/12/2025", "Payment Date: 2025-12-15", "Date: 15 Dec 2025"
            var datePatterns = new[]
            {
                @"(?:Payment\s+)?Date\s*:?\s*(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{2,4})",
                @"(?:Payment\s+)?Date\s*:?\s*(\d{4})[\/\-](\d{1,2})[\/\-](\d{1,2})",
                @"(?:Payment\s+)?Date\s*:?\s*(\d{1,2})\s+(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+(\d{2,4})"
            };
            
            DateTime? paymentDate = null;
            foreach (var pattern in datePatterns)
            {
                var dateMatch = System.Text.RegularExpressions.Regex.Match(
                    pdfText, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (dateMatch.Success && dateMatch.Groups.Count > 3)
                {
                    try
                    {
                        int day, month, year;
                        if (dateMatch.Groups[2].Value.Length > 2) // Month name
                        {
                            var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                            month = Array.IndexOf(monthNames, dateMatch.Groups[2].Value.Substring(0, 3)) + 1;
                            day = int.Parse(dateMatch.Groups[1].Value);
                            year = int.Parse(dateMatch.Groups[3].Value);
                        }
                        else
                        {
                            day = int.Parse(dateMatch.Groups[1].Value);
                            month = int.Parse(dateMatch.Groups[2].Value);
                            year = int.Parse(dateMatch.Groups[3].Value);
                        }
                        
                        if (year < 100) year += 2000;
                        paymentDate = new DateTime(year, month, day);
                        break;
                    }
                    catch { }
                }
            }

            // Use email received date if payment date not found
            if (!paymentDate.HasValue)
            {
                paymentDate = emailMessage.ReceivedAt.Date;
            }

            if (!paymentAmount.HasValue)
            {
                _logger.LogWarning("Could not extract payment amount from PDF in payment advice email: {EmailId}", emailMessage.Id);
                return false;
            }

            _logger.LogInformation(
                "Extracted payment info: AdviceRef={AdviceRef}, CustomerRef={CustomerRef}, Amount={Amount}, Date={Date}, Method={Method}",
                adviceRef, customerRef, paymentAmount, paymentDate, paymentMethod);

            // Find invoice by Customer Ref or Second Party Ref
            Invoice? invoice = null;
            if (!string.IsNullOrEmpty(customerRef))
            {
                invoice = await _context.Invoices
                    .Where(i => i.InvoiceNumber == customerRef && 
                               (companyId == null || i.CompanyId == companyId))
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (invoice == null && !string.IsNullOrEmpty(secondPartyRef))
            {
                // Try to find by second party ref (might be in a different field or format)
                invoice = await _context.Invoices
                    .Where(i => (i.InvoiceNumber.Contains(secondPartyRef) || 
                                i.SubmissionId == secondPartyRef) &&
                               (companyId == null || i.CompanyId == companyId))
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // Save PDF attachment
            Guid? pdfAttachmentId = null;
            try
            {
                var uploadDto = new FileUploadDto
                {
                    File = pdfFile,
                    Module = "Billing",
                    EntityId = emailMessage.Id,
                    EntityType = "EmailMessage"
                };
                var savedFile = await _fileService.UploadFileAsync(
                    uploadDto, companyId ?? Guid.Empty, Guid.Empty, cancellationToken);
                pdfAttachmentId = savedFile.Id;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save PDF attachment for payment advice email: {EmailId}", emailMessage.Id);
            }

            // Create Payment record (idempotency key prevents duplicate if same email processed twice)
            var createPaymentDto = new CreatePaymentDto
            {
                IdempotencyKey = $"email-payment-{emailMessage.Id}",
                PaymentType = PaymentType.Income, // Payment received from customer/partner
                PaymentMethod = paymentMethod,
                PaymentDate = paymentDate.Value,
                Amount = paymentAmount.Value,
                Currency = "MYR",
                PayerPayeeName = "TIME DotCom Bhd", // Default payer name
                BankReference = adviceRef,
                InvoiceId = invoice?.Id,
                Description = $"Payment Advice - {adviceRef ?? "N/A"}",
                Notes = $"Payment received via email notification (Email ID: {emailMessage.Id})\n" +
                       $"Customer Ref: {customerRef ?? "N/A"}\n" +
                       $"Second Party Ref: {secondPartyRef ?? "N/A"}\n" +
                       $"Second Party ID: {secondPartyId ?? "N/A"}\n" +
                       $"Subject: {subject}",
                AttachmentPath = pdfAttachmentId.HasValue ? pdfAttachmentId.Value.ToString() : null
            };

            var payment = await _paymentService.CreatePaymentAsync(
                createPaymentDto, companyId, Guid.Empty, cancellationToken); // System user

            _logger.LogInformation(
                "✅ Payment created: {PaymentId} (PaymentNumber: {PaymentNumber}), Amount: {Amount}, Invoice: {InvoiceId}",
                payment.Id, payment.PaymentNumber, paymentAmount, invoice?.Id);

            // If invoice was found and linked, it's already updated to Paid by PaymentService
            if (invoice == null)
            {
                _logger.LogWarning(
                    "Invoice not found for Customer Ref '{CustomerRef}' or Second Party Ref '{SecondPartyRef}' in payment advice email. Payment created without invoice link.",
                    customerRef, secondPartyRef);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment advice email {EmailId}", emailMessage.Id);
            return false;
        }
    }

    /// <summary>
    /// Send notifications for VIP emails based on configured recipients
    /// </summary>
    private async Task SendVipEmailNotificationsAsync(
        VipEmail vipEmail,
        EmailMessage emailMessage,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        var recipientUserIds = new List<Guid>();

        // Priority 1: Specific user from VipEmail.NotifyUserId
        if (vipEmail.NotifyUserId.HasValue)
        {
            recipientUserIds.Add(vipEmail.NotifyUserId.Value);
            _logger.LogInformation("VIP notification: Added specific user {UserId}", vipEmail.NotifyUserId.Value);
        }

        // Priority 2: Users by role from VipEmail.NotifyRole
        if (!string.IsNullOrWhiteSpace(vipEmail.NotifyRole))
        {
            var usersByRole = await _notificationService.ResolveUsersByRoleAsync(
                vipEmail.NotifyRole,
                companyId,
                cancellationToken);
            
            recipientUserIds.AddRange(usersByRole);
            _logger.LogInformation("VIP notification: Added {Count} users from role {Role}", usersByRole.Count, vipEmail.NotifyRole);
        }

        // Priority 3: Users by department from VipEmail.DepartmentId
        if (vipEmail.DepartmentId.HasValue)
        {
            var usersByDepartment = await _notificationService.ResolveUsersByDepartmentAsync(
                vipEmail.DepartmentId.Value,
                cancellationToken);
            
            recipientUserIds.AddRange(usersByDepartment);
            _logger.LogInformation("VIP notification: Added {Count} users from department {DepartmentId}", 
                usersByDepartment.Count, vipEmail.DepartmentId.Value);
        }

        // Priority 4: Default VIP recipients from settings (if no specific recipients)
        if (recipientUserIds.Count == 0)
        {
            var defaultRecipients = await _notificationService.GetDefaultVipRecipientsAsync(companyId, cancellationToken);
            recipientUserIds.AddRange(defaultRecipients);
            _logger.LogInformation("VIP notification: Using {Count} default VIP recipients", defaultRecipients.Count);
        }

        // Remove duplicates
        recipientUserIds = recipientUserIds.Distinct().ToList();

        // Create notifications for each recipient
        foreach (var userId in recipientUserIds)
        {
            try
            {
                var notification = new CreateNotificationDto
                {
                    CompanyId = companyId,
                    UserId = userId,
                    Type = "VipEmail",
                    Priority = "High",
                    Title = $"VIP Email from {emailMessage.FromAddress}",
                    Message = $"Subject: {emailMessage.Subject}\n\nPreview: {emailMessage.BodyPreview}",
                    ActionUrl = $"/email?messageId={emailMessage.Id}",
                    ActionText = "View Email",
                    RelatedEntityId = emailMessage.Id,
                    RelatedEntityType = "EmailMessage",
                    DeliveryChannels = "InApp,Email"
                };

                await _notificationService.CreateNotificationAsync(notification, cancellationToken);
                _logger.LogInformation("VIP notification sent to user {UserId} for email {EmailId}", userId, emailMessage.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create VIP notification for user {UserId}", userId);
            }
        }

        if (recipientUserIds.Count > 0)
        {
            _logger.LogInformation("VIP email notification sent to {Count} recipients", recipientUserIds.Count);
        }
        else
        {
            _logger.LogWarning("VIP email detected but no recipients resolved for {From}", vipEmail.EmailAddress);
        }
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

