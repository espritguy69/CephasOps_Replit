namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// Read-model projection: one row per WorkflowTransitionCompleted event. Used for workflow history queries and replay-safe rebuilds.
/// Keyed by EventId so replay is idempotent (upsert by event id).
/// </summary>
public class WorkflowTransitionHistoryEntry
{
    public Guid EventId { get; set; }
    public Guid WorkflowJobId { get; set; }
    public Guid? CompanyId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
