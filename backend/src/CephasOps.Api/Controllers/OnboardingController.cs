using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Onboarding;
using CephasOps.Application.Onboarding.DTOs;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Controllers;

/// <summary>Tenant onboarding wizard: get progress and mark steps complete. Requires authenticated tenant user.</summary>
[ApiController]
[Route("api/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingProgressService _onboardingService;
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(
        IOnboardingProgressService onboardingService,
        ApplicationDbContext context,
        ITenantProvider tenantProvider,
        ILogger<OnboardingController> logger)
    {
        _onboardingService = onboardingService;
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>Get onboarding progress for the current tenant.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<OnboardingStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<OnboardingStatusDto>>> GetStatus(CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (!companyId.HasValue)
            return this.Unauthorized<OnboardingStatusDto>("Company context required");

        var tenantId = await _context.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId.Value)
            .Select(c => c.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!tenantId.HasValue)
            return this.Unauthorized<OnboardingStatusDto>("Tenant not found for company");

        var status = await _onboardingService.GetStatusAsync(tenantId.Value, cancellationToken);
        if (status == null)
            status = await _onboardingService.EnsureProgressCreatedAsync(tenantId.Value, cancellationToken);
        return this.Success(status);
    }

    /// <summary>Mark an onboarding step as complete. Step: company, department, invitations, config.</summary>
    [HttpPatch("status")]
    [ProducesResponseType(typeof(ApiResponse<OnboardingStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<OnboardingStatusDto>>> UpdateStep(
        [FromBody] OnboardingStepUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (!companyId.HasValue)
            return this.Unauthorized<OnboardingStatusDto>("Company context required");

        var tenantId = await _context.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId.Value)
            .Select(c => c.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!tenantId.HasValue)
            return this.Unauthorized<OnboardingStatusDto>("Tenant not found for company");

        if (string.IsNullOrWhiteSpace(request?.Step))
            return this.Error<OnboardingStatusDto>("Step is required", 400);

        try
        {
            var status = await _onboardingService.SetStepCompleteAsync(tenantId.Value, request.Step, cancellationToken);
            return this.Success(status, "Step marked complete.");
        }
        catch (ArgumentException ex)
        {
            return this.Error<OnboardingStatusDto>(ex.Message, 400);
        }
    }
}
