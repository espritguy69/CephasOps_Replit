using System.Text.Json;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Governance for payout anomalies. Operational metadata only; does not change payout or snapshot logic.
/// </summary>
public class PayoutAnomalyReviewService : IPayoutAnomalyReviewService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayoutAnomalyReviewService> _logger;

    public PayoutAnomalyReviewService(ApplicationDbContext context, ILogger<PayoutAnomalyReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PayoutAnomalyReviewDto> AcknowledgeAsync(string anomalyId, PayoutAnomalyDto? anomalySnapshot, CancellationToken cancellationToken = default)
    {
        var review = await GetOrCreateReviewAsync(anomalyId, anomalySnapshot, cancellationToken);
        review.Status = PayoutAnomalyReviewStatus.Acknowledged;
        review.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(review, cancellationToken);
    }

    public async Task<PayoutAnomalyReviewDto> AssignAsync(string anomalyId, Guid? assignedToUserId, PayoutAnomalyDto? anomalySnapshot, CancellationToken cancellationToken = default)
    {
        var review = await GetOrCreateReviewAsync(anomalyId, anomalySnapshot, cancellationToken);
        review.AssignedToUserId = assignedToUserId;
        review.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(review, cancellationToken);
    }

    public async Task<PayoutAnomalyReviewDto> ResolveAsync(string anomalyId, PayoutAnomalyDto? anomalySnapshot, CancellationToken cancellationToken = default)
    {
        var review = await GetOrCreateReviewAsync(anomalyId, anomalySnapshot, cancellationToken);
        review.Status = PayoutAnomalyReviewStatus.Resolved;
        review.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(review, cancellationToken);
    }

    public async Task<PayoutAnomalyReviewDto> MarkFalsePositiveAsync(string anomalyId, PayoutAnomalyDto? anomalySnapshot, CancellationToken cancellationToken = default)
    {
        var review = await GetOrCreateReviewAsync(anomalyId, anomalySnapshot, cancellationToken);
        review.Status = PayoutAnomalyReviewStatus.FalsePositive;
        review.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(review, cancellationToken);
    }

    public async Task<PayoutAnomalyReviewDto> AddCommentAsync(string anomalyId, Guid userId, string? userName, string text, PayoutAnomalyDto? anomalySnapshot, CancellationToken cancellationToken = default)
    {
        var review = await GetOrCreateReviewAsync(anomalyId, anomalySnapshot, cancellationToken);
        var comments = ParseNotes(review.NotesJson);
        comments.Add(new NoteEntry { At = DateTime.UtcNow, UserId = userId, UserName = userName ?? "", Text = text ?? "" });
        review.NotesJson = JsonSerializer.Serialize(comments, JsonOptions);
        review.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(review, cancellationToken);
    }

    public async Task<IReadOnlyList<PayoutAnomalyReviewDto>> GetReviewsAsync(DateTime? from, DateTime? to, string? status, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var q = _context.PayoutAnomalyReviews.AsNoTracking();
        if (from.HasValue) q = q.Where(r => r.DetectedAt >= from.Value);
        if (to.HasValue) q = q.Where(r => r.DetectedAt <= to.Value.AddDays(1));
        if (!string.IsNullOrEmpty(status)) q = q.Where(r => r.Status == status);
        var list = await q.OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var dtos = new List<PayoutAnomalyReviewDto>();
        foreach (var r in list)
            dtos.Add(await MapToDtoAsync(r, cancellationToken));
        return dtos;
    }

    public async Task<PayoutAnomalyReviewSummaryDto> GetReviewSummaryAsync(CancellationToken cancellationToken = default)
    {
        var open = await _context.PayoutAnomalyReviews.AsNoTracking().CountAsync(r => r.Status == PayoutAnomalyReviewStatus.Open, cancellationToken);
        var investigating = await _context.PayoutAnomalyReviews.AsNoTracking().CountAsync(r => r.Status == PayoutAnomalyReviewStatus.Investigating, cancellationToken);
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
        var resolvedToday = await _context.PayoutAnomalyReviews.AsNoTracking()
            .CountAsync(r => r.Status == PayoutAnomalyReviewStatus.Resolved && r.UpdatedAt >= todayStart && r.UpdatedAt < todayEnd, cancellationToken);
        return new PayoutAnomalyReviewSummaryDto { OpenCount = open, InvestigatingCount = investigating, ResolvedTodayCount = resolvedToday };
    }

    public async Task<PayoutAnomalyReviewDto?> GetReviewByFingerprintAsync(string anomalyId, CancellationToken cancellationToken = default)
    {
        var review = await _context.PayoutAnomalyReviews.AsNoTracking()
            .FirstOrDefaultAsync(r => r.AnomalyFingerprintId == anomalyId, cancellationToken);
        return review == null ? null : await MapToDtoAsync(review, cancellationToken);
    }

    private async Task<PayoutAnomalyReview> GetOrCreateReviewAsync(string anomalyId, PayoutAnomalyDto? snapshot, CancellationToken ct)
    {
        var review = await _context.PayoutAnomalyReviews.FirstOrDefaultAsync(r => r.AnomalyFingerprintId == anomalyId, ct);
        if (review != null) return review;

        review = new PayoutAnomalyReview
        {
            AnomalyFingerprintId = anomalyId,
            Status = PayoutAnomalyReviewStatus.Open
        };
        if (snapshot != null)
        {
            review.AnomalyType = snapshot.AnomalyType ?? "";
            review.OrderId = snapshot.OrderId;
            review.InstallerId = snapshot.InstallerId;
            review.PayoutSnapshotId = snapshot.PayoutSnapshotId;
            review.Severity = snapshot.Severity ?? "Medium";
            review.DetectedAt = snapshot.DetectedAt;
        }
        _context.PayoutAnomalyReviews.Add(review);
        await _context.SaveChangesAsync(ct);
        return review;
    }

    private static List<NoteEntry> ParseNotes(string? notesJson)
    {
        if (string.IsNullOrWhiteSpace(notesJson)) return new List<NoteEntry>();
        try
        {
            var list = JsonSerializer.Deserialize<List<NoteEntry>>(notesJson, JsonOptions);
            return list ?? new List<NoteEntry>();
        }
        catch { return new List<NoteEntry>(); }
    }

    private async Task<PayoutAnomalyReviewDto> MapToDtoAsync(PayoutAnomalyReview r, CancellationToken ct)
    {
        string? assignedName = null;
        if (r.AssignedToUserId.HasValue)
        {
            var u = await _context.Users.AsNoTracking()
                .Where(x => x.Id == r.AssignedToUserId)
                .Select(x => new { x.Email, x.Name })
                .FirstOrDefaultAsync(ct);
            assignedName = u != null ? (u.Name ?? u.Email) : null;
        }
        return new PayoutAnomalyReviewDto
        {
            Id = r.Id,
            AnomalyFingerprintId = r.AnomalyFingerprintId,
            AnomalyType = r.AnomalyType,
            OrderId = r.OrderId,
            InstallerId = r.InstallerId,
            PayoutSnapshotId = r.PayoutSnapshotId,
            Severity = r.Severity,
            DetectedAt = r.DetectedAt,
            Status = r.Status,
            AssignedToUserId = r.AssignedToUserId,
            AssignedToUserName = assignedName,
            NotesJson = r.NotesJson,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }

    private sealed class NoteEntry
    {
        public DateTime At { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
