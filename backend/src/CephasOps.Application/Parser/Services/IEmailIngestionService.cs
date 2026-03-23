using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service for ingesting emails from configured mailboxes
/// </summary>
public interface IEmailIngestionService
{
    /// <summary>
    /// Ingest emails from a specific email account
    /// </summary>
    Task<EmailIngestionResultDto> IngestEmailsAsync(Guid emailAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest emails from all active email accounts
    /// </summary>
    Task<List<EmailIngestionResultDto>> IngestAllEmailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually trigger email polling for an account
    /// </summary>
    Task<EmailIngestionResultDto> TriggerPollAsync(Guid emailAccountId, Guid companyId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an email ingestion operation
/// </summary>
public class EmailIngestionResultDto
{
    public Guid EmailAccountId { get; set; }
    public string EmailAccountName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int EmailsFetched { get; set; }
    public int ParseSessionsCreated { get; set; }
    public int DraftsCreated { get; set; }
    public int Errors { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ProcessedEmails { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

