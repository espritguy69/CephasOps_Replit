using CephasOps.Application.Auth.DTOs;
using CephasOps.Application.Auth.Services;
using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Admin session management: list and revoke active sessions (refresh tokens). v1.4 Phase 3.
/// </summary>
[ApiController]
[Route("api/admin/security/sessions")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminSecuritySessionsController : ControllerBase
{
    private readonly IUserSessionService _sessionService;

    public AdminSecuritySessionsController(IUserSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// Get sessions with optional filters (user, date range, active only).
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.AdminSecurityView)]
    [ProducesResponseType(typeof(ApiResponse<List<UserSessionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetSessions(
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var list = await _sessionService.GetSessionsAsync(userId, dateFrom, dateTo, activeOnly, cancellationToken);
        return this.Success(list);
    }

    /// <summary>
    /// Get sessions for a specific user.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<UserSessionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserSessionDto>>>> GetSessionsForUser(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var list = await _sessionService.GetSessionsForUserAsync(userId, cancellationToken);
        return this.Success(list);
    }

    /// <summary>
    /// Revoke a single session. Admin revoking their own session is allowed (confirm in UI).
    /// </summary>
    [HttpPost("{sessionId:guid}/revoke")]
    [RequirePermission(PermissionCatalog.AdminSecuritySessionsRevoke)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> RevokeSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var revoked = await _sessionService.RevokeSessionAsync(sessionId, cancellationToken);
        if (!revoked)
            return this.NotFound("Session not found.");
        return this.Success<object>(new { revoked = true }, "Session revoked.");
    }

    /// <summary>
    /// Revoke all sessions for a user.
    /// </summary>
    [HttpPost("revoke-all/{userId:guid}")]
    [RequirePermission(PermissionCatalog.AdminSecuritySessionsRevoke)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> RevokeAllSessionsForUser(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var count = await _sessionService.RevokeAllSessionsForUserAsync(userId, cancellationToken);
        return this.Success<object>(new { revokedCount = count }, $"Revoked {count} session(s).");
    }
}
