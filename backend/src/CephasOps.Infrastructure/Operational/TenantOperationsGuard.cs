using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Infrastructure.Operational;

/// <summary>
/// Detection-only guard: records operational events per tenant and logs warnings when thresholds are exceeded.
/// Does not block operations. Used for alerting signals (job failure spike, retry storm, notification/request errors).
/// </summary>
public interface ITenantOperationsGuard
{
    void RecordJobFailure(Guid? tenantId);
    void RecordJobRetry(Guid? tenantId);
    void RecordNotificationFailure(Guid? tenantId);
    void RecordRequestError(Guid? tenantId);
}

/// <summary>
/// In-memory sliding-window implementation. Thresholds and window are configurable.
/// </summary>
public class TenantOperationsGuard : ITenantOperationsGuard
{
    private readonly ILogger<TenantOperationsGuard> _logger;
    private readonly TenantOperationsGuardOptions _options;
    private readonly ConcurrentDictionary<string, List<DateTime>> _jobFailures = new();
    private readonly ConcurrentDictionary<string, List<DateTime>> _jobRetries = new();
    private readonly ConcurrentDictionary<string, List<DateTime>> _notificationFailures = new();
    private readonly ConcurrentDictionary<string, List<DateTime>> _requestErrors = new();

    public TenantOperationsGuard(ILogger<TenantOperationsGuard> logger, IOptions<TenantOperationsGuardOptions>? options = null)
    {
        _logger = logger;
        _options = options?.Value ?? new TenantOperationsGuardOptions();
    }

    public void RecordJobFailure(Guid? tenantId)
    {
        RecordAndCheck("job_failure", _jobFailures, tenantId, _options.JobFailureThreshold, _options.WindowMinutes,
            (tid, count) => _logger.LogWarning("TenantOperationsGuard: Tenant job failure spike detected. TenantId={TenantId}, FailureCount={Count} in last {Window} minutes", tid, count, _options.WindowMinutes));
    }

    public void RecordJobRetry(Guid? tenantId)
    {
        RecordAndCheck("job_retry", _jobRetries, tenantId, _options.JobRetryThreshold, _options.WindowMinutes,
            (tid, count) => _logger.LogWarning("TenantOperationsGuard: Tenant job retry threshold exceeded. TenantId={TenantId}, RetryCount={Count} in last {Window} minutes", tid, count, _options.WindowMinutes));
    }

    public void RecordNotificationFailure(Guid? tenantId)
    {
        RecordAndCheck("notification_failure", _notificationFailures, tenantId, _options.NotificationFailureThreshold, _options.WindowMinutes,
            (tid, count) => _logger.LogWarning("TenantOperationsGuard: Tenant notification failures detected. TenantId={TenantId}, FailureCount={Count} in last {Window} minutes", tid, count, _options.WindowMinutes));
    }

    public void RecordRequestError(Guid? tenantId)
    {
        RecordAndCheck("request_error", _requestErrors, tenantId, _options.RequestErrorThreshold, _options.WindowMinutes,
            (tid, count) => _logger.LogWarning("TenantOperationsGuard: Tenant request error rate spike. TenantId={TenantId}, ErrorCount={Count} in last {Window} minutes", tid, count, _options.WindowMinutes));
    }

    private void RecordAndCheck(string keyPrefix, ConcurrentDictionary<string, List<DateTime>> store, Guid? tenantId, int threshold, int windowMinutes, Action<string, int> onThresholdExceeded)
    {
        if (threshold <= 0) return;
        var key = Key(tenantId);
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-windowMinutes);

        var list = store.AddOrUpdate(key, _ => new List<DateTime> { now }, (_, existing) =>
        {
            lock (existing)
            {
                for (var i = existing.Count - 1; i >= 0; i--)
                    if (existing[i] < windowStart) existing.RemoveAt(i);
                existing.Add(now);
                return existing;
            }
        });

        int count;
        bool crossedThreshold;
        lock (list)
        {
            count = list.Count;
            crossedThreshold = count >= threshold && (count - 1) < threshold;
        }

        if (crossedThreshold)
            onThresholdExceeded(tenantId.HasValue && tenantId.Value != Guid.Empty ? tenantId.Value.ToString("N") : "platform", count);
    }

    private static string Key(Guid? tenantId) => tenantId.HasValue && tenantId.Value != Guid.Empty ? tenantId.Value.ToString("N") : "platform";
}

/// <summary>Options for TenantOperationsGuard thresholds and window.</summary>
public class TenantOperationsGuardOptions
{
    public const string SectionName = "TenantOperations:Guard";

    public int WindowMinutes { get; set; } = 5;
    public int JobFailureThreshold { get; set; } = 10;
    public int JobRetryThreshold { get; set; } = 20;
    public int NotificationFailureThreshold { get; set; } = 5;
    public int RequestErrorThreshold { get; set; } = 50;
}
