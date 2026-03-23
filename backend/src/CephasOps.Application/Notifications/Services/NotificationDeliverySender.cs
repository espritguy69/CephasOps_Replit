using CephasOps.Application.Parser.Services;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Notifications.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Sends a single NotificationDispatch via Sms, WhatsApp, or Email (Phase 2 + Phase 6).
/// </summary>
public class NotificationDeliverySender : INotificationDeliverySender
{
    private readonly ISmsTemplateService _smsTemplateService;
    private readonly IWhatsAppTemplateService _whatsAppTemplateService;
    private readonly SmsProviderFactory _smsProviderFactory;
    private readonly WhatsAppProviderFactory _whatsAppProviderFactory;
    private readonly IEmailSendingService? _emailSendingService;
    private readonly IDefaultEmailAccountIdProvider? _defaultEmailAccountIdProvider;
    private readonly ILogger<NotificationDeliverySender> _logger;

    public NotificationDeliverySender(
        ISmsTemplateService smsTemplateService,
        IWhatsAppTemplateService whatsAppTemplateService,
        SmsProviderFactory smsProviderFactory,
        WhatsAppProviderFactory whatsAppProviderFactory,
        ILogger<NotificationDeliverySender> logger,
        IEmailSendingService? emailSendingService = null,
        IDefaultEmailAccountIdProvider? defaultEmailAccountIdProvider = null)
    {
        _smsTemplateService = smsTemplateService;
        _whatsAppTemplateService = whatsAppTemplateService;
        _smsProviderFactory = smsProviderFactory;
        _whatsAppProviderFactory = whatsAppProviderFactory;
        _logger = logger;
        _emailSendingService = emailSendingService;
        _defaultEmailAccountIdProvider = defaultEmailAccountIdProvider;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorMessage)> SendAsync(NotificationDispatch dispatch, CancellationToken cancellationToken = default)
    {
        var companyId = dispatch.CompanyId ?? Guid.Empty;
        var placeholders = string.IsNullOrEmpty(dispatch.PayloadJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(dispatch.PayloadJson) ?? new Dictionary<string, string>();

        if (string.Equals(dispatch.Channel, "Sms", StringComparison.OrdinalIgnoreCase))
            return await SendSmsAsync(companyId, dispatch.Target, dispatch.TemplateKey, placeholders, cancellationToken);

        if (string.Equals(dispatch.Channel, "WhatsApp", StringComparison.OrdinalIgnoreCase))
            return await SendWhatsAppAsync(companyId, dispatch.Target, dispatch.TemplateKey, placeholders, cancellationToken);

        if (string.Equals(dispatch.Channel, "Email", StringComparison.OrdinalIgnoreCase))
            return await SendEmailAsync(dispatch, cancellationToken);

        _logger.LogWarning("Unsupported notification channel {Channel} for dispatch {DispatchId}", dispatch.Channel, dispatch.Id);
        return (false, "Unsupported channel");
    }

    private async Task<(bool Success, string? ErrorMessage)> SendEmailAsync(NotificationDispatch dispatch, CancellationToken cancellationToken)
    {
        if (_emailSendingService == null || _defaultEmailAccountIdProvider == null)
        {
            _logger.LogWarning("Email dispatch {DispatchId}: IEmailSendingService or IDefaultEmailAccountIdProvider not configured", dispatch.Id);
            return (false, "Email sending not configured");
        }

        var accountId = await _defaultEmailAccountIdProvider.GetDefaultEmailAccountIdAsync(cancellationToken);
        if (!accountId.HasValue)
        {
            _logger.LogWarning("Email dispatch {DispatchId}: no active email account", dispatch.Id);
            return (false, "No active email account");
        }

        string? subject = null;
        string? body = null;
        if (!string.IsNullOrEmpty(dispatch.PayloadJson))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(dispatch.PayloadJson);
                if (payload != null)
                {
                    payload.TryGetValue("subject", out subject);
                    payload.TryGetValue("body", out body);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Email dispatch {DispatchId}: invalid PayloadJson", dispatch.Id);
                return (false, "Invalid payload");
            }
        }

        if (string.IsNullOrWhiteSpace(dispatch.Target))
        {
            _logger.LogWarning("Email dispatch {DispatchId}: missing target address", dispatch.Id);
            return (false, "Missing target");
        }
        if (string.IsNullOrWhiteSpace(subject)) subject = "(No subject)";
        if (body == null) body = string.Empty;

        var result = await _emailSendingService.SendEmailAsync(
            accountId.Value,
            dispatch.Target.Trim(),
            subject,
            body,
            cancellationToken: cancellationToken);

        return (result.Success, result.Success ? null : result.ErrorMessage);
    }

    private async Task<(bool Success, string? ErrorMessage)> SendSmsAsync(Guid companyId, string toPhone, string? templateCode, Dictionary<string, string> placeholders, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(templateCode))
            return (false, "Missing template code");
        var template = await _smsTemplateService.GetTemplateByCodeAsync(companyId, templateCode, cancellationToken);
        if (template == null || !template.IsActive)
            return (false, "Template not found or inactive");
        var message = await _smsTemplateService.RenderMessageAsync(template.Id, placeholders, cancellationToken);
        var provider = await _smsProviderFactory.CreateProviderAsync(cancellationToken);
        var result = await provider.SendSmsAsync(toPhone, message, cancellationToken);
        return (result.Success, result.Success ? null : result.ErrorMessage);
    }

    private async Task<(bool Success, string? ErrorMessage)> SendWhatsAppAsync(Guid companyId, string toPhone, string? templateCode, Dictionary<string, string> placeholders, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(templateCode))
            return (false, "Missing template code");
        var template = await _whatsAppTemplateService.GetTemplateByCodeAsync(companyId, templateCode, cancellationToken);
        if (template == null || !template.IsActive)
            return (false, "Template not found or inactive");
        var message = template.MessageBody ?? string.Empty;
        foreach (var p in placeholders)
            message = message.Replace($"{{{p.Key}}}", p.Value);
        var provider = await _whatsAppProviderFactory.CreateProviderAsync(cancellationToken);
        var result = await provider.SendMessageAsync(toPhone, message, cancellationToken);
        return (result.Success, result.Success ? null : result.ErrorMessage);
    }
}
