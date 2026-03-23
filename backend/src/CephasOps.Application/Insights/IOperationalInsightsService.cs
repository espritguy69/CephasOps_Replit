namespace CephasOps.Application.Insights;

/// <summary>Read-only operational insights for platform and tenant dashboards.</summary>
public interface IOperationalInsightsService
{
    /// <summary>Platform admin only. Aggregated health across all tenants.</summary>
    Task<PlatformHealthDto> GetPlatformHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>Tenant scoped. Performance metrics for the current company.</summary>
    Task<TenantPerformanceDto> GetTenantPerformanceAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Tenant scoped. Operations control metrics and stuck orders.</summary>
    Task<OperationsControlDto> GetOperationsControlAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Tenant scoped. Financial overview from payout snapshots and revenue.</summary>
    Task<FinancialOverviewDto> GetFinancialOverviewAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Tenant scoped. Risk and quality metrics.</summary>
    Task<RiskQualityDto> GetRiskQualityAsync(Guid companyId, CancellationToken cancellationToken = default);
}
