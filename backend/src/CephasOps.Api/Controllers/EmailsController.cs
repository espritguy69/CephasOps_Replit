using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Files.Services;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Email messages endpoints (inbox and sent)
/// </summary>
[ApiController]
[Route("api/emails")]
[Authorize]
public class EmailsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFileService _fileService;
    private readonly ILogger<EmailsController> _logger;

    public EmailsController(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IFileService fileService,
        ILogger<EmailsController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// Get email messages (inbox or sent)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmailMessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<EmailMessageDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<EmailMessageDto>>>> GetEmails(
        [FromQuery] string? direction = null, // Inbound, Outbound
        [FromQuery] string? status = null,
        [FromQuery] Guid? emailAccountId = null,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var now = DateTime.UtcNow;
            var query = _context.EmailMessages.AsQueryable();
            query = query.Where(e => e.CompanyId == companyId);

            // Exclude expired emails (48-hour TTL)
            query = query.Where(e => e.ExpiresAt > now);

            // Filter by direction
            if (!string.IsNullOrWhiteSpace(direction))
            {
                query = query.Where(e => e.Direction == direction);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(e => e.ParserStatus == status);
            }

            // Filter by email account
            if (emailAccountId.HasValue)
            {
                query = query.Where(e => e.EmailAccountId == emailAccountId.Value);
            }

            // Order by date (most recent first)
            // Use COALESCE in SQL since SentAt is nullable but ReceivedAt is not
            query = query.OrderByDescending(e => e.SentAt.HasValue ? e.SentAt.Value : e.ReceivedAt);

            // Limit results
            if (limit.HasValue && limit.Value > 0)
            {
                query = query.Take(limit.Value);
            }

            var emails = await query.ToListAsync(cancellationToken);

            var dtos = emails.Select(e => new EmailMessageDto
            {
                Id = e.Id,
                EmailAccountId = e.EmailAccountId,
                MessageId = e.MessageId,
                FromAddress = e.FromAddress,
                ToAddresses = e.ToAddresses,
                CcAddresses = e.CcAddresses,
                Subject = e.Subject,
                BodyPreview = e.BodyPreview,
                // Only include BodyText/BodyHtml in list view if needed (for snippet)
                // Full body is returned in GetEmail detail endpoint
                BodyText = null, // Exclude full body from list for performance
                BodyHtml = null, // Exclude full body from list for performance
                ReceivedAt = e.ReceivedAt,
                SentAt = e.SentAt,
                Direction = e.Direction,
                HasAttachments = e.HasAttachments,
                ParserStatus = e.ParserStatus,
                ParserError = e.ParserError,
                IsVip = e.IsVip,
                DepartmentId = e.DepartmentId,
                CompanyId = e.CompanyId,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                ExpiresAt = e.ExpiresAt,
                IsExpired = e.IsExpired
            }).ToList();

            return this.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emails");
            return this.InternalServerError<List<EmailMessageDto>>($"Failed to get emails: {ex.Message}");
        }
    }

    /// <summary>
    /// Get email message by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EmailMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmailMessageDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmailMessageDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmailMessageDto>>> GetEmail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var now = DateTime.UtcNow;
            var query = _context.EmailMessages
                .Include(e => e.Attachments)
                .Where(e => e.Id == id);
            
            if (companyId != Guid.Empty)
            {
                query = query.Where(e => e.CompanyId == companyId);
            }

            var email = await query.FirstOrDefaultAsync(cancellationToken);

            if (email == null)
            {
                return this.NotFound<EmailMessageDto>($"Email message with ID {id} not found");
            }

            // Check if expired
            if (email.ExpiresAt <= now)
            {
                return this.NotFound<EmailMessageDto>($"Email message with ID {id} has expired and is no longer available");
            }

            var dto = new EmailMessageDto
            {
                Id = email.Id,
                EmailAccountId = email.EmailAccountId,
                MessageId = email.MessageId,
                FromAddress = email.FromAddress,
                ToAddresses = email.ToAddresses,
                CcAddresses = email.CcAddresses,
                Subject = email.Subject,
                BodyPreview = email.BodyPreview,
                BodyText = email.BodyText, // Include full body in detail view
                BodyHtml = email.BodyHtml, // Include full body in detail view
                ReceivedAt = email.ReceivedAt,
                SentAt = email.SentAt,
                Direction = email.Direction,
                HasAttachments = email.HasAttachments,
                ParserStatus = email.ParserStatus,
                ParserError = email.ParserError,
                IsVip = email.IsVip,
                DepartmentId = email.DepartmentId,
                CompanyId = email.CompanyId,
                CreatedAt = email.CreatedAt,
                UpdatedAt = email.UpdatedAt,
                ExpiresAt = email.ExpiresAt,
                IsExpired = email.IsExpired,
                Attachments = email.Attachments
                    .Where(a => a.ExpiresAt > now) // Only non-expired attachments
                    .Select(a => new EmailAttachmentDto
                    {
                        Id = a.Id,
                        EmailMessageId = a.EmailMessageId,
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        SizeBytes = a.SizeBytes,
                        IsInline = a.IsInline,
                        ContentId = a.ContentId,
                        ExpiresAt = a.ExpiresAt,
                        IsExpired = a.ExpiresAt <= now
                    }).ToList()
            };

            return this.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email {EmailId}", id);
            return this.InternalServerError<EmailMessageDto>($"Failed to get email: {ex.Message}");
        }
    }

    /// <summary>
    /// Download email attachment (on-demand)
    /// </summary>
    [HttpGet("{emailId}/attachments/{attachmentId}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status410Gone)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DownloadAttachment(
        Guid emailId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var now = DateTime.UtcNow;

        try
        {
            // Verify email exists and is not expired
            var emailQuery = _context.EmailMessages.Where(e => e.Id == emailId);
            if (companyId != Guid.Empty)
            {
                emailQuery = emailQuery.Where(e => e.CompanyId == companyId);
            }

            var email = await emailQuery.FirstOrDefaultAsync(cancellationToken);
            if (email == null)
            {
                return NotFound(new { success = false, message = $"Email message with ID {emailId} not found" });
            }

            if (email.ExpiresAt <= now)
            {
                return StatusCode(410, new { success = false, message = "Email message has expired and is no longer available" });
            }

            // Get attachment
            var attachmentQuery = _context.Set<EmailAttachment>()
                .Where(a => a.Id == attachmentId && a.EmailMessageId == emailId);
            
            if (companyId != Guid.Empty)
            {
                attachmentQuery = attachmentQuery.Where(a => a.CompanyId == companyId);
            }

            var attachment = await attachmentQuery.FirstOrDefaultAsync(cancellationToken);
            if (attachment == null)
            {
                return NotFound(new { success = false, message = $"Attachment with ID {attachmentId} not found" });
            }

            if (attachment.ExpiresAt <= now)
            {
                return StatusCode(410, new { success = false, message = "Attachment has expired and is no longer available" });
            }

            // Download file via FileService
            if (!attachment.FileId.HasValue)
            {
                return NotFound(new { success = false, message = "Attachment file not found (FileId is null)" });
            }

            var (fileStream, fileName, contentType) = await _fileService.DownloadFileAsync(
                attachment.FileId.Value,
                companyId,
                cancellationToken);

            _logger.LogInformation("Downloading attachment {AttachmentId} ({FileName}) for email {EmailId}",
                attachmentId, fileName, emailId);

            return File(fileStream, contentType ?? attachment.ContentType, attachment.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading attachment {AttachmentId} for email {EmailId}", attachmentId, emailId);
            return StatusCode(500, new { success = false, message = $"Failed to download attachment: {ex.Message}" });
        }
    }
}

/// <summary>
/// Email message DTO
/// </summary>
public class EmailMessageDto
{
    public Guid Id { get; set; }
    public Guid EmailAccountId { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddresses { get; set; } = string.Empty;
    public string? CcAddresses { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? BodyPreview { get; set; }
    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string Direction { get; set; } = "Inbound";
    public bool HasAttachments { get; set; }
    public string ParserStatus { get; set; } = "Pending";
    public string? ParserError { get; set; }
    public bool IsVip { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? CompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public List<EmailAttachmentDto>? Attachments { get; set; }
}

/// <summary>
/// Email attachment DTO
/// </summary>
public class EmailAttachmentDto
{
    public Guid Id { get; set; }
    public Guid EmailMessageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsInline { get; set; }
    public string? ContentId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
}

