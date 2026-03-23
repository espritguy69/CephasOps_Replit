using CephasOps.Infrastructure;
using CephasOps.Infrastructure.Metrics;
using CephasOps.Domain.Audit.Entities;
using CephasOps.Domain.Common;
using CephasOps.Domain.Integration.Entities;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Domain.Users.Entities;
using CephasOps.Domain.Workflow.Entities;

namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Final defensive guard against accidental cross-tenant data access.
/// Use for: (1) Assert tenant context before high-risk paths (e.g. IgnoreQueryFilters),
/// (2) Platform-wide operations must call EnterPlatformBypass/ExitPlatformBypass so SaveChanges does not throw.
/// Does not replace ITenantProvider, TenantGuardMiddleware, RequireCompanyId(), TenantScope, or EF global filters.
/// </summary>
public static class TenantSafetyGuard
{
    private static readonly AsyncLocal<int> _platformBypassCount = new();

    /// <summary>
    /// Whether a platform bypass is active (retention, seeding, design-time). When true, SaveChanges tenant validation is skipped and AssertTenantContext does not throw.
    /// </summary>
    public static bool IsPlatformBypassActive => _platformBypassCount.Value > 0;

    /// <summary>
    /// Enter a platform-wide operation scope. Call before retention cleanup, seeding, or when creating DbContext at design-time.
    /// Must be paired with ExitPlatformBypass in a finally block.
    /// </summary>
    public static void EnterPlatformBypass()
    {
        _platformBypassCount.Value = _platformBypassCount.Value + 1;
        TenantSafetyMetrics.RecordPlatformBypassEntered();
    }

    /// <summary>
    /// Exit a platform-wide operation scope. Call in finally after EnterPlatformBypass.
    /// </summary>
    public static void ExitPlatformBypass()
    {
        var current = _platformBypassCount.Value;
        if (current > 0)
            _platformBypassCount.Value = current - 1;
    }

    /// <summary>
    /// Throws if tenant context is missing and platform bypass is not active.
    /// Call before using IgnoreQueryFilters on tenant-scoped entities or other high-risk paths.
    /// </summary>
    /// <exception cref="InvalidOperationException">When TenantScope.CurrentTenantId is null or Guid.Empty and no platform bypass is active.</exception>
    public static void AssertTenantContext()
    {
        if (IsPlatformBypassActive)
            return;
        var tenantId = TenantScope.CurrentTenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            return;
        var message = "Tenant context is required. TenantScope.CurrentTenantId must be set or use EnterPlatformBypass/ExitPlatformBypass.";
        TenantSafetyMetrics.RecordMissingTenantContext();
        PlatformGuardLogger.LogViolation("TenantSafetyGuard", "AssertTenantContext", message);
        throw new InvalidOperationException(
            "TenantSafetyGuard: Tenant context is required for this operation. " +
            "TenantScope.CurrentTenantId must be set (e.g. by API middleware or job worker). " +
            "For platform-wide operations, use TenantSafetyGuard.EnterPlatformBypass() and ExitPlatformBypass().");
    }

    /// <summary>
    /// Returns true if the entity type is tenant-scoped (has CompanyId and is enforced by global query filters).
    /// Used by SaveChanges validation.
    /// </summary>
    internal static bool IsTenantScopedEntityType(Type entityType)
    {
        if (entityType == null) return false;
        if (typeof(CompanyScopedEntity).IsAssignableFrom(entityType)) return true;
        if (entityType == typeof(User)) return true;
        if (entityType == typeof(BackgroundJob)) return true;
        if (entityType == typeof(JobExecution)) return true;
        if (entityType == typeof(OrderPayoutSnapshot)) return true;
        if (entityType == typeof(InboundWebhookReceipt)) return true;
        if (entityType == typeof(Warehouse)) return true;
        if (entityType == typeof(Bin)) return true;
        if (entityType == typeof(AuditLog)) return true;
        if (entityType == typeof(JobRun)) return true;
        return false;
    }
}
