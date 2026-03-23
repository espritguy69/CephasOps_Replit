using CephasOps.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace CephasOps.Infrastructure.Services.External;

/// <summary>
/// Null SMS provider - fail-proof fallback when SMS is disabled
/// This provider does nothing but returns success to prevent workflow blocking
/// </summary>
public class NullSmsProvider : ISmsProvider
{
    private readonly ILogger<NullSmsProvider> _logger;

    public NullSmsProvider(ILogger<NullSmsProvider> logger)
    {
        _logger = logger;
    }

    public Task<SmsResult> SendSmsAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SMS is disabled. Skipping SMS to {To}", to);
        return Task.FromResult(SmsResult.SuccessResult("null", "skipped"));
    }

    public Task<SmsResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SMS is disabled. Returning skipped status for MessageId: {MessageId}", messageId);
        return Task.FromResult(SmsResult.SuccessResult(messageId, "skipped"));
    }
}

