namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Operational governance record for a detected payout anomaly. Read-only with respect to payout/snapshot logic.
/// Links to an anomaly by fingerprint (AnomalyFingerprintId). Status and comments are metadata only.
/// </summary>
public class PayoutAnomalyReview
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Deterministic id for the anomaly (computed from type, orderId, installerId, snapshotId, detectedAt, reason). Used in API as anomaly id.</summary>
    public string AnomalyFingerprintId { get; set; } = "";

    public string AnomalyType { get; set; } = "";
    public Guid? OrderId { get; set; }
    public Guid? InstallerId { get; set; }
    public Guid? PayoutSnapshotId { get; set; }
    public string Severity { get; set; } = "";
    public DateTime DetectedAt { get; set; }

    /// <summary>Open | Acknowledged | Investigating | Resolved | FalsePositive</summary>
    public string Status { get; set; } = "Open";

    public Guid? AssignedToUserId { get; set; }

    /// <summary>JSON array of { at, userId, userName, text } for comment thread.</summary>
    public string? NotesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
