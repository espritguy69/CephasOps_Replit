using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Authorization;

/// <summary>
/// Loads user permission names from DB (via UserRole -> RolePermission -> Permission).
/// </summary>
public class UserPermissionProvider : IUserPermissionProvider
{
    private readonly ApplicationDbContext _context;

    public UserPermissionProvider(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<string>> GetPermissionNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
            return Array.Empty<string>();

        var permissionNames = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(_context.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissionNames;
    }
}
