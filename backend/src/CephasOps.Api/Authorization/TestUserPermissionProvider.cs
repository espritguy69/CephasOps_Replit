using System.Security.Claims;
using CephasOps.Application.Authorization;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Api.Authorization;

/// <summary>
/// Used only when Environment is Testing. Returns permissions based on current user's roles
/// so integration tests can use role headers (e.g. Admin) without seeding UserRole in DB.
/// Matches seed: Admin gets admin.*, payout.*, rates.*, payroll.*, orders.*, reports.*, inventory.*, jobs.*, settings.*.
/// </summary>
public class TestUserPermissionProvider : IUserPermissionProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestUserPermissionProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<IReadOnlyList<string>> GetPermissionNamesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var roles = user?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            var adminSeed = PermissionCatalog.All
                .Where(p => p.StartsWith("admin.", StringComparison.Ordinal)
                    || p.StartsWith("payout.", StringComparison.Ordinal)
                    || p.StartsWith("rates.", StringComparison.Ordinal)
                    || p.StartsWith("payroll.", StringComparison.Ordinal)
                    || p.StartsWith("orders.", StringComparison.Ordinal)
                    || p.StartsWith("reports.", StringComparison.Ordinal)
                    || p.StartsWith("inventory.", StringComparison.Ordinal)
                    || p.StartsWith("jobs.", StringComparison.Ordinal)
                    || p.StartsWith("settings.", StringComparison.Ordinal))
                .ToList();
            return Task.FromResult<IReadOnlyList<string>>(adminSeed);
        }

        // Member in tests: grant view+edit for orders/inventory, reports view+export so department-scoped and report export integration tests pass.
        if (roles.Contains("Member", StringComparer.OrdinalIgnoreCase))
        {
            var memberPerms = new List<string>
            {
                PermissionCatalog.OrdersView,
                PermissionCatalog.OrdersEdit,
                PermissionCatalog.ReportsView,
                PermissionCatalog.ReportsExport,
                PermissionCatalog.InventoryView,
                PermissionCatalog.InventoryEdit
            };
            return Task.FromResult<IReadOnlyList<string>>(memberPerms);
        }

        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
}
