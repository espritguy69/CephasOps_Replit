namespace CephasOps.Domain.Events;

/// <summary>
/// Base class for domain events. Provides standard envelope fields for tracing, multi-tenancy, and versioning.
/// Set ParentEventId and CorrelationId when publishing a child event from a handler; set CausationId to the causing event id.
/// Set RootEventId for correlation tree (origin of the chain). Phase 8.
/// </summary>
public abstract class DomainEvent : IDomainEvent, IHasParentEvent, IHasRootEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    /// <inheritdoc />
    public string? Version { get; set; } = "1";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public Guid? CompanyId { get; set; }
    /// <inheritdoc />
    public Guid? CausationId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string? Source { get; set; }

    /// <summary>When this event was spawned from another (child event), set to the parent's EventId.</summary>
    public Guid? ParentEventId { get; set; }

    /// <summary>Origin event of the full causality chain (for correlation trees). Set when publishing child events.</summary>
    public Guid? RootEventId { get; set; }

    Guid? IHasParentEvent.ParentEventId => ParentEventId;
    Guid? IHasRootEvent.RootEventId => RootEventId;
}
