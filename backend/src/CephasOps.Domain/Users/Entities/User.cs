using CephasOps.Domain.Departments.Entities;

namespace CephasOps.Domain.Users.Entities;

/// <summary>
/// User entity - represents system users
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last successful login (UTC). Updated only on full login when MustChangePassword is false.
    /// </summary>
    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>
    /// When true, user must change password before using the app (e.g. after admin reset).
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>
    /// Number of consecutive failed login attempts. Reset on successful login. (v1.3 Phase C)
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// When set, account is locked until this time (UTC). Cleared on successful login. (v1.3 Phase C)
    /// </summary>
    public DateTime? LockoutEndUtc { get; set; }

    /// <summary>Primary company (tenant) for this user. Used for JWT company_id and tenant isolation. Nullable until backfilled.</summary>
    public Guid? CompanyId { get; set; }

    // Navigation
    public ICollection<DepartmentMembership> DepartmentMemberships { get; set; } = new List<DepartmentMembership>();
}

