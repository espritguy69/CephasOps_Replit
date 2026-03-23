using CephasOps.Application.Admin.DTOs;

namespace CephasOps.Application.Admin.Services;

/// <summary>
/// Administrative service for system maintenance and monitoring
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Rebuild search indexes
    /// </summary>
    Task ReindexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Flush settings cache
    /// </summary>
    Task FlushCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get system health status
    /// </summary>
    Task<SystemHealthDto> GetHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get read-only email ingestion diagnostics (last successful job, job counts by state, account last poll, drafts today). No secrets.
    /// </summary>
    Task<EmailIngestionDiagnosticsDto> GetEmailIngestionDiagnosticsAsync(CancellationToken cancellationToken = default);
}

