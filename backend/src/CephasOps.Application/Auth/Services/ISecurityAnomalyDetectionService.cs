using CephasOps.Application.Audit.DTOs;

namespace CephasOps.Application.Auth.Services;

/// <summary>
/// Detects suspicious patterns in auth audit events. Read-only analysis. v1.4 Phase 2.
/// </summary>
public interface ISecurityAnomalyDetectionService
{
    /// <summary>
    /// Run detection rules over auth events in the given range. Optionally filter by user.
    /// </summary>
    Task<List<SecurityAlertDto>> DetectAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        Guid? userId = null,
        string? alertType = null,
        CancellationToken cancellationToken = default);
}
