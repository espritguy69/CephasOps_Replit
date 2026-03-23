using CephasOps.Application.Common.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CephasOps.Api.Production;

/// <summary>
/// Validates that critical tenant-safety services are registered at startup. Fails fast if missing.
/// </summary>
public static class TenantSafetyStartupValidator
{
    /// <summary>
    /// Ensures ITenantProvider and ApplicationDbContext can be resolved. Throws if any are missing.
    /// </summary>
    public static void Validate(IServiceProvider serviceProvider)
    {
        var errors = new List<string>();

        try
        {
            _ = serviceProvider.GetRequiredService<ITenantProvider>();
        }
        catch (InvalidOperationException)
        {
            errors.Add("ITenantProvider is not registered. Required for tenant context in API requests.");
        }

        try
        {
            _ = serviceProvider.GetRequiredService<ApplicationDbContext>();
        }
        catch (InvalidOperationException)
        {
            errors.Add("ApplicationDbContext is not registered. Required for SaveChanges tenant validation.");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("Tenant safety startup validation failed: " + string.Join(" ", errors));
    }
}
