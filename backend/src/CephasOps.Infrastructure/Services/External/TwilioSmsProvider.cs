using CephasOps.Domain.Notifications;
using CephasOps.Domain.Settings;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CephasOps.Infrastructure.Services.External;

/// <summary>
/// Twilio SMS provider implementation
/// </summary>
public class TwilioSmsProvider : ISmsProvider
{
    private readonly IGlobalSettingsReader _globalSettingsReader;
    private readonly ILogger<TwilioSmsProvider> _logger;
    private string? _accountSid;
    private string? _authToken;
    private string? _fromNumber;

    public TwilioSmsProvider(
        IGlobalSettingsReader globalSettingsReader,
        ILogger<TwilioSmsProvider> logger)
    {
        _globalSettingsReader = globalSettingsReader;
        _logger = logger;
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_accountSid != null && _authToken != null && _fromNumber != null)
            return;

        _accountSid = await _globalSettingsReader.GetValueAsync<string>("SMS_Twilio_AccountSid", cancellationToken);
        _authToken = await _globalSettingsReader.GetValueAsync<string>("SMS_Twilio_AuthToken", cancellationToken);
        _fromNumber = await _globalSettingsReader.GetValueAsync<string>("SMS_Twilio_FromNumber", cancellationToken);

        if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(_fromNumber))
        {
            throw new InvalidOperationException("Twilio SMS credentials are not configured. Please set SMS_Twilio_AccountSid, SMS_Twilio_AuthToken, and SMS_Twilio_FromNumber in GlobalSettings.");
        }

        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task<SmsResult> SendSmsAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            await InitializeAsync(cancellationToken);

            _logger.LogInformation("Sending SMS via Twilio to {To}", to);

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_fromNumber),
                to: new PhoneNumber(to)
            );

            _logger.LogInformation("SMS sent successfully. MessageId: {MessageId}, Status: {Status}",
                messageResource.Sid, messageResource.Status);

            return SmsResult.SuccessResult(
                messageResource.Sid,
                messageResource.Status.ToString() ?? "sent",
                messageResource.DateSent
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS via Twilio to {To}", to);
            return SmsResult.FailedResult(
                $"Failed to send SMS: {ex.Message}",
                ex.GetType().Name
            );
        }
    }

    public async Task<SmsResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            await InitializeAsync(cancellationToken);

            _logger.LogInformation("Getting SMS status for MessageId: {MessageId}", messageId);

            var messageResource = await MessageResource.FetchAsync(
                pathSid: messageId
            );

            return SmsResult.SuccessResult(
                messageResource.Sid,
                messageResource.Status.ToString() ?? "unknown",
                messageResource.DateSent
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SMS status for MessageId: {MessageId}", messageId);
            return SmsResult.FailedResult(
                $"Failed to get SMS status: {ex.Message}",
                ex.GetType().Name
            );
        }
    }
}
