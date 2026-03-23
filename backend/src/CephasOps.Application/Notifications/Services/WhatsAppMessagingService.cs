using CephasOps.Domain.Notifications;
using CephasOps.Infrastructure.Services.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WhatsAppTemplateParameter = CephasOps.Infrastructure.Services.External.WhatsAppTemplateParameter;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// WhatsApp messaging service implementation
/// Handles template messages with dynamic parameters for various notification scenarios
/// </summary>
public class WhatsAppMessagingService : IWhatsAppMessagingService
{
    private readonly WhatsAppProviderFactory _whatsAppProviderFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppMessagingService> _logger;

    public WhatsAppMessagingService(
        WhatsAppProviderFactory whatsAppProviderFactory,
        IConfiguration configuration,
        ILogger<WhatsAppMessagingService> logger)
    {
        _whatsAppProviderFactory = whatsAppProviderFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WhatsAppResult> SendTemplateMessageAsync(
        string to,
        string templateName,
        Dictionary<string, string>? parameters = null,
        string? languageCode = "en",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending WhatsApp template message '{TemplateName}' to {To}", templateName, to);

        // Convert dictionary parameters to WhatsApp template parameter format
        List<WhatsAppTemplateParameter>? templateParams = null;
        if (parameters != null && parameters.Count > 0)
        {
            templateParams = parameters.Values
                .Select(value => new WhatsAppTemplateParameter { Value = value, Type = "text" })
                .ToList();
        }

        var provider = await _whatsAppProviderFactory.CreateProviderAsync(cancellationToken);
        
        // Check if provider supports template messages (WhatsAppCloudApiProvider)
        if (provider is WhatsAppCloudApiProvider cloudProvider)
        {
            return await cloudProvider.SendTemplateMessageAsync(
                to,
                templateName,
                languageCode,
                templateParams,
                cancellationToken
            );
        }
        
        // Fallback: convert template to plain message for providers that don't support templates
        _logger.LogWarning("Provider {ProviderType} does not support template messages, sending as plain text", provider.GetType().Name);
        var message = $"Template: {templateName}";
        if (templateParams != null && templateParams.Count > 0)
        {
            message += " - " + string.Join(", ", templateParams.Select(p => p.Value));
        }
        return await provider.SendMessageAsync(to, message, cancellationToken);
    }

    public async Task<WhatsAppResult> SendJobUpdateAsync(
        string customerPhone,
        string orderNumber,
        string status,
        string? appointmentDate = null,
        string? installerName = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending job update WhatsApp to {Phone} for order {OrderNumber}", customerPhone, orderNumber);

        // Get template name from config or use default
        var templateName = _configuration["WhatsAppCloudApi:Templates:JobUpdate"] ?? "job_update";

        // Build parameters based on template structure
        // Template typically has: order_number, status, appointment_date, installer_name
        var parameters = new Dictionary<string, string>
        {
            { "order_number", orderNumber },
            { "status", status }
        };

        if (!string.IsNullOrEmpty(appointmentDate))
        {
            parameters["appointment_date"] = appointmentDate;
        }

        if (!string.IsNullOrEmpty(installerName))
        {
            parameters["installer_name"] = installerName;
        }

        // Convert to ordered list for template (order matters in WhatsApp templates)
        var templateParams = new List<WhatsAppTemplateParameter>();
        if (parameters.ContainsKey("order_number"))
            templateParams.Add(new WhatsAppTemplateParameter { Value = parameters["order_number"] });
        if (parameters.ContainsKey("status"))
            templateParams.Add(new WhatsAppTemplateParameter { Value = parameters["status"] });
        if (parameters.ContainsKey("appointment_date"))
            templateParams.Add(new WhatsAppTemplateParameter { Value = parameters["appointment_date"] });
        if (parameters.ContainsKey("installer_name"))
            templateParams.Add(new WhatsAppTemplateParameter { Value = parameters["installer_name"] });

        var provider = await _whatsAppProviderFactory.CreateProviderAsync(cancellationToken);
        
        if (provider is WhatsAppCloudApiProvider cloudProvider)
        {
            return await cloudProvider.SendTemplateMessageAsync(
                customerPhone,
                templateName,
                "en",
                templateParams,
                cancellationToken
            );
        }
        
        _logger.LogWarning("Provider {ProviderType} does not support template messages, sending as plain text", provider.GetType().Name);
        var message = $"Job Update: Order {orderNumber} - Status: {status}";
        if (!string.IsNullOrEmpty(appointmentDate)) message += $" - Appointment: {appointmentDate}";
        if (!string.IsNullOrEmpty(installerName)) message += $" - Installer: {installerName}";
        return await provider.SendMessageAsync(customerPhone, message, cancellationToken);
    }

    public async Task<WhatsAppResult> SendSiOnTheWayAlertAsync(
        string customerPhone,
        string orderNumber,
        string installerName,
        string? estimatedArrival = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending SI on-the-way alert WhatsApp to {Phone} for order {OrderNumber}", 
            customerPhone, orderNumber);

        var templateName = _configuration["WhatsAppCloudApi:Templates:SiOnTheWay"] ?? "si_on_the_way";

        var templateParams = new List<WhatsAppTemplateParameter>
        {
            new() { Value = orderNumber },
            new() { Value = installerName }
        };

        if (!string.IsNullOrEmpty(estimatedArrival))
        {
            templateParams.Add(new WhatsAppTemplateParameter { Value = estimatedArrival });
        }

        var provider = await _whatsAppProviderFactory.CreateProviderAsync(cancellationToken);
        
        if (provider is WhatsAppCloudApiProvider cloudProvider)
        {
            return await cloudProvider.SendTemplateMessageAsync(
                customerPhone,
                templateName,
                "en",
                templateParams,
                cancellationToken
            );
        }
        
        _logger.LogWarning("Provider {ProviderType} does not support template messages, sending as plain text", provider.GetType().Name);
        var message = $"SI Alert: Order {orderNumber} - Installer: {installerName}";
        if (!string.IsNullOrEmpty(estimatedArrival)) message += $" - ETA: {estimatedArrival}";
        return await provider.SendMessageAsync(customerPhone, message, cancellationToken);
    }

    public async Task<WhatsAppResult> SendTtktNotificationAsync(
        string customerPhone,
        string ticketNumber,
        string issueDescription,
        string? resolution = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending TTKT notification WhatsApp to {Phone} for ticket {TicketNumber}", 
            customerPhone, ticketNumber);

        var templateName = _configuration["WhatsAppCloudApi:Templates:Ttkt"] ?? "ttkt_notification";

        var templateParams = new List<WhatsAppTemplateParameter>
        {
            new() { Value = ticketNumber },
            new() { Value = issueDescription }
        };

        if (!string.IsNullOrEmpty(resolution))
        {
            templateParams.Add(new WhatsAppTemplateParameter { Value = resolution });
        }

        var provider = await _whatsAppProviderFactory.CreateProviderAsync(cancellationToken);
        
        if (provider is WhatsAppCloudApiProvider cloudProvider)
        {
            return await cloudProvider.SendTemplateMessageAsync(
                customerPhone,
                templateName,
                "en",
                templateParams,
                cancellationToken
            );
        }
        
        _logger.LogWarning("Provider {ProviderType} does not support template messages, sending as plain text", provider.GetType().Name);
        var message = $"TTKT: Ticket {ticketNumber} - Issue: {issueDescription}";
        if (!string.IsNullOrEmpty(resolution)) message += $" - Resolution: {resolution}";
        return await provider.SendMessageAsync(customerPhone, message, cancellationToken);
    }
}

