using CephasOps.Infrastructure.Persistence;

namespace CephasOps.Api.Tests.Infrastructure;

/// <summary>
/// TenantSafetyGuard requires tenant scope (or platform bypass) for SaveChanges on tenant-scoped entities.
/// Integration tests that purge then seed the in-memory DB must use bypass for cross-tenant deletes,
/// then <see cref="TenantScopeExecutor.RunWithTenantScopeAsync"/> for inserts tied to the seed company id.
/// </summary>
public static class IntegrationTestDbSeeder
{
    /// <summary>
    /// Runs <paramref name="purge"/> under platform bypass, then <paramref name="seed"/> under tenant scope for <paramref name="companyId"/>.
    /// </summary>
    public static async Task PurgeThenSeedAsync(
        Guid companyId,
        Func<CancellationToken, Task> purge,
        Func<CancellationToken, Task> seed,
        CancellationToken cancellationToken = default)
    {
        await TenantScopeExecutor.RunWithPlatformBypassAsync(purge, cancellationToken);
        await TenantScopeExecutor.RunWithTenantScopeAsync(companyId, seed, cancellationToken);
    }
}
