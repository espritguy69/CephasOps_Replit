namespace CephasOps.Application.Insights;

/// <summary>Read-only SLA breach engine. Uses Order.KpiDueAt as authoritative due time. Tenant methods require company context; platform summary is admin-only.</summary>
public interface ISlaBreachService
{
    /// <summary>Tenant-scoped: distribution counts (OnTrack, NearingBreach, Breached, NoSla) for active orders.</summary>
    Task<SlaBreachSummaryDto> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Tenant-scoped: orders with SLA state (at-risk = NearingBreach or Breached). Optional filter by state/severity.</summary>
    Task<IReadOnlyList<SlaBreachOrderItemDto>> GetOrdersAtRiskAsync(Guid companyId, string? breachState = null, string? severity = null, CancellationToken cancellationToken = default);

    /// <summary>Platform admin only: aggregate distribution across tenants (counts only).</summary>
    Task<SlaBreachSummaryDto> GetPlatformSummaryAsync(CancellationToken cancellationToken = default);
}
