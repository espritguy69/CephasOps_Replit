using CephasOps.Application.Common.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Services;

/// <summary>
/// Resolves current tenant from effective request company (Phase 11). Uses ITenantProvider so SuperAdmin X-Company-Id and guard/subscription stay consistent.
/// </summary>
public class TenantContextService : ITenantContext
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ApplicationDbContext _db;
    private (Guid? TenantId, string? Slug)? _resolved;

    public TenantContextService(ITenantProvider tenantProvider, ApplicationDbContext db)
    {
        _tenantProvider = tenantProvider;
        _db = db;
    }

    public Guid? TenantId
    {
        get
        {
            EnsureResolved();
            return _resolved?.TenantId;
        }
    }

    public string? TenantSlug
    {
        get
        {
            EnsureResolved();
            return _resolved?.Slug;
        }
    }

    public bool IsTenantResolved => TenantId.HasValue;

    private void EnsureResolved()
    {
        if (_resolved.HasValue) return;

        var companyId = _tenantProvider.CurrentTenantId;
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
        {
            _resolved = (null, null);
            return;
        }

        var company = _db.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId.Value)
            .Select(c => new { c.TenantId })
            .FirstOrDefault();

        if (company?.TenantId == null)
        {
            _resolved = (null, null);
            return;
        }

        var tenant = _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == company.TenantId)
            .Select(t => new { t.Id, t.Slug })
            .FirstOrDefault();

        _resolved = tenant != null ? (tenant.Id, tenant.Slug) : (company.TenantId, null);
    }
}
