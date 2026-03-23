namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Helper to ensure tenant context exists before executing sensitive operations.
/// Use in background jobs, data import pipelines, batch operations, and maintenance tasks
/// to fail fast when tenant scope is missing instead of silently returning wrong or cross-tenant data.
/// </summary>
/// <remarks>
/// This delegates to <see cref="TenantSafetyGuard.AssertTenantContext"/>.
/// For running work under a specific tenant scope or platform bypass, use
/// <see cref="TenantScopeExecutor.RunWithTenantScopeAsync"/> or
/// <see cref="TenantScopeExecutor.RunWithPlatformBypassAsync"/> instead.
/// </remarks>
public static class TenantScopeGuard
{
    /// <summary>
    /// Asserts that tenant context is present (TenantScope.CurrentTenantId is set and non-empty).
    /// Throws <see cref="InvalidOperationException"/> if context is missing and platform bypass is not active.
    /// Call this before using IgnoreQueryFilters on tenant-scoped entities or before batch/import logic
    /// that writes tenant-owned data.
    /// </summary>
    /// <exception cref="InvalidOperationException">When tenant context is missing and platform bypass is not active.</exception>
    public static void RequireTenantContext()
    {
        TenantSafetyGuard.AssertTenantContext();
    }
}
