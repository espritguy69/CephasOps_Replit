namespace CephasOps.Application.Tenants.DTOs;

/// <summary>
/// Request to create a tenant (Phase 11).
/// </summary>
public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
