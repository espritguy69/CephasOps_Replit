using CephasOps.Application.Auth.DTOs;

namespace CephasOps.Application.Auth.Services;

/// <summary>
/// Admin session management: list and revoke refresh tokens (sessions). v1.4 Phase 3.
/// </summary>
public interface IUserSessionService
{
    /// <summary>
    /// Get sessions with optional filters. Admin/SuperAdmin only.
    /// </summary>
    Task<List<UserSessionDto>> GetSessionsAsync(
        Guid? userId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get sessions for a specific user.
    /// </summary>
    Task<List<UserSessionDto>> GetSessionsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a single session (refresh token). Returns true if found and revoked.
    /// </summary>
    Task<bool> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all sessions for a user. Returns count revoked.
    /// </summary>
    Task<int> RevokeAllSessionsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
