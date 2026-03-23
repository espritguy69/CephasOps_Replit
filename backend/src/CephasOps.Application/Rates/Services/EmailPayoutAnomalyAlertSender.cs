using System.Text;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Sends payout anomaly alerts via email. Read-only with respect to payout logic.
/// </summary>
public class EmailPayoutAnomalyAlertSender : IPayoutAnomalyAlertSender
{
    private readonly IEmailSendingService _emailSendingService;
    private readonly ApplicationDbContext _context;
    private readonly IOptions<PayoutAnomalyAlertOptions> _options;
    private readonly ILogger<EmailPayoutAnomalyAlertSender> _logger;

    public EmailPayoutAnomalyAlertSender(
        IEmailSendingService emailSendingService,
        ApplicationDbContext context,
        IOptions<PayoutAnomalyAlertOptions> options,
        ILogger<EmailPayoutAnomalyAlertSender> logger)
    {
        _emailSendingService = emailSendingService;
        _context = context;
        _options = options;
        _logger = logger;
    }

    public string ChannelName => PayoutAnomalyAlertChannel.Email;

    public async Task<int> SendAsync(
        IReadOnlyList<PayoutAnomalyDto> anomalies,
        IReadOnlyList<string> recipients,
        CancellationToken cancellationToken = default)
    {
        if (anomalies.Count == 0 || recipients.Count == 0)
            return 0;

        var emailAccountId = _options.Value.EmailAccountId;
        if (!emailAccountId.HasValue || emailAccountId.Value == Guid.Empty)
        {
            var account = await _context.Set<EmailAccount>()
                .Where(ea => ea.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (account == null)
            {
                _logger.LogWarning("Payout anomaly email alert skipped: no EmailAccountId in options and no active email account found.");
                return 0;
            }
            emailAccountId = account.Id;
        }
        else
        {
            var exists = await _context.Set<EmailAccount>()
                .AnyAsync(ea => ea.Id == emailAccountId.Value, cancellationToken);
            if (!exists)
            {
                _logger.LogWarning("Payout anomaly email alert skipped: configured EmailAccountId {Id} not found.", emailAccountId);
                return 0;
            }
        }

        var to = string.Join(",", recipients.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()));
        if (string.IsNullOrEmpty(to))
            return 0;

        var subject = $"CephasOps: Payout anomaly alert – {anomalies.Count} high-severity item(s)";
        var body = BuildBody(anomalies);

        var result = await _emailSendingService.SendEmailAsync(
            emailAccountId!.Value,
            to,
            subject,
            body,
            cancellationToken: cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Payout anomaly alert email sent to {Recipients}, {Count} anomalies.", to, anomalies.Count);
            return anomalies.Count;
        }

        _logger.LogWarning("Payout anomaly alert email failed: {Error}", result.ErrorMessage);
        return 0;
    }

    private static string BuildBody(IReadOnlyList<PayoutAnomalyDto> anomalies)
    {
        var sb = new StringBuilder();
        sb.Append("<p>The following payout anomalies were detected and require review.</p>");
        sb.Append("<table border=\"1\" cellpadding=\"4\" cellspacing=\"0\"><thead><tr><th>Type</th><th>Severity</th><th>Detected</th><th>Reason</th></tr></thead><tbody>");
        foreach (var a in anomalies)
        {
            sb.Append("<tr>");
            sb.Append("<td>").Append(System.Net.WebUtility.HtmlEncode(a.AnomalyType)).Append("</td>");
            sb.Append("<td>").Append(System.Net.WebUtility.HtmlEncode(a.Severity)).Append("</td>");
            sb.Append("<td>").Append(a.DetectedAt.ToString("yyyy-MM-dd HH:mm")).Append("</td>");
            sb.Append("<td>").Append(System.Net.WebUtility.HtmlEncode(a.Reason ?? "")).Append("</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
        sb.Append("<p>Please review in CephasOps: Payout Health → Anomalies.</p>");
        return sb.ToString();
    }
}
