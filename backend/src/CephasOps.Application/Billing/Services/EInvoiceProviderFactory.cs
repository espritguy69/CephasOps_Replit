using CephasOps.Domain.Billing;
using CephasOps.Domain.Settings;
using CephasOps.Infrastructure.Services.External;
using Microsoft.Extensions.DependencyInjection;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Factory for creating e-invoice providers based on GlobalSettings
/// Similar pattern to SmsProviderFactory
/// </summary>
public class EInvoiceProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IGlobalSettingsReader _globalSettings;

    public EInvoiceProviderFactory(
        IServiceProvider serviceProvider,
        IGlobalSettingsReader globalSettings)
    {
        _serviceProvider = serviceProvider;
        _globalSettings = globalSettings;
    }

    /// <summary>
    /// Get the configured e-invoice provider
    /// </summary>
    public async Task<CephasOps.Domain.Billing.IEInvoiceProvider> GetProviderAsync(CancellationToken cancellationToken = default)
    {
        var providerType = await _globalSettings.GetValueAsync<string>("EInvoice_Provider", cancellationToken) ?? "Null";
        var isEnabled = await _globalSettings.GetValueAsync<bool>("EInvoice_Enabled", cancellationToken);

        if (!isEnabled || providerType.Equals("Null", StringComparison.OrdinalIgnoreCase))
        {
            return _serviceProvider.GetRequiredService<NullEInvoiceProvider>();
        }

        return providerType.ToLower() switch
        {
            "myinvois" => _serviceProvider.GetRequiredService<MyInvoisApiProvider>(),
            _ => _serviceProvider.GetRequiredService<NullEInvoiceProvider>()
        };
    }
}

