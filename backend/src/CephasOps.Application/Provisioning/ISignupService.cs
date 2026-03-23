using CephasOps.Application.Provisioning.DTOs;

namespace CephasOps.Application.Provisioning;

/// <summary>Public self-service tenant signup. Validates email, company code and slug uniqueness then provisions tenant with trial.</summary>
public interface ISignupService
{
    /// <summary>Validate and provision a new tenant (trial subscription, admin user). Throws on validation or duplicate.</summary>
    Task<SignupResultDto> SignupAsync(SignupRequestDto request, CancellationToken cancellationToken = default);
}
