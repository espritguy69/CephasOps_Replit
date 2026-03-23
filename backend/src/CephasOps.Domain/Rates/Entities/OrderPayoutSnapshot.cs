using CephasOps.Domain.Rates;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Immutable snapshot of installer payout calculation for an order when it reaches a completed state.
/// Captures results after RateEngineService resolution so historical payouts remain stable if pricing rules change.
/// One snapshot per order; do not update after save.
/// </summary>
public class OrderPayoutSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Guid? CompanyId { get; set; }
    /// <summary>Assigned service installer at time of snapshot.</summary>
    public Guid? InstallerId { get; set; }

    /// <summary>Matched rate group (when path is BaseWorkRate).</summary>
    public Guid? RateGroupId { get; set; }
    /// <summary>Matched base work rate ID (when path is BaseWorkRate).</summary>
    public Guid? BaseWorkRateId { get; set; }
    /// <summary>Matched service profile ID (when resolution used profile).</summary>
    public Guid? ServiceProfileId { get; set; }
    /// <summary>Matched custom rate ID (when path is CustomOverride).</summary>
    public Guid? CustomRateId { get; set; }
    /// <summary>Matched legacy SI job rate ID (when path is Legacy).</summary>
    public Guid? LegacyRateId { get; set; }

    /// <summary>Base amount before modifiers (null when CustomOverride).</summary>
    public decimal? BaseAmount { get; set; }
    /// <summary>Modifier trace as JSON array of { ModifierType, Operation, Value, AmountBefore, AmountAfter }.</summary>
    public string? ModifierTraceJson { get; set; }

    /// <summary>Final payout amount.</summary>
    public decimal FinalPayout { get; set; }
    public string Currency { get; set; } = "MYR";

    /// <summary>Resolution match level: Custom | ExactCategory | ServiceProfile | BroadRateGroup | Legacy.</summary>
    public string? ResolutionMatchLevel { get; set; }
    /// <summary>Payout path: CustomOverride | BaseWorkRate | Legacy.</summary>
    public string? PayoutPath { get; set; }

    /// <summary>Resolution steps and warnings as JSON (e.g. { "steps": [], "warnings": [] }).</summary>
    public string? ResolutionTraceJson { get; set; }

    /// <summary>When the calculation was performed (UTC).</summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>How this snapshot was created: NormalFlow, RepairJob, Backfill, ManualBackfill, or Unknown (pre-provenance).</summary>
    public string Provenance { get; set; } = SnapshotProvenance.Unknown;
}
