using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Records and reads payout anomaly alert run history.
/// </summary>
public class AlertRunHistoryService : IAlertRunHistoryService
{
    private readonly ApplicationDbContext _context;

    public AlertRunHistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RecordRunAsync(
        RunPayoutAnomalyAlertsResultDto result,
        string triggerSource,
        DateTime startedAt,
        DateTime completedAt,
        CancellationToken cancellationToken = default)
    {
        var run = new PayoutAnomalyAlertRun
        {
            Id = Guid.NewGuid(),
            StartedAt = startedAt,
            CompletedAt = completedAt,
            EvaluatedCount = result.AnomaliesConsidered,
            SentCount = result.AnomaliesAlerted,
            SkippedCount = result.SkippedCount,
            ErrorCount = result.AlertsFailed,
            TriggerSource = triggerSource.Length > 32 ? triggerSource[..32] : triggerSource
        };
        _context.PayoutAnomalyAlertRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PayoutAnomalyAlertRunDto?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        var run = await _context.PayoutAnomalyAlertRuns
            .AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return run == null ? null : new PayoutAnomalyAlertRunDto
        {
            Id = run.Id,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            EvaluatedCount = run.EvaluatedCount,
            SentCount = run.SentCount,
            SkippedCount = run.SkippedCount,
            ErrorCount = run.ErrorCount,
            TriggerSource = run.TriggerSource ?? ""
        };
    }
}
