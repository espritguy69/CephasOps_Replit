namespace CephasOps.Application.Events.Ledger.DTOs;

public class WorkflowTransitionTimelineItemDto
{
    public Guid LedgerEntryId { get; set; }
    public Guid SourceEventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid? CompanyId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public Guid? WorkflowJobId { get; set; }
    public string? PayloadSnapshot { get; set; }
}
