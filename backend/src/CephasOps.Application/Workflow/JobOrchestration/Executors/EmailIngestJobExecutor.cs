using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Runs email ingestion for one account (Phase 8). Payload: emailAccountId (guid string).
/// Replaces legacy emailingest BackgroundJob execution.
/// </summary>
public sealed class EmailIngestJobExecutor : IJobExecutor
{
    public string JobType => "emailingest";

    private readonly IEmailIngestionService _emailIngestionService;
    private readonly ILogger<EmailIngestJobExecutor> _logger;

    public EmailIngestJobExecutor(
        IEmailIngestionService emailIngestionService,
        ILogger<EmailIngestJobExecutor> logger)
    {
        _emailIngestionService = emailIngestionService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing email ingest job {JobId}", job.Id);

        if (string.IsNullOrEmpty(job.PayloadJson))
            throw new ArgumentException("Email ingest job requires payload with emailAccountId");

        Guid emailAccountId;
        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(job.PayloadJson);
            if (payload == null || !payload.TryGetValue("emailAccountId", out var el))
                throw new ArgumentException("Email ingest job payload must contain emailAccountId");
            var idStr = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
            if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out emailAccountId))
                throw new ArgumentException("Invalid emailAccountId in payload");
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid email ingest payload JSON", ex);
        }

        var result = await _emailIngestionService.IngestEmailsAsync(emailAccountId, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Email ingestion failed for account {EmailAccountId}: {Error}", emailAccountId, result.ErrorMessage);
            throw new InvalidOperationException(result.ErrorMessage ?? "Email ingestion failed");
        }

        _logger.LogInformation(
            "Email ingest job {JobId} completed for account {EmailAccountId}: EmailsFetched={EmailsFetched}, ParseSessionsCreated={ParseSessions}, DraftsCreated={Drafts}",
            job.Id, emailAccountId, result.EmailsFetched, result.ParseSessionsCreated, result.DraftsCreated);
        return true;
    }
}
