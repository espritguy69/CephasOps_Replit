namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Central helper for tenant-scope and platform-bypass execution. Use instead of manually
/// setting TenantScope / EnterPlatformBypass and restoring in finally. Ensures previous
/// state is always restored (AsyncLocal-safe) and makes tenant vs platform intent explicit.
/// Prefer this for hosted services, event dispatchers, replay, webhooks, and retention.
/// See docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md (section 3.1).
/// </summary>
public static class TenantScopeExecutor
{
    /// <summary>
    /// Run work under the given tenant scope. Use for tenant-owned execution when the company is known.
    /// Restores previous TenantScope.CurrentTenantId in finally (even on exception).
    /// </summary>
    /// <param name="companyId">Non-empty company ID. Do not pass Guid.Empty; use <see cref="RunWithTenantScopeOrBypassAsync"/> only when the flow is designed to accept null/empty.</param>
    /// <param name="work">Work to run under tenant scope. Receives the cancellation token.</param>
    /// <param name="cancellationToken">Cancellation token passed to <paramref name="work"/>.</param>
    /// <exception cref="ArgumentException">When <paramref name="companyId"/> is Guid.Empty. Ensures tenant-owned work is never accidentally run without a valid tenant.</exception>
    public static async Task RunWithTenantScopeAsync(Guid companyId, Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            ThrowEmptyCompanyIdForTenantScope(nameof(companyId));

        var previous = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            await work(cancellationToken);
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    /// <summary>
    /// Run work under the given tenant scope. Use for tenant-owned execution when the company is known.
    /// Restores previous TenantScope.CurrentTenantId in finally (even on exception).
    /// </summary>
    /// <param name="companyId">Non-empty company ID. Do not pass Guid.Empty; use <see cref="RunWithTenantScopeOrBypassAsync"/> only when the flow is designed to accept null/empty.</param>
    /// <param name="work">Work to run under tenant scope. Receives the cancellation token.</param>
    /// <param name="cancellationToken">Cancellation token passed to <paramref name="work"/>.</param>
    /// <exception cref="ArgumentException">When <paramref name="companyId"/> is Guid.Empty.</exception>
    public static async Task<T> RunWithTenantScopeAsync<T>(Guid companyId, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            ThrowEmptyCompanyIdForTenantScope(nameof(companyId));

        var previous = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            return await work(cancellationToken);
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    /// <summary>
    /// Run work under platform bypass (tenant checks disabled). Use only for intentional platform-wide operations
    /// (e.g. retention across tenants, reap, scheduler enumeration). Do not use for tenant-owned work.
    /// Exits bypass in finally (even on exception).
    /// </summary>
    public static async Task RunWithPlatformBypassAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
    {
        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            await work(cancellationToken);
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }
    }

    /// <summary>
    /// Run work under platform bypass (tenant checks disabled). Use only for intentional platform-wide operations.
    /// Exits bypass in finally (even on exception).
    /// </summary>
    public static async Task<T> RunWithPlatformBypassAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default)
    {
        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            return await work(cancellationToken);
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }
    }

    /// <summary>
    /// When <paramref name="companyIdOrNullForPlatformBypass"/> has a non-empty value, run under tenant scope; otherwise run under platform bypass.
    /// EXCEPTIONAL: Use only when the operation is designed to run either as tenant or as platform (e.g. event dispatch, webhook request, retention with optional company filter).
    /// Restores previous state in finally (even on exception). For tenant-owned work with a known company, prefer <see cref="RunWithTenantScopeAsync"/> so invalid (empty) IDs fail fast.
    /// </summary>
    /// <param name="companyIdOrNullForPlatformBypass">When set and non-empty: tenant scope. When null or Guid.Empty: platform bypass. Call site should make this intent explicit.</param>
    /// <param name="work">Work to run under tenant scope or platform bypass.</param>
    /// <param name="cancellationToken">Cancellation token passed to <paramref name="work"/>.</param>
    public static async Task RunWithTenantScopeOrBypassAsync(Guid? companyIdOrNullForPlatformBypass, Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
    {
        var useBypass = !companyIdOrNullForPlatformBypass.HasValue || companyIdOrNullForPlatformBypass.Value == Guid.Empty;
        if (useBypass)
        {
            TenantSafetyGuard.EnterPlatformBypass();
            try
            {
                await work(cancellationToken);
            }
            finally
            {
                TenantSafetyGuard.ExitPlatformBypass();
            }
        }
        else
        {
            var previous = TenantScope.CurrentTenantId;
            TenantScope.CurrentTenantId = companyIdOrNullForPlatformBypass!.Value;
            try
            {
                await work(cancellationToken);
            }
            finally
            {
                TenantScope.CurrentTenantId = previous;
            }
        }
    }

    /// <summary>
    /// When <paramref name="companyIdOrNullForPlatformBypass"/> has a non-empty value, run under tenant scope; otherwise run under platform bypass.
    /// EXCEPTIONAL: Use only when the operation is designed to run either as tenant or as platform. Restores previous state in finally.
    /// </summary>
    /// <param name="companyIdOrNullForPlatformBypass">When set and non-empty: tenant scope. When null or Guid.Empty: platform bypass.</param>
    /// <param name="work">Work to run under tenant scope or platform bypass.</param>
    /// <param name="cancellationToken">Cancellation token passed to <paramref name="work"/>.</param>
    public static async Task<T> RunWithTenantScopeOrBypassAsync<T>(Guid? companyIdOrNullForPlatformBypass, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default)
    {
        var useBypass = !companyIdOrNullForPlatformBypass.HasValue || companyIdOrNullForPlatformBypass.Value == Guid.Empty;
        if (useBypass)
        {
            TenantSafetyGuard.EnterPlatformBypass();
            try
            {
                return await work(cancellationToken);
            }
            finally
            {
                TenantSafetyGuard.ExitPlatformBypass();
            }
        }
        else
        {
            var previous = TenantScope.CurrentTenantId;
            TenantScope.CurrentTenantId = companyIdOrNullForPlatformBypass!.Value;
            try
            {
                return await work(cancellationToken);
            }
            finally
            {
                TenantScope.CurrentTenantId = previous;
            }
        }
    }

    private static void ThrowEmptyCompanyIdForTenantScope(string paramName)
    {
        throw new ArgumentException(
            "A non-empty company ID is required for tenant-scoped execution. Use RunWithTenantScopeOrBypassAsync only when the flow is designed to accept null/empty (e.g. event entry, webhook).",
            paramName);
    }
}
