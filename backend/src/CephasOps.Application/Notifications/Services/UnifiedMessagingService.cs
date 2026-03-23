using CephasOps.Application.Notifications.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Notifications;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Unified messaging service implementation
/// Handles smart routing between SMS and WhatsApp based on customer preferences and message type
/// </summary>
public class UnifiedMessagingService : IUnifiedMessagingService
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppMessagingService _whatsAppMessagingService;
    private readonly SmsProviderFactory _smsProviderFactory;
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly ILogger<UnifiedMessagingService> _logger;

    public UnifiedMessagingService(
        ApplicationDbContext context,
        IWhatsAppMessagingService whatsAppMessagingService,
        SmsProviderFactory smsProviderFactory,
        IGlobalSettingsService globalSettingsService,
        ILogger<UnifiedMessagingService> logger)
    {
        _context = context;
        _whatsAppMessagingService = whatsAppMessagingService;
        _smsProviderFactory = smsProviderFactory;
        _globalSettingsService = globalSettingsService;
        _logger = logger;
    }

    public async Task<MessagingResult> SendJobUpdateAsync(
        string customerPhone,
        string orderNumber,
        string status,
        string? appointmentDate = null,
        string? installerName = null,
        bool isUrgent = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending job update to {Phone} for order {OrderNumber} (Urgent: {IsUrgent})", 
            customerPhone, orderNumber, isUrgent);

        var normalizedPhone = NormalizePhoneNumber(customerPhone);
        var customerPref = await GetOrCreateCustomerPreferenceAsync(normalizedPhone, cancellationToken);

        var result = new MessagingResult();

        // Routing logic based on table
        if (isUrgent)
        {
            // Urgent: Send both SMS and WhatsApp
            _logger.LogInformation("Urgent update - sending both SMS and WhatsApp");
            result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"Job Update: {orderNumber} - {status}", cancellationToken);
            result.WhatsAppSent = await TrySendWhatsAppJobUpdateAsync(normalizedPhone, orderNumber, status, appointmentDate, installerName, customerPref, cancellationToken);
        }
        else
        {
            // Non-urgent: Check customer preference
            if (customerPref.UsesWhatsApp == false)
            {
                // Customer doesn't use WhatsApp: SMS only
                _logger.LogInformation("Customer doesn't use WhatsApp - sending SMS only");
                result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"Job Update: {orderNumber} - {status}", cancellationToken);
            }
            else
            {
                // Customer uses WhatsApp (or unknown): Try WhatsApp first
                result.WhatsAppSent = await TrySendWhatsAppJobUpdateAsync(normalizedPhone, orderNumber, status, appointmentDate, installerName, customerPref, cancellationToken);

                // Optional SMS fallback (if configured)
                var sendSmsFallback = await _globalSettingsService.GetValueAsync<bool>("Messaging_SendSmsFallback", cancellationToken);
                if (sendSmsFallback && result.WhatsAppSent)
                {
                    _logger.LogInformation("SMS fallback enabled - sending SMS alongside WhatsApp");
                    result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"Job Update: {orderNumber} - {status}", cancellationToken);
                }
                else if (!result.WhatsAppSent)
                {
                    // WhatsApp failed - fallback to SMS
                    _logger.LogWarning("WhatsApp failed - falling back to SMS");
                    result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"Job Update: {orderNumber} - {status}", cancellationToken);
                }
            }
        }

        // Update customer preference based on results
        await UpdateCustomerPreferenceAsync(customerPref, result.WhatsAppSent, cancellationToken);

        return result;
    }

    public async Task<MessagingResult> SendSiOnTheWayAlertAsync(
        string customerPhone,
        string orderNumber,
        string installerName,
        string? estimatedArrival = null,
        bool isUrgent = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending SI on-the-way alert to {Phone} for order {OrderNumber} (Urgent: {IsUrgent})", 
            customerPhone, orderNumber, isUrgent);

        var normalizedPhone = NormalizePhoneNumber(customerPhone);
        var customerPref = await GetOrCreateCustomerPreferenceAsync(normalizedPhone, cancellationToken);

        var result = new MessagingResult();

        if (isUrgent)
        {
            // Urgent: Send both
            result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"SI {installerName} is on the way for order {orderNumber}", cancellationToken);
            result.WhatsAppSent = await TrySendWhatsAppSiOnTheWayAsync(normalizedPhone, orderNumber, installerName, estimatedArrival, customerPref, cancellationToken);
        }
        else if (customerPref.UsesWhatsApp == false)
        {
            // Customer doesn't use WhatsApp: SMS only
            result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"SI {installerName} is on the way for order {orderNumber}", cancellationToken);
        }
        else
        {
            // Try WhatsApp first
            result.WhatsAppSent = await TrySendWhatsAppSiOnTheWayAsync(normalizedPhone, orderNumber, installerName, estimatedArrival, customerPref, cancellationToken);

            var sendSmsFallback = await _globalSettingsService.GetValueAsync<bool>("Messaging_SendSmsFallback", cancellationToken);
            if (sendSmsFallback && result.WhatsAppSent)
            {
                result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"SI {installerName} is on the way for order {orderNumber}", cancellationToken);
            }
            else if (!result.WhatsAppSent)
            {
                // Fallback to SMS
                result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"SI {installerName} is on the way for order {orderNumber}", cancellationToken);
            }
        }

        await UpdateCustomerPreferenceAsync(customerPref, result.WhatsAppSent, cancellationToken);

        return result;
    }

    public async Task<MessagingResult> SendTtktNotificationAsync(
        string customerPhone,
        string ticketNumber,
        string issueDescription,
        string? resolution = null,
        bool isUrgent = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending TTKT notification to {Phone} for ticket {TicketNumber} (Urgent: {IsUrgent})", 
            customerPhone, ticketNumber, isUrgent);

        var normalizedPhone = NormalizePhoneNumber(customerPhone);
        var customerPref = await GetOrCreateCustomerPreferenceAsync(normalizedPhone, cancellationToken);

        var result = new MessagingResult();

        if (isUrgent)
        {
            // Urgent: Send both
            result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"TTKT {ticketNumber}: {issueDescription}", cancellationToken);
            result.WhatsAppSent = await TrySendWhatsAppTtktAsync(normalizedPhone, ticketNumber, issueDescription, resolution, customerPref, cancellationToken);
        }
        else if (customerPref.UsesWhatsApp == false)
        {
            // Customer doesn't use WhatsApp: SMS only
            result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"TTKT {ticketNumber}: {issueDescription}", cancellationToken);
        }
        else
        {
            // Try WhatsApp first
            result.WhatsAppSent = await TrySendWhatsAppTtktAsync(normalizedPhone, ticketNumber, issueDescription, resolution, customerPref, cancellationToken);

            var sendSmsFallback = await _globalSettingsService.GetValueAsync<bool>("Messaging_SendSmsFallback", cancellationToken);
            if (sendSmsFallback && result.WhatsAppSent)
            {
                result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"TTKT {ticketNumber}: {issueDescription}", cancellationToken);
            }
            else if (!result.WhatsAppSent)
            {
                // Fallback to SMS
                result.SmsSent = await TrySendSmsAsync(normalizedPhone, $"TTKT {ticketNumber}: {issueDescription}", cancellationToken);
            }
        }

        await UpdateCustomerPreferenceAsync(customerPref, result.WhatsAppSent, cancellationToken);

        return result;
    }

    private async Task<CustomerPreference> GetOrCreateCustomerPreferenceAsync(string normalizedPhone, CancellationToken cancellationToken)
    {
        var preference = await _context.CustomerPreferences
            .FirstOrDefaultAsync(p => p.CustomerPhone == normalizedPhone, cancellationToken);

        if (preference == null)
        {
            preference = new CustomerPreference
            {
                Id = Guid.NewGuid(),
                CustomerPhone = normalizedPhone,
                UsesWhatsApp = null, // Unknown - will be auto-detected
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CustomerPreferences.Add(preference);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new customer preference for {Phone}", normalizedPhone);
        }

        return preference;
    }

    private async Task<bool> TrySendSmsAsync(string phone, string message, CancellationToken cancellationToken)
    {
        try
        {
            var smsProvider = await _smsProviderFactory.CreateProviderAsync(cancellationToken);
            var result = await smsProvider.SendSmsAsync(phone, message, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("SMS sent successfully to {Phone}", phone);
                return true;
            }
            else
            {
                _logger.LogWarning("SMS failed to {Phone}: {Error}", phone, result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {Phone}", phone);
            return false;
        }
    }

    private async Task<bool> TrySendWhatsAppJobUpdateAsync(
        string phone,
        string orderNumber,
        string status,
        string? appointmentDate,
        string? installerName,
        CustomerPreference customerPref,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _whatsAppMessagingService.SendJobUpdateAsync(
                phone,
                orderNumber,
                status,
                appointmentDate,
                installerName,
                cancellationToken);

            if (result.Success)
            {
                customerPref.RecordWhatsAppSuccess();
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            else
            {
                customerPref.RecordWhatsAppFailure();
                await _context.SaveChangesAsync(cancellationToken);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp job update to {Phone}", phone);
            customerPref.RecordWhatsAppFailure();
            await _context.SaveChangesAsync(cancellationToken);
            return false;
        }
    }

    private async Task<bool> TrySendWhatsAppSiOnTheWayAsync(
        string phone,
        string orderNumber,
        string installerName,
        string? estimatedArrival,
        CustomerPreference customerPref,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _whatsAppMessagingService.SendSiOnTheWayAlertAsync(
                phone,
                orderNumber,
                installerName,
                estimatedArrival,
                cancellationToken);

            if (result.Success)
            {
                customerPref.RecordWhatsAppSuccess();
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            else
            {
                customerPref.RecordWhatsAppFailure();
                await _context.SaveChangesAsync(cancellationToken);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp SI alert to {Phone}", phone);
            customerPref.RecordWhatsAppFailure();
            await _context.SaveChangesAsync(cancellationToken);
            return false;
        }
    }

    private async Task<bool> TrySendWhatsAppTtktAsync(
        string phone,
        string ticketNumber,
        string issueDescription,
        string? resolution,
        CustomerPreference customerPref,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _whatsAppMessagingService.SendTtktNotificationAsync(
                phone,
                ticketNumber,
                issueDescription,
                resolution,
                cancellationToken);

            if (result.Success)
            {
                customerPref.RecordWhatsAppSuccess();
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            else
            {
                customerPref.RecordWhatsAppFailure();
                await _context.SaveChangesAsync(cancellationToken);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp TTKT to {Phone}", phone);
            customerPref.RecordWhatsAppFailure();
            await _context.SaveChangesAsync(cancellationToken);
            return false;
        }
    }

    private async Task UpdateCustomerPreferenceAsync(CustomerPreference preference, bool whatsAppSuccess, CancellationToken cancellationToken)
    {
        if (whatsAppSuccess)
        {
            preference.RecordWhatsAppSuccess();
        }
        else
        {
            preference.RecordWhatsAppFailure();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Remove all non-digit characters except leading +
        var cleaned = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        // If it starts with +, keep it; otherwise assume it's already in E.164 format
        if (!cleaned.StartsWith("+"))
        {
            // If it starts with country code (e.g., 60 for Malaysia), add +
            if (cleaned.Length >= 10)
            {
                cleaned = "+" + cleaned;
            }
        }

        return cleaned;
    }
}

