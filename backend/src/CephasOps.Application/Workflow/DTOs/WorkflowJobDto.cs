namespace CephasOps.Application.Workflow.DTOs;

/// <summary>
/// DTO for WorkflowJob
/// </summary>
public class WorkflowJobDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid WorkflowDefinitionId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public string TargetStatus { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? LastError { get; set; }
    public Dictionary<string, object>? Payload { get; set; }
    public Guid? InitiatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// DTO for executing a workflow transition
/// </summary>
public class ExecuteTransitionDto
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string TargetStatus { get; set; } = string.Empty;
    public Dictionary<string, object>? Payload { get; set; }

    /// <summary>
    /// Optional. When set for EntityType "Order", workflow resolution uses partner-specific workflow first.
    /// When null, engine resolves from entity (e.g. Order.PartnerId) for consistency.
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Optional. When set for EntityType "Order", workflow resolution may use department-specific workflow.
    /// When null, engine resolves from Order.DepartmentId.
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Optional. When set for EntityType "Order", workflow resolution may use order-type-specific workflow.
    /// Use parent order type code for subtypes (e.g. MODIFICATION for MODIFICATION_OUTDOOR). When null, engine resolves from Order.OrderTypeId (parent code if subtype).
    /// </summary>
    public string? OrderTypeCode { get; set; }

    /// <summary>
    /// Optional. Correlation ID for tracing (e.g. from HTTP X-Correlation-Id or parent JobRun). Propagated to WorkflowJob and emitted events.
    /// </summary>
    public string? CorrelationId { get; set; }
}

