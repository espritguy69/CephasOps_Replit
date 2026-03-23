namespace CephasOps.Domain.Users.Entities;

/// <summary>
/// User-Role assignment entity (company-scoped)
/// </summary>
public class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; } // Nullable - global roles don't require company
    public Guid RoleId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Role? Role { get; set; }
}

