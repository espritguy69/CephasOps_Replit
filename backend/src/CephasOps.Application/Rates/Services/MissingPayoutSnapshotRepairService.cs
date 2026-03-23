using CephasOps.Application.Rates;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Domain.Rates;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Detects completed orders without a payout snapshot and creates them via IOrderPayoutSnapshotService.
/// Idempotent: only orders with status Completed/OrderCompleted and no snapshot are processed;
/// CreateSnapshotForOrderIfEligibleAsync is itself idempotent (skips if snapshot already exists).
/// </summary>
public class MissingPayoutSnapshotRepairService : IMissingPayoutSnapshotRepairService
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderPayoutSnapshotService _orderPayoutSnapshotService;
    private readonly ILogger<MissingPayoutSnapshotRepairService> _logger;

    public MissingPayoutSnapshotRepairService(
        ApplicationDbContext context,
        IOrderPayoutSnapshotService orderPayoutSnapshotService,
        ILogger<MissingPayoutSnapshotRepairService> logger)
    {
        _context = context;
        _orderPayoutSnapshotService = orderPayoutSnapshotService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MissingPayoutSnapshotRepairResult> DetectMissingPayoutSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        var orderIds = await _context.Orders
            .AsNoTracking()
            .Where(o =>
                (o.Status == OrderStatus.Completed || o.Status == OrderStatus.OrderCompleted)
                && !_context.OrderPayoutSnapshots.Any(s => s.OrderId == o.Id))
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        if (orderIds.Count == 0)
        {
            _logger.LogDebug("Missing payout snapshot repair: no completed orders without a snapshot");
            return new MissingPayoutSnapshotRepairResult();
        }

        _logger.LogInformation(
            "Missing payout snapshot repair: found {Count} completed order(s) without a snapshot",
            orderIds.Count);

        var createdCount = 0;
        var skippedCount = 0;
        var errorOrderIds = new List<Guid>();

        foreach (var orderId in orderIds)
        {
            try
            {
                await _orderPayoutSnapshotService.CreateSnapshotForOrderIfEligibleAsync(orderId, cancellationToken, SnapshotProvenance.RepairJob);

                var hasSnapshot = await _context.OrderPayoutSnapshots
                    .AnyAsync(s => s.OrderId == orderId, cancellationToken);
                if (hasSnapshot)
                    createdCount++;
                else
                    skippedCount++;
            }
            catch (Exception ex)
            {
                errorOrderIds.Add(orderId);
                _logger.LogWarning(ex, "Missing payout snapshot repair: failed for order {OrderId}", orderId);
            }
        }

        var result = new MissingPayoutSnapshotRepairResult
        {
            CreatedCount = createdCount,
            SkippedCount = skippedCount,
            ErrorCount = errorOrderIds.Count,
            ErrorOrderIds = errorOrderIds
        };

        _logger.LogInformation(
            "Missing payout snapshot repair completed: created={Created}, skipped={Skipped}, errors={Errors}",
            result.CreatedCount, result.SkippedCount, result.ErrorCount);

        if (result.ErrorOrderIds.Count > 0)
        {
            _logger.LogWarning(
                "Missing payout snapshot repair: order(s) with errors: {OrderIds}",
                string.Join(", ", result.ErrorOrderIds.Select(id => id.ToString())));
        }

        return result;
    }
}
