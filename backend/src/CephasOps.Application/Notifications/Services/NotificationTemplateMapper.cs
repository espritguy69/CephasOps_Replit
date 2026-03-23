using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Orders.Enums;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Maps order status to template codes for SMS/WhatsApp notifications
/// </summary>
public static class NotificationTemplateMapper
{
    /// <summary>
    /// Map order status to SMS template code
    /// </summary>
    public static string? GetSmsTemplateCode(string orderStatus)
    {
        return orderStatus switch
        {
            OrderStatus.Assigned => "ASSIGNED",
            OrderStatus.OnTheWay => "OTW",
            OrderStatus.MetCustomer => "MET_CUSTOMER",
            OrderStatus.OrderCompleted => "IN_PROGRESS",
            OrderStatus.Completed => "COMPLETED",
            OrderStatus.Cancelled => "CANCELLED",
            OrderStatus.ReschedulePendingApproval => "RESCHEDULED",
            OrderStatus.Blocker => "BLOCKER",
            _ => null
        };
    }

    /// <summary>
    /// Map order status to WhatsApp template code
    /// </summary>
    public static string? GetWhatsAppTemplateCode(string orderStatus)
    {
        return orderStatus switch
        {
            OrderStatus.Assigned => "ASSIGNED",
            OrderStatus.OnTheWay => "OTW",
            OrderStatus.MetCustomer => "MET_CUSTOMER",
            OrderStatus.OrderCompleted => "IN_PROGRESS",
            OrderStatus.Completed => "COMPLETED",
            OrderStatus.Cancelled => "CANCELLED",
            OrderStatus.ReschedulePendingApproval => "RESCHEDULED",
            OrderStatus.Blocker => "BLOCKER",
            _ => null
        };
    }

    /// <summary>
    /// Get template code from GlobalSettings override (if exists)
    /// Falls back to default mapping if not configured
    /// </summary>
    public static async Task<string?> GetSmsTemplateCodeAsync(
        string orderStatus,
        IGlobalSettingsService globalSettingsService,
        CancellationToken cancellationToken = default)
    {
        // Try GlobalSettings override first
        var settingKey = $"Notification_{orderStatus}_SmsTemplateCode";
        var overrideCode = await globalSettingsService.GetValueAsync<string>(settingKey, cancellationToken);
        if (!string.IsNullOrEmpty(overrideCode))
        {
            return overrideCode;
        }

        // Fall back to default mapping
        return GetSmsTemplateCode(orderStatus);
    }

    /// <summary>
    /// Get template code from GlobalSettings override (if exists)
    /// Falls back to default mapping if not configured
    /// </summary>
    public static async Task<string?> GetWhatsAppTemplateCodeAsync(
        string orderStatus,
        IGlobalSettingsService globalSettingsService,
        CancellationToken cancellationToken = default)
    {
        // Try GlobalSettings override first
        var settingKey = $"Notification_{orderStatus}_WhatsAppTemplateCode";
        var overrideCode = await globalSettingsService.GetValueAsync<string>(settingKey, cancellationToken);
        if (!string.IsNullOrEmpty(overrideCode))
        {
            return overrideCode;
        }

        // Fall back to default mapping
        return GetWhatsAppTemplateCode(orderStatus);
    }
}

