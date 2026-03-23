using CephasOps.Domain.Notifications;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Notifications.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Creates NotificationDispatch records for order status changes (Phase 2).
/// Idempotent; worker performs actual delivery.
/// </summary>
public class NotificationDispatchRequestService : INotificationDispatchRequestService
{
    private readonly IOrderService _orderService;
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly INotificationDispatchStore _dispatchStore;
    private readonly ILogger<NotificationDispatchRequestService> _logger;

    public NotificationDispatchRequestService(
        IOrderService orderService,
        IGlobalSettingsService globalSettingsService,
        INotificationDispatchStore dispatchStore,
        ILogger<NotificationDispatchRequestService> logger)
    {
        _orderService = orderService;
        _globalSettingsService = globalSettingsService;
        _dispatchStore = dispatchStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RequestOrderStatusNotificationAsync(
        Guid orderId,
        string newStatus,
        Guid? companyId,
        Guid sourceEventId,
        string? correlationId,
        Guid? causationId,
        CancellationToken cancellationToken = default)
    {
        var smsAutoSend = await _globalSettingsService.GetValueAsync<bool>("SMS_AutoSendOnStatusChange", cancellationToken);
        var whatsAppAutoSend = await _globalSettingsService.GetValueAsync<bool>("WhatsApp_AutoSendOnStatusChange", cancellationToken);
        if (!smsAutoSend && !whatsAppAutoSend)
        {
            _logger.LogDebug("Auto-send notifications disabled; skipping dispatch request for order {OrderId}", orderId);
            return;
        }

        OrderDto? order = null;
        if (companyId.HasValue)
            order = await _orderService.GetOrderByIdAsync(orderId, companyId.Value, null, cancellationToken);
        else
            order = await _orderService.GetOrderByIdAsync(orderId, null, null, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found; cannot request notification dispatch.", orderId);
            return;
        }

        if (string.IsNullOrEmpty(order.CustomerPhone))
        {
            _logger.LogWarning("Order {OrderId} has no customer phone; skipping dispatch.", orderId);
            return;
        }

        var effectiveCompanyId = order.CompanyId ?? companyId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
        {
            _logger.LogWarning("Order {OrderId} has no CompanyId; skipping notification dispatch (tenant-boundary).", orderId);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(BuildPlaceholders(order));

        if (smsAutoSend)
        {
            var smsTemplateCode = await NotificationTemplateMapper.GetSmsTemplateCodeAsync(newStatus, _globalSettingsService, cancellationToken);
            if (!string.IsNullOrEmpty(smsTemplateCode))
            {
                // Tenant-safe: include CompanyId so the same key in different tenants does not collide
                var idempotencyKey = (effectiveCompanyId.HasValue && effectiveCompanyId.Value != Guid.Empty)
                    ? $"{effectiveCompanyId.Value:N}:{sourceEventId}:Sms:{order.CustomerPhone}"
                    : $"{sourceEventId}:Sms:{order.CustomerPhone}";
                if (!await _dispatchStore.ExistsByIdempotencyKeyAsync(idempotencyKey, cancellationToken))
                {
                    var dispatch = new NotificationDispatch
                    {
                        CompanyId = effectiveCompanyId,
                        Channel = "Sms",
                        Target = order.CustomerPhone,
                        TemplateKey = smsTemplateCode,
                        PayloadJson = payloadJson,
                        Status = "Pending",
                        MaxAttempts = 5,
                        CorrelationId = correlationId,
                        CausationId = causationId,
                        SourceEventId = sourceEventId,
                        IdempotencyKey = idempotencyKey
                    };
                    await _dispatchStore.AddAsync(dispatch, cancellationToken);
                    _logger.LogInformation("Enqueued Sms dispatch for order {OrderId}, status {Status}, dispatch {DispatchId}", orderId, newStatus, dispatch.Id);
                }
            }
        }

        if (whatsAppAutoSend)
        {
            var whatsAppTemplateCode = await NotificationTemplateMapper.GetWhatsAppTemplateCodeAsync(newStatus, _globalSettingsService, cancellationToken);
            if (!string.IsNullOrEmpty(whatsAppTemplateCode))
            {
                // Tenant-safe: include CompanyId so the same key in different tenants does not collide
                var idempotencyKey = (effectiveCompanyId.HasValue && effectiveCompanyId.Value != Guid.Empty)
                    ? $"{effectiveCompanyId.Value:N}:{sourceEventId}:WhatsApp:{order.CustomerPhone}"
                    : $"{sourceEventId}:WhatsApp:{order.CustomerPhone}";
                if (!await _dispatchStore.ExistsByIdempotencyKeyAsync(idempotencyKey, cancellationToken))
                {
                    var dispatch = new NotificationDispatch
                    {
                        CompanyId = effectiveCompanyId,
                        Channel = "WhatsApp",
                        Target = order.CustomerPhone,
                        TemplateKey = whatsAppTemplateCode,
                        PayloadJson = payloadJson,
                        Status = "Pending",
                        MaxAttempts = 5,
                        CorrelationId = correlationId,
                        CausationId = causationId,
                        SourceEventId = sourceEventId,
                        IdempotencyKey = idempotencyKey
                    };
                    await _dispatchStore.AddAsync(dispatch, cancellationToken);
                    _logger.LogInformation("Enqueued WhatsApp dispatch for order {OrderId}, status {Status}, dispatch {DispatchId}", orderId, newStatus, dispatch.Id);
                }
            }
        }
    }

    private static Dictionary<string, string> BuildPlaceholders(OrderDto order)
    {
        return new Dictionary<string, string>
        {
            { "OrderId", order.Id.ToString() },
            { "CustomerName", order.CustomerName ?? "" },
            { "CustomerPhone", order.CustomerPhone ?? "" },
            { "ServiceId", order.ServiceId ?? "" },
            { "Address", $"{order.AddressLine1} {order.AddressLine2}".Trim() },
            { "City", order.City ?? "" },
            { "State", order.State ?? "" },
            { "Postcode", order.Postcode ?? "" },
            { "AppointmentDate", order.AppointmentDate.ToString("dd/MM/yyyy") },
            { "AppointmentTime", $"{order.AppointmentWindowFrom:hh\\:mm} - {order.AppointmentWindowTo:hh\\:mm}" },
            { "Status", order.Status ?? "" },
            { "BuildingName", order.BuildingName ?? "" },
            { "UnitNo", order.UnitNo ?? "" }
        };
    }
}
