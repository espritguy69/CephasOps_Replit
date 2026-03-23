namespace CephasOps.Application.Events.Ledger.DTOs;

public class LedgerEntryDto
{
    public Guid Id { get; set; }
    public Guid? SourceEventId { get; set; }
    public Guid? ReplayOperationId { get; set; }
    public string LedgerFamily { get; set; } = string.Empty;
    public string? Category { get; set; }
    public Guid? CompanyId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public string? PayloadSnapshot { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string? OrderingStrategyId { get; set; }
}
