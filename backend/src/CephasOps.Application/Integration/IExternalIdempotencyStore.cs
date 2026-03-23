namespace CephasOps.Application.Integration;

/// <summary>
/// Idempotency for inbound webhooks: one successful processing per external idempotency key.
/// </summary>
public interface IExternalIdempotencyStore
{
    /// <summary>
    /// Try to claim processing for this key. Returns true if new (caller should process); false if already completed.
    /// </summary>
    Task<bool> TryClaimAsync(string idempotencyKey, string connectorKey, Guid? companyId, Guid receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark as completed (call after handler succeeded). Stores receipt id for diagnostics.
    /// Scoped by connectorKey and companyId for tenant-safe idempotency.
    /// </summary>
    Task MarkCompletedAsync(string idempotencyKey, string connectorKey, Guid? companyId, Guid receiptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if this key was already successfully processed (without claiming).
    /// Scoped by connectorKey and companyId for tenant-safe idempotency.
    /// </summary>
    Task<bool> IsCompletedAsync(string idempotencyKey, string connectorKey, Guid? companyId, CancellationToken cancellationToken = default);
}
