using CephasOps.Application.Admin.DTOs;

namespace CephasOps.Application.Admin.Services;

/// <summary>
/// Lightweight operational intelligence for Service Installer (SI) field operations.
/// Read-only; surfaces patterns from order status log, material replacements, and orders.
/// </summary>
public interface ISiOperationalInsightsService
{
    /// <summary>
    /// Get SI operational insights for the given company (tenant).
    /// All data is scoped to the company and limited to the last <paramref name="windowDays"/>.
    /// </summary>
    Task<SiOperationalInsightsDto> GetInsightsAsync(Guid companyId, int windowDays = 90, CancellationToken cancellationToken = default);
}
