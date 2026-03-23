using CephasOps.Application.Rebuild.DTOs;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;

namespace CephasOps.Application.Rebuild;

/// <summary>
/// Per-target rebuild runner. Performs preview and execute for one rebuild target.
/// No side effects (no notifications, no external integrations).
/// </summary>
public interface IRebuildRunner
{
    string TargetId { get; }

    Task<RebuildPreviewResultDto> PreviewAsync(
        ApplicationDbContext context,
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the rebuild: updates target state and the operation record.
    /// Uses the same context so operation and target changes are in one transaction.
    /// </summary>
    Task ExecuteAsync(
        ApplicationDbContext context,
        RebuildOperation operation,
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default);
}
