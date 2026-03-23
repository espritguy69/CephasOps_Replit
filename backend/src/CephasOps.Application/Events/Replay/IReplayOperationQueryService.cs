using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Query replay operations for list and detail views.
/// </summary>
public interface IReplayOperationQueryService
{
    Task<(IReadOnlyList<ReplayOperationListItemDto> Items, int Total)> ListAsync(int page, int pageSize, Guid? scopeCompanyId, CancellationToken cancellationToken = default);
    Task<ReplayOperationDetailDto?> GetByIdAsync(Guid id, Guid? scopeCompanyId, CancellationToken cancellationToken = default);
    /// <summary>Phase 2: progress for active/resumable operation.</summary>
    Task<ReplayOperationProgressDto?> GetProgressAsync(Guid id, Guid? scopeCompanyId, CancellationToken cancellationToken = default);
}
