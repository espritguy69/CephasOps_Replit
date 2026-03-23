using CephasOps.Domain.Settings;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Service for currency exchange rate conversion
/// Used for MyInvois v1.1 compliance when invoices are in foreign currency
/// </summary>
public class CurrencyExchangeService : ICurrencyExchangeService
{
    private readonly IGlobalSettingsReader _globalSettings;
    private readonly ILogger<CurrencyExchangeService> _logger;

    public CurrencyExchangeService(
        IGlobalSettingsReader globalSettings,
        ILogger<CurrencyExchangeService> logger)
    {
        _globalSettings = globalSettings;
        _logger = logger;
    }

    public async Task<decimal?> GetExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        // If same currency, return 1.0
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0m;
        }

        // If converting to MYR, try to get rate
        if (toCurrency.Equals("MYR", StringComparison.OrdinalIgnoreCase))
        {
            // Check if exchange rate is configured in GlobalSettings
            var rateKey = $"CurrencyExchange_{fromCurrency}_MYR";
            var configuredRate = await _globalSettings.GetValueAsync<decimal?>(rateKey, cancellationToken);

            if (configuredRate.HasValue)
            {
                _logger.LogInformation("Using configured exchange rate {FromCurrency} to MYR: {Rate}", fromCurrency, configuredRate.Value);
                return configuredRate.Value;
            }

            // TODO: In future, integrate with external exchange rate API (e.g., Bank Negara Malaysia API)
            _logger.LogWarning("Exchange rate not configured for {FromCurrency} to MYR. Using 1.0 as fallback.", fromCurrency);
            return 1.0m; // Fallback
        }

        // For other conversions, would need intermediate conversion
        _logger.LogWarning("Currency conversion from {FromCurrency} to {ToCurrency} not fully supported", fromCurrency, toCurrency);
        return 1.0m; // Fallback
    }

    public async Task<decimal> ConvertAmountAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var rate = await GetExchangeRateAsync(fromCurrency, toCurrency, date, cancellationToken);
        return amount * (rate ?? 1.0m);
    }
}

/// <summary>
/// Interface for currency exchange service
/// </summary>
public interface ICurrencyExchangeService
{
    /// <summary>
    /// Get exchange rate between two currencies
    /// </summary>
    Task<decimal?> GetExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        DateTime? date = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert amount from one currency to another
    /// </summary>
    Task<decimal> ConvertAmountAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        DateTime? date = null,
        CancellationToken cancellationToken = default);
}

