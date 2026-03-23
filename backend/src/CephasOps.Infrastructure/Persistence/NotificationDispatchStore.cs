using CephasOps.Domain.Notifications;
using CephasOps.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Persists notification delivery work (Phase 2 Notifications boundary).
/// </summary>
public class NotificationDispatchStore : INotificationDispatchStore
{
    private readonly ApplicationDbContext _context;

    public NotificationDispatchStore(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationDispatches
            .AnyAsync(d => d.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(NotificationDispatch dispatch, CancellationToken cancellationToken = default)
    {
        _context.NotificationDispatches.Add(dispatch);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificationDispatch>> ClaimNextPendingBatchAsync(int maxCount, string? nodeId, DateTime? leaseExpiresAtUtc, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
            return Array.Empty<NotificationDispatch>();

        var now = DateTime.UtcNow;
        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        var sql = """
            WITH sel AS (
                SELECT "Id" FROM "NotificationDispatches"
                WHERE "Status" = 'Pending' AND ("NextRetryAtUtc" IS NULL OR "NextRetryAtUtc" <= @now)
                ORDER BY "CreatedAtUtc"
                LIMIT @limit
                FOR UPDATE SKIP LOCKED
            )
            UPDATE "NotificationDispatches" AS d
            SET "Status" = 'Processing', "ProcessingNodeId" = @nodeId, "ProcessingLeaseExpiresAtUtc" = @lease, "UpdatedAtUtc" = @now
            FROM sel
            WHERE d."Id" = sel."Id"
            RETURNING d."Id", d."CompanyId", d."Channel", d."Target", d."TemplateKey", d."PayloadJson", d."Status", d."AttemptCount", d."MaxAttempts",
                d."NextRetryAtUtc", d."CreatedAtUtc", d."UpdatedAtUtc", d."LastError", d."LastErrorAtUtc", d."CorrelationId", d."CausationId", d."SourceEventId", d."IdempotencyKey",
                d."ProcessingNodeId", d."ProcessingLeaseExpiresAtUtc"
            """;

        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.Connection = connection;
        cmd.CommandText = sql;
        AddParam(cmd, "limit", maxCount);
        AddParam(cmd, "now", now, DbType.DateTime);
        AddParam(cmd, "nodeId", (object?)nodeId ?? DBNull.Value);
        AddParam(cmd, "lease", leaseExpiresAtUtc.HasValue ? (object)leaseExpiresAtUtc.Value : DBNull.Value, DbType.DateTime);

        var list = new List<NotificationDispatch>();
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
    public async Task MarkProcessedAsync(Guid id, bool success, string? errorMessage, bool isNonRetryable, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dispatch = await _context.NotificationDispatches.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (dispatch == null) return;

        dispatch.ProcessingNodeId = null;
        dispatch.ProcessingLeaseExpiresAtUtc = null;
        dispatch.UpdatedAtUtc = now;

        if (success)
        {
            dispatch.Status = "Sent";
            dispatch.NextRetryAtUtc = null;
        }
        else
        {
            dispatch.AttemptCount++;
            dispatch.LastError = errorMessage != null ? Truncate(errorMessage, 2000) : null;
            dispatch.LastErrorAtUtc = now;
            if (isNonRetryable || dispatch.AttemptCount >= dispatch.MaxAttempts)
            {
                dispatch.Status = "DeadLetter";
                dispatch.NextRetryAtUtc = null;
            }
            else
            {
                dispatch.Status = "Failed";
                var delaySeconds = dispatch.AttemptCount switch { 1 => 60, 2 => 300, 3 => 900, _ => 3600 };
                dispatch.NextRetryAtUtc = now.AddSeconds(delaySeconds);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value, DbType? dbType = null)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        if (dbType.HasValue) p.DbType = dbType.Value;
        cmd.Parameters.Add(p);
    }

    private static NotificationDispatch MapReader(System.Data.Common.DbDataReader r)
    {
        return new NotificationDispatch
        {
            Id = r.GetGuid(0),
            CompanyId = r.IsDBNull(1) ? null : r.GetGuid(1),
            Channel = r.GetString(2),
            Target = r.GetString(3),
            TemplateKey = r.IsDBNull(4) ? null : r.GetString(4),
            PayloadJson = r.IsDBNull(5) ? null : r.GetString(5),
            Status = r.GetString(6),
            AttemptCount = r.GetInt32(7),
            MaxAttempts = r.GetInt32(8),
            NextRetryAtUtc = r.IsDBNull(9) ? null : r.GetDateTime(9),
            CreatedAtUtc = r.GetDateTime(10),
            UpdatedAtUtc = r.IsDBNull(11) ? null : r.GetDateTime(11),
            LastError = r.IsDBNull(12) ? null : r.GetString(12),
            LastErrorAtUtc = r.IsDBNull(13) ? null : r.GetDateTime(13),
            CorrelationId = r.IsDBNull(14) ? null : r.GetString(14),
            CausationId = r.IsDBNull(15) ? null : r.GetGuid(15),
            SourceEventId = r.IsDBNull(16) ? null : r.GetGuid(16),
            IdempotencyKey = r.IsDBNull(17) ? null : r.GetString(17),
            ProcessingNodeId = r.IsDBNull(18) ? null : r.GetString(18),
            ProcessingLeaseExpiresAtUtc = r.IsDBNull(19) ? null : r.GetDateTime(19)
        };
    }

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
        return value.Length <= maxLen ? value : value[..maxLen];
    }
}
