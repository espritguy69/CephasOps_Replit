namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// Multi-step workflow orchestration instance. Tracks current step and status.
/// </summary>
public class WorkflowInstance
{
    public Guid Id { get; set; }
    public Guid? WorkflowDefinitionId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public string Status { get; set; } = Statuses.Running;
    public string? CorrelationId { get; set; }
    public string? PayloadJson { get; set; }
    public Guid? CompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public static class Statuses
    {
        public const string Running = "Running";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Compensating = "Compensating";
    }
}
