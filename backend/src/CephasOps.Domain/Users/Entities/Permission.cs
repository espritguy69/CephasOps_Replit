namespace CephasOps.Domain.Users.Entities;

/// <summary>
/// Permission entity - represents system permissions
/// </summary>
public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // orders.view, orders.create, etc.
    public string? Description { get; set; }
}

