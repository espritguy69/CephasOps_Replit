using CephasOps.Domain.Notifications;
using CephasOps.Domain.Settings;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CephasOps.Infrastructure.Services.External;

/// <summary>
/// Twilio WhatsApp provider implementation
/// </summary>
public class TwilioWhatsAppProvider : IWhatsAppProvider
{
    private readonly IGlobalSettingsReader _globalSettingsReader;
    private readonly ILogger<TwilioWhatsAppProvider> _logger;
    private string? _accountSid;
    private string? _authToken;
    private string? _fromNumber;

    public TwilioWhatsAppProvider(
        IGlobalSettingsReader globalSettingsReader,
        ILogger<TwilioWhatsAppProvider> logger)
    {
        _globalSettingsReader = globalSettingsReader;
        _logger = logger;
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_accountSid != null && _authToken != null && _fromNumber != null)
            return;

        _accountSid = await _globalSettingsReader.GetValueAsync<string>("WhatsApp_Twilio_AccountSid", cancellationToken);
        _authToken = await _globalSettingsReader.GetValueAsync<string>("WhatsApp_Twilio_AuthToken", cancellationToken);
        _fromNumber = await _globalSettingsReader.GetValueAsync<string>("WhatsApp_Twilio_FromNumber", cancellationToken);

        if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(_fromNumber))
        {
            throw new InvalidOperationException("Twilio WhatsApp credentials are not configured. Please set WhatsApp_Twilio_AccountSid, WhatsApp_Twilio_AuthToken, and WhatsApp_Twilio_FromNumber in GlobalSettings.");
        }

        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task<WhatsAppResult> SendMessageAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            await InitializeAsync(cancellationToken);

            // Twilio WhatsApp uses whatsapp: prefix for phone numbers
            var whatsappTo = to.StartsWith("whatsapp:") ? to : $"whatsapp:{to}";
            var whatsappFrom = _fromNumber!.StartsWith("whatsapp:") ? _fromNumber : $"whatsapp:{_fromNumber}";

            _logger.LogInformation("Sending WhatsApp message via Twilio to {To}", whatsappTo);

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(whatsappFrom),
                to: new PhoneNumber(whatsappTo)
            );

            _logger.LogInformation("WhatsApp message sent successfully. MessageId: {MessageId}, Status: {Status}",
                messageResource.Sid, messageResource.Status);

            return WhatsAppResult.SuccessResult(
                messageResource.Sid,
                messageResource.Status.ToString() ?? "sent",
                messageResource.DateSent
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message via Twilio to {To}", to);
            return WhatsAppResult.FailedResult(
                $"Failed to send WhatsApp message: {ex.Message}",
                ex.GetType().Name
            );
        }
    }

    public async Task<WhatsAppResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            await InitializeAsync(cancellationToken);

            _logger.LogInformation("Getting WhatsApp message status for MessageId: {MessageId}", messageId);

            var messageResource = await MessageResource.FetchAsync(
                pathSid: messageId
            );

            return WhatsAppResult.SuccessResult(
                messageResource.Sid,
                messageResource.Status.ToString() ?? "unknown",
                messageResource.DateSent
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get WhatsApp message status for MessageId: {MessageId}", messageId);
            return WhatsAppResult.FailedResult(
                $"Failed to get WhatsApp message status: {ex.Message}",
                ex.GetType().Name
            );
        }
    }
}

