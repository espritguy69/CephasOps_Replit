namespace CephasOps.Application.Notifications;

/// <summary>
/// Resolves the default (first active) email account for sending. Used by notification email dispatch (Phase 6).
/// </summary>
public interface IDefaultEmailAccountIdProvider
{
    /// <summary>Returns the first active email account id, or null if none.</summary>
    Task<Guid?> GetDefaultEmailAccountIdAsync(CancellationToken cancellationToken = default);
}
