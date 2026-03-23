using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Dry-run preview for operational replay. Returns counts, sample events, and blocked reasons without executing handlers.
/// </summary>
public interface IReplayPreviewService
{
    /// <summary>
    /// Preview what would be replayed for the given request. Does not execute any handlers.
    /// </summary>
    Task<ReplayPreviewResultDto> PreviewAsync(ReplayRequestDto request, Guid? scopeCompanyId, CancellationToken cancellationToken = default);
}
