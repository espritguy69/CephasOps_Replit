namespace CephasOps.Application.Authorization;

/// <summary>
/// Provides the set of permission names for a user (from their roles). Used for auth/me and permission authorization.
/// </summary>
public interface IUserPermissionProvider
{
    /// <summary>
    /// Get distinct permission names for the user (from Role -> RolePermission -> Permission). SuperAdmin is not special here; caller may bypass.
    /// </summary>
    Task<IReadOnlyList<string>> GetPermissionNamesAsync(Guid userId, CancellationToken cancellationToken = default);
}
