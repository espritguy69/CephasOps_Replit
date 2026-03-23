using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Domain.Authorization;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Admin role and role-permission matrix. SuperAdmin/Admin only.
/// </summary>
[ApiController]
[Route("api/admin/roles")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminRolesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminRolesController> _logger;

    public AdminRolesController(ApplicationDbContext context, ILogger<AdminRolesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List all roles (Id, Name, Scope).
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.AdminRolesView)]
    [ProducesResponseType(typeof(ApiResponse<List<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> List(CancellationToken cancellationToken = default)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto { Id = r.Id, Name = r.Name, Scope = r.Scope })
            .ToListAsync(cancellationToken);
        return this.Success(roles);
    }

    /// <summary>
    /// Get permission names assigned to a role.
    /// </summary>
    [HttpGet("{roleId:guid}/permissions")]
    [RequirePermission(PermissionCatalog.AdminRolesView)]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetPermissions(Guid roleId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
        if (!exists)
            return this.NotFound<List<string>>("Role not found");

        var names = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Join(_context.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p.Name)
            .ToListAsync(cancellationToken);
        return this.Success(names);
    }

    /// <summary>
    /// Set permissions for a role (replaces existing). Validates permission names against catalog. Prevents removing all permissions from SuperAdmin.
    /// </summary>
    [HttpPut("{roleId:guid}/permissions")]
    [RequirePermission(PermissionCatalog.AdminRolesEdit)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> SetPermissions(
        Guid roleId,
        [FromBody] SetRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role == null)
            return this.NotFound<object>("Role not found");

        var permissionNames = request?.PermissionNames?.Distinct(StringComparer.Ordinal).ToList() ?? new List<string>();
        var valid = PermissionCatalog.FilterValid(permissionNames);
        var invalid = permissionNames.Except(valid, StringComparer.Ordinal).ToList();
        if (invalid.Count > 0)
        {
            return this.BadRequest<object>($"Invalid permission name(s): {string.Join(", ", invalid)}");
        }

        if (role.Name == "SuperAdmin" && valid.Count == 0)
        {
            return this.BadRequest<object>("Cannot remove all permissions from SuperAdmin role.");
        }

        var permissionIds = await _context.Permissions
            .Where(p => valid.Contains(p.Name))
            .ToDictionaryAsync(p => p.Name, p => p.Id, cancellationToken);

        var current = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);
        _context.RolePermissions.RemoveRange(current);
        foreach (var name in valid)
        {
            if (permissionIds.TryGetValue(name, out var pid))
                _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pid });
        }
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated permissions for role {RoleName} ({RoleId}), count: {Count}", role.Name, roleId, valid.Count);
        return this.Success<object>(new { }, "Permissions updated.");
    }

    /// <summary>
    /// List all permissions (catalog from DB: Id, Name, Description).
    /// </summary>
    [HttpGet("~/api/admin/permissions")]
    [RequirePermission(PermissionCatalog.AdminRolesView)]
    [ProducesResponseType(typeof(ApiResponse<List<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetPermissionsCatalog(CancellationToken cancellationToken = default)
    {
        var list = await _context.Permissions
            .AsNoTracking()
            .Where(p => PermissionCatalog.All.Contains(p.Name))
            .OrderBy(p => p.Name)
            .Select(p => new PermissionDto { Id = p.Id, Name = p.Name, Description = p.Description })
            .ToListAsync(cancellationToken);
        return this.Success(list);
    }
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class SetRolePermissionsRequest
{
    public List<string>? PermissionNames { get; set; }
}
