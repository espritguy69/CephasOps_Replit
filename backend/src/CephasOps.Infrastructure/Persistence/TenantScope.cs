namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Request-scoped company/tenant id for global query filters. Set by API middleware from ITenantProvider.
/// </summary>
public static class TenantScope
{
    private static readonly AsyncLocal<Guid?> _currentTenantId = new();

    /// <summary>Current company (tenant) id for this async context. Null when no tenant (e.g. migrations).</summary>
    public static Guid? CurrentTenantId
    {
        get => _currentTenantId.Value;
        set => _currentTenantId.Value = value;
    }
}
