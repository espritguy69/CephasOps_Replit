namespace CephasOps.Application.Events.Ledger.DTOs;

/// <summary>
/// Single item in the unified operational history for an order (WorkflowTransition + OrderLifecycle ledger entries merged by time).
/// </summary>
public class UnifiedOrderHistoryItemDto
{
    public Guid LedgerEntryId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public string LedgerFamily { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? PriorStatus { get; set; }
    public string? NewStatus { get; set; }
    public Guid? SourceEventId { get; set; }
    public string? OrderingStrategyId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
}
