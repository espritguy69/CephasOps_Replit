using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Billing.Subscription.Services;

/// <summary>Enforces subscription validity: expired, trial ended, seat/storage limits.</summary>
public class SubscriptionEnforcementService : ISubscriptionEnforcementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubscriptionEnforcementService> _logger;

    public SubscriptionEnforcementService(ApplicationDbContext context, ILogger<SubscriptionEnforcementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> ValidateTenantSubscriptionAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            return null;

        var sub = await _context.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId.Value)
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (sub == null)
            return "No subscription";

        if (sub.Status == TenantSubscriptionStatus.Cancelled)
            return "Subscription cancelled";

        if (sub.Status == TenantSubscriptionStatus.PastDue)
            return "Subscription past due";

        var now = DateTime.UtcNow;
        if (sub.TrialEndsAtUtc.HasValue && sub.TrialEndsAtUtc.Value < now)
            return "Trial expired";

        if (sub.CurrentPeriodEndUtc.HasValue && sub.CurrentPeriodEndUtc.Value < now)
            return "Subscription expired";

        return null;
    }

    public async Task<bool> IsWithinSeatLimitAsync(Guid tenantId, int currentUserCount, CancellationToken cancellationToken = default)
    {
        var sub = await GetActiveSubscriptionAsync(tenantId, cancellationToken);
        if (sub?.SeatLimit == null) return true;
        return currentUserCount <= sub.SeatLimit.Value;
    }

    public async Task<bool> IsWithinStorageLimitAsync(Guid tenantId, long currentStorageBytes, CancellationToken cancellationToken = default)
    {
        var sub = await GetActiveSubscriptionAsync(tenantId, cancellationToken);
        if (sub?.StorageLimitBytes == null) return true;
        return currentStorageBytes <= sub.StorageLimitBytes.Value;
    }

    private async Task<TenantSubscription?> GetActiveSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _context.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && (s.Status == TenantSubscriptionStatus.Active || s.Status == TenantSubscriptionStatus.Trialing))
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
