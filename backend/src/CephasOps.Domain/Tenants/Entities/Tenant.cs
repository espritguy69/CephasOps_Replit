namespace CephasOps.Domain.Tenants.Entities;

/// <summary>
/// Tenant entity - top-level isolation boundary for multi-tenant SaaS.
/// Hierarchy: Tenant → Companies → Departments → Users/Roles.
/// Phase 11.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe identifier (e.g. for subdomains or routing).</summary>
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
