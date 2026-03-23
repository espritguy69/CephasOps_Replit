using CephasOps.Application.Billing.Subscription.DTOs;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Domain.Companies.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Platform;

public class PlatformAdminService : IPlatformAdminService
{
    private readonly ApplicationDbContext _context;

    public PlatformAdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlatformTenantListDto>> ListTenantsAsync(string? search, int skip, int take, CancellationToken cancellationToken = default)
    {
        var tenantsQuery = _context.Tenants.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            tenantsQuery = tenantsQuery.Where(t =>
                (t.Name != null && t.Name.ToLower().Contains(term)) ||
                (t.Slug != null && t.Slug.ToLower().Contains(term)));
        }

        var tenants = await tenantsQuery.OrderBy(t => t.Name).Skip(skip).Take(take).ToListAsync(cancellationToken);
        if (tenants.Count == 0) return new List<PlatformTenantListDto>();

        var tenantIds = tenants.Select(t => t.Id).ToList();
        var companies = await _context.Companies.AsNoTracking()
            .Where(c => c.TenantId != null && tenantIds.Contains(c.TenantId!.Value))
            .ToListAsync(cancellationToken);
        var subs = await _context.TenantSubscriptions.AsNoTracking()
            .Where(s => tenantIds.Contains(s.TenantId))
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);

        var subByTenant = subs.GroupBy(s => s.TenantId).ToDictionary(g => g.Key, g => g.First());
        var companyByTenant = companies.Where(c => c.TenantId.HasValue).GroupBy(c => c.TenantId!.Value).ToDictionary(g => g.Key, g => g.First());

        return tenants.Select(t =>
        {
            var company = companyByTenant.GetValueOrDefault(t.Id);
            var sub = subByTenant.GetValueOrDefault(t.Id);
            return new PlatformTenantListDto
            {
                TenantId = t.Id,
                TenantName = t.Name,
                Slug = t.Slug,
                TenantIsActive = t.IsActive,
                CompanyId = company?.Id,
                CompanyCode = company?.Code,
                CompanyLegalName = company?.LegalName,
                CompanyStatus = company?.Status,
                SubscriptionStatus = sub?.Status.ToString(),
                TrialEndsAtUtc = sub?.TrialEndsAtUtc,
                NextBillingDateUtc = sub?.NextBillingDateUtc,
                CreatedAtUtc = t.CreatedAtUtc
            };
        }).ToList();
    }

    public async Task<PlatformTenantDiagnosticsDto?> GetTenantDiagnosticsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant == null) return null;

        var company = await _context.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        var sub = await _context.TenantSubscriptions.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var companyId = company?.Id;
        var userCount = companyId.HasValue
            ? await _context.Users.CountAsync(u => u.CompanyId == companyId, cancellationToken)
            : 0;
        var orderCount = companyId.HasValue
            ? await _context.Orders.CountAsync(o => o.CompanyId == companyId.Value, cancellationToken)
            : 0;

        return new PlatformTenantDiagnosticsDto
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Slug = tenant.Slug,
            CompanyId = companyId,
            CompanyCode = company?.Code,
            CompanyStatus = company?.Status.ToString(),
            UserCount = userCount,
            OrderCount = orderCount,
            SubscriptionStatus = sub?.Status.ToString(),
            TrialEndsAtUtc = sub?.TrialEndsAtUtc,
            NextBillingDateUtc = sub?.NextBillingDateUtc
        };
    }

    public async Task<Guid?> GetCompanyIdByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return company != Guid.Empty ? company : null;
    }

    public async Task<TenantSubscriptionDto?> GetTenantSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var sub = await _context.TenantSubscriptions.AsNoTracking()
            .Where(s => s.TenantId == tenantId && (s.Status == TenantSubscriptionStatus.Active || s.Status == TenantSubscriptionStatus.Trialing))
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await _context.TenantSubscriptions.AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.StartedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        if (sub == null) return null;
        var plan = await _context.BillingPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == sub.BillingPlanId, cancellationToken);
        return MapSub(sub, plan);
    }

    public async Task<TenantSubscriptionDto?> UpdateTenantSubscriptionAsync(Guid tenantId, PlatformTenantSubscriptionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var sub = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (sub == null) return null;

        if (!string.IsNullOrWhiteSpace(request.PlanSlug))
        {
            var plan = await _context.BillingPlans.FirstOrDefaultAsync(p => p.Slug == request.PlanSlug!.Trim() && p.IsActive, cancellationToken);
            if (plan == null)
                throw new ArgumentException($"Billing plan with slug '{request.PlanSlug}' not found or inactive.", nameof(request));
            sub.BillingPlanId = plan.Id;
        }

        if (request.Status != null)
        {
            if (!Enum.TryParse<TenantSubscriptionStatus>(request.Status.Trim(), true, out var status))
                throw new ArgumentException($"Invalid Status. Use: {string.Join(", ", Enum.GetNames<TenantSubscriptionStatus>())}", nameof(request));
            sub.Status = status;
        }

        if (request.TrialEndsAtUtc.HasValue)
            sub.TrialEndsAtUtc = request.TrialEndsAtUtc.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.TrialEndsAtUtc.Value, DateTimeKind.Utc) : request.TrialEndsAtUtc.Value;
        if (request.NextBillingDateUtc.HasValue)
            sub.NextBillingDateUtc = request.NextBillingDateUtc.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.NextBillingDateUtc.Value, DateTimeKind.Utc) : request.NextBillingDateUtc.Value;

        if (request.SeatLimit.HasValue)
            sub.SeatLimit = request.SeatLimit.Value >= 0 ? request.SeatLimit : null;
        if (request.StorageLimitBytes.HasValue)
            sub.StorageLimitBytes = request.StorageLimitBytes.Value >= 0 ? request.StorageLimitBytes : null;

        sub.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var planAfter = await _context.BillingPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == sub.BillingPlanId, cancellationToken);
        return MapSub(sub, planAfter);
    }

    private static TenantSubscriptionDto MapSub(TenantSubscription s, BillingPlan? plan) => new()
    {
        Id = s.Id,
        TenantId = s.TenantId,
        BillingPlanId = s.BillingPlanId,
        PlanSlug = plan?.Slug,
        Status = s.Status,
        StartedAtUtc = s.StartedAtUtc,
        CurrentPeriodEndUtc = s.CurrentPeriodEndUtc,
        TrialEndsAtUtc = s.TrialEndsAtUtc,
        BillingCycle = s.BillingCycle,
        SeatLimit = s.SeatLimit,
        StorageLimitBytes = s.StorageLimitBytes,
        NextBillingDateUtc = s.NextBillingDateUtc,
        ExternalSubscriptionId = s.ExternalSubscriptionId,
        CreatedAtUtc = s.CreatedAtUtc
    };
}
