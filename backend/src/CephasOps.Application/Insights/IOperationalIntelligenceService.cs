namespace CephasOps.Application.Insights;

/// <summary>Read-only, rule-based operational intelligence. Tenant-scoped methods require companyId; platform summary is admin-only.</summary>
public interface IOperationalIntelligenceService
{
    /// <summary>Summary counts for tenant (orders/installers/buildings at risk, severity bands).</summary>
    Task<OperationalIntelligenceSummaryDto> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Orders flagged with risk signals (stuck, likely stuck, reschedule-heavy, blocker accumulation, replacement-heavy, silent).</summary>
    Task<IReadOnlyList<OrderRiskSignalDto>> GetOrdersAtRiskAsync(Guid companyId, string? severity = null, CancellationToken cancellationToken = default);

    /// <summary>Installers flagged with risk signals (repeated blockers, high replacement rate, stuck orders, high issue ratio vs peers).</summary>
    Task<IReadOnlyList<InstallerRiskSignalDto>> GetInstallersAtRiskAsync(Guid companyId, string? severity = null, CancellationToken cancellationToken = default);

    /// <summary>Buildings/sites flagged with risk signals (repeated blockers, replacements, completion failures).</summary>
    Task<IReadOnlyList<BuildingRiskSignalDto>> GetBuildingsAtRiskAsync(Guid companyId, string? severity = null, CancellationToken cancellationToken = default);

    /// <summary>Tenant-level risk signals (spike in stuck/blocker-heavy orders, abnormal replacement ratio, etc.).</summary>
    Task<IReadOnlyList<TenantRiskSignalDto>> GetTenantRiskSignalsAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Platform admin only. Safe aggregate summary across tenants (counts only, no tenant business data).</summary>
    Task<OperationalIntelligenceSummaryDto> GetPlatformSummaryAsync(CancellationToken cancellationToken = default);
}
