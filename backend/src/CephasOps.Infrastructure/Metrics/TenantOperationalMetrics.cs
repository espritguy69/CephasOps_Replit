using System.Diagnostics.Metrics;

namespace CephasOps.Infrastructure.Metrics;

/// <summary>
/// Lightweight per-tenant operational metrics for observability and fairness.
/// Exported via OpenTelemetry when AddMeter("CephasOps.TenantOperations") is configured.
/// </summary>
public static class TenantOperationalMetrics
{
    public const string MeterName = "CephasOps.TenantOperations";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> RequestsTotal = Meter.CreateCounter<long>(
        "cephasops.tenant_operations.requests_total",
        description: "API requests per tenant (tag: tenant_id, success).");
    private static readonly Counter<long> JobsExecutedTotal = Meter.CreateCounter<long>(
        "cephasops.tenant_operations.jobs_executed_total",
        description: "Background jobs executed per tenant.");
    private static readonly Counter<long> JobFailuresTotal = Meter.CreateCounter<long>(
        "cephasops.tenant_operations.job_failures_total",
        description: "Background job failures per tenant.");
    private static readonly Counter<long> NotificationsSentTotal = Meter.CreateCounter<long>(
        "cephasops.tenant_operations.notifications_sent_total",
        description: "Notification dispatches sent per tenant (tag: tenant_id, success).");
    private static readonly Counter<long> IntegrationDeliveriesTotal = Meter.CreateCounter<long>(
        "cephasops.tenant_operations.integration_deliveries_total",
        description: "Outbound integration deliveries per tenant (tag: tenant_id, success).");

    private static string TenantTag(Guid? tenantId) => tenantId.HasValue && tenantId.Value != Guid.Empty ? tenantId.Value.ToString("N") : "platform";

    /// <summary>Record an API request for a tenant (call from request pipeline after response).</summary>
    public static void RecordRequest(Guid? tenantId, bool success)
    {
        var tenant = TenantTag(tenantId);
        RequestsTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenant), new KeyValuePair<string, object?>("success", success));
    }

    /// <summary>Record a background job execution (success).</summary>
    public static void RecordJobExecuted(Guid? tenantId)
    {
        var tenant = TenantTag(tenantId);
        JobsExecutedTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenant));
    }

    /// <summary>Record a background job failure.</summary>
    public static void RecordJobFailure(Guid? tenantId)
    {
        var tenant = TenantTag(tenantId);
        JobFailuresTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenant));
    }

    /// <summary>Record a notification dispatch (sent or failed).</summary>
    public static void RecordNotificationSent(Guid? tenantId, bool success)
    {
        var tenant = TenantTag(tenantId);
        NotificationsSentTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenant), new KeyValuePair<string, object?>("success", success));
    }

    /// <summary>Record an outbound integration delivery attempt.</summary>
    public static void RecordIntegrationDelivery(Guid? tenantId, bool success)
    {
        var tenant = TenantTag(tenantId);
        IntegrationDeliveriesTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenant), new KeyValuePair<string, object?>("success", success));
    }
}
