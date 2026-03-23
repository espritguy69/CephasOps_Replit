using System.Diagnostics.Metrics;

namespace CephasOps.Infrastructure.Metrics;

/// <summary>
/// Lightweight metrics for tenant-safety observability. Exported via OpenTelemetry when AddMeter("CephasOps.TenantSafety") is configured.
/// </summary>
public static class TenantSafetyMetrics
{
    public const string MeterName = "CephasOps.TenantSafety";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> GuardViolations = Meter.CreateCounter<long>(
        "cephasops.tenant_safety.guard_violations",
        description: "Number of tenant guard violations (SaveChanges or AssertTenantContext).");
    private static readonly Counter<long> MissingTenantContextAttempts = Meter.CreateCounter<long>(
        "cephasops.tenant_safety.missing_tenant_context",
        description: "Number of attempts where tenant context was required but missing.");
    private static readonly Counter<long> PlatformBypassEntered = Meter.CreateCounter<long>(
        "cephasops.tenant_safety.platform_bypass_entered",
        description: "Number of times platform bypass was entered (each EnterPlatformBypass call).");

    /// <summary>Record a guard violation (call just before the guard throws).</summary>
    public static void RecordGuardViolation(string guardName, string operation)
    {
        GuardViolations.Add(1, new KeyValuePair<string, object?>("guard", guardName), new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>Record a missing-tenant-context attempt (e.g. AssertTenantContext about to throw).</summary>
    public static void RecordMissingTenantContext()
    {
        MissingTenantContextAttempts.Add(1);
    }

    /// <summary>Record that platform bypass was entered (each EnterPlatformBypass call).</summary>
    public static void RecordPlatformBypassEntered()
    {
        PlatformBypassEntered.Add(1);
    }
}
