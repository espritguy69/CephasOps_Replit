using CephasOps.Application.Billing.Abstractions;
using CephasOps.Application.Billing.Subscription.DTOs;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Billing.Subscription.Services;

public class TenantSubscriptionService : ITenantSubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentProvider _paymentProvider;

    public TenantSubscriptionService(ApplicationDbContext context, IPaymentProvider paymentProvider)
    {
        _context = context;
        _paymentProvider = paymentProvider;
    }

    public async Task<List<TenantSubscriptionDto>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var list = await _context.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);
        var planIds = list.Select(s => s.BillingPlanId).Distinct().ToList();
        var plans = await _context.BillingPlans.AsNoTracking().Where(p => planIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        return list.Select(s => Map(s, plans.GetValueOrDefault(s.BillingPlanId))).ToList();
    }

    public async Task<TenantSubscriptionDto?> GetActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var s = await _context.TenantSubscriptions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == TenantSubscriptionStatus.Active)
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (s == null) return null;
        var plan = await _context.BillingPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == s.BillingPlanId, cancellationToken);
        return Map(s, plan);
    }

    public async Task<TenantSubscriptionDto?> SubscribeAsync(Guid tenantId, string planSlug, CancellationToken cancellationToken = default)
    {
        var plan = await _context.BillingPlans.FirstOrDefaultAsync(p => p.Slug == planSlug && p.IsActive, cancellationToken);
        if (plan == null) return null;

        var existing = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && s.Status == TenantSubscriptionStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        string? externalId = null;
        if (existing != null)
        {
            var result = await _paymentProvider.CreateOrUpdateSubscriptionAsync(tenantId, planSlug, existing.ExternalSubscriptionId, cancellationToken);
            if (!result.Success) return null;
            externalId = result.ExternalId;
            existing.Status = TenantSubscriptionStatus.Cancelled;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        var resultNew = await _paymentProvider.CreateOrUpdateSubscriptionAsync(tenantId, planSlug, externalId, cancellationToken);
        if (!resultNew.Success) return null;

        var periodEnd = plan.BillingCycle == BillingCycle.Monthly
            ? DateTime.UtcNow.AddMonths(1)
            : DateTime.UtcNow.AddYears(1);

        var sub = new TenantSubscription
        {
            TenantId = tenantId,
            BillingPlanId = plan.Id,
            Status = TenantSubscriptionStatus.Active,
            StartedAtUtc = DateTime.UtcNow,
            CurrentPeriodEndUtc = periodEnd,
            BillingCycle = plan.BillingCycle,
            NextBillingDateUtc = periodEnd,
            ExternalSubscriptionId = resultNew.ExternalId
        };
        _context.TenantSubscriptions.Add(sub);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(sub, plan);
    }

    public async Task<bool> CancelAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var sub = await _context.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == TenantSubscriptionStatus.Active, cancellationToken);
        if (sub == null) return false;
        if (!string.IsNullOrEmpty(sub.ExternalSubscriptionId))
            await _paymentProvider.CancelSubscriptionAsync(sub.ExternalSubscriptionId, cancellationToken);
        sub.Status = TenantSubscriptionStatus.Cancelled;
        sub.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TenantSubscriptionDto Map(TenantSubscription s, BillingPlan? plan) => new()
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
