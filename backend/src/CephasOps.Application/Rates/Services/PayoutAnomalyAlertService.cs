using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Runs payout anomaly alerting: gets current anomalies, applies duplicate prevention, sends via channels, records alerts.
/// Does not change payout or anomaly detection logic.
/// </summary>
public class PayoutAnomalyAlertService : IPayoutAnomalyAlertService
{
    private const int MaxAnomaliesPerRun = 500;

    private readonly IPayoutAnomalyService _anomalyService;
    private readonly IEnumerable<IPayoutAnomalyAlertSender> _senders;
    private readonly ApplicationDbContext _context;
    private readonly IOptions<PayoutAnomalyAlertOptions> _options;
    private readonly ILogger<PayoutAnomalyAlertService> _logger;

    public PayoutAnomalyAlertService(
        IPayoutAnomalyService anomalyService,
        IEnumerable<IPayoutAnomalyAlertSender> senders,
        ApplicationDbContext context,
        IOptions<PayoutAnomalyAlertOptions> options,
        ILogger<PayoutAnomalyAlertService> logger)
    {
        _anomalyService = anomalyService;
        _senders = senders;
        _context = context;
        _options = options;
        _logger = logger;
    }

    public async Task<RunPayoutAnomalyAlertsResultDto> RunAlertsAsync(
        RunPayoutAnomalyAlertsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = new RunPayoutAnomalyAlertsResultDto();
        var recipients = request.RecipientEmails?.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList() ?? new List<string>();
        if (recipients.Count == 0 && !string.IsNullOrWhiteSpace(_options.Value.DefaultRecipientEmails))
            recipients = _options.Value.DefaultRecipientEmails.Split(',', ';').Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e)).ToList();

        if (recipients.Count == 0)
        {
            result.Errors.Add("No recipient emails configured or provided.");
            return result;
        }

        var fromDate = DateTime.UtcNow.Date.AddDays(-PayoutAnomalyThresholds.LookbackDays);
        var filter = new PayoutAnomalyFilterDto
        {
            FromDate = fromDate,
            ToDate = DateTime.UtcNow,
            Page = 1,
            PageSize = MaxAnomaliesPerRun
        };
        var listResult = await _anomalyService.GetAnomaliesAsync(filter, cancellationToken);
        var all = listResult.Items;

        var toAlert = all
            .Where(a => a.Severity == PayoutAnomalySeverity.High ||
                        (_options.Value.IncludeMediumRepeated || request.IncludeMediumRepeated == true) && a.Severity == PayoutAnomalySeverity.Medium)
            .ToList();
        result.AnomaliesConsidered = toAlert.Count;

        if (toAlert.Count == 0)
            return result;

        var windowHours = Math.Max(1, _options.Value.DuplicatePreventionHours);
        var since = DateTime.UtcNow.AddHours(-windowHours);
        var alreadySent = await _context.PayoutAnomalyAlerts
            .AsNoTracking()
            .Where(x => x.Status == PayoutAnomalyAlertStatus.Sent && x.SentAtUtc >= since)
            .Select(x => x.AnomalyFingerprintId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var alreadySet = new HashSet<string>(alreadySent);
        var toSend = toAlert.Where(a => !alreadySet.Contains(a.Id)).ToList();
        result.SkippedCount = toAlert.Count - toSend.Count;

        if (toSend.Count == 0)
            return result;

        foreach (var sender in _senders)
        {
            result.ChannelsUsed.Add(sender.ChannelName);
            try
            {
                var sent = await sender.SendAsync(toSend, recipients, cancellationToken);
                result.AlertsSent += sent;
                if (sent > 0) result.AnomaliesAlerted = toSend.Count;

                foreach (var a in toSend.Take(sent))
                {
                    _context.PayoutAnomalyAlerts.Add(new PayoutAnomalyAlert
                    {
                        AnomalyFingerprintId = a.Id,
                        Channel = sender.ChannelName,
                        SentAtUtc = DateTime.UtcNow,
                        Status = PayoutAnomalyAlertStatus.Sent,
                        RetryCount = 0,
                        RecipientId = recipients.Count == 1 ? recipients[0] : null
                    });
                }
                if (sent < toSend.Count)
                {
                    foreach (var a in toSend.Skip(sent))
                    {
                        _context.PayoutAnomalyAlerts.Add(new PayoutAnomalyAlert
                        {
                            AnomalyFingerprintId = a.Id,
                            Channel = sender.ChannelName,
                            SentAtUtc = DateTime.UtcNow,
                            Status = PayoutAnomalyAlertStatus.Failed,
                            RetryCount = 0,
                            ErrorMessage = "Sender did not report full send."
                        });
                        result.AlertsFailed++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payout anomaly alert sender {Channel} failed.", sender.ChannelName);
                result.Errors.Add($"{sender.ChannelName}: {ex.Message}");
                foreach (var a in toSend)
                {
                    _context.PayoutAnomalyAlerts.Add(new PayoutAnomalyAlert
                    {
                        AnomalyFingerprintId = a.Id,
                        Channel = sender.ChannelName,
                        SentAtUtc = DateTime.UtcNow,
                        Status = PayoutAnomalyAlertStatus.Failed,
                        RetryCount = 0,
                        ErrorMessage = ex.Message?.Length > 2000 ? ex.Message[..2000] : ex.Message
                    });
                    result.AlertsFailed++;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }
}
