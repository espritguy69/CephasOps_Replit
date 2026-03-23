using CephasOps.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Append-only persistence for domain events. Persist before dispatch for audit and retry.
/// Supports outbox-style AppendInCurrentTransaction and concurrency-safe ClaimNextPendingBatchAsync.
/// </summary>
public class EventStoreRepository : IEventStore
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventStoreRepository>? _logger;

    /// <summary>Retry delays in seconds per attempt: 1→+1min, 2→+5min, 3→+15min, 4→+60min, 5→dead-letter.</summary>
    private static readonly int[] RetryDelaySeconds = { 60, 300, 900, 3600 };

    public EventStoreRepository(ApplicationDbContext context, ILogger<EventStoreRepository>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AppendAsync(IDomainEvent domainEvent, EventStoreEnvelopeMetadata? envelope = null, CancellationToken cancellationToken = default)
    {
        EventStoreConsistencyGuard.RequireTenantOrBypassForAppend("Append");
        var alreadyExists = await _context.Set<EventStoreEntry>().AsNoTracking()
            .AnyAsync(e => e.EventId == domainEvent.EventId, cancellationToken);
        if (alreadyExists)
        {
            EventStoreConsistencyGuard.RequireDuplicateAppendRejected(domainEvent.EventId, domainEvent.CompanyId, _logger);
        }
        AppendInCurrentTransaction(domainEvent, envelope);

        var entry = _context.ChangeTracker.Entries<EventStoreEntry>()
            .Where(e => e.State == EntityState.Added && e.Entity.EventId == domainEvent.EventId)
            .Select(e => e.Entity)
            .FirstOrDefault();
        if (entry == null)
        {
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        EventStoreEntry? parentEntry = null;
        EventStoreEntry? rootEntry = null;
        if (entry.ParentEventId.HasValue)
            parentEntry = await GetByEventIdAsync(entry.ParentEventId.Value, cancellationToken);
        if (entry.RootEventId.HasValue && entry.RootEventId.Value != entry.EventId)
            rootEntry = await GetByEventIdAsync(entry.RootEventId.Value, cancellationToken);
        EventStoreConsistencyGuard.RequireParentRootCompanyMatch(entry, parentEntry, rootEntry);

        if (!string.IsNullOrWhiteSpace(entry.EntityType) || entry.EntityId.HasValue)
        {
            var priorInStream = await _context.Set<EventStoreEntry>()
                .AsNoTracking()
                .Where(e => e.EntityType == entry.EntityType && e.EntityId == entry.EntityId && e.EventId != entry.EventId)
                .ToListAsync(cancellationToken);
            EventStoreConsistencyGuard.RequireStreamConsistency(entry, priorInStream);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void AppendInCurrentTransaction(IDomainEvent domainEvent, EventStoreEnvelopeMetadata? envelope = null)
    {
        EventStoreConsistencyGuard.RequireTenantOrBypassForAppend("AppendInCurrentTransaction");
        var payload = JsonSerializer.Serialize(domainEvent);
        var now = DateTime.UtcNow;
        var entry = new EventStoreEntry
        {
            EventId = domainEvent.EventId,
            EventType = domainEvent.EventType,
            Payload = payload,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
            CreatedAtUtc = now,
            RetryCount = 0,
            Status = "Pending",
            CorrelationId = domainEvent.CorrelationId,
            CompanyId = domainEvent.CompanyId,
            TriggeredByUserId = domainEvent.TriggeredByUserId,
            Source = domainEvent.Source,
            PayloadVersion = domainEvent.Version,
            CausationId = domainEvent.CausationId
        };
        if (domainEvent is IHasEntityContext entityContext)
        {
            entry.EntityType = entityContext.EntityType;
            entry.EntityId = entityContext.EntityId;
        }
        if (domainEvent is IHasParentEvent parentEvent && parentEvent.ParentEventId.HasValue)
            entry.ParentEventId = parentEvent.ParentEventId;
        // Phase 8: platform envelope
        entry.RootEventId = envelope?.RootEventId ?? (domainEvent is IHasRootEvent root ? root.RootEventId : null);
        entry.PartitionKey = envelope?.PartitionKey;
        entry.ReplayId = envelope?.ReplayId;
        entry.SourceService = envelope?.SourceService;
        entry.SourceModule = envelope?.SourceModule;
        entry.CapturedAtUtc = envelope?.CapturedAtUtc ?? now;
        entry.IdempotencyKey = envelope?.IdempotencyKey;
        entry.TraceId = envelope?.TraceId;
        entry.SpanId = envelope?.SpanId;
        entry.Priority = envelope?.Priority;

        EventStoreConsistencyGuard.RequireEventMetadata(entry);
        EventStoreConsistencyGuard.RequireCompanyWhenEntityContext(entry);
        EventStoreConsistencyGuard.RequireValidParentRootLinkage(entry);

        // Same-transaction stream consistency: reject if another event for the same entity stream (same EntityType+EntityId) was already added in this context with different company/entity-type
        if (!string.IsNullOrWhiteSpace(entry.EntityType) || entry.EntityId.HasValue)
        {
            var priorInStream = _context.ChangeTracker.Entries<EventStoreEntry>()
                .Where(e => e.State == EntityState.Added && e.Entity.EventId != entry.EventId)
                .Where(e => e.Entity.EntityType == entry.EntityType && e.Entity.EntityId == entry.EntityId)
                .Select(e => e.Entity)
                .ToList();
            EventStoreConsistencyGuard.RequireStreamConsistency(entry, priorInStream);
        }

        _context.Set<EventStoreEntry>().Add(entry);
    }

    /// <inheritdoc />
    public async Task MarkAsProcessingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Set<EventStoreEntry>().FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (entry == null) return;
        var now = DateTime.UtcNow;
        entry.Status = "Processing";
        entry.ProcessingStartedAtUtc = now;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EventStoreMarkProcessedResult?> MarkProcessedAsync(Guid eventId, bool success, string? errorMessage = null, string? lastHandler = null, string? errorType = null, bool isNonRetryable = false, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Set<EventStoreEntry>().FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (entry == null) return null;

        var now = DateTime.UtcNow;
        var createdAt = entry.CreatedAtUtc;
        entry.ProcessedAtUtc = now;
        entry.LastHandler = lastHandler;
        entry.ProcessingStartedAtUtc = null;
        // Clear lease on completion (success or failure)
        entry.ProcessingNodeId = null;
        entry.ProcessingLeaseExpiresAtUtc = null;
        entry.LastClaimedAtUtc = null;

        if (success)
        {
            entry.Status = "Processed";
            entry.NextRetryAtUtc = null;
        }
        else
        {
            entry.RetryCount++;
            entry.LastErrorType = errorType != null ? Truncate(errorType, 100) : null;
            entry.LastError = errorMessage != null ? Truncate(errorMessage, 2000) : null;
            entry.LastErrorAtUtc = now;
            if (isNonRetryable)
            {
                entry.Status = "DeadLetter";
                entry.NextRetryAtUtc = null;
                _logger?.LogWarning("Event dead-lettered (non-retryable). EventId={EventId}, EventType={EventType}, CompanyId={CompanyId}, ErrorType={ErrorType}, LastError={LastError}",
                    entry.EventId, entry.EventType, entry.CompanyId, entry.LastErrorType, entry.LastError);
            }
            else
            {
                entry.Status = entry.RetryCount >= MaxRetriesBeforeDeadLetter ? "DeadLetter" : "Failed";
                entry.NextRetryAtUtc = entry.Status == "Failed"
                    ? now.AddSeconds(GetRetryDelaySeconds(entry.RetryCount))
                    : (DateTime?)null;

                if (entry.Status == "DeadLetter")
                    _logger?.LogWarning("Event dead-lettered. EventId={EventId}, EventType={EventType}, CompanyId={CompanyId}, CorrelationId={CorrelationId}, Attempts={Attempts}, LastError={LastError}",
                        entry.EventId, entry.EventType, entry.CompanyId, entry.CorrelationId, entry.RetryCount, entry.LastError);
                else
                    _logger?.LogInformation("Event retry scheduled. EventId={EventId}, EventType={EventType}, CompanyId={CompanyId}, CorrelationId={CorrelationId}, Attempt={Attempt}, NextRetryAtUtc={NextRetryAtUtc}",
                        entry.EventId, entry.EventType, entry.CompanyId, entry.CorrelationId, entry.RetryCount, entry.NextRetryAtUtc);
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
        return new EventStoreMarkProcessedResult
        {
            Success = success,
            NewStatus = entry.Status,
            EventType = entry.EventType,
            CompanyId = entry.CompanyId,
            LastHandler = entry.LastHandler,
            CreatedAtUtc = createdAt,
            ProcessedAtUtc = now,
            IsNonRetryable = isNonRetryable,
            RetryCount = entry.RetryCount
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventStoreEntry>> ClaimNextPendingBatchAsync(int maxCount, int maxRetriesBeforeDeadLetter, CancellationToken cancellationToken = default, string? nodeId = null, DateTime? leaseExpiresAtUtc = null)
    {
        if (!_context.Database.IsRelational())
            return new List<EventStoreEntry>();

        var now = DateTime.UtcNow;
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        // Use underlying ADO.NET transaction for raw command (EF Core Relational)
        var underlyingTransaction = transaction.GetDbTransaction();

        // Claim rows with FOR UPDATE SKIP LOCKED; stamp node and lease when provided (Phase 7).
        var sql = """
            WITH claimed AS (
                SELECT "EventId" FROM "EventStore"
                WHERE ("Status" = 'Pending' AND ("NextRetryAtUtc" IS NULL OR "NextRetryAtUtc" <= @now)
                    OR ("Status" = 'Failed' AND "NextRetryAtUtc" IS NOT NULL AND "NextRetryAtUtc" <= @now AND "RetryCount" < @maxRetries))
                ORDER BY "PartitionKey" NULLS LAST, "CreatedAtUtc", "EventId"
                LIMIT @limit
                FOR UPDATE SKIP LOCKED
            )
            UPDATE "EventStore" AS e
            SET "Status" = 'Processing', "ProcessingStartedAtUtc" = @now,
                "ProcessingNodeId" = @nodeId, "ProcessingLeaseExpiresAtUtc" = @leaseExpiresAtUtc,
                "LastClaimedAtUtc" = @now, "LastClaimedBy" = @nodeId
            FROM claimed
            WHERE e."EventId" = claimed."EventId"
            RETURNING e."EventId", e."EventType", e."Payload", e."OccurredAtUtc", e."CreatedAtUtc", e."ProcessedAtUtc",
                e."RetryCount", e."Status", e."CorrelationId", e."CompanyId", e."TriggeredByUserId", e."Source",
                e."EntityType", e."EntityId", e."LastError", e."LastErrorAtUtc", e."LastHandler", e."ParentEventId",
                e."CausationId", e."NextRetryAtUtc", e."PayloadVersion", e."ProcessingStartedAtUtc",
                e."ProcessingNodeId", e."ProcessingLeaseExpiresAtUtc", e."LastClaimedAtUtc", e."LastClaimedBy",
                e."RootEventId", e."PartitionKey", e."ReplayId", e."SourceService", e."SourceModule", e."CapturedAtUtc",
                e."IdempotencyKey", e."TraceId", e."SpanId", e."Priority"
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = underlyingTransaction;
        var pLimit = cmd.CreateParameter();
        pLimit.ParameterName = "limit";
        pLimit.Value = maxCount;
        cmd.Parameters.Add(pLimit);
        var pNow = cmd.CreateParameter();
        pNow.ParameterName = "now";
        pNow.Value = now;
        pNow.DbType = System.Data.DbType.DateTime;
        cmd.Parameters.Add(pNow);
        var pMaxRetries = cmd.CreateParameter();
        pMaxRetries.ParameterName = "maxRetries";
        pMaxRetries.Value = maxRetriesBeforeDeadLetter;
        cmd.Parameters.Add(pMaxRetries);
        var pNodeId = cmd.CreateParameter();
        pNodeId.ParameterName = "nodeId";
        pNodeId.Value = (object?)nodeId ?? DBNull.Value;
        cmd.Parameters.Add(pNodeId);
        var pLease = cmd.CreateParameter();
        pLease.ParameterName = "leaseExpiresAtUtc";
        pLease.Value = leaseExpiresAtUtc.HasValue ? (object)leaseExpiresAtUtc.Value : DBNull.Value;
        pLease.DbType = System.Data.DbType.DateTime;
        cmd.Parameters.Add(pLease);

        var list = new List<EventStoreEntry>();
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(MapReaderToEntry(reader));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return list;
    }

    private static EventStoreEntry MapReaderToEntry(System.Data.Common.DbDataReader reader)
    {
        var e = new EventStoreEntry
        {
            EventId = reader.GetGuid(0),
            EventType = reader.GetString(1),
            Payload = reader.GetString(2),
            OccurredAtUtc = reader.GetDateTime(3),
            CreatedAtUtc = reader.GetDateTime(4),
            ProcessedAtUtc = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            RetryCount = reader.GetInt32(6),
            Status = reader.GetString(7),
            CorrelationId = reader.IsDBNull(8) ? null : reader.GetString(8),
            CompanyId = reader.IsDBNull(9) ? null : reader.GetGuid(9),
            TriggeredByUserId = reader.IsDBNull(10) ? null : reader.GetGuid(10),
            Source = reader.IsDBNull(11) ? null : reader.GetString(11),
            EntityType = reader.IsDBNull(12) ? null : reader.GetString(12),
            EntityId = reader.IsDBNull(13) ? null : reader.GetGuid(13),
            LastError = reader.IsDBNull(14) ? null : reader.GetString(14),
            LastErrorAtUtc = reader.IsDBNull(15) ? null : reader.GetDateTime(15),
            LastHandler = reader.IsDBNull(16) ? null : reader.GetString(16),
            ParentEventId = reader.IsDBNull(17) ? null : reader.GetGuid(17),
            CausationId = reader.IsDBNull(18) ? null : reader.GetGuid(18),
            NextRetryAtUtc = reader.IsDBNull(19) ? null : reader.GetDateTime(19),
            PayloadVersion = reader.IsDBNull(20) ? null : reader.GetString(20),
            ProcessingStartedAtUtc = reader.IsDBNull(21) ? null : reader.GetDateTime(21),
            ProcessingNodeId = reader.IsDBNull(22) ? null : reader.GetString(22),
            ProcessingLeaseExpiresAtUtc = reader.IsDBNull(23) ? null : reader.GetDateTime(23),
            LastClaimedAtUtc = reader.IsDBNull(24) ? null : reader.GetDateTime(24),
            LastClaimedBy = reader.IsDBNull(25) ? null : reader.GetString(25),
            // Phase 8 (columns 26-35; reader may have fewer if migration not applied)
            RootEventId = ReaderGuid(reader, 26),
            PartitionKey = ReaderString(reader, 27),
            ReplayId = ReaderString(reader, 28),
            SourceService = ReaderString(reader, 29),
            SourceModule = ReaderString(reader, 30),
            CapturedAtUtc = ReaderDateTime(reader, 31),
            IdempotencyKey = ReaderString(reader, 32),
            TraceId = ReaderString(reader, 33),
            SpanId = ReaderString(reader, 34),
            Priority = ReaderString(reader, 35)
        };
        return e;
    }

    private static Guid? ReaderGuid(System.Data.Common.DbDataReader reader, int ordinal)
    {
        if (ordinal >= reader.FieldCount) return null;
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    private static string? ReaderString(System.Data.Common.DbDataReader reader, int ordinal)
    {
        if (ordinal >= reader.FieldCount) return null;
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? ReaderDateTime(System.Data.Common.DbDataReader reader, int ordinal)
    {
        if (ordinal >= reader.FieldCount) return null;
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    /// <inheritdoc />
    public Task<int> ResetStuckProcessingAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
            return Task.FromResult(0);

        return _context.Database.CreateExecutionStrategy().ExecuteAsync(async (ct) =>
        {
            var now = DateTime.UtcNow;
            var cutoff = now - timeout;
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(ct);
            var underlyingTransaction = transaction.GetDbTransaction();

            // Stuck = lease expired OR (ProcessingStartedAtUtc or CreatedAtUtc) older than timeout (Phase 7).
            var selectSql = """
                SELECT "EventId", "EventType", "CompanyId", "LastHandler", "RetryCount", "ProcessingStartedAtUtc", "CreatedAtUtc", "ProcessingNodeId"
                FROM "EventStore"
                WHERE "Status" = 'Processing'
                  AND (("ProcessingLeaseExpiresAtUtc" IS NOT NULL AND "ProcessingLeaseExpiresAtUtc" < @now)
                       OR ("ProcessingStartedAtUtc" IS NOT NULL AND "ProcessingStartedAtUtc" < @cutoff)
                       OR ("ProcessingStartedAtUtc" IS NULL AND "CreatedAtUtc" < @cutoff))
                """;
            var stuckList = new List<(Guid EventId, string EventType, Guid? CompanyId, string? LastHandler, int RetryCount, DateTime StartedAtUtc, string? NodeId)>();
            await using (var selectCmd = connection.CreateCommand())
            {
                selectCmd.Transaction = underlyingTransaction;
                selectCmd.CommandText = selectSql;
                var pCutoff = selectCmd.CreateParameter();
                pCutoff.ParameterName = "cutoff";
                pCutoff.Value = cutoff;
                pCutoff.DbType = System.Data.DbType.DateTime;
                selectCmd.Parameters.Add(pCutoff);
                var pNowSel = selectCmd.CreateParameter();
                pNowSel.ParameterName = "now";
                pNowSel.Value = now;
                pNowSel.DbType = System.Data.DbType.DateTime;
                selectCmd.Parameters.Add(pNowSel);
                await using var reader = await selectCmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var eventId = reader.GetGuid(0);
                    var eventType = reader.GetString(1);
                    var companyId = reader.IsDBNull(2) ? (Guid?)null : reader.GetGuid(2);
                    var lastHandler = reader.IsDBNull(3) ? null : reader.GetString(3);
                    var retryCount = reader.GetInt32(4);
                    var startedAt = reader.IsDBNull(5) ? reader.GetDateTime(6) : reader.GetDateTime(5);
                    var nodeId = reader.IsDBNull(7) ? null : reader.GetString(7);
                    stuckList.Add((eventId, eventType, companyId, lastHandler, retryCount, startedAt, nodeId));
                    _logger?.LogWarning(
                        "Stuck Processing event reset for retry. EventId={EventId}, EventType={EventType}, CompanyId={CompanyId}, RetryCount={RetryCount}, AgeSeconds={AgeSeconds}",
                        eventId, eventType, companyId, retryCount, (now - startedAt).TotalSeconds);
                }
            }

            foreach (var s in stuckList)
            {
                _context.Set<EventStoreAttemptHistory>().Add(new EventStoreAttemptHistory
                {
                    EventId = s.EventId,
                    EventType = s.EventType,
                    CompanyId = s.CompanyId,
                    HandlerName = s.LastHandler ?? "Dispatcher",
                    AttemptNumber = s.RetryCount + 1,
                    Status = "RecoveredFromStuck",
                    StartedAtUtc = s.StartedAtUtc,
                    FinishedAtUtc = now,
                    DurationMs = (int)(now - s.StartedAtUtc).TotalMilliseconds,
                    ProcessingNodeId = s.NodeId,
                    ErrorMessage = "Stuck processing reset for retry",
                    WasRetried = true,
                    WasDeadLettered = false
                });
            }
            await _context.SaveChangesAsync(ct);

            var updateSql = """
                UPDATE "EventStore"
                SET "Status" = 'Failed', "NextRetryAtUtc" = @now, "ProcessingStartedAtUtc" = NULL,
                    "ProcessingNodeId" = NULL, "ProcessingLeaseExpiresAtUtc" = NULL, "LastClaimedAtUtc" = NULL, "LastClaimedBy" = NULL
                WHERE "Status" = 'Processing'
                  AND (("ProcessingLeaseExpiresAtUtc" IS NOT NULL AND "ProcessingLeaseExpiresAtUtc" < @now)
                       OR ("ProcessingStartedAtUtc" IS NOT NULL AND "ProcessingStartedAtUtc" < @cutoff)
                       OR ("ProcessingStartedAtUtc" IS NULL AND "CreatedAtUtc" < @cutoff))
                """;
            await using var updateCmd = connection.CreateCommand();
            updateCmd.Transaction = underlyingTransaction;
            updateCmd.CommandText = updateSql;
            var pCutoff2 = updateCmd.CreateParameter();
            pCutoff2.ParameterName = "cutoff";
            pCutoff2.Value = cutoff;
            pCutoff2.DbType = System.Data.DbType.DateTime;
            updateCmd.Parameters.Add(pCutoff2);
            var pNow = updateCmd.CreateParameter();
            pNow.ParameterName = "now";
            pNow.Value = now;
            pNow.DbType = System.Data.DbType.DateTime;
            updateCmd.Parameters.Add(pNow);
            var count = await updateCmd.ExecuteNonQueryAsync(ct);
            await transaction.CommitAsync(ct);
            if (count > 0)
                _logger?.LogInformation("Reset {Count} stuck Processing event(s) to Failed for retry. TimeoutMinutes={TimeoutMinutes}", count, timeout.TotalMinutes);
            return count;
        }, cancellationToken);
    }

    private static int GetRetryDelaySeconds(int retryCount)
    {
        var index = Math.Min(Math.Max(0, retryCount - 1), RetryDelaySeconds.Length - 1);
        return RetryDelaySeconds[index];
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    /// <inheritdoc />
    public async Task<EventStoreEntry?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<EventStoreEntry>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ResetDeadLetterToPendingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Set<EventStoreEntry>().FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (entry == null || entry.Status != "DeadLetter")
            return false;
        entry.Status = "Pending";
        entry.NextRetryAtUtc = null;
        entry.ProcessingStartedAtUtc = null;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> BulkResetDeadLetterToPendingAsync(EventStoreBulkFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = await BuildBulkQuery(filter, "DeadLetter").Select(e => e.EventId).Take(filter.MaxCount).ToListAsync(cancellationToken);
        if (ids.Count == 0) return 0;
        var count = await _context.Set<EventStoreEntry>()
            .Where(e => ids.Contains(e.EventId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, "Pending")
                .SetProperty(e => e.NextRetryAtUtc, (DateTime?)null)
                .SetProperty(e => e.ProcessingStartedAtUtc, (DateTime?)null)
                .SetProperty(e => e.ProcessingNodeId, (string?)null)
                .SetProperty(e => e.ProcessingLeaseExpiresAtUtc, (DateTime?)null)
                .SetProperty(e => e.LastClaimedAtUtc, (DateTime?)null)
                .SetProperty(e => e.LastClaimedBy, (string?)null), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<int> BulkResetFailedToPendingAsync(EventStoreBulkFilter filter, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var ids = await BuildBulkQuery(filter, "Failed")
            .Where(e => e.NextRetryAtUtc != null && e.NextRetryAtUtc <= now)
            .Select(e => e.EventId)
            .Take(filter.MaxCount)
            .ToListAsync(cancellationToken);
        if (ids.Count == 0) return 0;
        var count = await _context.Set<EventStoreEntry>()
            .Where(e => ids.Contains(e.EventId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, "Pending")
                .SetProperty(e => e.NextRetryAtUtc, (DateTime?)null)
                .SetProperty(e => e.ProcessingStartedAtUtc, (DateTime?)null)
                .SetProperty(e => e.ProcessingNodeId, (string?)null)
                .SetProperty(e => e.ProcessingLeaseExpiresAtUtc, (DateTime?)null)
                .SetProperty(e => e.LastClaimedAtUtc, (DateTime?)null)
                .SetProperty(e => e.LastClaimedBy, (string?)null), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<int> BulkResetStuckAsync(EventStoreBulkFilter filter, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - timeout;
        var ids = await _context.Set<EventStoreEntry>().AsNoTracking()
            .Where(e => e.Status == "Processing")
            .Where(e => (e.ProcessingLeaseExpiresAtUtc != null && e.ProcessingLeaseExpiresAtUtc < now)
                || (e.ProcessingStartedAtUtc != null && e.ProcessingStartedAtUtc < cutoff)
                || (e.ProcessingStartedAtUtc == null && e.CreatedAtUtc < cutoff))
            .Where(e => !filter.CompanyId.HasValue || e.CompanyId == filter.CompanyId.Value)
            .Where(e => string.IsNullOrEmpty(filter.EventType) || e.EventType == filter.EventType)
            .Where(e => !filter.FromUtc.HasValue || e.OccurredAtUtc >= filter.FromUtc.Value)
            .Where(e => !filter.ToUtc.HasValue || e.OccurredAtUtc <= filter.ToUtc.Value)
            .Where(e => !filter.RetryCountMin.HasValue || e.RetryCount >= filter.RetryCountMin.Value)
            .Where(e => !filter.RetryCountMax.HasValue || e.RetryCount <= filter.RetryCountMax.Value)
            .Select(e => e.EventId)
            .Take(filter.MaxCount)
            .ToListAsync(cancellationToken);
        if (ids.Count == 0) return 0;
        var count = await _context.Set<EventStoreEntry>()
            .Where(e => ids.Contains(e.EventId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, "Failed")
                .SetProperty(e => e.NextRetryAtUtc, now)
                .SetProperty(e => e.ProcessingStartedAtUtc, (DateTime?)null)
                .SetProperty(e => e.ProcessingNodeId, (string?)null)
                .SetProperty(e => e.ProcessingLeaseExpiresAtUtc, (DateTime?)null)
                .SetProperty(e => e.LastClaimedAtUtc, (DateTime?)null)
                .SetProperty(e => e.LastClaimedBy, (string?)null), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<int> BulkCancelPendingAsync(EventStoreBulkFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = await BuildBulkQuery(filter, "Pending").Select(e => e.EventId).Take(filter.MaxCount).ToListAsync(cancellationToken);
        if (ids.Count == 0) return 0;
        var count = await _context.Set<EventStoreEntry>()
            .Where(e => ids.Contains(e.EventId))
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, "Cancelled"), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<int> CountByBulkFilterAsync(EventStoreBulkFilter filter, string status, CancellationToken cancellationToken = default)
    {
        var q = BuildBulkQuery(filter, status);
        return await q.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountFailedDueForRetryByFilterAsync(EventStoreBulkFilter filter, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var q = BuildBulkQuery(filter, "Failed")
            .Where(e => e.NextRetryAtUtc != null && e.NextRetryAtUtc <= now);
        return await q.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountStuckByFilterAsync(EventStoreBulkFilter filter, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - timeout;
        var q = _context.Set<EventStoreEntry>().AsNoTracking()
            .Where(e => e.Status == "Processing")
            .Where(e => (e.ProcessingLeaseExpiresAtUtc != null && e.ProcessingLeaseExpiresAtUtc < now)
                || (e.ProcessingStartedAtUtc != null && e.ProcessingStartedAtUtc < cutoff)
                || (e.ProcessingStartedAtUtc == null && e.CreatedAtUtc < cutoff));
        if (filter.CompanyId.HasValue) q = q.Where(e => e.CompanyId == filter.CompanyId.Value);
        if (!string.IsNullOrEmpty(filter.EventType)) q = q.Where(e => e.EventType == filter.EventType);
        if (filter.FromUtc.HasValue) q = q.Where(e => e.OccurredAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) q = q.Where(e => e.OccurredAtUtc <= filter.ToUtc.Value);
        if (filter.RetryCountMin.HasValue) q = q.Where(e => e.RetryCount >= filter.RetryCountMin.Value);
        if (filter.RetryCountMax.HasValue) q = q.Where(e => e.RetryCount <= filter.RetryCountMax.Value);
        return await q.CountAsync(cancellationToken);
    }

    private IQueryable<EventStoreEntry> BuildBulkQuery(EventStoreBulkFilter filter, string status)
    {
        var q = _context.Set<EventStoreEntry>().AsNoTracking()
            .Where(e => e.Status == status);
        if (filter.CompanyId.HasValue)
            q = q.Where(e => e.CompanyId == filter.CompanyId.Value);
        if (!string.IsNullOrEmpty(filter.EventType))
            q = q.Where(e => e.EventType == filter.EventType);
        if (filter.FromUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            q = q.Where(e => e.OccurredAtUtc <= filter.ToUtc.Value);
        if (filter.RetryCountMin.HasValue)
            q = q.Where(e => e.RetryCount >= filter.RetryCountMin.Value);
        if (filter.RetryCountMax.HasValue)
            q = q.Where(e => e.RetryCount <= filter.RetryCountMax.Value);
        return q;
    }

    /// <summary>After this many handler failures, event is marked DeadLetter (poison).</summary>
    public const int MaxRetriesBeforeDeadLetter = 5;
}
