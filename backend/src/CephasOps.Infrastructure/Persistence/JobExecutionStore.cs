using CephasOps.Domain.Workflow;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Persists job orchestration work (Phase 3).
/// </summary>
public class JobExecutionStore : IJobExecutionStore
{
    private readonly ApplicationDbContext _context;

    public JobExecutionStore(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        _context.JobExecutions.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobExecution>> ClaimNextPendingBatchAsync(int maxCount, string? nodeId, DateTime? leaseExpiresAtUtc, int? maxPerTenant = null, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
            return Array.Empty<JobExecution>();

        var now = DateTime.UtcNow;
        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        string sql;
        if (maxPerTenant.HasValue && maxPerTenant.Value > 0)
        {
            // Tenant fairness: take at most maxPerTenant jobs per CompanyId (round-robin style)
            sql = """
                WITH ranked AS (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY COALESCE("CompanyId", '00000000-0000-0000-0000-000000000000'::uuid) ORDER BY "Priority" DESC, "CreatedAtUtc") AS rn
                    FROM "JobExecutions"
                    WHERE "Status" = 'Pending' AND ("NextRunAtUtc" IS NULL OR "NextRunAtUtc" <= @now)
                    FOR UPDATE SKIP LOCKED
                ),
                sel AS (SELECT "Id" FROM ranked WHERE rn <= @maxPerTenant LIMIT @limit)
                UPDATE "JobExecutions" AS j
                SET "Status" = 'Running', "ProcessingNodeId" = @nodeId, "ProcessingLeaseExpiresAtUtc" = @lease, "ClaimedAtUtc" = @now, "StartedAtUtc" = @now, "UpdatedAtUtc" = @now
                FROM sel
                WHERE j."Id" = sel."Id"
                RETURNING j."Id", j."JobType", j."PayloadJson", j."Status", j."AttemptCount", j."MaxAttempts",
                    j."NextRunAtUtc", j."CreatedAtUtc", j."UpdatedAtUtc", j."StartedAtUtc", j."CompletedAtUtc", j."LastError", j."LastErrorAtUtc",
                    j."CompanyId", j."CorrelationId", j."CausationId", j."ProcessingNodeId", j."ProcessingLeaseExpiresAtUtc", j."ClaimedAtUtc", j."Priority"
                """;
        }
        else
        {
            sql = """
                WITH sel AS (
                    SELECT "Id" FROM "JobExecutions"
                    WHERE "Status" = 'Pending' AND ("NextRunAtUtc" IS NULL OR "NextRunAtUtc" <= @now)
                    ORDER BY "Priority" DESC, "CreatedAtUtc"
                    LIMIT @limit
                    FOR UPDATE SKIP LOCKED
                )
                UPDATE "JobExecutions" AS j
                SET "Status" = 'Running', "ProcessingNodeId" = @nodeId, "ProcessingLeaseExpiresAtUtc" = @lease, "ClaimedAtUtc" = @now, "StartedAtUtc" = @now, "UpdatedAtUtc" = @now
                FROM sel
                WHERE j."Id" = sel."Id"
                RETURNING j."Id", j."JobType", j."PayloadJson", j."Status", j."AttemptCount", j."MaxAttempts",
                    j."NextRunAtUtc", j."CreatedAtUtc", j."UpdatedAtUtc", j."StartedAtUtc", j."CompletedAtUtc", j."LastError", j."LastErrorAtUtc",
                    j."CompanyId", j."CorrelationId", j."CausationId", j."ProcessingNodeId", j."ProcessingLeaseExpiresAtUtc", j."ClaimedAtUtc", j."Priority"
                """;
        }

        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.Connection = connection;
        cmd.CommandText = sql;
        AddParam(cmd, "limit", maxCount);
        AddParam(cmd, "now", now, DbType.DateTime);
        AddParam(cmd, "nodeId", (object?)nodeId ?? DBNull.Value);
        AddParam(cmd, "lease", leaseExpiresAtUtc.HasValue ? (object)leaseExpiresAtUtc.Value : DBNull.Value, DbType.DateTime);
        if (maxPerTenant.HasValue && maxPerTenant.Value > 0)
            AddParam(cmd, "maxPerTenant", maxPerTenant.Value);

        var list = new List<JobExecution>();
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(MapReader(reader));
            }
        }

