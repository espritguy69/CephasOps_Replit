using CephasOps.Application.Onboarding.DTOs;

namespace CephasOps.Application.Onboarding;

/// <summary>Get and update tenant onboarding wizard progress.</summary>
public interface IOnboardingProgressService
{
    Task<OnboardingStatusDto?> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<OnboardingStatusDto> SetStepCompleteAsync(Guid tenantId, string step, CancellationToken cancellationToken = default);
    Task<OnboardingStatusDto> EnsureProgressCreatedAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
