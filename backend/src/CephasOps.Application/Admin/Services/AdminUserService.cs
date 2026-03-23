using System.Text.Json;
using CephasOps.Application.Admin.DTOs;
using CephasOps.Application.Audit.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Admin.Services;

/// <summary>
/// Admin user management: list, create, update, activate/deactivate, roles, reset password.
/// Uses DatabaseSeeder for password hashing (same as login).
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminUserService> _logger;
    private readonly IAuditLogService? _auditLogService;
    private readonly IPasswordHasher _passwordHasher;

    private static readonly string[] AdminRoleNames = { "SuperAdmin", "Admin" };
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public AdminUserService(
        ApplicationDbContext context,
        ILogger<AdminUserService> logger,
        IPasswordHasher passwordHasher,
        IAuditLogService? auditLogService = null)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
    }

    public async Task<AdminUserListResultDto> ListAsync(
        int page,
        int pageSize,
        string? search,
        string? roleName,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Users.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u =>
                (u.Name != null && u.Name.ToLower().Contains(s)) ||
                (u.Email != null && u.Email.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            var roleId = await _context.Roles
                .Where(r => r.Name == roleName)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (roleId != Guid.Empty)
            {
                var userIdsWithRole = await _context.UserRoles
                    .Where(ur => ur.RoleId == roleId)
                    .Select(ur => ur.UserId)
                    .ToListAsync(cancellationToken);
                query = query.Where(u => userIdsWithRole.Contains(u.Id));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var userIds = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
            return new AdminUserListResultDto { TotalCount = totalCount, Page = page, PageSize = pageSize };

        var userRoles = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Include(ur => ur.Role)
            .ToListAsync(cancellationToken);

        var memberships = await _context.DepartmentMemberships
            .AsNoTracking()
            .Where(dm => userIds.Contains(dm.UserId))
            .Include(dm => dm.Department)
            .ToListAsync(cancellationToken);

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);

        var roleMap = userRoles
            .Where(ur => ur.Role != null)
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key, g => g.Select(ur => ur.Role!.Name).Distinct().ToList());

        var deptMap = memberships
            .GroupBy(dm => dm.UserId)
            .ToDictionary(g => g.Key, g => g.Select(dm => new AdminUserDepartmentDto
            {
                DepartmentId = dm.DepartmentId,
                DepartmentName = dm.Department?.Name ?? "",
                Role = dm.Role
            }).ToList());

        var items = users.Select(u => new AdminUserListItemDto
        {
            Id = u.Id,
            Name = u.Name ?? "",
            Email = u.Email ?? "",
            Phone = u.Phone,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAtUtc = u.LastLoginAtUtc,
            MustChangePassword = u.MustChangePassword,
            Roles = roleMap.GetValueOrDefault(u.Id, new List<string>()),
            Departments = deptMap.GetValueOrDefault(u.Id, new List<AdminUserDepartmentDto>())
        }).ToList();

        return new AdminUserListResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDetailDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.DepartmentMemberships)
            .ThenInclude(dm => dm.Department)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return null;

        var roles = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role != null ? ur.Role.Name : null)
            .Where(n => n != null)
            .Cast<string>()
            .ToListAsync(cancellationToken);

        var departments = user.DepartmentMemberships.Select(dm => new AdminUserDepartmentDto
        {
            DepartmentId = dm.DepartmentId,
            DepartmentName = dm.Department?.Name ?? "",
            Role = dm.Role
        }).ToList();

        return new AdminUserDetailDto
        {
            Id = user.Id,
            Name = user.Name ?? "",
            Email = user.Email ?? "",
            Phone = user.Phone,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAtUtc = user.LastLoginAtUtc,
            MustChangePassword = user.MustChangePassword,
            Roles = roles,
            Departments = departments
        };
    }

    public async Task<List<string>> GetRoleNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(CreateAdminUserRequestDto request, Guid? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var emailNorm = request.Email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(emailNorm))
            throw new InvalidOperationException("Email is required.");

        var exists = await _context.Users.AnyAsync(u => u.Email != null && u.Email.Trim().ToLowerInvariant() == emailNorm, cancellationToken);
        if (exists)
            throw new InvalidOperationException("A user with this email already exists.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new InvalidOperationException("Password must be at least 6 characters.");

        var roleIds = await ResolveRoleIdsAsync(request.RoleNames ?? new List<string>(), cancellationToken);
        if (roleIds.Count == 0)
            throw new InvalidOperationException("At least one role is required.");

        var departmentMemberships = await NormalizeAndValidateDepartmentMembershipsAsync(
            request.DepartmentMemberships,
            nameof(CreateAdminUserRequestDto.DepartmentMemberships),
            cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name?.Trim() ?? "",
            Email = emailNorm,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        foreach (var roleId in roleIds)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                CompanyId = null,
                CreatedAt = DateTime.UtcNow
            });
        }

        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a user.");
        foreach (var d in departmentMemberships)
        {
            var dept = await _context.Departments
                .FirstOrDefaultAsync(dept => dept.Id == d.DepartmentId && dept.CompanyId == tenantId.Value, cancellationToken);
            if (dept == null)
                throw new InvalidOperationException($"Department {d.DepartmentId} not found.");
            _context.DepartmentMemberships.Add(new DepartmentMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DepartmentId = d.DepartmentId,
                CompanyId = dept.CompanyId,
                Role = string.IsNullOrWhiteSpace(d.Role) ? "Member" : d.Role.Trim(),
                IsDefault = d.IsDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin created user {UserId} {Email}", user.Id, user.Email);

        await LogUserAuditAsync("Created", user.Id, actorUserId, fieldChanges: new (string, object?, object?)[] { ("Email", null, emailNorm), ("Roles", null, string.Join(",", request.RoleNames ?? new List<string>())) }, metadata: $"Name={user.Name}", cancellationToken: cancellationToken);

        return user.Id;
    }

    public async Task UpdateAsync(Guid userId, UpdateAdminUserRequestDto request, Guid? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var user = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.Users
                .Include(u => u.DepartmentMemberships)
                .FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == tenantId.Value, cancellationToken)
            : await _context.Users
                .Include(u => u.DepartmentMemberships)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var emailNorm = request.Email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(emailNorm))
            throw new InvalidOperationException("Email is required.");

        var duplicate = await _context.Users
            .AnyAsync(u => u.Id != userId && u.Email != null && u.Email.Trim().ToLowerInvariant() == emailNorm, cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("A user with this email already exists.");

        var roleIds = await ResolveRoleIdsAsync(request.RoleNames ?? new List<string>(), cancellationToken);
        if (roleIds.Count == 0)
            throw new InvalidOperationException("At least one role is required.");

        var departmentMemberships = await NormalizeAndValidateDepartmentMembershipsAsync(
            request.DepartmentMemberships,
            nameof(UpdateAdminUserRequestDto.DepartmentMemberships),
            cancellationToken);

        var oldName = user.Name ?? "";
        var oldEmail = user.Email ?? "";
        var oldRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role != null ? ur.Role.Name : null)
            .Where(n => n != null)
            .ToListAsync(cancellationToken);
        var oldDeptSummary = user.DepartmentMemberships.Count.ToString();

        user.Name = request.Name?.Trim() ?? "";
        user.Email = emailNorm;
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        var existingUserRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);
        _context.UserRoles.RemoveRange(existingUserRoles);
        foreach (var roleId in roleIds)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                CompanyId = null,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.DepartmentMemberships.RemoveRange(user.DepartmentMemberships);
        var updateTenantId = user.CompanyId ?? tenantId;
        if (!updateTenantId.HasValue || updateTenantId.Value == Guid.Empty)
            throw new InvalidOperationException("User company context is required to update department memberships.");
        foreach (var d in departmentMemberships)
        {
            var dept = await _context.Departments
                .FirstOrDefaultAsync(dept => dept.Id == d.DepartmentId && dept.CompanyId == updateTenantId.Value, cancellationToken);
            if (dept == null)
                throw new InvalidOperationException($"Department {d.DepartmentId} not found.");
            _context.DepartmentMemberships.Add(new DepartmentMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DepartmentId = d.DepartmentId,
                CompanyId = dept.CompanyId,
                Role = string.IsNullOrWhiteSpace(d.Role) ? "Member" : d.Role.Trim(),
                IsDefault = d.IsDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin updated user {UserId}", userId);

        var newRoles = request.RoleNames ?? new List<string>();
        var fieldChanges = new List<object>
        {
            new { field = "Name", oldValue = oldName, newValue = user.Name },
            new { field = "Email", oldValue = oldEmail, newValue = emailNorm },
            new { field = "Roles", oldValue = string.Join(",", oldRoles), newValue = string.Join(",", newRoles) },
            new { field = "Departments", oldValue = oldDeptSummary, newValue = departmentMemberships.Count.ToString() }
        };
        await LogUserAuditAsync("Updated", userId, actorUserId, fieldChangesJson: JsonSerializer.Serialize(fieldChanges), metadata: null, cancellationToken: cancellationToken);
    }

    public async Task SetActiveAsync(Guid userId, bool isActive, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (userId == currentUserId && !isActive)
            throw new InvalidOperationException("You cannot deactivate your own account.");

        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var user = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == tenantId.Value, cancellationToken)
            : await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (!isActive)
        {
            var isAdmin = await UserHasAnyRoleAsync(userId, AdminRoleNames, cancellationToken);
            if (isAdmin)
            {
                var otherActiveAdmins = await _context.UserRoles
                    .Where(ur => ur.UserId != userId && _context.Roles.Any(r => r.Id == ur.RoleId && (r.Name == "SuperAdmin" || r.Name == "Admin")))
                    .Where(ur => _context.Users.Any(u => u.Id == ur.UserId && u.IsActive))
                    .CountAsync(cancellationToken);
                if (otherActiveAdmins == 0)
                    throw new InvalidOperationException("Cannot deactivate the last active administrator.");
            }
        }

        user.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin set user {UserId} IsActive={IsActive}", userId, isActive);
        await LogUserAuditAsync(isActive ? "Activated" : "Deactivated", userId, currentUserId, metadata: $"IsActive={isActive}", cancellationToken: cancellationToken);
    }

    public async Task SetRolesAsync(Guid userId, List<string> roleNames, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var user = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == tenantId.Value, cancellationToken)
            : await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var newRoleIds = await ResolveRoleIdsAsync(roleNames ?? new List<string>(), cancellationToken);
        var newSet = await _context.Roles.Where(r => newRoleIds.Contains(r.Id)).Select(r => r.Name).ToListAsync(cancellationToken);
        var willHaveAdmin = newSet.Any(r => AdminRoleNames.Contains(r, StringComparer.OrdinalIgnoreCase));

        if (!willHaveAdmin && userId == currentUserId)
            throw new InvalidOperationException("You cannot remove your own administrator role.");

        if (!willHaveAdmin)
        {
            var otherActiveAdmins = await _context.UserRoles
                .Where(ur => ur.UserId != userId && _context.Roles.Any(r => r.Id == ur.RoleId && (r.Name == "SuperAdmin" || r.Name == "Admin")))
                .Where(ur => _context.Users.Any(u => u.Id == ur.UserId && u.IsActive))
                .CountAsync(cancellationToken);
            if (otherActiveAdmins == 0)
                throw new InvalidOperationException("Cannot remove the last active administrator.");
        }

        var existing = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);
        _context.UserRoles.RemoveRange(existing);
        foreach (var roleId in newRoleIds)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                CompanyId = null,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin set roles for user {UserId}", userId);
        var rolesSummary = string.Join(",", roleNames ?? new List<string>());
        await LogUserAuditAsync("RolesChanged", userId, currentUserId, metadata: $"Roles=[{rolesSummary}]", cancellationToken: cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid userId, AdminResetPasswordRequestDto request, Guid? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var newPassword = request?.NewPassword ?? "";
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new InvalidOperationException("Password must be at least 6 characters.");

        var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var user = (tenantId.HasValue && tenantId.Value != Guid.Empty)
            ? await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == tenantId.Value, cancellationToken)
            : await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.MustChangePassword = request?.ForceMustChangePassword ?? true;

        var oldTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var rt in oldTokens)
        {
            rt.IsRevoked = true;
            rt.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin reset password for user {UserId}, ForceMustChangePassword={Force}, refresh tokens revoked={Count}", userId, user.MustChangePassword, oldTokens.Count);
        await LogUserAuditAsync("PasswordReset", userId, actorUserId, metadata: $"Password reset by admin; ForceMustChangePassword={user.MustChangePassword}; refresh tokens revoked (hash not logged)", cancellationToken: cancellationToken);
        await LogAuthEventAsync(CephasOps.Application.Auth.AuthEventTypes.AdminPasswordReset, userId, actorUserId, cancellationToken);
    }

    private async Task<List<AdminUserDepartmentMembershipDto>> NormalizeAndValidateDepartmentMembershipsAsync(
        List<AdminUserDepartmentMembershipDto>? list,
        string paramName,
        CancellationToken cancellationToken)
    {
        if (list == null || list.Count == 0)
            return new List<AdminUserDepartmentMembershipDto>();

        var distinctByDept = list
            .GroupBy(d => d.DepartmentId)
            .Select(g => g.First())
            .ToList();

        if (distinctByDept.Count != list.Count)
            throw new InvalidOperationException("Duplicate department IDs are not allowed in department memberships.");

        var ids = distinctByDept.Select(d => d.DepartmentId).Distinct().ToList();
        var existingIds = await _context.Departments
            .Where(d => ids.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);
        var missing = ids.Except(existingIds).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"One or more department IDs are invalid or not found: {string.Join(", ", missing)}.");

        return distinctByDept;
    }

    private async Task LogUserAuditAsync(
        string action,
        Guid targetUserId,
        Guid? actorUserId,
        IEnumerable<(string field, object? oldValue, object? newValue)>? fieldChanges = null,
        string? fieldChangesJson = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (_auditLogService == null) return;
        var changesJson = fieldChangesJson ?? (fieldChanges != null
            ? JsonSerializer.Serialize(fieldChanges.Select(c => new { field = c.field, oldValue = c.oldValue, newValue = c.newValue }), JsonOptions)
            : null);
        await _auditLogService.LogAuditAsync(
            companyId: null,
            userId: actorUserId,
            entityType: "User",
            entityId: targetUserId,
            action,
            fieldChangesJson: changesJson,
            channel: "Api",
            ipAddress: null,
            metadataJson: metadata,
            cancellationToken);
    }

    private async Task LogAuthEventAsync(string action, Guid targetUserId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        if (_auditLogService == null) return;
        await _auditLogService.LogAuditAsync(
            companyId: null,
            userId: actorUserId,
            entityType: "Auth",
            entityId: targetUserId,
            action,
            fieldChangesJson: null,
            channel: "Api",
            ipAddress: null,
            metadataJson: null,
            cancellationToken);
    }

    private async Task<List<Guid>> ResolveRoleIdsAsync(List<string> roleNames, CancellationToken cancellationToken)
    {
        if (roleNames == null || roleNames.Count == 0) return new List<Guid>();
        var names = roleNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct().ToList();
        if (names.Count == 0) return new List<Guid>();
        return await _context.Roles
            .Where(r => names.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> UserHasAnyRoleAsync(Guid userId, string[] roleNames, CancellationToken cancellationToken)
    {
        return await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && _context.Roles.Any(r => r.Id == ur.RoleId && roleNames.Contains(r.Name)), cancellationToken);
    }
}
