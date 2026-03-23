namespace CephasOps.Domain.Users.Entities;

/// <summary>
/// Role-Permission assignment entity
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    // Navigation properties
    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}

