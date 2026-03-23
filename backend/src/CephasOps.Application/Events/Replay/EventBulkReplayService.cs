using CephasOps.Application.Events.DTOs;
using CephasOps.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Bulk operator actions for event store (Phase 7). All actions are audited and support dry-run.
/// </summary>
public class EventBulkReplayService : IEventBulkReplayService
{
    private readonly IEventStore _eventStore;
    private readonly EventBusDispatcherMetrics? _metrics;
    private readonly ILogger<EventBulkReplayService> _logger;
    private const int DefaultBulkMaxCount = 1000;

    public EventBulkReplayService(
        IEventStore eventStore,
        ILogger<EventBulkReplayService> logger,
        EventBusDispatcherMetrics? metrics = null)
    {
        _eventStore = eventStore;
        _logger = logger;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public async Task<BulkActionResult> ReplayDeadLetterByFilterAsync(EventStoreFilterDto filter, Guid? scopeCompanyId, Guid? initiatedByUserId, bool dryRun, CancellationToken cancellationToken = default)
    {
        var bulkFilter = ToBulkFilter(filter, scopeCompanyId);
        if (dryRun)
        {
            var count = await CountMatchingAsync("DeadLetter", bulkFilter, scopeCompanyId, cancellationToken);
            _logger.LogInformation("Bulk replay dead-letter dry-run. Would affect {Count} event(s). InitiatedBy={UserId}, Filter={Filter}",
                count, initiatedByUserId, DescribeFilter(filter));
            return new BulkActionResult { Success = true, CountAffected = count, DryRun = true };
        }
        try
        {
            var count = await _eventStore.BulkResetDeadLetterToPendingAsync(bulkFilter, cancellationToken);
            _logger.LogInformation("Bulk replay dead-letter completed. CountAffected={Count}, InitiatedBy={UserId}, Filter={Filter}",
                count, initiatedByUserId, DescribeFilter(filter));
            _metrics?.RecordBulkReplayed(count);
            return new BulkActionResult { Success = true, CountAffected = count, DryRun = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk replay dead-letter failed. Filter={Filter}", DescribeFilter(filter));
            return new BulkActionResult { Success = false, CountAffected = 0, ErrorMessage = ex.Message, DryRun = false };
        }
    }

    /// <inheritdoc />
    public async Task<BulkActionResult> ReplayFailedByFilterAsync(EventStoreFilterDto filter, Guid? scopeCompanyId, Guid? initiatedByUserId, bool dryRun, CancellationToken cancellationToken = default)
    {
        var bulkFilter = ToBulkFilter(filter, scopeCompanyId);
        if (dryRun)
        {
            var count = await CountFailedDueForRetryAsync(bulkFilter, scopeCompanyId, cancellationToken);
            _logger.LogInformation("Bulk replay failed (due retry) dry-run. Would affect {Count} event(s). InitiatedBy={UserId}, Filter={Filter}",
                count, initiatedByUserId, DescribeFilter(filter));
            return new BulkActionResult { Success = true, CountAffected = count, DryRun = true };
        }
        try
        {
            var count = await _eventStore.BulkResetFailedToPendingAsync(bulkFilter, cancellationToken);
            _logger.LogInformation("Bulk replay failed completed. CountAffected={Count}, InitiatedBy={UserId}, Filter={Filter}",
                count, initiatedByUserId, DescribeFilter(filter));
            _metrics?.RecordBulkReplayed(count);
            return new BulkActionResult { Success = true, CountAffected = count, DryRun = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk replay failed failed. Filter={Filter}", DescribeFilter(filter));
            return new BulkActionResult { Success = false, CountAffected = 0, ErrorMessage = ex.Message, DryRun = false };
        }
    }

    /// <inheritdoc />
    public async Task<BulkActionResult> ResetStuckByFilterAsync(EventStoreFilterDto filter, Guid? scopeCompanyId, bool dryRun, CancellationToken cancellationToken = default)
    {
        var bulkFilter = ToBulkFilter(filter, scopeCompanyId);
        var timeout = TimeSpan.FromMinutes(15);
        if (dryRun)
        {
            var count = await _eventStore.CountStuckByFilterAsync(bulkFilter, timeout, cancellationToken);
            _logger.LogInformation("Bulk reset stuck dry-run. Would affect {Count} event(s). Filter={Filter}", count, DescribeFilter(filter));
            return new BulkActionResult { Success = true, CountAffected = count, DryRun = true };
        }
        try
        {
            var count = await _eventStore.BulkResetStuckAsync(bulkFilter, timeout, cancellationToken);
            _logger.LogInformation("Bulk reset stuck completed. CountAffected={Count}, Filter={Filter}", count, DescribeFilter(filter));
            return new BulkActionResult { Success = true, CountAffected = count, DryRun = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk reset stuck failed. Filter={Filter}", DescribeFilter(filter));
            return new BulkActionResult { Success = false, CountAffected = 0, ErrorMessage = ex.Message, DryRun = false };
        }
    }

    /// <inheritdoc />
    public async Task<BulkActionResult> CancelPendingByFilterAsync(EventStoreFilterDto filter, Guid? scopeCompanyId, Guid? initiatedByUserId, bool dryRun, CancellationToken cancellationToken = default)
    {
        var bulkFilter = ToBulkFilter(filter, scopeCompanyId);
        if (dryRun)
        {
            var count = await CountMatchingAsync("Pending", bulkFilter, scopeCompanyId, cancellationToken);
            _logger.LogInformation("Bulk cancel pending dry-run. Would affect {Count} event(s). InitiatedBy={UserId}, Filter={Filter}",
                count, initiatedByUserId, DescribeFilter(filter));
            return new BulkActionResult { Success = true, CountAffected = count, DryRun = true };
        }
        try
        {
            var count = await _eventStore.BulkCancelPendingAsync(bulkFilter, cancellationToken);
            _logger.LogInformation("Bulk cancel pending completed. CountAffected={Count}, InitiatedBy={UserId}, Filter={Filter}",
                count, initiatedByUserId, DescribeFilter(filter));
            _metrics?.RecordBulkCancelled(count);
            return new BulkActionResult { Success = true, CountAffected = count, DryRun = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk cancel pending failed. Filter={Filter}", DescribeFilter(filter));
            return new BulkActionResult { Success = false, CountAffected = 0, ErrorMessage = ex.Message, DryRun = false };
        }
    }

    private static EventStoreBulkFilter ToBulkFilter(EventStoreFilterDto filter, Guid? scopeCompanyId)
    {
        return new EventStoreBulkFilter
        {
            CompanyId = scopeCompanyId ?? filter.CompanyId,
            EventType = filter.EventType,
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc,
            RetryCountMin = filter.RetryCountMin,
            RetryCountMax = filter.RetryCountMax,
            MaxCount = filter.PageSize > 0 ? Math.Min(filter.PageSize, DefaultBulkMaxCount) : DefaultBulkMaxCount
        };
    }

    private async Task<int> CountMatchingAsync(string status, EventStoreBulkFilter bulkFilter, Guid? scopeCompanyId, CancellationToken cancellationToken)
    {
        // Use repository to count: we need IEventStore or query; simplest is to add a count method or use existing query service.
        // For dry-run we can run the same bulk update in a transaction and rollback, or add CountByFilter. Simpler: run the bulk method with a temp filter with MaxCount=1 to see if any, then get count via a separate path. Actually the bulk methods return count - for dry-run we need to know the count without updating. So we need a way to count. The repository has BuildBulkQuery - it's private. We could add IEventStore.GetCountByFilterAsync(EventStoreBulkFilter filter, string status). Or the bulk service could use IEventStoreQueryService.GetEventsAsync with the filter and status and then count - but that's paginated. Easiest: add to IEventStore a method CountByBulkFilterAsync(filter, status) that returns the count. Let me add that to the repository and interface.
        return await _eventStore.CountByBulkFilterAsync(bulkFilter, status, cancellationToken);
    }

    private async Task<int> CountFailedDueForRetryAsync(EventStoreBulkFilter bulkFilter, Guid? scopeCompanyId, CancellationToken cancellationToken)
    {
        return await _eventStore.CountFailedDueForRetryByFilterAsync(bulkFilter, cancellationToken);
    }

    private static string DescribeFilter(EventStoreFilterDto f)
    {
        var parts = new List<string>();
        if (f.CompanyId.HasValue) parts.Add($"CompanyId={f.CompanyId}");
        if (!string.IsNullOrEmpty(f.EventType)) parts.Add($"EventType={f.EventType}");
        if (f.FromUtc.HasValue) parts.Add($"From={f.FromUtc:O}");
        if (f.ToUtc.HasValue) parts.Add($"To={f.ToUtc:O}");
        if (f.RetryCountMin.HasValue) parts.Add($"RetryCountMin={f.RetryCountMin}");
        if (f.RetryCountMax.HasValue) parts.Add($"RetryCountMax={f.RetryCountMax}");
        return string.Join(", ", parts);
    }
}
