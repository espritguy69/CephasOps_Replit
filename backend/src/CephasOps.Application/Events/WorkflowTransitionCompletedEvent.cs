using CephasOps.Domain.Events;

namespace CephasOps.Application.Events;

/// <summary>
/// Emitted after every successful workflow transition. Enables event-driven orchestration and observability.
/// </summary>
public class WorkflowTransitionCompletedEvent : DomainEvent, IHasEntityContext
{
    public Guid WorkflowDefinitionId { get; set; }
    /// <summary>Transition definition id (from WorkflowTransitionDto.Id).</summary>
    public Guid? WorkflowTransitionId { get; set; }
    public Guid WorkflowJobId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    string? IHasEntityContext.EntityType => string.IsNullOrEmpty(EntityType) ? null : EntityType;
    Guid? IHasEntityContext.EntityId => EntityId;

    public WorkflowTransitionCompletedEvent()
    {
        EventType = PlatformEventTypes.WorkflowTransitionCompleted;
        Version = "1";
        Source = "WorkflowEngine";
    }
}
