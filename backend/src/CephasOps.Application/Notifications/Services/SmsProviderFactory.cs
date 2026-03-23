using CephasOps.Domain.Notifications;
using CephasOps.Application.Settings.Services;
using CephasOps.Infrastructure.Services.External;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Notifications.Services;

/// <summary>
/// Factory for creating SMS provider instances based on GlobalSettings
/// </summary>
public class SmsProviderFactory
{
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SmsProviderFactory> _logger;

    public SmsProviderFactory(
        IGlobalSettingsService globalSettingsService,
        IServiceProvider serviceProvider,
        ILogger<SmsProviderFactory> logger)
    {
        _globalSettingsService = globalSettingsService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Create SMS provider based on GlobalSettings configuration
    /// </summary>
    public async Task<ISmsProvider> CreateProviderAsync(CancellationToken cancellationToken = default)
    {
        var enabled = await _globalSettingsService.GetValueAsync<bool>("SMS_Enabled", cancellationToken);
        if (!enabled)
        {
            _logger.LogInformation("SMS is disabled. Using NullSmsProvider");
            return _serviceProvider.GetRequiredService<NullSmsProvider>();
        }

        var provider = await _globalSettingsService.GetValueAsync<string>("SMS_Provider", cancellationToken);
        
        return provider?.ToLowerInvariant() switch
        {
            "twilio" => _serviceProvider.GetRequiredService<TwilioSmsProvider>(),
            "sms_gateway" or "gateway" => _serviceProvider.GetRequiredService<SmsGatewaySender>(),
            "none" or null => _serviceProvider.GetRequiredService<NullSmsProvider>(),
            _ => throw new InvalidOperationException($"Unknown SMS provider: {provider}. Supported providers: Twilio, SMS_Gateway, None")
        };
    }
}

