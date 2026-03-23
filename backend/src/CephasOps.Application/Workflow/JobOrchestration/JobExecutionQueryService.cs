using CephasOps.Domain.Workflow;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>Operational visibility over JobExecution (Phase 4).</summary>
public class JobExecutionQueryService : IJobExecutionQueryService
{
    private readonly ApplicationDbContext _context;

    public JobExecutionQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JobExecutionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pending = await _context.JobExecutions.CountAsync(j => j.Status == JobExecutionStatus.Pending && (j.NextRunAtUtc == null || j.NextRunAtUtc <= now), cancellationToken);
        var running = await _context.JobExecutions.CountAsync(j => j.Status == JobExecutionStatus.Running, cancellationToken);
        var failedRetry = await _context.JobExecutions.CountAsync(j => j.Status == JobExecutionStatus.Failed && j.NextRunAtUtc != null && j.NextRunAtUtc > now, cancellationToken);
        var deadLetter = await _context.JobExecutions.CountAsync(j => j.Status == JobExecutionStatus.DeadLetter, cancellationToken);
        var succeeded = await _context.JobExecutions.CountAsync(j => j.Status == JobExecutionStatus.Succeeded, cancellationToken);
        return new JobExecutionSummaryDto
        {
            PendingCount = pending,
            RunningCount = running,
            FailedRetryScheduledCount = failedRetry,
            DeadLetterCount = deadLetter,
            SucceededCount = succeeded
        };
    }

    public async Task<IReadOnlyList<JobExecutionListItemDto>> GetPendingAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.JobExecutions
            .Where(j => j.Status == JobExecutionStatus.Pending && (j.NextRunAtUtc == null || j.NextRunAtUtc <= now))
            .OrderBy(j => j.Priority).ThenBy(j => j.CreatedAtUtc)
            .Take(limit)
            .Select(j => Map(j))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobExecutionListItemDto>> GetRunningAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .Where(j => j.Status == JobExecutionStatus.Running)
            .OrderBy(j => j.ClaimedAtUtc)
            .Take(limit)
            .Select(j => Map(j))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobExecutionListItemDto>> GetFailedRetryScheduledAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.JobExecutions
            .Where(j => j.Status == JobExecutionStatus.Failed && j.NextRunAtUtc != null && j.NextRunAtUtc > now)
            .OrderBy(j => j.NextRunAtUtc)
            .Take(limit)
            .Select(j => Map(j))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobExecutionListItemDto>> GetDeadLetterAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .Where(j => j.Status == JobExecutionStatus.DeadLetter)
            .OrderByDescending(j => j.UpdatedAtUtc)
            .Take(limit)
            .Select(j => Map(j))
            .ToListAsync(cancellationToken);
    }

    private static JobExecutionListItemDto Map(JobExecution j)
    {
        return new JobExecutionListItemDto
        {
            Id = j.Id,
            JobType = j.JobType,
            Status = j.Status,
            AttemptCount = j.AttemptCount,
            MaxAttempts = j.MaxAttempts,
            NextRunAtUtc = j.NextRunAtUtc,
            CreatedAtUtc = j.CreatedAtUtc,
            StartedAtUtc = j.StartedAtUtc,
            CompletedAtUtc = j.CompletedAtUtc,
            LastError = j.LastError,
            LastErrorAtUtc = j.LastErrorAtUtc,
            CompanyId = j.CompanyId,
            ProcessingNodeId = j.ProcessingNodeId,
            ProcessingLeaseExpiresAtUtc = j.ProcessingLeaseExpiresAtUtc
        };
    }
}
