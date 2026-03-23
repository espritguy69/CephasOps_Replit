namespace CephasOps.Application.Onboarding.DTOs;

/// <summary>Request to mark an onboarding step as complete.</summary>
public class OnboardingStepUpdateDto
{
    public string Step { get; set; } = string.Empty;
}
