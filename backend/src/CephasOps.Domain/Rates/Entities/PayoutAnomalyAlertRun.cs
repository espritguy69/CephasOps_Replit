using CephasOps.Domain.Rates;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Record of a single payout anomaly alert run (scheduler or manual). Read-only history for operations.
/// </summary>
public class PayoutAnomalyAlertRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int EvaluatedCount { get; set; }
    public int SentCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }

    /// <summary>Scheduler or Manual.</summary>
    public string TriggerSource { get; set; } = AlertRunTriggerSource.Scheduler;
}
