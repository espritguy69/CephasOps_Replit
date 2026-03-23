namespace CephasOps.Domain.Users.Entities;

/// <summary>
/// Role entity - represents user roles
/// </summary>
public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // Admin, Scheduler, Warehouse, etc.
    public string Scope { get; set; } = string.Empty; // Company, Global
}

