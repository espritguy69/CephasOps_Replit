namespace CephasOps.Application.Events.Lineage;

/// <summary>
/// Event lineage tree for correlation/causality visualization.
/// </summary>
public sealed class EventLineageTreeDto
{
    public Guid? RootEventId { get; set; }
    public string? CorrelationId { get; set; }
    public List<EventLineageNodeDto> Nodes { get; set; } = new();
    public int TotalCount { get; set; }
    public bool Truncated { get; set; }
}

/// <summary>
/// Single node in the lineage tree.
/// </summary>
public sealed class EventLineageNodeDto
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string? Status { get; set; }
    public Guid? ParentEventId { get; set; }
    public Guid? CausationId { get; set; }
    public string? PartitionKey { get; set; }
    public string? ReplayId { get; set; }
    public int Depth { get; set; }
}
