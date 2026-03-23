using CephasOps.Application.Auth.DTOs;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Auth.Services;

/// <summary>
/// Lists and revokes refresh tokens (sessions). v1.4 Phase 3. No JWT changes; revoke logic matches existing flows.
/// </summary>
public class UserSessionService : IUserSessionService
{
    private readonly ApplicationDbContext _context;

    public UserSessionService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<UserSessionDto>> GetSessionsAsync(
        Guid? userId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _context.RefreshTokens
            .AsNoTracking()
            .Include(rt => rt.User)
            .Where(rt => true);

        if (userId.HasValue)
            query = query.Where(rt => rt.UserId == userId.Value);
        if (dateFrom.HasValue)
            query = query.Where(rt => rt.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(rt => rt.CreatedAt <= dateTo.Value);
        if (activeOnly)
            query = query.Where(rt => !rt.IsRevoked && rt.ExpiresAt > now);

        var list = await query
            .OrderByDescending(rt => rt.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        return list.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<List<UserSessionDto>> GetSessionsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var list = await _context.RefreshTokens
            .AsNoTracking()
            .Include(rt => rt.User)
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return list.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == sessionId, cancellationToken);
        if (token == null) return false;
        if (token.IsRevoked) return true; // already revoked

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> RevokeAllSessionsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var rt in tokens)
        {
            rt.IsRevoked = true;
            rt.RevokedAt = now;
        }
        await _context.SaveChangesAsync(cancellationToken);
        return tokens.Count;
    }

    private static UserSessionDto Map(RefreshToken rt)
    {
        return new UserSessionDto
        {
            SessionId = rt.Id,
            UserId = rt.UserId,
            UserEmail = rt.User?.Email,
            CreatedAtUtc = rt.CreatedAt,
            ExpiresAtUtc = rt.ExpiresAt,
            IpAddress = rt.CreatedFromIp,
            UserAgent = rt.UserAgent,
            IsRevoked = rt.IsRevoked
        };
    }
}
