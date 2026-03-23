namespace CephasOps.Domain.Rates;

/// <summary>
/// How a payout snapshot was created. Used for audit and dashboard (normal flow vs repaired later).
/// </summary>
public static class SnapshotProvenance
{
    /// <summary>Snapshot created when order status changed to Completed/OrderCompleted via OrderService (normal flow).</summary>
    public const string NormalFlow = "NormalFlow";

    /// <summary>Snapshot created by the missing-snapshot repair job (scheduler or manual).</summary>
    public const string RepairJob = "RepairJob";

    /// <summary>Snapshot created by a backfill process (batch).</summary>
    public const string Backfill = "Backfill";

    /// <summary>Snapshot created by manual backfill (e.g. admin tool).</summary>
    public const string ManualBackfill = "ManualBackfill";

    /// <summary>Pre-provenance snapshots or unknown origin. Existing snapshots before provenance was added default to this.</summary>
    public const string Unknown = "Unknown";

    public static readonly string[] All = new[] { NormalFlow, RepairJob, Backfill, ManualBackfill, Unknown };
}
