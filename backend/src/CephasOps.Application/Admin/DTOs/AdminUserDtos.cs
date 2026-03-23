namespace CephasOps.Application.Admin.DTOs;

/// <summary>
/// Single user row for admin user list (paged).
/// </summary>
public class AdminUserListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public bool MustChangePassword { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<AdminUserDepartmentDto> Departments { get; set; } = new();
}

/// <summary>
/// Department membership for admin user list/detail.
/// </summary>
public class AdminUserDepartmentDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? Role { get; set; }
}

/// <summary>
/// Paged result for admin user list.
/// </summary>
public class AdminUserListResultDto
{
    public List<AdminUserListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Full user detail for admin view/edit.
/// </summary>
public class AdminUserDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public bool MustChangePassword { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<AdminUserDepartmentDto> Departments { get; set; } = new();
}

/// <summary>
/// Request to create a new user (admin only).
/// </summary>
public class CreateAdminUserRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
    public List<string> RoleNames { get; set; } = new();
    public List<AdminUserDepartmentMembershipDto>? DepartmentMemberships { get; set; }
}

/// <summary>
/// Request to update an existing user (admin only).
/// </summary>
public class UpdateAdminUserRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public List<string> RoleNames { get; set; } = new();
    public List<AdminUserDepartmentMembershipDto>? DepartmentMemberships { get; set; }
}

/// <summary>
/// Department membership for create/update.
/// </summary>
public class AdminUserDepartmentMembershipDto
{
    public Guid DepartmentId { get; set; }
    public string Role { get; set; } = "Member";
    public bool IsDefault { get; set; }
}

/// <summary>
/// Request to set user active/inactive.
/// </summary>
public class SetUserActiveRequestDto
{
    public bool IsActive { get; set; }
}

/// <summary>
/// Request to change user roles.
/// </summary>
public class SetUserRolesRequestDto
{
    public List<string> RoleNames { get; set; } = new();
}

/// <summary>
/// Request to reset user password (admin sets new temporary password).
/// </summary>
public class AdminResetPasswordRequestDto
{
    public string NewPassword { get; set; } = string.Empty;
    /// <summary>
    /// When true, user must change password on next login. Default true for admin reset.
    /// </summary>
    public bool ForceMustChangePassword { get; set; } = true;
}

/// <summary>
/// Result of create user (returned in response body).
/// </summary>
public class CreateAdminUserResultDto
{
    public Guid Id { get; set; }
}
