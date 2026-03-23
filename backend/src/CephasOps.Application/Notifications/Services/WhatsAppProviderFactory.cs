using CephasOps.Domain.Notifications;
using Microsoft.Extensions.DependencyInjection;
using CephasOps.Application.Settings.Services;
using CephasOps.Infrastructure.Services.External;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Factory for creating WhatsApp provider instances based on GlobalSettings
/// </summary>
public class WhatsAppProviderFactory
{
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WhatsAppProviderFactory> _logger;

    public WhatsAppProviderFactory(
        IGlobalSettingsService globalSettingsService,
        IServiceProvider serviceProvider,
        ILogger<WhatsAppProviderFactory> logger)
    {
        _globalSettingsService = globalSettingsService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Create WhatsApp provider based on GlobalSettings configuration
    /// </summary>
    public async Task<IWhatsAppProvider> CreateProviderAsync(CancellationToken cancellationToken = default)
    {
        var enabled = await _globalSettingsService.GetValueAsync<bool>("WhatsApp_Enabled", cancellationToken);
        if (!enabled)
        {
            _logger.LogInformation("WhatsApp is disabled. Using NullWhatsAppProvider");
            return _serviceProvider.GetRequiredService<NullWhatsAppProvider>();
        }

        var provider = await _globalSettingsService.GetValueAsync<string>("WhatsApp_Provider", cancellationToken);
        
        return provider?.ToLowerInvariant() switch
        {
            "cloudapi" or "cloud_api" or "whatsapp_cloud" => _serviceProvider.GetRequiredService<WhatsAppCloudApiProvider>(),
            "twilio" => _serviceProvider.GetRequiredService<TwilioWhatsAppProvider>(),
            "none" or null => _serviceProvider.GetRequiredService<NullWhatsAppProvider>(),
            _ => throw new InvalidOperationException($"Unknown WhatsApp provider: {provider}. Supported providers: CloudApi, Twilio, None")
        };
    }
}

