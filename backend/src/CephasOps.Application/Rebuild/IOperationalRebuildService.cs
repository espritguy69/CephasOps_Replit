using CephasOps.Application.Rebuild.DTOs;

namespace CephasOps.Application.Rebuild;

/// <summary>
/// Executes operational state rebuilds: resolves target, runs rebuild runner, records result.
/// Phase 2: async enqueue, lock, checkpoint/resume, progress. No side effects (notifications, external integrations).
/// </summary>
public interface IOperationalRebuildService
{
    Task<RebuildPreviewResultDto?> PreviewAsync(
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default);

    Task<RebuildExecutionResultDto> ExecuteAsync(
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Enqueue rebuild for background execution. Returns operation id (State=Pending).</summary>
    Task<Guid> EnqueueRebuildAsync(
        RebuildRequestDto request,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Execute an existing operation (by id). Used by background job; acquires lock, runs runner, releases lock.</summary>
    Task<RebuildExecutionResultDto> ExecuteByOperationIdAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Resume a PartiallyCompleted or Pending operation (sync).</summary>
    Task<RebuildExecutionResultDto> ExecuteResumeAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        string? rerunReason,
        CancellationToken cancellationToken = default);

    /// <summary>Enqueue resume job for an existing operation.</summary>
    Task EnqueueResumeAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        Guid? requestedByUserId,
        string? rerunReason,
        CancellationToken cancellationToken = default);

    Task<RebuildOperationSummaryDto?> GetOperationAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default);

    Task<RebuildProgressDto?> GetProgressAsync(
        Guid operationId,
        Guid? scopeCompanyId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<RebuildOperationSummaryDto> Items, int Total)> ListOperationsAsync(
        int page,
        int pageSize,
        Guid? scopeCompanyId,
        string? state = null,
        string? rebuildTargetId = null,
        CancellationToken cancellationToken = default);
}
