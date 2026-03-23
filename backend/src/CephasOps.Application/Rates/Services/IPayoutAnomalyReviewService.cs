using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Governance for payout anomalies: acknowledge, assign, resolve, comment. Operational metadata only; no payout/snapshot logic changed.
/// </summary>
public interface IPayoutAnomalyReviewService
{
    Task<PayoutAnomalyReviewDto> AcknowledgeAsync(string anomalyId, PayoutAnomalyDto? anomalySnapshot, CancellationToken cancellationToken = default);
    Task<PayoutAnomalyReviewDto> AssignAsync(string anomalyId, Guid? assignedToUserId, PayoutAnomalyDto? anomalySnapshot, CancellationToken cancellationToken = default);
    Task<PayoutAnomalyReviewDto> ResolveAsync(string anomalyId, PayoutAnomalyDto? anomalySnapshot, CancellationToken cancellationToken = default);
    Task<PayoutAnomalyReviewDto> MarkFalsePositiveAsync(string anomalyId, PayoutAnomalyDto? anomalySnapshot, CancellationToken cancellationToken = default);
    Task<PayoutAnomalyReviewDto> AddCommentAsync(string anomalyId, Guid userId, string? userName, string text, PayoutAnomalyDto? anomalySnapshot, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PayoutAnomalyReviewDto>> GetReviewsAsync(DateTime? from, DateTime? to, string? status, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<PayoutAnomalyReviewSummaryDto> GetReviewSummaryAsync(CancellationToken cancellationToken = default);
    Task<PayoutAnomalyReviewDto?> GetReviewByFingerprintAsync(string anomalyId, CancellationToken cancellationToken = default);
}
