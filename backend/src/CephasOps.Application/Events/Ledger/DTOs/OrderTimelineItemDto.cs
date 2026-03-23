namespace CephasOps.Application.Events.Ledger.DTOs;

/// <summary>
/// Single item in an order timeline derived from ledger entries (OrderLifecycle family).
/// Operator-friendly for display and audit.
/// </summary>
public class OrderTimelineItemDto
{
    public Guid LedgerEntryId { get; set; }
    public Guid? SourceEventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public string LedgerFamily { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Category { get; set; }
    /// <summary>Status before the change (when applicable).</summary>
    public string? PriorStatus { get; set; }
    /// <summary>Status after the change (when applicable).</summary>
    public string? NewStatus { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string? OrderingStrategyId { get; set; }
    public string? PayloadSnapshot { get; set; }
}
