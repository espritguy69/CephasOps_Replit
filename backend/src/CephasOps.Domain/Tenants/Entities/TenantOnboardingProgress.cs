namespace CephasOps.Domain.Tenants.Entities;

/// <summary>Tracks tenant onboarding wizard progress (company setup, departments, invitations, config).</summary>
public class TenantOnboardingProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public bool CompanySetupDone { get; set; }
    public bool DepartmentSetupDone { get; set; }
    public bool UserInvitationsDone { get; set; }
    public bool BasicConfigDone { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
