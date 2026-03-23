using CephasOps.Application.Notifications.DTOs;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Service for sending customer notifications (SMS/WhatsApp) based on order status changes
/// </summary>
public class CustomerNotificationService
{
    private readonly SmsProviderFactory _smsProviderFactory;
    private readonly WhatsAppProviderFactory _whatsAppProviderFactory;
    private readonly ISmsTemplateService _smsTemplateService;
    private readonly IWhatsAppTemplateService _whatsAppTemplateService;
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly ILogger<CustomerNotificationService> _logger;

    public CustomerNotificationService(
        SmsProviderFactory smsProviderFactory,
        WhatsAppProviderFactory whatsAppProviderFactory,
        ISmsTemplateService smsTemplateService,
        IWhatsAppTemplateService whatsAppTemplateService,
        IGlobalSettingsService globalSettingsService,
        ILogger<CustomerNotificationService> logger)
    {
        _smsProviderFactory = smsProviderFactory;
        _whatsAppProviderFactory = whatsAppProviderFactory;
        _smsTemplateService = smsTemplateService;
        _whatsAppTemplateService = whatsAppTemplateService;
        _globalSettingsService = globalSettingsService;
        _logger = logger;
    }

    /// <summary>
    /// Send order status notification to customer via SMS and/or WhatsApp
    /// </summary>
    public async Task SendOrderStatusNotificationAsync(
        OrderDto order,
        string newStatus,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if auto-send is enabled
            var smsAutoSend = await _globalSettingsService.GetValueAsync<bool>("SMS_AutoSendOnStatusChange", cancellationToken);
            var whatsAppAutoSend = await _globalSettingsService.GetValueAsync<bool>("WhatsApp_AutoSendOnStatusChange", cancellationToken);

            if (!smsAutoSend && !whatsAppAutoSend)
            {
                _logger.LogInformation("Auto-send notifications are disabled. Skipping notification for order {OrderId}", order.Id);
                return;
            }

            // Get customer phone number
            if (string.IsNullOrEmpty(order.CustomerPhone))
            {
                _logger.LogWarning("Order {OrderId} has no customer phone number. Cannot send notification.", order.Id);
                return;
            }

            // Get template codes
            var smsTemplateCode = await NotificationTemplateMapper.GetSmsTemplateCodeAsync(newStatus, _globalSettingsService, cancellationToken);
            var whatsAppTemplateCode = await NotificationTemplateMapper.GetWhatsAppTemplateCodeAsync(newStatus, _globalSettingsService, cancellationToken);

            // Send SMS if enabled and template exists
            if (smsAutoSend && !string.IsNullOrEmpty(smsTemplateCode))
            {
                var smsProvider = await _smsProviderFactory.CreateProviderAsync(cancellationToken);
                await SendSmsNotificationAsync(order, smsTemplateCode, companyId, smsProvider, cancellationToken);
            }

            // Send WhatsApp if enabled and template exists
            if (whatsAppAutoSend && !string.IsNullOrEmpty(whatsAppTemplateCode))
            {
                var whatsAppProvider = await _whatsAppProviderFactory.CreateProviderAsync(cancellationToken);
                await SendWhatsAppNotificationAsync(order, whatsAppTemplateCode, companyId, whatsAppProvider, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - notifications should never block workflow
            _logger.LogError(ex, "Failed to send order status notification for order {OrderId}. Error: {ErrorMessage}", 
                order.Id, ex.Message);
        }
    }

    private async Task SendSmsNotificationAsync(
        OrderDto order,
        string templateCode,
        Guid companyId,
        ISmsProvider smsProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get template by code
            var template = await _smsTemplateService.GetTemplateByCodeAsync(companyId, templateCode, cancellationToken);
            if (template == null || !template.IsActive)
            {
                _logger.LogWarning("SMS template {TemplateCode} not found or inactive. Skipping SMS notification.", templateCode);
                return;
            }

            // Build placeholders from order
            var placeholders = BuildPlaceholders(order);

            // Render message
            var message = await _smsTemplateService.RenderMessageAsync(template.Id, placeholders, cancellationToken);

            // Send SMS (provider is passed as parameter)
            var result = await smsProvider.SendSmsAsync(order.CustomerPhone, message, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("SMS notification sent successfully for order {OrderId}. MessageId: {MessageId}", 
                    order.Id, result.MessageId);
            }
            else
            {
                _logger.LogWarning("Failed to send SMS notification for order {OrderId}. Error: {Error}", 
                    order.Id, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS notification for order {OrderId}", order.Id);
        }
    }

    private async Task SendWhatsAppNotificationAsync(
        OrderDto order,
        string templateCode,
        Guid companyId,
        IWhatsAppProvider whatsAppProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get template by code
            var template = await _whatsAppTemplateService.GetTemplateByCodeAsync(companyId, templateCode, cancellationToken);
            if (template == null || !template.IsActive)
            {
                _logger.LogWarning("WhatsApp template {TemplateCode} not found or inactive. Skipping WhatsApp notification.", templateCode);
                return;
            }

            // Build placeholders from order
            var placeholders = BuildPlaceholders(order);

            // Render message (similar to SMS - replace placeholders in MessageBody)
            var message = template.MessageBody ?? string.Empty;
            foreach (var placeholder in placeholders)
            {
                message = message.Replace($"{{{placeholder.Key}}}", placeholder.Value);
            }

            // Send WhatsApp (provider is passed as parameter)
            var result = await whatsAppProvider.SendMessageAsync(order.CustomerPhone, message, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("WhatsApp notification sent successfully for order {OrderId}. MessageId: {MessageId}", 
                    order.Id, result.MessageId);
            }
            else
            {
                _logger.LogWarning("Failed to send WhatsApp notification for order {OrderId}. Error: {Error}", 
                    order.Id, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp notification for order {OrderId}", order.Id);
        }
    }

    private static Dictionary<string, string> BuildPlaceholders(OrderDto order)
    {
        return new Dictionary<string, string>
        {
            { "OrderId", order.Id.ToString() },
            { "CustomerName", order.CustomerName },
            { "CustomerPhone", order.CustomerPhone },
            { "ServiceId", order.ServiceId },
            { "Address", $"{order.AddressLine1} {order.AddressLine2}".Trim() },
            { "City", order.City },
            { "State", order.State },
            { "Postcode", order.Postcode },
            { "AppointmentDate", order.AppointmentDate.ToString("dd/MM/yyyy") },
            { "AppointmentTime", $"{order.AppointmentWindowFrom:hh\\:mm} - {order.AppointmentWindowTo:hh\\:mm}" },
            { "Status", order.Status },
            { "BuildingName", order.BuildingName ?? "" },
            { "UnitNo", order.UnitNo ?? "" }
        };
    }
}

