namespace CephasOps.Domain.Integration.Entities;

/// <summary>
/// Defines a connector type (e.g. Webhook, HttpPush, Notification). Used for routing and policy.
/// </summary>
public class ConnectorDefinition
{
    public Guid Id { get; set; }

    /// <summary>Unique key for resolution (e.g. "sla-alerts", "partner-crm").</summary>
    public string ConnectorKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Connector category: Webhook, HttpPush, Notification, Crm, etc.</summary>
    public string ConnectorType { get; set; } = string.Empty;

    /// <summary>Direction: Outbound, Inbound, Bidirectional.</summary>
    public string Direction { get; set; } = "Outbound";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
