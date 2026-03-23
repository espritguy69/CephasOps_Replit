namespace CephasOps.Application.Provisioning.DTOs;

/// <summary>Result of a successful tenant provisioning.</summary>
public class ProvisionTenantResultDto
{
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid AdminUserId { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public List<ProvisionedDepartmentDto> Departments { get; set; } = new();
    public Guid? SubscriptionId { get; set; }
    public string? PlanSlug { get; set; }
}

public class ProvisionedDepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
