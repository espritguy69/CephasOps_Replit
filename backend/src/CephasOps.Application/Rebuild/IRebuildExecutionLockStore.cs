namespace CephasOps.Application.Rebuild;

/// <summary>
/// Durable rebuild lock: one active rebuild per (RebuildTargetId, ScopeKey).
/// Prevents same target + same company scope, or same target + global scope, from overlapping.
/// </summary>
public interface IRebuildExecutionLockStore
{
    /// <summary>Scope key for locking: company Guid "N" or "global".</summary>
    string GetScopeKey(Guid? scopeCompanyId);

    Task<bool> TryAcquireAsync(string rebuildTargetId, string scopeKey, Guid rebuildOperationId, CancellationToken cancellationToken = default);

    Task ReleaseAsync(string rebuildTargetId, string scopeKey, Guid rebuildOperationId, CancellationToken cancellationToken = default);
}
