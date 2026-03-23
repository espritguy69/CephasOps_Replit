using CephasOps.Domain.Billing.Enums;
using CephasOps.Domain.Companies.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Subscription;

/// <summary>Central subscription/tenant access evaluation (Phase 3). Company status is the primary gate.</summary>
public class SubscriptionAccessService : ISubscriptionAccessService
{
    private readonly ApplicationDbContext _context;

    public SubscriptionAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionAccessResult> GetAccessForCompanyAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            return SubscriptionAccessResult.Allow(); // Legacy / no tenant context: allow (existing behaviour)

        var company = await _context.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId.Value)
            .Select(c => new { c.Status, c.TenantId })
            .FirstOrDefaultAsync(cancellationToken);

        if (company == null)
            return SubscriptionAccessResult.Allow(); // Unknown company: allow (e.g. legacy)

        return await Evaluate(company.Status, company.TenantId, cancellationToken);
    }

    public async Task<SubscriptionAccessResult> GetAccessForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .Select(c => new { c.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (company == null)
            return SubscriptionAccessResult.Allow(); // No company for tenant: allow

        return await EvaluateWithSubscriptionAsync(company.Status, tenantId, cancellationToken);
    }

    public async Task<bool> CanPerformWritesAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        var result = await GetAccessForCompanyAsync(companyId, cancellationToken);
        return result.Allowed && !result.ReadOnlyMode;
    }

    private async Task<SubscriptionAccessResult> EvaluateWithSubscriptionAsync(CompanyStatus companyStatus, Guid tenantId, CancellationToken cancellationToken)
    {
        // Primary gate: company status
        var primary = EvaluateStatus(companyStatus);
        if (!primary.Allowed)
            return primary;

        // Secondary: subscription status (optional strictness)
        var sub = await _context.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartedAtUtc)
            .Select(s => new { s.Status, s.CurrentPeriodEndUtc, s.TrialEndsAtUtc })
            .FirstOrDefaultAsync(cancellationToken);

        if (sub == null)
            return SubscriptionAccessResult.Allow(); // No subscription record: allow (e.g. legacy or trial not yet linked)

        if (sub.Status == TenantSubscriptionStatus.Cancelled)
            return SubscriptionAccessResult.Deny(SubscriptionDenialReasons.SubscriptionCancelled);
        if (sub.Status == TenantSubscriptionStatus.PastDue)
            return SubscriptionAccessResult.ReadOnly(SubscriptionDenialReasons.SubscriptionPastDue);
        var now = DateTime.UtcNow;
        if (sub.TrialEndsAtUtc.HasValue && sub.TrialEndsAtUtc.Value < now)
            return SubscriptionAccessResult.Deny("Trial expired");
        if (sub.CurrentPeriodEndUtc.HasValue && sub.CurrentPeriodEndUtc.Value < now)
            return SubscriptionAccessResult.Deny(SubscriptionDenialReasons.SubscriptionExpired);

        return SubscriptionAccessResult.Allow();
    }

    private async Task<SubscriptionAccessResult> Evaluate(CompanyStatus companyStatus, Guid? tenantId, CancellationToken cancellationToken)
    {
        var primary = EvaluateStatus(companyStatus);
        if (!primary.Allowed)
            return primary;
        if (tenantId.HasValue)
            return await EvaluateWithSubscriptionAsync(companyStatus, tenantId.Value, cancellationToken);
        return SubscriptionAccessResult.Allow();
    }

    private static SubscriptionAccessResult EvaluateStatus(CompanyStatus status)
    {
        return status switch
        {
            CompanyStatus.Active => SubscriptionAccessResult.Allow(),
            CompanyStatus.Trial => SubscriptionAccessResult.Allow(),
            CompanyStatus.Suspended => SubscriptionAccessResult.Deny(SubscriptionDenialReasons.TenantSuspended),
            CompanyStatus.Disabled => SubscriptionAccessResult.Deny(SubscriptionDenialReasons.TenantDisabled),
            CompanyStatus.PendingProvisioning => SubscriptionAccessResult.Deny("tenant_pending_provisioning"),
            CompanyStatus.Archived => SubscriptionAccessResult.Deny("tenant_archived"),
            _ => SubscriptionAccessResult.Allow()
        };
    }
}
