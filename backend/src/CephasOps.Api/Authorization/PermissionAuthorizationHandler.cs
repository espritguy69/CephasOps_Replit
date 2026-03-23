using System.Security.Claims;
using CephasOps.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace CephasOps.Api.Authorization;

/// <summary>
/// Handles PermissionRequirement: allows if user is SuperAdmin or has the required permission via their roles.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserPermissionProvider _userPermissionProvider;

    public PermissionAuthorizationHandler(IUserPermissionProvider userPermissionProvider)
    {
        _userPermissionProvider = userPermissionProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (string.IsNullOrEmpty(requirement.Permission))
        {
            context.Fail();
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Fail();
            return;
        }

        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return;
        }

        var permissions = await _userPermissionProvider.GetPermissionNamesAsync(userId, default);
        if (permissions.Contains(requirement.Permission, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