        tx.Commit();
        return list;
    }

    /// <inheritdoc />
    public async Task MarkSucceededAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var job = await _context.JobExecutions.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
        if (job == null) return;

        job.Status = JobExecutionStatus.Succeeded;
        job.CompletedAtUtc = now;
        job.UpdatedAtUtc = now;
        job.ProcessingNodeId = null;
        job.ProcessingLeaseExpiresAtUtc = null;
        job.NextRunAtUtc = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(Guid id, string? errorMessage, bool isNonRetryable, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var job = await _context.JobExecutions.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
        if (job == null) return;

        job.AttemptCount++;
        job.LastError = errorMessage != null ? Truncate(errorMessage, 2000) : null;
        job.LastErrorAtUtc = now;
        job.UpdatedAtUtc = now;
        job.ProcessingNodeId = null;
        job.ProcessingLeaseExpiresAtUtc = null;

        if (isNonRetryable || job.AttemptCount >= job.MaxAttempts)
        {
            job.Status = JobExecutionStatus.DeadLetter;
            job.CompletedAtUtc = now;
            job.NextRunAtUtc = null;
        }
        else
        {
            job.Status = JobExecutionStatus.Failed;
            var delaySeconds = job.AttemptCount switch { 1 => 60, 2 => 300, 3 => 900, _ => 3600 };
            job.NextRunAtUtc = now.AddSeconds(delaySeconds);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ResetStuckRunningAsync(TimeSpan leaseExpiry, CancellationToken cancellationToken = default)
    {
        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            var now = DateTime.UtcNow;
            var cutoff = now - leaseExpiry;
            var stuck = await _context.JobExecutions
                .Where(j => j.Status == JobExecutionStatus.Running
                    && ((j.ProcessingLeaseExpiresAtUtc != null && j.ProcessingLeaseExpiresAtUtc < now)
                        || (j.ProcessingLeaseExpiresAtUtc == null && (j.StartedAtUtc == null || j.StartedAtUtc < cutoff))))
                .ToListAsync(cancellationToken);
            foreach (var j in stuck)
            {
                j.Status = JobExecutionStatus.Pending;
                j.ProcessingNodeId = null;
                j.ProcessingLeaseExpiresAtUtc = null;
                j.UpdatedAtUtc = now;
            }
            if (stuck.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);
            return stuck.Count;
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value, DbType? dbType = null)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        if (dbType.HasValue) p.DbType = dbType.Value;
        cmd.Parameters.Add(p);
    }

    private static JobExecution MapReader(System.Data.Common.DbDataReader r)
    {
        return new JobExecution
        {
            Id = r.GetGuid(0),
            JobType = r.GetString(1),
            PayloadJson = r.GetString(2),
            Status = r.GetString(3),
            AttemptCount = r.GetInt32(4),
            MaxAttempts = r.GetInt32(5),
            NextRunAtUtc = r.IsDBNull(6) ? null : r.GetDateTime(6),
            CreatedAtUtc = r.GetDateTime(7),
            UpdatedAtUtc = r.IsDBNull(8) ? null : r.GetDateTime(8),
            StartedAtUtc = r.IsDBNull(9) ? null : r.GetDateTime(9),
            CompletedAtUtc = r.IsDBNull(10) ? null : r.GetDateTime(10),
            LastError = r.IsDBNull(11) ? null : r.GetString(11),
            LastErrorAtUtc = r.IsDBNull(12) ? null : r.GetDateTime(12),
            CompanyId = r.IsDBNull(13) ? null : r.GetGuid(13),
            CorrelationId = r.IsDBNull(14) ? null : r.GetString(14),
            CausationId = r.IsDBNull(15) ? null : r.GetGuid(15),
            ProcessingNodeId = r.IsDBNull(16) ? null : r.GetString(16),
            ProcessingLeaseExpiresAtUtc = r.IsDBNull(17) ? null : r.GetDateTime(17),
            ClaimedAtUtc = r.IsDBNull(18) ? null : r.GetDateTime(18),
            Priority = r.GetInt32(19)
        };
    }

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
        return value.Length <= maxLen ? value : value[..maxLen];
    }
}
