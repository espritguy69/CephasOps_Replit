using CephasOps.Application.Billing.Subscription.DTOs;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Billing.Subscription.Services;

public class BillingPlanService : IBillingPlanService
{
    private readonly ApplicationDbContext _context;

    public BillingPlanService(ApplicationDbContext context) => _context = context;

    public async Task<List<BillingPlanDto>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.BillingPlans.AsNoTracking();
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);
        var list = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<BillingPlanDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var p = await _context.BillingPlans.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
        return p == null ? null : Map(p);
    }

    private static BillingPlanDto Map(BillingPlan p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        BillingCycle = p.BillingCycle,
        Price = p.Price,
        Currency = p.Currency,
        IsActive = p.IsActive,
        CreatedAtUtc = p.CreatedAtUtc
    };
}
