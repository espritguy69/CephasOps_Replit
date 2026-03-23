using CephasOps.Application.Audit.DTOs;
using CephasOps.Application.Audit.Services;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Auth.Services;

/// <summary>
/// Scans recent Auth audit events and detects rule violations. No new table; computed on demand. v1.4 Phase 2.
/// </summary>
public class SecurityAnomalyDetectionService : ISecurityAnomalyDetectionService
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SecurityAnomalyDetectionService> _logger;

    public SecurityAnomalyDetectionService(
        IAuditLogService auditLogService,
        ILogger<SecurityAnomalyDetectionService> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<SecurityAlertDto>> DetectAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        Guid? userId = null,
        string? alertType = null,
        CancellationToken cancellationToken = default)
    {
        var windowStart = dateFrom ?? DateTime.UtcNow.AddHours(-24);
        var windowEnd = dateTo ?? DateTime.UtcNow;

        var events = await _auditLogService.GetAuthEventsForDetectionAsync(
            windowStart, windowEnd, userId, 5000, cancellationToken);

        var alerts = new List<SecurityAlertDto>();

        if (alertType == null || alertType == SecurityAlertTypes.ExcessiveLoginFailures)
            alerts.AddRange(DetectExcessiveLoginFailures(events));

        if (alertType == null || alertType == SecurityAlertTypes.PasswordResetAbuse)
            alerts.AddRange(DetectPasswordResetAbuse(events));

        if (alertType == null || alertType == SecurityAlertTypes.MultipleIpLogin)
            alerts.AddRange(DetectMultipleIpLogin(events));

        return alerts.OrderByDescending(a => a.DetectedAtUtc).ToList();
    }

    private static List<SecurityAlertDto> DetectExcessiveLoginFailures(List<SecurityActivityEntryDto> events)
    {
        var failures = events
            .Where(e => e.Action == AuthEventTypes.LoginFailed)
            .OrderBy(e => e.Timestamp)
            .ToList();

        var windowMinutes = TimeSpan.FromMinutes(SecurityDetectionRules.ExcessiveLoginFailuresWindowMinutes);
        var threshold = SecurityDetectionRules.ExcessiveLoginFailuresThreshold;
        var byUser = failures.GroupBy(e => e.UserId ?? Guid.Empty);

        var alerts = new List<SecurityAlertDto>();
        foreach (var userGroup in byUser.Where(g => g.Key != Guid.Empty))
        {
            var list = userGroup.ToList();
            DateTime? detectedAt = null;
            int maxCount = 0;
            var ips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < list.Count; i++)
            {
                var windowEnd = list[i].Timestamp;
                var windowStart = windowEnd - windowMinutes;
                var inWindow = list.Where(e => e.Timestamp >= windowStart && e.Timestamp <= windowEnd).ToList();
                var count = inWindow.Count;
                if (count > threshold && count > maxCount)
                {
                    maxCount = count;
                    detectedAt = windowEnd;
                    ips.Clear();
                    foreach (var e in inWindow.Where(e => !string.IsNullOrEmpty(e.IpAddress)))
                        ips.Add(e.IpAddress!);
                }
            }

            if (detectedAt.HasValue && maxCount > threshold)
            {
                var first = list.First();
                alerts.Add(new SecurityAlertDto
                {
                    DetectedAtUtc = detectedAt.Value,
                    UserId = first.UserId,
                    UserEmail = first.UserEmail,
                    AlertType = SecurityAlertTypes.ExcessiveLoginFailures,
                    Description = $"Excessive login failures detected for user {first.UserEmail ?? first.UserId?.ToString() ?? "unknown"}.",
                    IpSummary = ips.Count > 0 ? string.Join(", ", ips.Take(10)) : null,
                    EventCount = maxCount,
                    WindowMinutes = SecurityDetectionRules.ExcessiveLoginFailuresWindowMinutes
                });
            }
        }

        return alerts;
    }

    private static List<SecurityAlertDto> DetectPasswordResetAbuse(List<SecurityActivityEntryDto> events)
    {
        var resets = events
            .Where(e => e.Action == AuthEventTypes.PasswordResetRequested)
            .OrderBy(e => e.Timestamp)
            .ToList();

        var windowMinutes = TimeSpan.FromMinutes(SecurityDetectionRules.PasswordResetAbuseWindowMinutes);
        var threshold = SecurityDetectionRules.PasswordResetAbuseThreshold;
        var byUser = resets.GroupBy(e => e.UserId ?? Guid.Empty);

        var alerts = new List<SecurityAlertDto>();
        foreach (var userGroup in byUser.Where(g => g.Key != Guid.Empty))
        {
            var list = userGroup.ToList();
            DateTime? detectedAt = null;
            int maxCount = 0;
            var ips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < list.Count; i++)
            {
                var windowEnd = list[i].Timestamp;
                var windowStart = windowEnd - windowMinutes;
                var inWindow = list.Where(e => e.Timestamp >= windowStart && e.Timestamp <= windowEnd).ToList();
                var count = inWindow.Count;
                if (count > threshold && count > maxCount)
                {
                    maxCount = count;
                    detectedAt = windowEnd;
                    ips.Clear();
                    foreach (var e in inWindow.Where(e => !string.IsNullOrEmpty(e.IpAddress)))
                        ips.Add(e.IpAddress!);
                }
            }

            if (detectedAt.HasValue && maxCount > threshold)
            {
                var first = list.First();
                alerts.Add(new SecurityAlertDto
                {
                    DetectedAtUtc = detectedAt.Value,
                    UserId = first.UserId,
                    UserEmail = first.UserEmail,
                    AlertType = SecurityAlertTypes.PasswordResetAbuse,
                    Description = $"Password reset abuse detected for user {first.UserEmail ?? first.UserId?.ToString() ?? "unknown"}.",
                    IpSummary = ips.Count > 0 ? string.Join(", ", ips.Take(10)) : null,
                    EventCount = maxCount,
                    WindowMinutes = SecurityDetectionRules.PasswordResetAbuseWindowMinutes
                });
            }
        }

        return alerts;
    }

    private static List<SecurityAlertDto> DetectMultipleIpLogin(List<SecurityActivityEntryDto> events)
    {
        var logins = events
            .Where(e => e.Action == AuthEventTypes.LoginSuccess)
            .OrderBy(e => e.Timestamp)
            .ToList();

        var windowMinutes = TimeSpan.FromMinutes(SecurityDetectionRules.MultipleIpLoginWindowMinutes);
        var requiredDistinctIps = SecurityDetectionRules.MultipleIpLoginDistinctIpCount;
        var byUser = logins.GroupBy(e => e.UserId ?? Guid.Empty);

        var alerts = new List<SecurityAlertDto>();
        foreach (var userGroup in byUser.Where(g => g.Key != Guid.Empty))
        {
            var list = userGroup.ToList();
            DateTime? detectedAt = null;
            int maxDistinct = 0;
            var ips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < list.Count; i++)
            {
                var windowEnd = list[i].Timestamp;
                var windowStart = windowEnd - windowMinutes;
                var inWindow = list.Where(e => e.Timestamp >= windowStart && e.Timestamp <= windowEnd).ToList();
                var distinct = inWindow.Where(e => !string.IsNullOrEmpty(e.IpAddress)).Select(e => e.IpAddress!).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                if (distinct >= requiredDistinctIps && distinct > maxDistinct)
                {
                    maxDistinct = distinct;
                    detectedAt = windowEnd;
                    ips.Clear();
                    foreach (var e in inWindow.Where(e => !string.IsNullOrEmpty(e.IpAddress)))
                        ips.Add(e.IpAddress!);
                }
            }

            if (detectedAt.HasValue && maxDistinct >= requiredDistinctIps)
            {
                var first = list.First();
                alerts.Add(new SecurityAlertDto
                {
                    DetectedAtUtc = detectedAt.Value,
                    UserId = first.UserId,
                    UserEmail = first.UserEmail,
                    AlertType = SecurityAlertTypes.MultipleIpLogin,
                    Description = $"Multiple IP login detected for user {first.UserEmail ?? first.UserId?.ToString() ?? "unknown"} (successful logins from {maxDistinct} different IPs).",
                    IpSummary = ips.Count > 0 ? string.Join(", ", ips.Take(10)) : null,
                    EventCount = maxDistinct,
                    WindowMinutes = SecurityDetectionRules.MultipleIpLoginWindowMinutes
                });
            }
        }

        return alerts;
    }
}
