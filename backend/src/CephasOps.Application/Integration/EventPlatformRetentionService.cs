using CephasOps.Domain.Events;
using CephasOps.Domain.Integration.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Integration;

/// <summary>
/// Runs retention cleanup for event platform tables in safe batches. Only deletes rows older than configured retention windows.
/// Order: EventProcessingLog → EventStore → OutboundIntegrationDeliveries (cascade attempts) → InboundWebhookReceipts → ExternalIdempotencyRecords.
/// </summary>
public sealed class EventPlatformRetentionService : IEventPlatformRetentionService
{
    private readonly ApplicationDbContext _context;
    private readonly IOptions<EventPlatformRetentionOptions> _options;
    private readonly ILogger<EventPlatformRetentionService> _logger;

    public EventPlatformRetentionService(
        ApplicationDbContext context,
        IOptions<EventPlatformRetentionOptions> options,
        ILogger<EventPlatformRetentionService> logger)
    {
        _context = context;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventPlatformRetentionResult> RunRetentionAsync(CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        var started = DateTime.UtcNow;
        var result = new EventPlatformRetentionResult { RunStartedAtUtc = started };
        var errors = new List<string>();

        try
        {
            await TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
            {
                // 1. EventProcessingLog (Completed, old)
                if (opts.EventProcessingLogCompletedDays > 0)
                {
                    var cutoffLog = DateTime.UtcNow.AddDays(-opts.EventProcessingLogCompletedDays);
                    var logIds = await _context.EventProcessingLog
                        .Where(e => e.State == EventProcessingLog.States.Completed && e.CompletedAtUtc != null && e.CompletedAtUtc < cutoffLog)
                        .OrderBy(e => e.CompletedAtUtc)
                        .Take(opts.MaxDeletesPerTablePerRun)
                        .Select(e => e.Id)
                        .ToListAsync(ct);
                    if (logIds.Count > 0)
                    {
                        var deleted = await _context.EventProcessingLog.Where(e => logIds.Contains(e.Id)).ExecuteDeleteAsync(ct);
                        result.EventProcessingLogDeleted = deleted;
                        _logger.LogInformation("Event platform retention: EventProcessingLog deleted {Count} rows (Completed before {Cutoff:yyyy-MM-dd})", deleted, cutoffLog);
                    }
                }

                // 2. EventStore (Processed or DeadLetter, old by ProcessedAtUtc or CreatedAtUtc)
                if (opts.EventStoreProcessedAndDeadLetterDays > 0)
                {
                    var cutoffStore = DateTime.UtcNow.AddDays(-opts.EventStoreProcessedAndDeadLetterDays);
                    var eventIds = await _context.EventStore
                        .Where(e => (e.Status == "Processed" || e.Status == "DeadLetter") &&
                                    (e.ProcessedAtUtc != null ? e.ProcessedAtUtc < cutoffStore : e.CreatedAtUtc < cutoffStore))
                        .OrderBy(e => e.CreatedAtUtc)
                        .Take(opts.MaxDeletesPerTablePerRun)
                        .Select(e => e.EventId)
                        .ToListAsync(ct);
                    if (eventIds.Count > 0)
                    {
                        var deleted = await _context.EventStore.Where(e => eventIds.Contains(e.EventId)).ExecuteDeleteAsync(ct);
                        result.EventStoreDeleted = deleted;
                        _logger.LogInformation("Event platform retention: EventStore deleted {Count} rows (Processed/DeadLetter before {Cutoff:yyyy-MM-dd})", deleted, cutoffStore);
                    }
                }

                // 3. OutboundIntegrationDeliveries (Delivered, old) — cascade deletes OutboundIntegrationAttempts
                if (opts.OutboundDeliveredDays > 0)
                {
                    var cutoffOut = DateTime.UtcNow.AddDays(-opts.OutboundDeliveredDays);
                    var deliveryIds = await _context.OutboundIntegrationDeliveries
                        .Where(d => d.Status == OutboundIntegrationDelivery.Statuses.Delivered && d.DeliveredAtUtc != null && d.DeliveredAtUtc < cutoffOut)
                        .OrderBy(d => d.DeliveredAtUtc)
                        .Take(opts.MaxDeletesPerTablePerRun)
                        .Select(d => d.Id)
                        .ToListAsync(ct);
                    if (deliveryIds.Count > 0)
                    {
                        var deleted = await _context.OutboundIntegrationDeliveries.Where(d => deliveryIds.Contains(d.Id)).ExecuteDeleteAsync(ct);
                        result.OutboundDeliveriesDeleted = deleted;
                        _logger.LogInformation("Event platform retention: OutboundIntegrationDeliveries deleted {Count} rows (Delivered before {Cutoff:yyyy-MM-dd})", deleted, cutoffOut);
                    }
                }

                // 4. InboundWebhookReceipts (Processed, old) — platform-wide cleanup; bypass tenant filter
                if (opts.InboundProcessedDays > 0)
                {
                    var cutoffIn = DateTime.UtcNow.AddDays(-opts.InboundProcessedDays);
                    var receiptIds = await _context.InboundWebhookReceipts
                        .IgnoreQueryFilters()
                        .Where(r => r.Status == InboundWebhookReceipt.Statuses.Processed && r.ProcessedAtUtc != null && r.ProcessedAtUtc < cutoffIn)
                        .OrderBy(r => r.ProcessedAtUtc)
                        .Take(opts.MaxDeletesPerTablePerRun)
                        .Select(r => r.Id)
                        .ToListAsync(ct);
                    if (receiptIds.Count > 0)
                    {
                        var deleted = await _context.InboundWebhookReceipts.IgnoreQueryFilters().Where(r => receiptIds.Contains(r.Id)).ExecuteDeleteAsync(ct);
                        result.InboundReceiptsDeleted = deleted;
                        _logger.LogInformation("Event platform retention: InboundWebhookReceipts deleted {Count} rows (Processed before {Cutoff:yyyy-MM-dd})", deleted, cutoffIn);
                    }
                }

                // 5. ExternalIdempotencyRecords (completed, old)
                if (opts.ExternalIdempotencyCompletedDays > 0)
                {
                    var cutoffExt = DateTime.UtcNow.AddDays(-opts.ExternalIdempotencyCompletedDays);
                    var extIds = await _context.ExternalIdempotencyRecords
                        .Where(e => e.CompletedAtUtc != null && e.CompletedAtUtc < cutoffExt)
                        .OrderBy(e => e.CompletedAtUtc)
                        .Take(opts.MaxDeletesPerTablePerRun)
                        .Select(e => e.Id)
                        .ToListAsync(ct);
                    if (extIds.Count > 0)
                    {
                        var deleted = await _context.ExternalIdempotencyRecords.Where(e => extIds.Contains(e.Id)).ExecuteDeleteAsync(ct);
                        result.ExternalIdempotencyDeleted = deleted;
                        _logger.LogInformation("Event platform retention: ExternalIdempotencyRecords deleted {Count} rows (Completed before {Cutoff:yyyy-MM-dd})", deleted, cutoffExt);
                    }
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            _logger.LogError(ex, "Event platform retention run failed");
        }

        result.RunCompletedAtUtc = DateTime.UtcNow;
        result.Errors = errors;
        if (result.TotalDeleted > 0 || errors.Count > 0)
            _logger.LogInformation("Event platform retention run completed. TotalDeleted={Total}, EventStore={Es}, EventProcessingLog={Epl}, Outbound={Ob}, Inbound={Ib}, ExternalIdempotency={Ext}, Errors={Err}",
                result.TotalDeleted, result.EventStoreDeleted, result.EventProcessingLogDeleted, result.OutboundDeliveriesDeleted, result.InboundReceiptsDeleted, result.ExternalIdempotencyDeleted, errors.Count);
        return result;
    }
}
