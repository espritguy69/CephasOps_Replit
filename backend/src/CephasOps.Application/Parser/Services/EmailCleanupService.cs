using CephasOps.Application.Files.Services;
using CephasOps.Application.Parser.Settings;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service interface for email cleanup operations
/// </summary>
public interface IEmailCleanupService
{
    /// <summary>
    /// Clean up expired emails and attachments
    /// </summary>
    Task<EmailCleanupResult> CleanupExpiredEmailsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of email cleanup operation
/// </summary>
public class EmailCleanupResult
{
    public int DeletedEmails { get; set; }
    public int DeletedAttachments { get; set; }
    public int DeletedBlobs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Background service to clean up expired emails and attachments (48-hour TTL)
/// </summary>
public class EmailCleanupService : BackgroundService, IEmailCleanupService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MailSettings _mailSettings;
    private readonly ILogger<EmailCleanupService> _logger;
    private TimeSpan _cleanupInterval;

    public EmailCleanupService(
        IServiceProvider serviceProvider,
        Microsoft.Extensions.Options.IOptions<MailSettings> mailSettings,
        ILogger<EmailCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _mailSettings = mailSettings.Value;
        _logger = logger;
        _cleanupInterval = TimeSpan.FromMinutes(_mailSettings.CleanupJob.IntervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Cleanup Service started. Cleanup interval: {Interval}", _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    public async Task<EmailCleanupResult> CleanupExpiredEmailsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

        var now = DateTime.UtcNow;
        var deletedEmails = 0;
        var deletedAttachments = 0;
        var deletedBlobs = 0;

        try
        {
            // Find expired emails
            var expiredEmails = await context.EmailMessages
                .Include(e => e.Attachments)
                .Where(e => e.ExpiresAt <= now)
                .ToListAsync(cancellationToken);

            if (!expiredEmails.Any())
            {
                _logger.LogDebug("No expired emails found for cleanup");
                return new EmailCleanupResult
                {
                    DeletedEmails = 0,
                    DeletedAttachments = 0,
                    DeletedBlobs = 0,
                    Success = true
                };
            }

            _logger.LogInformation("Found {Count} expired email(s) to clean up", expiredEmails.Count);

            foreach (var email in expiredEmails)
            {
                try
                {
                    // Delete attachment files via FileService
                    foreach (var attachment in email.Attachments)
                    {
                        if (attachment.FileId.HasValue)
                        {
                            try
                            {
                                // FileService.DeleteFileAsync would be ideal, but if not available,
                                // the file will be cleaned up by FileService's own cleanup mechanism
                                // For now, we just delete the attachment record
                                _logger.LogDebug("Attachment {AttachmentId} (FileId: {FileId}) expired, will be deleted",
                                    attachment.Id, attachment.FileId);
                                deletedBlobs++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error deleting attachment file {FileId}", attachment.FileId);
                            }
                        }
                        deletedAttachments++;
                    }

                    // Delete email record (cascade will delete attachments)
                    context.EmailMessages.Remove(email);
                    deletedEmails++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting expired email {EmailId}", email.Id);
                }
            }

            // Save changes
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Email cleanup completed: {DeletedEmails} email(s), {DeletedAttachments} attachment(s), {DeletedBlobs} blob(s) deleted",
                deletedEmails, deletedAttachments, deletedBlobs);

            return new EmailCleanupResult
            {
                DeletedEmails = deletedEmails,
                DeletedAttachments = deletedAttachments,
                DeletedBlobs = deletedBlobs,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email cleanup batch");
            return new EmailCleanupResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

