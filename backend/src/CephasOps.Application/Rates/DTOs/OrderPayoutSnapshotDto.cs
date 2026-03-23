namespace CephasOps.Application.Rates.DTOs;

/// <summary>
/// DTO for reading an immutable order payout snapshot.
/// </summary>
public class OrderPayoutSnapshotDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? InstallerId { get; set; }
    public Guid? RateGroupId { get; set; }
    public Guid? BaseWorkRateId { get; set; }
    public Guid? ServiceProfileId { get; set; }
    public Guid? CustomRateId { get; set; }
    public Guid? LegacyRateId { get; set; }
    public decimal? BaseAmount { get; set; }
    public string? ModifierTraceJson { get; set; }
    public decimal FinalPayout { get; set; }
    public string Currency { get; set; } = "MYR";
    public string? ResolutionMatchLevel { get; set; }
    public string? PayoutPath { get; set; }
    public string? ResolutionTraceJson { get; set; }
    public DateTime CalculatedAt { get; set; }
    /// <summary>How the snapshot was created: NormalFlow, RepairJob, Backfill, ManualBackfill, Unknown.</summary>
    public string? Provenance { get; set; }
}

/// <summary>
/// Response when reading payout for an order: snapshot or live resolution, with source indicator.
/// </summary>
public class OrderPayoutSnapshotResponseDto
{
    /// <summary>Whether the payout came from a stored snapshot or live resolution.</summary>
    public string Source { get; set; } = "Live"; // "Snapshot" | "Live"
    /// <summary>Payout result in the same shape as rate resolution (for UI reuse).</summary>
    public GponRateResolutionResult Result { get; set; } = null!;
}
