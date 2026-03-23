using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Api.Authorization;
using CephasOps.Domain.Authorization;
using CephasOps.Domain.Rates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Payout Health Dashboard: snapshot coverage, anomaly counts, top unusual payouts, recent snapshots.
/// Anomaly governance (acknowledge, assign, resolve, comment) is operational metadata only; no payout/snapshot logic changed.
/// </summary>
[Authorize]
[ApiController]
[Route("api/payout-health")]
public class PayoutHealthController : ControllerBase
{
    private readonly IPayoutHealthDashboardService _dashboardService;
    private readonly IPayoutAnomalyService _anomalyService;
    private readonly IPayoutAnomalyReviewService _reviewService;
    private readonly IPayoutAnomalyAlertService _alertService;
    private readonly IAlertRunHistoryService _alertRunHistoryService;
    private readonly IPayoutAnomalyResponseTrackingService _responseTrackingService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PayoutHealthController> _logger;

    public PayoutHealthController(
        IPayoutHealthDashboardService dashboardService,
        IPayoutAnomalyService anomalyService,
        IPayoutAnomalyReviewService reviewService,
        IPayoutAnomalyAlertService alertService,
        IAlertRunHistoryService alertRunHistoryService,
        IPayoutAnomalyResponseTrackingService responseTrackingService,
        ICurrentUserService currentUserService,
        ILogger<PayoutHealthController> logger)
    {
        _dashboardService = dashboardService;
        _anomalyService = anomalyService;
        _reviewService = reviewService;
        _alertService = alertService;
        _alertRunHistoryService = alertRunHistoryService;
        _responseTrackingService = responseTrackingService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard data: snapshot health, anomaly summary, top unusual payouts, recent snapshots.
    /// </summary>
    [HttpGet("dashboard")]
    [RequirePermission(PermissionCatalog.PayoutHealthView)]
    [ProducesResponseType(typeof(ApiResponse<PayoutHealthDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutHealthDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var data = await _dashboardService.GetDashboardAsync(cancellationToken);
        return this.Success(data);
    }

    /// <summary>
    /// Get anomaly detection summary counts (by type and severity) for dashboard cards.
    /// </summary>
    [HttpGet("anomaly-summary")]
    [RequirePermission(PermissionCatalog.PayoutHealthView)]
    [ProducesResponseType(typeof(ApiResponse<PayoutAnomalyDetectionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutAnomalyDetectionSummaryDto>>> GetAnomalySummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? installerId,
        [FromQuery] string? anomalyType,
        [FromQuery] string? severity,
        [FromQuery] string? payoutPath,
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var filter = new PayoutAnomalyFilterDto
        {
            FromDate = from,
            ToDate = to,
            InstallerId = installerId,
            AnomalyType = anomalyType,
            Severity = severity,
            PayoutPath = payoutPath,
            CompanyId = companyId
        };
        var summary = await _anomalyService.GetAnomalySummaryAsync(filter, cancellationToken);
        return this.Success(summary);
    }

    /// <summary>
    /// Get paged list of payout anomalies with optional filters.
    /// </summary>
    [HttpGet("anomalies")]
    [RequirePermission(PermissionCatalog.PayoutHealthView)]
    [ProducesResponseType(typeof(ApiResponse<PayoutAnomalyListResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutAnomalyListResultDto>>> GetAnomalies(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? installerId,
        [FromQuery] string? anomalyType,
        [FromQuery] string? severity,
        [FromQuery] string? payoutPath,
        [FromQuery] Guid? companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = new PayoutAnomalyFilterDto
        {
            FromDate = from,
            ToDate = to,
            InstallerId = installerId,
            AnomalyType = anomalyType,
            Severity = severity,
            PayoutPath = payoutPath,
            CompanyId = companyId,
            Page = page,
            PageSize = Math.Clamp(pageSize, 1, 200)
        };
        var result = await _anomalyService.GetAnomaliesAsync(filter, cancellationToken);
        return this.Success(result);
    }

    /// <summary>
    /// Get top anomaly clusters (e.g. installers with most custom overrides, contexts with most legacy fallback).
    /// </summary>
    [HttpGet("anomaly-clusters")]
    [RequirePermission(PermissionCatalog.PayoutHealthView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PayoutAnomalyClusterDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PayoutAnomalyClusterDto>>>> GetAnomalyClusters(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        var filter = new PayoutAnomalyFilterDto { FromDate = from, ToDate = to };
        var clusters = await _anomalyService.GetTopClustersAsync(filter, Math.Clamp(top, 1, 50), cancellationToken);
        return this.Success(clusters);
    }

    /// <summary>
    /// Acknowledge an anomaly (by fingerprint id). Creates a review if none exists. Optional body can include anomaly snapshot to populate review.
    /// </summary>
    [HttpPost("anomalies/{id}/acknowledge")]
    [RequirePermission(PermissionCatalog.PayoutAnomaliesReview)]
    [ProducesResponseType(typeof(ApiResponse<PayoutAnomalyReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutAnomalyReviewDto>>> AcknowledgeAnomaly(string id, [FromBody] PayoutAnomalyDto? body, CancellationToken cancellationToken)
    {
        var review = await _reviewService.AcknowledgeAsync(id, body, cancellationToken);
        return this.Success(review);
    }

    /// <summary>
    /// Assign an anomaly review to a user.
    /// </summary>
    [HttpPost("anomalies/{id}/assign")]
    [RequirePermission(PermissionCatalog.PayoutAnomaliesReview)]
    [ProducesResponseType(typeof(ApiResponse<PayoutAnomalyReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutAnomalyReviewDto>>> AssignAnomaly(string id, [FromBody] AssignAnomalyRequestDto request, CancellationToken cancellationToken)
    {
        var review = await _reviewService.AssignAsync(id, request.AssignedToUserId, null, cancellationToken);
        return this.Success(review);
    }

    /// <summary>
    /// Mark an anomaly as resolved.
    /// </summary>
    [HttpPost("anomalies/{id}/resolve")]
    [RequirePermission(PermissionCatalog.PayoutAnomaliesReview)]
    [ProducesResponseType(typeof(ApiResponse<PayoutAnomalyReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutAnomalyReviewDto>>> ResolveAnomaly(string id, [FromBody] PayoutAnomalyDto? body, CancellationToken cancellationToken)
    {
        var review = await _reviewService.ResolveAsync(id, body, cancellationToken);
        return this.Success(review);
    }

    /// <summary>
    /// Mark an anomaly as false positive.
    /// </summary>
    [HttpPost("anomalies/{id}/false-positive")]
    [RequirePermission(PermissionCatalog.PayoutAnomaliesReview)]
    [ProducesResponseType(typeof(ApiResponse<PayoutAnomalyReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutAnomalyReviewDto>>> MarkAnomalyFalsePositive(string id, [FromBody] PayoutAnomalyDto? body, CancellationToken cancellationToken)
    {
        var review = await _reviewService.MarkFalsePositiveAsync(id, body, cancellationToken);
        return this.Success(review);
    }

    /// <summary>
    /// Add a comment to an anomaly review. Current user is recorded as comment author.
    /// </summary>
    [HttpPost("anomalies/{id}/comment")]
    [RequirePermission(PermissionCatalog.PayoutAnomaliesReview)]
    [ProducesResponseType(typeof(ApiResponse<PayoutAnomalyReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutAnomalyReviewDto>>> AddAnomalyComment(string id, [FromBody] AddAnomalyCommentRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var userName = _currentUserService.Email ?? "Unknown";
        var review = await _reviewService.AddCommentAsync(id, userId, userName, request.Text ?? "", null, cancellationToken);
        return this.Success(review);
    }

    /// <summary>
    /// Get anomaly reviews with optional filters (date range, status). Paginated.
    /// </summary>
    [HttpGet("anomaly-reviews")]
    [RequirePermission(PermissionCatalog.PayoutHealthView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PayoutAnomalyReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PayoutAnomalyReviewDto>>>> GetAnomalyReviews(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var list = await _reviewService.GetReviewsAsync(from, to, status, page, Math.Clamp(pageSize, 1, 200), cancellationToken);
        return this.Success(list);
    }

    /// <summary>
    /// Get anomaly review summary for dashboard (open count, investigating count, resolved today).
    /// </summary>
    [HttpGet("anomaly-reviews/summary")]
    [RequirePermission(PermissionCatalog.PayoutHealthView)]
    [ProducesResponseType(typeof(ApiResponse<PayoutAnomalyReviewSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutAnomalyReviewSummaryDto>>> GetAnomalyReviewSummary(CancellationToken cancellationToken)
    {
        var summary = await _reviewService.GetReviewSummaryAsync(cancellationToken);
        return this.Success(summary);
    }

    /// <summary>
    /// Get a single review by anomaly fingerprint id (if exists).
    /// </summary>
    [HttpGet("anomalies/{id}/review")]
    [RequirePermission(PermissionCatalog.PayoutHealthView)]
    [ProducesResponseType(typeof(ApiResponse<PayoutAnomalyReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutAnomalyReviewDto>>> GetAnomalyReview(string id, CancellationToken cancellationToken)
    {
        var review = await _reviewService.GetReviewByFingerprintAsync(id, cancellationToken);
        if (review == null) return this.NotFound("No review found for this anomaly.");
        return this.Success(review);
    }

    /// <summary>
    /// Run payout anomaly alerting: send alerts for new high-severity (and optionally repeated medium) anomalies.
    /// Intended for cron or manual trigger. Does not change payout or detection logic.
    /// </summary>
    [HttpPost("run-anomaly-alerts")]
    [RequirePermission(PermissionCatalog.PayoutAnomaliesReview)]
    [ProducesResponseType(typeof(ApiResponse<RunPayoutAnomalyAlertsResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<RunPayoutAnomalyAlertsResultDto>>> RunAnomalyAlerts(
        [FromBody] RunPayoutAnomalyAlertsRequestDto? request,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        var result = await _alertService.RunAlertsAsync(request ?? new RunPayoutAnomalyAlertsRequestDto(), cancellationToken);
        var completedAt = DateTime.UtcNow;
        try
        {
            await _alertRunHistoryService.RecordRunAsync(result, AlertRunTriggerSource.Manual, startedAt, completedAt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist manual alert run history");
        }
        return this.Success(result);
    }

    /// <summary>
    /// Get the most recent payout anomaly alert run (scheduler or manual), if any.
    /// </summary>
    [HttpGet("alert-runs/latest")]
    [RequirePermission(PermissionCatalog.PayoutHealthView)]
    [ProducesResponseType(typeof(ApiResponse<PayoutAnomalyAlertRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PayoutAnomalyAlertRunDto?>>> GetLatestAlertRun(CancellationToken cancellationToken)
    {
        var run = await _alertRunHistoryService.GetLatestAsync(cancellationToken);
        return this.Success<PayoutAnomalyAlertRunDto?>(run);
    }

    /// <summary>
    /// Get alert response summary: counts of alerted anomalies by review status and stale count. Read-only.
    /// </summary>
    /// <param name="from">Filter from date (optional).</param>
    /// <param name="to">Filter to date (optional).</param>
    /// <param name="installerId">Filter by installer ID (optional).</param>
    /// <param name="anomalyType">Filter by anomaly type (optional).</param>
    /// <param name="severity">Filter by severity (optional).</param>
    /// <param name="companyId">Filter by company ID (optional).</param>
    /// <param name="staleThresholdHours">Optional override for stale threshold (hours). When null, config default is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("alert-response-summary")]
    [RequirePermission(PermissionCatalog.PayoutHealthView)]
    [ProducesResponseType(typeof(ApiResponse<AlertResponseSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AlertResponseSummaryDto>>> GetAlertResponseSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? installerId,
        [FromQuery] string? anomalyType,
        [FromQuery] string? severity,
        [FromQuery] Guid? companyId,
        [FromQuery] int? staleThresholdHours,
        CancellationToken cancellationToken = default)
    {
        var filter = new PayoutAnomalyFilterDto
        {
            FromDate = from,
            ToDate = to,
            InstallerId = installerId,
            AnomalyType = anomalyType,
            Severity = severity,
            CompanyId = companyId
        };
        var summary = await _responseTrackingService.GetAlertResponseSummaryAsync(filter, staleThresholdHours, cancellationToken);
        return this.Success(summary);
    }

    /// <summary>
    /// Get stale alerted anomalies (alerted, still open, no action after configured threshold). Read-only.
    /// </summary>
    /// <param name="from">Filter from date (optional).</param>
    /// <param name="to">Filter to date (optional).</param>
    /// <param name="staleThresholdHours">Optional override for stale threshold (hours). When null, config default is used.</param>
    /// <param name="limit">Maximum number of anomalies to return (optional, clamped 1–100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("stale-alerted-anomalies")]
    [RequirePermission(PermissionCatalog.PayoutHealthView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PayoutAnomalyDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PayoutAnomalyDto>>>> GetStaleAlertedAnomalies(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? staleThresholdHours,
        [FromQuery] int? limit,
        CancellationToken cancellationToken = default)
    {
        var filter = new PayoutAnomalyFilterDto { FromDate = from, ToDate = to };
        var limitValue = Math.Clamp(limit ?? 50, 1, 100);
        var list = await _responseTrackingService.GetStaleAlertedAnomaliesAsync(filter, limitValue, staleThresholdHours, cancellationToken);
        return this.Success(list);
    }
}
