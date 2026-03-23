using CephasOps.Application.Rates.DTOs;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Read-only response tracking for alerted anomalies: summary by status, stale list, time-to-action.
/// Does not change payout, detection, or alert logic.
/// </summary>
public class PayoutAnomalyResponseTrackingService : IPayoutAnomalyResponseTrackingService
{
    private const int MaxAnomaliesForSummary = 500;

    private readonly IPayoutAnomalyService _anomalyService;
    private readonly IOptions<PayoutAnomalyAlertOptions> _options;

    public PayoutAnomalyResponseTrackingService(
        IPayoutAnomalyService anomalyService,
        IOptions<PayoutAnomalyAlertOptions> options)
    {
        _anomalyService = anomalyService;
        _options = options;
    }

    public async Task<AlertResponseSummaryDto> GetAlertResponseSummaryAsync(PayoutAnomalyFilterDto filter, int? staleThresholdHoursOverride = null, CancellationToken cancellationToken = default)
    {
        var listFilter = new PayoutAnomalyFilterDto
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            InstallerId = filter.InstallerId,
            AnomalyType = filter.AnomalyType,
            Severity = filter.Severity,
            PayoutPath = filter.PayoutPath,
            CompanyId = filter.CompanyId,
            Page = 1,
            PageSize = MaxAnomaliesForSummary
        };
        var result = await _anomalyService.GetAnomaliesAsync(listFilter, cancellationToken).ConfigureAwait(false);
        var items = result.Items;

        var alerted = items.Where(a => a.Alerted).ToList();
        var alertedOpen = alerted.Count(a => string.IsNullOrEmpty(a.ReviewStatus) || a.ReviewStatus == PayoutAnomalyReviewStatus.Open);
        var alertedAcknowledged = alerted.Count(a => a.ReviewStatus == PayoutAnomalyReviewStatus.Acknowledged);
        var alertedInvestigating = alerted.Count(a => a.ReviewStatus == PayoutAnomalyReviewStatus.Investigating);
        var alertedResolved = alerted.Count(a => a.ReviewStatus == PayoutAnomalyReviewStatus.Resolved);
        var alertedFalsePositive = alerted.Count(a => a.ReviewStatus == PayoutAnomalyReviewStatus.FalsePositive);

        var thresholdHours = Math.Max(0, staleThresholdHoursOverride ?? _options.Value.StaleThresholdHours);
        var staleCutoff = DateTime.UtcNow.AddHours(-thresholdHours);
        var staleCount = alerted.Count(a =>
            (string.IsNullOrEmpty(a.ReviewStatus) || a.ReviewStatus == PayoutAnomalyReviewStatus.Open)
            && a.LastAlertedAt.HasValue && a.LastAlertedAt.Value < staleCutoff);

        double? avgMinutes = null;
        var withAction = alerted.Where(a => a.LastActionAt.HasValue && a.LastAlertedAt.HasValue).ToList();
        if (withAction.Count > 0)
        {
            avgMinutes = withAction.Average(a => (a.LastActionAt!.Value - a.LastAlertedAt!.Value).TotalMinutes);
        }

        return new AlertResponseSummaryDto
        {
            AlertedOpen = alertedOpen,
            AlertedAcknowledged = alertedAcknowledged,
            AlertedInvestigating = alertedInvestigating,
            AlertedResolved = alertedResolved,
            AlertedFalsePositive = alertedFalsePositive,
            StaleCount = staleCount,
            AverageTimeToFirstActionMinutes = avgMinutes
        };
    }

    public async Task<IReadOnlyList<PayoutAnomalyDto>> GetStaleAlertedAnomaliesAsync(PayoutAnomalyFilterDto? filter, int limit = 50, int? staleThresholdHoursOverride = null, CancellationToken cancellationToken = default)
    {
        var listFilter = filter ?? new PayoutAnomalyFilterDto();
        listFilter = new PayoutAnomalyFilterDto
        {
            FromDate = listFilter.FromDate,
            ToDate = listFilter.ToDate,
            InstallerId = listFilter.InstallerId,
            AnomalyType = listFilter.AnomalyType,
            Severity = listFilter.Severity,
            PayoutPath = listFilter.PayoutPath,
            CompanyId = listFilter.CompanyId,
            Page = 1,
            PageSize = MaxAnomaliesForSummary
        };
        var result = await _anomalyService.GetAnomaliesAsync(listFilter, cancellationToken).ConfigureAwait(false);
        var items = result.Items;

        var thresholdHours = Math.Max(0, staleThresholdHoursOverride ?? _options.Value.StaleThresholdHours);
        var staleCutoff = DateTime.UtcNow.AddHours(-thresholdHours);

        var stale = items
            .Where(a => a.Alerted
                && (string.IsNullOrEmpty(a.ReviewStatus) || a.ReviewStatus == PayoutAnomalyReviewStatus.Open)
                && a.LastAlertedAt.HasValue && a.LastAlertedAt.Value < staleCutoff)
            .OrderBy(a => a.LastAlertedAt!.Value)
            .Take(limit)
            .ToList();

        return stale;
    }
}
