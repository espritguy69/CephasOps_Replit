using CephasOps.Domain.Common;

namespace CephasOps.Domain.Workflow.Entities;

/// <summary>
/// Workflow job entity - represents an in-progress workflow action or scheduled step
/// </summary>
public class WorkflowJob : CompanyScopedEntity
{
    /// <summary>
    /// Foreign key to WorkflowDefinition
    /// </summary>
    public Guid WorkflowDefinitionId { get; set; }

    /// <summary>
    /// Entity type being processed (e.g., "Order", "Invoice")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity being processed
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Current business status of the entity
    /// </summary>
    public string CurrentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Intended next status
    /// </summary>
    public string TargetStatus { get; set; } = string.Empty;

    /// <summary>
    /// Current state of the workflow job (Pending, Running, Succeeded, Failed)
    /// </summary>
    public WorkflowJobState State { get; set; } = WorkflowJobState.Pending;

    /// <summary>
    /// Last error message if the job failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// JSON payload with transition context (actor, metadata, etc.)
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>
    /// User ID who initiated this workflow job
    /// </summary>
    public Guid? InitiatedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the job started processing
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the job completed (successfully or failed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Correlation ID for tracing (HTTP request, parent JobRun, etc.). Connects Workflow → JobRun → Event.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Navigation property to WorkflowDefinition
    /// </summary>
    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
}

/// <summary>
/// Enumeration of workflow job states
/// </summary>
public enum WorkflowJobState
{
    /// <summary>
    /// Job is queued and waiting to be processed
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently being processed
    /// </summary>
    Running = 1,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// Job failed with an error
    /// </summary>
    Failed = 3
}

