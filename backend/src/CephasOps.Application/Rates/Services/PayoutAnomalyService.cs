using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CephasOps.Application.Common;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Read-only anomaly detection over payout snapshots and P&amp;L data. No payout or payroll logic changed.
/// </summary>
public class PayoutAnomalyService : IPayoutAnomalyService
{
    private sealed class SnapshotRow
    {
        public OrderPayoutSnapshot Snapshot { get; init; } = null!;
        public Guid? OrderCompanyId { get; init; }
        public Guid OrderTypeId { get; init; }
        public Guid? OrderCategoryId { get; init; }
        public Guid? InstallationMethodId { get; init; }
        public Guid? DepartmentId { get; init; }
    }

    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayoutAnomalyService> _logger;

    public PayoutAnomalyService(ApplicationDbContext context, ILogger<PayoutAnomalyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PayoutAnomalyDetectionSummaryDto> GetAnomalySummaryAsync(PayoutAnomalyFilterDto filter, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("GetAnomalySummary");
        var list = await RunAllRulesAsync(filter, cancellationToken);
        return AggregateSummary(list);
    }

    public async Task<PayoutAnomalyListResultDto> GetAnomaliesAsync(PayoutAnomalyFilterDto filter, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("GetAnomalies");
        var list = await RunAllRulesAsync(filter, cancellationToken);
        var filtered = ApplyListFilters(list, filter);
        var total = filtered.Count;
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);
        var items = filtered
            .OrderByDescending(a => a.DetectedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        items = await MergeReviewInfoAsync(items, cancellationToken);
        return new PayoutAnomalyListResultDto { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    private async Task<List<PayoutAnomalyDto>> MergeReviewInfoAsync(List<PayoutAnomalyDto> items, CancellationToken ct)
    {
        if (items.Count == 0) return items;
        var fingerprints = items.Select(a => a.Id).Distinct().ToList();
        var reviews = await _context.PayoutAnomalyReviews
            .AsNoTracking()
            .Where(r => fingerprints.Contains(r.AnomalyFingerprintId))
            .ToListAsync(ct);
        var userIds = reviews.Where(r => r.AssignedToUserId.HasValue).Select(r => r.AssignedToUserId!.Value).Distinct().ToList();
        var userMap = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name, u.Email })
                .ToDictionaryAsync(x => x.Id, x => x.Name ?? x.Email ?? "", ct);
        var reviewMap = reviews.ToDictionary(r => r.AnomalyFingerprintId);

        // Latest successful alert per fingerprint (for Alerted / LastAlertedAt)
        var alertLatest = await _context.PayoutAnomalyAlerts
            .AsNoTracking()
            .Where(x => x.Status == "Sent" && fingerprints.Contains(x.AnomalyFingerprintId))
            .GroupBy(x => x.AnomalyFingerprintId)
            .Select(g => new { Fingerprint = g.Key, SentAt = g.Max(x => x.SentAtUtc) })
            .ToDictionaryAsync(x => x.Fingerprint, x => x.SentAt, ct);

        return items.Select(a =>
        {
            var rev = reviewMap.GetValueOrDefault(a.Id);
            var name = rev != null && rev.AssignedToUserId.HasValue && userMap.TryGetValue(rev.AssignedToUserId.Value, out var n) ? n : null;
            var hasAlert = alertLatest.TryGetValue(a.Id, out var lastAlertedAt);
            var lastActionAt = rev != null ? (rev.UpdatedAt ?? rev.CreatedAt) : (DateTime?)null;
            return a with
            {
                ReviewStatus = rev?.Status,
                AssignedToUserId = rev?.AssignedToUserId,
                AssignedToUserName = name,
                Alerted = hasAlert,
                LastAlertedAt = hasAlert ? lastAlertedAt : (DateTime?)null,
                LastActionAt = lastActionAt
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<PayoutAnomalyClusterDto>> GetTopClustersAsync(PayoutAnomalyFilterDto filter, int top = 10, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("GetTopClusters");
        var list = await RunAllRulesAsync(filter, cancellationToken);
        var clusters = new List<PayoutAnomalyClusterDto>();

        var byInstallerCustom = list
            .Where(a => a.AnomalyType == PayoutAnomalyTypes.ExcessiveCustomOverride && a.InstallerId.HasValue && a.CustomOverrideCount.HasValue)
            .GroupBy(a => a.InstallerId!.Value)
            .Select(g => new PayoutAnomalyClusterDto
            {
                ClusterKind = "CustomOverrideByInstaller",
                Label = g.First().InstallerName ?? g.Key.ToString(),
                EntityId = g.Key,
                AnomalyCount = g.Count(),
                ExtraCount = g.First().CustomOverrideCount
            })
            .OrderByDescending(c => c.ExtraCount ?? c.AnomalyCount)
            .Take(top)
            .ToList();
        clusters.AddRange(byInstallerCustom);

        var byContextLegacy = list
            .Where(a => a.AnomalyType == PayoutAnomalyTypes.ExcessiveLegacyFallback && a.LegacyFallbackCount.HasValue)
            .GroupBy(a => a.CompanyId + "|" + (a.OrderTypeId ?? Guid.Empty))
            .Select(g => new PayoutAnomalyClusterDto
            {
                ClusterKind = "LegacyFallbackByContext",
                Label = "Context " + g.Key,
                ContextKey = g.Key,
                AnomalyCount = g.Count(),
                ExtraCount = g.First().LegacyFallbackCount
            })
            .OrderByDescending(c => c.ExtraCount ?? c.AnomalyCount)
            .Take(top)
            .ToList();
        clusters.AddRange(byContextLegacy);

        var byContextNegative = list
            .Where(a => a.AnomalyType == PayoutAnomalyTypes.NegativeMarginCluster && a.NegativeMarginCount.HasValue)
            .GroupBy(a => a.CompanyId + "|" + (a.OrderTypeId ?? Guid.Empty))
            .Select(g => new PayoutAnomalyClusterDto
            {
                ClusterKind = "NegativeMarginByContext",
                Label = "Context " + g.Key,
                ContextKey = g.Key,
                AnomalyCount = g.Count(),
                ExtraCount = g.First().NegativeMarginCount
            })
            .OrderByDescending(c => c.ExtraCount ?? c.AnomalyCount)
            .Take(top)
            .ToList();
        clusters.AddRange(byContextNegative);

        return clusters.OrderByDescending(c => c.AnomalyCount).Take(top * 3).ToList();
    }

    private async Task<List<PayoutAnomalyDto>> RunAllRulesAsync(PayoutAnomalyFilterDto filter, CancellationToken cancellationToken)
    {
        var to = filter.ToDate ?? DateTime.UtcNow.Date;
        var from = filter.FromDate ?? to.AddDays(-PayoutAnomalyThresholds.LookbackDays);
        var cutoff = from;

        var snapshotRows = await _context.OrderPayoutSnapshots
            .AsNoTracking()
            .Where(s => s.CalculatedAt >= cutoff && s.CalculatedAt <= to.AddDays(1))
            .Join(
                _context.Orders.AsNoTracking(),
                s => s.OrderId,
                o => o.Id,
                (s, o) => new SnapshotRow
                {
                    Snapshot = s,
                    OrderCompanyId = o.CompanyId,
                    OrderTypeId = o.OrderTypeId,
                    OrderCategoryId = o.OrderCategoryId,
                    InstallationMethodId = o.InstallationMethodId,
                    DepartmentId = o.DepartmentId
                })
            .ToListAsync(cancellationToken);

        // Cross-tenant mismatch: snapshot and order must belong to the same company.
        var mismatch = snapshotRows.FirstOrDefault(r => r.Snapshot.CompanyId != r.OrderCompanyId);
        if (mismatch != null)
        {
            FinancialIsolationGuard.RequireSameCompany(
                mismatch.Snapshot.CompanyId,
                mismatch.OrderCompanyId,
                "PayoutSnapshot",
                "Order",
                mismatch.Snapshot.Id,
                mismatch.Snapshot.OrderId);
        }

        var installerIds = snapshotRows.Select(x => x.Snapshot.InstallerId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var installerNames = await _context.ServiceInstallers
            .AsNoTracking()
            .Where(si => installerIds.Contains(si.Id))
            .Select(si => new { si.Id, si.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var all = new List<PayoutAnomalyDto>();

        foreach (var row in snapshotRows)
            all.AddRange(RuleHighPayoutVsPeer(row, snapshotRows));
        all.AddRange(RuleZeroPayout(snapshotRows, installerNames));
        all.AddRange(RuleExcessiveCustomOverride(snapshotRows, installerNames));
        all.AddRange(RuleExcessiveLegacyFallback(snapshotRows));
        all.AddRange(RuleRepeatedWarnings(snapshotRows, installerNames));
        all.AddRange(await RuleNegativeMarginClusterAsync(cutoff, to, cancellationToken));
        all.AddRange(RuleInstallerDeviation(snapshotRows, installerNames));

        var withIds = all.Select(a => a with { Id = ComputeFingerprint(a) }).ToList();
        return withIds.DistinctBy(a => (a.AnomalyType, a.OrderId, a.InstallerId, a.DetectedAt, a.Reason)).ToList();
    }

    /// <summary>Deterministic fingerprint for governance (review lookup by anomaly). Max 64 chars for DB.</summary>
    private static string ComputeFingerprint(PayoutAnomalyDto a)
    {
        var payload = $"{a.AnomalyType}|{a.OrderId ?? Guid.Empty}|{a.InstallerId ?? Guid.Empty}|{a.PayoutSnapshotId ?? Guid.Empty}|{a.DetectedAt:O}|{a.Reason}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes.AsSpan(0, 16)).ToLowerInvariant();
    }

    private static List<PayoutAnomalyDto> RuleHighPayoutVsPeer(SnapshotRow row, List<SnapshotRow> snapshotRows)
    {
        var s = row.Snapshot;
        if (s.FinalPayout <= 0) return new List<PayoutAnomalyDto>();

        var key = (s.RateGroupId ?? Guid.Empty, s.ServiceProfileId ?? Guid.Empty, row.OrderCategoryId ?? Guid.Empty, row.InstallationMethodId ?? Guid.Empty, s.PayoutPath ?? "");
        var peerGroup = snapshotRows
            .Where(r =>
            {
                var sn = r.Snapshot;
                var k = (sn.RateGroupId ?? Guid.Empty, sn.ServiceProfileId ?? Guid.Empty, r.OrderCategoryId ?? Guid.Empty, r.InstallationMethodId ?? Guid.Empty, sn.PayoutPath ?? "");
                return k.Equals(key);
            })
            .Select(r => r.Snapshot.FinalPayout)
            .ToList();
        if (peerGroup.Count < 2) return new List<PayoutAnomalyDto>();
        var avg = (double)peerGroup.Average();
        if (avg <= 0) return new List<PayoutAnomalyDto>();
        var multiple = (double)s.FinalPayout / avg;
        if (multiple <= PayoutAnomalyThresholds.HighPayoutMultipleOfPeer) return new List<PayoutAnomalyDto>();

        var severity = multiple >= 3 ? PayoutAnomalySeverity.High : (multiple >= 2.5 ? PayoutAnomalySeverity.Medium : PayoutAnomalySeverity.Low);
        return new List<PayoutAnomalyDto>
        {
            new()
            {
                AnomalyType = PayoutAnomalyTypes.HighPayoutVsPeer,
                Severity = severity,
                OrderId = s.OrderId,
                InstallerId = s.InstallerId,
                PayoutSnapshotId = s.Id,
                PayoutAmount = s.FinalPayout,
                BaselineAmount = (decimal)avg,
                DeviationPercent = (multiple - 1) * 100,
                PayoutPath = s.PayoutPath,
                RateGroupId = s.RateGroupId,
                ServiceProfileId = s.ServiceProfileId,
                DetectedAt = s.CalculatedAt,
                Reason = $"Payout {multiple:F2}x peer average for same rate context."
            }
        };
    }

    private static List<PayoutAnomalyDto> RuleZeroPayout(List<SnapshotRow> snapshotRows, Dictionary<Guid, string> installerNames)
    {
        var list = new List<PayoutAnomalyDto>();
        foreach (var row in snapshotRows)
        {
            var s = row.Snapshot;
            if (s.FinalPayout != 0) continue;
            list.Add(new PayoutAnomalyDto
            {
                AnomalyType = PayoutAnomalyTypes.ZeroPayout,
                Severity = PayoutAnomalySeverity.Medium,
                OrderId = s.OrderId,
                InstallerId = s.InstallerId,
                InstallerName = s.InstallerId.HasValue && installerNames.TryGetValue(s.InstallerId.Value, out var n) ? n : null,
                PayoutSnapshotId = s.Id,
                PayoutAmount = 0,
                PayoutPath = s.PayoutPath,
                DetectedAt = s.CalculatedAt,
                Reason = "Completed order has zero payout."
            });
        }
        return list;
    }

    private static List<PayoutAnomalyDto> RuleExcessiveCustomOverride(List<SnapshotRow> snapshotRows, Dictionary<Guid, string> installerNames)
    {
        var byInstaller = snapshotRows
            .Where(r => r.Snapshot.PayoutPath == "CustomOverride")
            .GroupBy(r => r.Snapshot.InstallerId)
            .Where(g => g.Key.HasValue && g.Count() > PayoutAnomalyThresholds.ExcessiveCustomOverrideCount);
        var list = new List<PayoutAnomalyDto>();
        foreach (var g in byInstaller)
        {
            list.Add(new PayoutAnomalyDto
            {
                AnomalyType = PayoutAnomalyTypes.ExcessiveCustomOverride,
                Severity = PayoutAnomalySeverity.Medium,
                InstallerId = g.Key,
                InstallerName = g.Key.HasValue && installerNames.TryGetValue(g.Key.Value, out var nm) ? nm : null,
                CustomOverrideCount = g.Count(),
                DetectedAt = g.Max(x => x.Snapshot.CalculatedAt),
                Reason = $"Installer has {g.Count()} custom override(s) in lookback period (threshold {PayoutAnomalyThresholds.ExcessiveCustomOverrideCount})."
            });
        }
        return list;
    }

    private static List<PayoutAnomalyDto> RuleExcessiveLegacyFallback(List<SnapshotRow> snapshotRows)
    {
        var byContext = snapshotRows
            .Where(r => r.Snapshot.PayoutPath == "Legacy")
            .GroupBy(r => new { r.OrderCompanyId, r.OrderTypeId })
            .Where(g => g.Count() > PayoutAnomalyThresholds.ExcessiveLegacyFallbackCount);
        var list = new List<PayoutAnomalyDto>();
        foreach (var g in byContext)
        {
            list.Add(new PayoutAnomalyDto
            {
                AnomalyType = PayoutAnomalyTypes.ExcessiveLegacyFallback,
                Severity = PayoutAnomalySeverity.Medium,
                CompanyId = g.Key.OrderCompanyId,
                OrderTypeId = g.Key.OrderTypeId,
                LegacyFallbackCount = g.Count(),
                DetectedAt = g.Max(x => x.Snapshot.CalculatedAt),
                Reason = $"Context has {g.Count()} legacy fallback(s) in lookback period (threshold {PayoutAnomalyThresholds.ExcessiveLegacyFallbackCount})."
            });
        }
        return list;
    }

    private static List<PayoutAnomalyDto> RuleRepeatedWarnings(List<SnapshotRow> snapshotRows, Dictionary<Guid, string> installerNames)
    {
        var withWarnings = new List<(OrderPayoutSnapshot Snapshot, Guid? InstallerId)>();
        foreach (var row in snapshotRows)
        {
            var s = row.Snapshot;
            if (string.IsNullOrEmpty(s.ResolutionTraceJson)) continue;
            try
            {
                using var doc = JsonDocument.Parse(s.ResolutionTraceJson);
                if (doc.RootElement.TryGetProperty("warnings", out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
                    withWarnings.Add((s, s.InstallerId));
            }
            catch { /* ignore */ }
        }
        var byInstaller = withWarnings.GroupBy(x => x.InstallerId).Where(g => g.Key.HasValue && g.Count() > PayoutAnomalyThresholds.RepeatedWarningsCount);
        var list = new List<PayoutAnomalyDto>();
        foreach (var g in byInstaller)
        {
            list.Add(new PayoutAnomalyDto
            {
                AnomalyType = PayoutAnomalyTypes.RepeatedWarnings,
                Severity = PayoutAnomalySeverity.Low,
                InstallerId = g.Key,
                InstallerName = g.Key.HasValue && installerNames.TryGetValue(g.Key.Value, out var n) ? n : null,
                WarningCount = g.Count(),
                DetectedAt = g.Max(x => x.Snapshot.CalculatedAt),
                Reason = $"Installer has {g.Count()} job(s) with resolution warnings in lookback period (threshold {PayoutAnomalyThresholds.RepeatedWarningsCount})."
            });
        }
        return list;
    }

    private async Task<List<PayoutAnomalyDto>> RuleNegativeMarginClusterAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var negative = await _context.PnlDetailPerOrders
            .AsNoTracking()
            .Where(p => p.ProfitForOrder < 0 && p.CalculatedAt >= from && p.CalculatedAt <= to.AddDays(1))
            .Select(p => new { p.OrderId, p.CompanyId, p.DepartmentId, OrderType = p.OrderType })
            .ToListAsync(ct);
        var orderIds = negative.Select(x => x.OrderId).Distinct().ToList();
        if (orderIds.Count == 0) return new List<PayoutAnomalyDto>();
        var orderTypes = await _context.Orders
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.CompanyId, o.OrderTypeId })
            .ToDictionaryAsync(x => x.Id, x => x, ct);
        var byContext = negative
            .Select(p => new { p.CompanyId, OrderTypeId = orderTypes.GetValueOrDefault(p.OrderId)?.OrderTypeId ?? Guid.Empty })
            .GroupBy(x => new { x.CompanyId, x.OrderTypeId })
            .Where(g => g.Count() > PayoutAnomalyThresholds.NegativeMarginClusterCount);
        var list = new List<PayoutAnomalyDto>();
        foreach (var g in byContext)
        {
            list.Add(new PayoutAnomalyDto
            {
                AnomalyType = PayoutAnomalyTypes.NegativeMarginCluster,
                Severity = PayoutAnomalySeverity.High,
                CompanyId = g.Key.CompanyId,
                OrderTypeId = g.Key.OrderTypeId == Guid.Empty ? null : g.Key.OrderTypeId,
                NegativeMarginCount = g.Count(),
                DetectedAt = to,
                Reason = $"Context has {g.Count()} negative-margin job(s) in lookback period (threshold {PayoutAnomalyThresholds.NegativeMarginClusterCount})."
            });
        }
        return list;
    }

    private static List<PayoutAnomalyDto> RuleInstallerDeviation(List<SnapshotRow> snapshotRows, Dictionary<Guid, string> installerNames)
    {
        var byInstaller = snapshotRows
            .Where(r => r.Snapshot.InstallerId.HasValue && r.Snapshot.FinalPayout > 0)
            .GroupBy(r => r.Snapshot.InstallerId!.Value);
        var byPeerKey = snapshotRows
            .GroupBy(r => (r.Snapshot.RateGroupId ?? Guid.Empty, r.Snapshot.PayoutPath ?? ""))
            .ToDictionary(g => g.Key, g => g.Average(x => (double)x.Snapshot.FinalPayout));
        var list = new List<PayoutAnomalyDto>();
        foreach (var g in byInstaller)
        {
            var installerAvg = g.Average(x => (double)x.Snapshot.FinalPayout);
            var peerAvgs = g.Select(x =>
            {
                var key = (x.Snapshot.RateGroupId ?? Guid.Empty, x.Snapshot.PayoutPath ?? "");
                return byPeerKey.GetValueOrDefault(key, 0);
            }).Where(a => a > 0).ToList();
            if (peerAvgs.Count == 0) continue;
            var peerAvg = peerAvgs.Average();
            if (peerAvg <= 0) continue;
            var ratio = installerAvg / peerAvg;
            if (ratio <= (1 + PayoutAnomalyThresholds.InstallerDeviationAbovePeerPercent)) continue;
            list.Add(new PayoutAnomalyDto
            {
                AnomalyType = PayoutAnomalyTypes.InstallerDeviation,
                Severity = ratio >= 2 ? PayoutAnomalySeverity.High : PayoutAnomalySeverity.Medium,
                InstallerId = g.Key,
                InstallerName = installerNames.TryGetValue(g.Key, out var n) ? n : null,
                PayoutAmount = (decimal)installerAvg,
                BaselineAmount = (decimal)peerAvg,
                DeviationPercent = (ratio - 1) * 100,
                DetectedAt = g.Max(x => x.Snapshot.CalculatedAt),
                Reason = $"Installer average payout {(ratio - 1) * 100:F0}% above peer average for similar jobs."
            });
        }
        return list;
    }

    private static List<PayoutAnomalyDto> ApplyListFilters(List<PayoutAnomalyDto> list, PayoutAnomalyFilterDto filter)
    {
        var q = list.AsEnumerable();
        if (filter.FromDate.HasValue) q = q.Where(a => a.DetectedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) q = q.Where(a => a.DetectedAt <= filter.ToDate.Value.AddDays(1));
        if (filter.InstallerId.HasValue) q = q.Where(a => a.InstallerId == filter.InstallerId);
        if (!string.IsNullOrEmpty(filter.AnomalyType)) q = q.Where(a => a.AnomalyType == filter.AnomalyType);
        if (!string.IsNullOrEmpty(filter.Severity)) q = q.Where(a => a.Severity == filter.Severity);
        if (!string.IsNullOrEmpty(filter.PayoutPath)) q = q.Where(a => a.PayoutPath == filter.PayoutPath);
        if (filter.CompanyId.HasValue) q = q.Where(a => a.CompanyId == filter.CompanyId);
        return q.ToList();
    }

    private static PayoutAnomalyDetectionSummaryDto AggregateSummary(List<PayoutAnomalyDto> list)
    {
        return new PayoutAnomalyDetectionSummaryDto
        {
            HighPayoutVsPeerCount = list.Count(a => a.AnomalyType == PayoutAnomalyTypes.HighPayoutVsPeer),
            ExcessiveCustomOverrideCount = list.Count(a => a.AnomalyType == PayoutAnomalyTypes.ExcessiveCustomOverride),
            ExcessiveLegacyFallbackCount = list.Count(a => a.AnomalyType == PayoutAnomalyTypes.ExcessiveLegacyFallback),
            RepeatedWarningsCount = list.Count(a => a.AnomalyType == PayoutAnomalyTypes.RepeatedWarnings),
            ZeroPayoutCount = list.Count(a => a.AnomalyType == PayoutAnomalyTypes.ZeroPayout),
            NegativeMarginClusterCount = list.Count(a => a.AnomalyType == PayoutAnomalyTypes.NegativeMarginCluster),
            InstallerDeviationCount = list.Count(a => a.AnomalyType == PayoutAnomalyTypes.InstallerDeviation),
            TotalCount = list.Count,
            HighSeverityCount = list.Count(a => a.Severity == PayoutAnomalySeverity.High),
            MediumSeverityCount = list.Count(a => a.Severity == PayoutAnomalySeverity.Medium),
            LowSeverityCount = list.Count(a => a.Severity == PayoutAnomalySeverity.Low)
        };
    }
}
