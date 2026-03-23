namespace CephasOps.Application.Tenants.DTOs;

/// <summary>
/// Request to update a tenant (Phase 11).
/// </summary>
public class UpdateTenantRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public bool? IsActive { get; set; }
}
