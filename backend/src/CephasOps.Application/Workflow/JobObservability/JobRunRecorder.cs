using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Workflow.JobObservability;

/// <summary>
/// Records job runs to the JobRuns table for observability.
/// </summary>
public class JobRunRecorder : IJobRunRecorder
{
    private readonly ApplicationDbContext _context;

    public const int ErrorDetailsMaxLength = 2000;
    public const int PayloadSummaryMaxLength = 1000;

    public JobRunRecorder(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Guid> StartAsync(StartJobRunDto dto, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var run = new JobRun
        {
            Id = Guid.NewGuid(),
            BackgroundJobId = dto.BackgroundJobId,
            CompanyId = dto.CompanyId,
            JobName = dto.JobName,
            JobType = dto.JobType,
            TriggerSource = dto.TriggerSource,
            CorrelationId = dto.CorrelationId,
            QueueOrChannel = dto.QueueOrChannel,
            PayloadSummary = Truncate(dto.PayloadSummary, PayloadSummaryMaxLength),
            Status = "Running",
            StartedAtUtc = now,
            RetryCount = dto.RetryCount,
            WorkerNode = dto.WorkerNode,
            InitiatedByUserId = dto.InitiatedByUserId,
            ParentJobRunId = dto.ParentJobRunId,
            RelatedEntityType = dto.RelatedEntityType,
            RelatedEntityId = dto.RelatedEntityId,
            EventId = dto.EventId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        _context.JobRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);
        return run.Id;
    }

    /// <inheritdoc />
    public async Task CompleteAsync(Guid jobRunId, CancellationToken cancellationToken = default)
    {
        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var run = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.JobRuns.FirstOrDefaultAsync(r => r.Id == jobRunId && r.CompanyId == tenantId.Value, cancellationToken)
            : await _context.JobRuns.FirstOrDefaultAsync(r => r.Id == jobRunId, cancellationToken);
        if (run == null) return;
        var now = DateTime.UtcNow;
        run.Status = "Succeeded";
        run.CompletedAtUtc = now;
        run.DurationMs = (long)(now - run.StartedAtUtc).TotalMilliseconds;
        run.UpdatedAtUtc = now;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task FailAsync(Guid jobRunId, FailJobRunDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var run = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.JobRuns.FirstOrDefaultAsync(r => r.Id == jobRunId && r.CompanyId == tenantId.Value, cancellationToken)
            : await _context.JobRuns.FirstOrDefaultAsync(r => r.Id == jobRunId, cancellationToken);
        if (run == null) return;
        var now = DateTime.UtcNow;
        run.Status = dto.Status ?? "Failed";
        run.CompletedAtUtc = now;
        run.DurationMs = (long)(now - run.StartedAtUtc).TotalMilliseconds;
        run.ErrorCode = dto.ErrorCode;
        run.ErrorMessage = Truncate(dto.ErrorMessage, 500);
        run.ErrorDetails = SanitizeAndTruncateErrorDetails(dto.ErrorDetails, ErrorDetailsMaxLength);
        run.UpdatedAtUtc = now;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelAsync(Guid jobRunId, CancellationToken cancellationToken = default)
    {
        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var run = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.JobRuns.FirstOrDefaultAsync(r => r.Id == jobRunId && r.CompanyId == tenantId.Value, cancellationToken)
            : await _context.JobRuns.FirstOrDefaultAsync(r => r.Id == jobRunId, cancellationToken);
        if (run == null) return;
        var now = DateTime.UtcNow;
        run.Status = "Cancelled";
        run.CompletedAtUtc = now;
        run.DurationMs = (long)(now - run.StartedAtUtc).TotalMilliseconds;
        run.UpdatedAtUtc = now;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Build a safe payload summary (no secrets) from JSON payload, truncated.
    /// </summary>
    public static string? BuildPayloadSummary(string? payloadJson, int maxLength = PayloadSummaryMaxLength)
    {
        if (string.IsNullOrEmpty(payloadJson)) return null;
        try
        {
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(payloadJson);
            if (dict == null || dict.Count == 0) return null;
            var parts = new List<string>();
            foreach (var kv in dict)
            {
                var key = kv.Key;
                if (string.Equals(key, "password", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(key, "pwd", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("secret", StringComparison.OrdinalIgnoreCase))
                    continue;
                var val = kv.Value.ValueKind == System.Text.Json.JsonValueKind.String
                    ? kv.Value.GetString()
                    : kv.Value.GetRawText();
                if (string.IsNullOrEmpty(val)) continue;
                if (val.Length > 80) val = val[..80] + "...";
                parts.Add($"{key}={val}");
            }
            var summary = string.Join("; ", parts);
            return Truncate(summary, maxLength);
        }
        catch
        {
            return Truncate(payloadJson, Math.Min(200, maxLength));
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    /// <summary>
    /// Sanitize error details (remove connection strings, tokens) and truncate.
    /// </summary>
    private static string? SanitizeAndTruncateErrorDetails(string? details, int maxLength)
    {
        if (string.IsNullOrEmpty(details)) return details;
        var sanitized = details;
var patterns = new[] {
                ("password=[^;\\s]*", "password=***"),
                ("pwd=[^;\\s]*", "pwd=***"),
                ("Token=[^;\\s]*", "Token=***"),
                ("api[_-]?key=[^;\\s]*", "api_key=***"),
                ("Authorization:\\s*Bearer\\s+\\S+", "Authorization: Bearer ***")
            };
            foreach (var pair in patterns)
            {
                try
                {
                    sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, pair.Item1, pair.Item2, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                catch
                {
                    // ignore regex errors
                }
            }
        return Truncate(sanitized, maxLength);
    }
}
