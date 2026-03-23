using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace CephasOps.Api.Authorization;

/// <summary>
/// Specifies that the endpoint requires the given permission (or SuperAdmin). Use with permission names from PermissionCatalog.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission ?? string.Empty;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (string.IsNullOrEmpty(Permission))
        {
            context.Result = new ForbidResult();
            return;
        }

        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        var user = context.HttpContext.User;
        var requirement = new PermissionRequirement(Permission);
        var result = await authService.AuthorizeAsync(user, context.ActionDescriptor.DisplayName ?? "Resource", requirement);
        if (!result.Succeeded)
        {
            context.Result = new ForbidResult();
        }
    }
}
