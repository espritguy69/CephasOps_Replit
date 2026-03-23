using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Notifications;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// SMS messaging service implementation
/// Handles SMS sending via configured provider (Twilio, SMS Gateway, etc.)
/// </summary>
public class SmsMessagingService : ISmsMessagingService
{
    private readonly SmsProviderFactory _smsProviderFactory;
    private readonly ISmsTemplateService _smsTemplateService;
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly ILogger<SmsMessagingService> _logger;

    public SmsMessagingService(
        SmsProviderFactory smsProviderFactory,
        ISmsTemplateService smsTemplateService,
        IGlobalSettingsService globalSettingsService,
        ILogger<SmsMessagingService> logger)
    {
        _smsProviderFactory = smsProviderFactory;
        _smsTemplateService = smsTemplateService;
        _globalSettingsService = globalSettingsService;
        _logger = logger;
    }

    public async Task<SmsResult> SendSmsAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending SMS to {To}", to);

        // Check if SMS is enabled
        var enabled = await _globalSettingsService.GetValueAsync<bool>("SMS_Enabled", cancellationToken);
        if (!enabled)
        {
            _logger.LogWarning("SMS is disabled. Message not sent to {To}", to);
            return new SmsResult
            {
                Success = false,
                MessageId = null,
                Status = "Disabled",
                ErrorMessage = "SMS is disabled in system settings"
            };
        }

        try
        {
            var provider = await _smsProviderFactory.CreateProviderAsync(cancellationToken);
            return await provider.SendSmsAsync(to, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {To}", to);
            return new SmsResult
            {
                Success = false,
                MessageId = null,
                Status = "Failed",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<SmsResult> SendTemplateSmsAsync(
        string to,
        string templateCode,
        Dictionary<string, string>? placeholders = null,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending template SMS '{TemplateCode}' to {To}", templateCode, to);

        var enabled = await _globalSettingsService.GetValueAsync<bool>("SMS_Enabled", cancellationToken);
        if (!enabled)
        {
            _logger.LogWarning("SMS is disabled. Template message not sent to {To}", to);
            return new SmsResult
            {
                Success = false,
                MessageId = null,
                Status = "Disabled",
                ErrorMessage = "SMS is disabled in system settings"
            };
        }

        try
        {
            var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
            if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            {
                return new SmsResult
                {
                    Success = false,
                    MessageId = null,
                    Status = "Failed",
                    ErrorMessage = "Company context is required for template SMS"
                };
            }

            var template = await _smsTemplateService.GetTemplateByCodeAsync(effectiveCompanyId.Value, templateCode, cancellationToken);
            if (template == null)
            {
                return new SmsResult
                {
                    Success = false,
                    MessageId = null,
                    Status = "Failed",
                    ErrorMessage = $"SMS template '{templateCode}' not found"
                };
            }

            if (!template.IsActive)
            {
                return new SmsResult
                {
                    Success = false,
                    MessageId = null,
                    Status = "Failed",
                    ErrorMessage = $"SMS template '{templateCode}' is not active"
                };
            }

            // Render template with placeholders
            var message = await _smsTemplateService.RenderMessageAsync(template.Id, placeholders ?? new Dictionary<string, string>(), cancellationToken);

            // Send via provider
            var provider = await _smsProviderFactory.CreateProviderAsync(cancellationToken);
            return await provider.SendSmsAsync(to, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending template SMS '{TemplateCode}' to {To}", templateCode, to);
            return new SmsResult
            {
                Success = false,
                MessageId = null,
                Status = "Failed",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<SmsResult> GetStatusAsync(string messageId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting SMS status for message {MessageId}", messageId);

        try
        {
            var provider = await _smsProviderFactory.CreateProviderAsync(cancellationToken);
            return await provider.GetStatusAsync(messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMS status for {MessageId}", messageId);
            return new SmsResult
            {
                Success = false,
                MessageId = messageId,
                Status = "Unknown",
                ErrorMessage = ex.Message
            };
        }
    }
}

