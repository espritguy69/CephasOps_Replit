using CephasOps.Application.Admin.DTOs;

namespace CephasOps.Application.Admin.Services;

/// <summary>
/// Builds a compact operational overview from existing job execution, event store, payout health, and system health sources.
/// For internal operator visibility only; no schema changes.
/// </summary>
public interface IOperationsOverviewService
{
    /// <summary>
    /// Get operational overview: job executions, event store (last 24h), payout health, system health.
    /// All data from existing persisted sources and services.
    /// </summary>
    Task<OperationalOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}
