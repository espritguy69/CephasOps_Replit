using CephasOps.Application.Rebuild.DTOs;

namespace CephasOps.Application.Rebuild;

/// <summary>
/// Enqueues an operational rebuild to run asynchronously via the background job system.
/// Creates a Pending RebuildOperation and a job to process it.
/// </summary>
public interface IRebuildJobEnqueuer
{
    /// <summary>
    /// Create a RebuildOperation with State=Pending and enqueue an OperationalRebuild background job. Returns the operation id.
    /// </summary>
    Task<Guid> EnqueueRebuildAsync(RebuildRequestDto request, Guid? scopeCompanyId, Guid? requestedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueue a job to resume an existing operation (State PartiallyCompleted or Pending). Operation must already exist.
    /// </summary>
    Task EnqueueResumeAsync(Guid operationId, Guid? scopeCompanyId, Guid? requestedByUserId, CancellationToken cancellationToken = default);
}
