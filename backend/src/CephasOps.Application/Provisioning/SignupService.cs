using CephasOps.Application.Provisioning.DTOs;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Provisioning;

/// <summary>Self-service signup: validates then provisions tenant with trial subscription.</summary>
public class SignupService : ISignupService
{
    private readonly ICompanyProvisioningService _provisioningService;
    private readonly ILogger<SignupService> _logger;

    public SignupService(ICompanyProvisioningService provisioningService, ILogger<SignupService> logger)
    {
        _provisioningService = provisioningService;
        _logger = logger;
    }

    public async Task<SignupResultDto> SignupAsync(SignupRequestDto request, CancellationToken cancellationToken = default)
    {
        var provisionRequest = new ProvisionTenantRequestDto
        {
            CompanyName = request.CompanyName.Trim(),
            CompanyCode = request.CompanyCode.Trim(),
            Slug = request.Slug?.Trim(),
            AdminFullName = request.AdminFullName.Trim(),
            AdminEmail = request.AdminEmail.Trim().ToLowerInvariant(),
            AdminPassword = request.AdminPassword,
            PlanSlug = null,
            TrialDays = 14,
            InitialStatus = "Trial"
        };

        var result = await _provisioningService.ProvisionAsync(provisionRequest, cancellationToken);

        _logger.LogInformation("Self-service signup completed: TenantId={TenantId}, CompanyCode={Code}, AdminEmail={Email}",
            result.TenantId, result.CompanyCode, result.AdminEmail);

        return new SignupResultDto
        {
            TenantId = result.TenantId,
            CompanyId = result.CompanyId,
            CompanyName = result.CompanyName,
            Slug = result.Slug,
            AdminEmail = result.AdminEmail,
            MustChangePassword = result.MustChangePassword,
            Message = "Signup successful. You can log in with your email and password. Your trial has started."
        };
    }
}
