using CephasOps.Application.Onboarding.DTOs;
using CephasOps.Domain.Tenants.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Onboarding;

public class OnboardingProgressService : IOnboardingProgressService
{
    private readonly ApplicationDbContext _context;

    public OnboardingProgressService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OnboardingStatusDto?> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var progress = await _context.TenantOnboardingProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
        return progress == null ? null : Map(progress);
    }

    public async Task<OnboardingStatusDto> EnsureProgressCreatedAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var progress = await _context.TenantOnboardingProgress
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
        if (progress != null)
            return Map(progress);

        progress = new TenantOnboardingProgress
        {
            TenantId = tenantId,
            CompanySetupDone = false,
            DepartmentSetupDone = false,
            UserInvitationsDone = false,
            BasicConfigDone = false
        };
        _context.TenantOnboardingProgress.Add(progress);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(progress);
    }

    public async Task<OnboardingStatusDto> SetStepCompleteAsync(Guid tenantId, string step, CancellationToken cancellationToken = default)
    {
        var progress = await _context.TenantOnboardingProgress
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
        if (progress == null)
        {
            progress = new TenantOnboardingProgress { TenantId = tenantId };
            _context.TenantOnboardingProgress.Add(progress);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var stepNorm = step?.Trim().ToLowerInvariant() ?? "";
        switch (stepNorm)
        {
            case "company":
            case "companysetup":
                progress.CompanySetupDone = true;
                break;
            case "department":
            case "departmentsetup":
                progress.DepartmentSetupDone = true;
                break;
            case "invitations":
            case "userinvitations":
                progress.UserInvitationsDone = true;
                break;
            case "config":
            case "basicconfig":
                progress.BasicConfigDone = true;
                break;
            default:
                throw new ArgumentException($"Unknown onboarding step: '{step}'. Use: company, department, invitations, config.", nameof(step));
        }

        progress.UpdatedAtUtc = DateTime.UtcNow;
        if (progress.CompanySetupDone && progress.DepartmentSetupDone && progress.UserInvitationsDone && progress.BasicConfigDone)
            progress.CompletedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(progress);
    }

    private static OnboardingStatusDto Map(TenantOnboardingProgress p) => new()
    {
        TenantId = p.TenantId,
        CompanySetupDone = p.CompanySetupDone,
        DepartmentSetupDone = p.DepartmentSetupDone,
        UserInvitationsDone = p.UserInvitationsDone,
        BasicConfigDone = p.BasicConfigDone,
        IsComplete = p.CompanySetupDone && p.DepartmentSetupDone && p.UserInvitationsDone && p.BasicConfigDone,
        CompletedAtUtc = p.CompletedAtUtc
    };
}
