namespace CephasOps.Application.Integration;

/// <summary>
/// Normalized integration message for outbound dispatch or inbound normalization.
/// Keeps internal event identity and correlation; payload is integration contract.
/// </summary>
public sealed class IntegrationMessage
{
    public Guid MessageId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EventVersion { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? RootEventId { get; set; }
    public Guid? ParentEventId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public Guid? CommandId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
