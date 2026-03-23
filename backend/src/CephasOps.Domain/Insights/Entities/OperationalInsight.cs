namespace CephasOps.Domain.Insights.Entities;

/// <summary>
/// Field ops intelligence: a single insight record (e.g. order completed, backlog signal, SLA risk).
/// Tenant-scoped by CompanyId. Extensible via Type and PayloadJson.
/// </summary>
public class OperationalInsight
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    /// <summary>Insight type, e.g. OrderCompleted, BacklogRisk, SlaBreachRisk, InventoryDiscrepancy.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Optional JSON payload (order id, counts, scores, etc.).</summary>
    public string? PayloadJson { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Optional entity type for linking (Order, PayrollRun, etc.).</summary>
    public string? EntityType { get; set; }
    /// <summary>Optional entity id for linking.</summary>
    public Guid? EntityId { get; set; }
}
