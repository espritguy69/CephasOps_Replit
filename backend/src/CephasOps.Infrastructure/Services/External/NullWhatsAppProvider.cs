using CephasOps.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace CephasOps.Infrastructure.Services.External;

/// <summary>
/// Null WhatsApp provider - fail-proof fallback when WhatsApp is disabled
/// This provider does nothing but returns success to prevent workflow blocking
/// </summary>
public class NullWhatsAppProvider : IWhatsAppProvider
{
    private readonly ILogger<NullWhatsAppProvider> _logger;

    public NullWhatsAppProvider(ILogger<NullWhatsAppProvider> logger)
    {
        _logger = logger;
    }

    public Task<WhatsAppResult> SendMessageAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WhatsApp is disabled. Skipping WhatsApp message to {To}", to);
        return Task.FromResult(WhatsAppResult.SuccessResult("null", "skipped"));
    }

    public Task<WhatsAppResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WhatsApp is disabled. Returning skipped status for MessageId: {MessageId}", messageId);
        return Task.FromResult(WhatsAppResult.SuccessResult(messageId, "skipped"));
    }
}

