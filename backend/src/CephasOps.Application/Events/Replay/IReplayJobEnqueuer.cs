using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Enqueues an operational replay to run asynchronously via the background job system. Creates a Pending ReplayOperation and a job to process it.
/// </summary>
public interface IReplayJobEnqueuer
{
    /// <summary>
    /// Create a ReplayOperation with State=Pending and enqueue an OperationalReplay background job. Returns the operation id.
    /// </summary>
    Task<Guid> EnqueueReplayAsync(ReplayRequestDto request, Guid? scopeCompanyId, Guid? requestedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueue a job to resume an existing operation (State PartiallyCompleted or Pending). Operation must already exist.
    /// </summary>
    Task EnqueueResumeAsync(Guid operationId, Guid? scopeCompanyId, Guid? requestedByUserId, CancellationToken cancellationToken = default);
}
