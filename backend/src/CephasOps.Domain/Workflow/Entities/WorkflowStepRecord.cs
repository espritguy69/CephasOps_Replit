namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// Record of a step execution within a workflow instance.
/// </summary>
public class WorkflowStepRecord
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = Statuses.Pending;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? PayloadJson { get; set; }
    public string? CompensationDataJson { get; set; }

    public WorkflowInstance? WorkflowInstance { get; set; }

    public static class Statuses
    {
        public const string Pending = "Pending";
        public const string Running = "Running";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
