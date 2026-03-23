namespace CephasOps.Application.Provisioning.DTOs;

/// <summary>Response after successful self-service signup. Tenant is provisioned with trial subscription.</summary>
public class SignupResultDto
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
}
