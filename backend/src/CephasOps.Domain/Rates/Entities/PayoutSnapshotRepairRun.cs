using CephasOps.Domain.Rates;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Record of a single run of the missing payout snapshot repair job. Read-only history for operations.
/// </summary>
public class PayoutSnapshotRepairRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int TotalProcessed { get; set; }
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }

    /// <summary>Order IDs that failed (JSON array of GUIDs).</summary>
    public string? ErrorOrderIdsJson { get; set; }

    /// <summary>Scheduler or Manual.</summary>
    public string TriggerSource { get; set; } = RepairRunTriggerSource.Scheduler;

    public string? Notes { get; set; }
}
