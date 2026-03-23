using Microsoft.AspNetCore.Authorization;

namespace CephasOps.Api.Authorization;

/// <summary>
/// Authorization requirement that the current user has the specified permission (or is SuperAdmin).
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? string.Empty;
    }
}
