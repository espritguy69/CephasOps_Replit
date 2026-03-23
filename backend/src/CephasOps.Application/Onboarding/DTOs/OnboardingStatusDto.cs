namespace CephasOps.Application.Onboarding.DTOs;

/// <summary>Current onboarding progress for the tenant.</summary>
public class OnboardingStatusDto
{
    public Guid TenantId { get; set; }
    public bool CompanySetupDone { get; set; }
    public bool DepartmentSetupDone { get; set; }
    public bool UserInvitationsDone { get; set; }
    public bool BasicConfigDone { get; set; }
    public bool IsComplete { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
