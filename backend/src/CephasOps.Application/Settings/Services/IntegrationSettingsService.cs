using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Notifications.Services;
using CephasOps.Domain.Billing;
using CephasOps.Domain.Notifications;
using CephasOps.Domain.Settings;
using CephasOps.Infrastructure.Services.External;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Service for managing integration settings
/// </summary>
public class IntegrationSettingsService : IIntegrationSettingsService
{
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly EInvoiceProviderFactory _eInvoiceProviderFactory;
    private readonly SmsProviderFactory _smsProviderFactory;
    private readonly WhatsAppProviderFactory _whatsAppProviderFactory;
    private readonly ILogger<IntegrationSettingsService> _logger;

    public IntegrationSettingsService(
        IGlobalSettingsService globalSettingsService,
        EInvoiceProviderFactory eInvoiceProviderFactory,
        SmsProviderFactory smsProviderFactory,
        WhatsAppProviderFactory whatsAppProviderFactory,
        ILogger<IntegrationSettingsService> logger)
    {
        _globalSettingsService = globalSettingsService;
        _eInvoiceProviderFactory = eInvoiceProviderFactory;
        _smsProviderFactory = smsProviderFactory;
        _whatsAppProviderFactory = whatsAppProviderFactory;
        _logger = logger;
    }

    public async Task<IntegrationSettingsDto> GetIntegrationSettingsAsync(CancellationToken cancellationToken = default)
    {
        return new IntegrationSettingsDto
        {
            MyInvois = new MyInvoisSettingsDto
            {
                IsEnabled = await _globalSettingsService.GetValueAsync<bool>("MyInvois_Enabled", cancellationToken),
                BaseUrl = await _globalSettingsService.GetValueAsync<string>("MyInvois_BaseUrl", cancellationToken) 
                    ?? "https://api-sandbox.myinvois.hasil.gov.my",
                ClientId = await _globalSettingsService.GetValueAsync<string>("MyInvois_ClientId", cancellationToken) ?? string.Empty,
                ClientSecret = await _globalSettingsService.GetValueAsync<string>("MyInvois_ClientSecret", cancellationToken) ?? string.Empty,
                Environment = await _globalSettingsService.GetValueAsync<string>("MyInvois_Environment", cancellationToken) ?? "Sandbox"
            },
            Sms = new SmsSettingsDto
            {
                IsEnabled = await _globalSettingsService.GetValueAsync<bool>("SMS_Enabled", cancellationToken),
                Provider = await _globalSettingsService.GetValueAsync<string>("SMS_Provider", cancellationToken) ?? "None",
                TwilioAccountSid = await _globalSettingsService.GetValueAsync<string>("SMS_Twilio_AccountSid", cancellationToken),
                TwilioAuthToken = await _globalSettingsService.GetValueAsync<string>("SMS_Twilio_AuthToken", cancellationToken),
                TwilioFromNumber = await _globalSettingsService.GetValueAsync<string>("SMS_Twilio_FromNumber", cancellationToken),
                GatewayUrl = await _globalSettingsService.GetValueAsync<string>("SMS_Gateway_Url", cancellationToken),
                GatewayApiKey = await _globalSettingsService.GetValueAsync<string>("SMS_Gateway_ApiKey", cancellationToken),
                GatewaySenderId = await _globalSettingsService.GetValueAsync<string>("SMS_Gateway_SenderId", cancellationToken)
            },
            WhatsApp = new WhatsAppSettingsDto
            {
                IsEnabled = await _globalSettingsService.GetValueAsync<bool>("WhatsApp_Enabled", cancellationToken),
                Provider = await _globalSettingsService.GetValueAsync<string>("WhatsApp_Provider", cancellationToken) ?? "CloudApi",
                PhoneNumberId = await _globalSettingsService.GetValueAsync<string>("WhatsApp_PhoneNumberId", cancellationToken),
                AccessToken = await _globalSettingsService.GetValueAsync<string>("WhatsApp_AccessToken", cancellationToken),
                BusinessAccountId = await _globalSettingsService.GetValueAsync<string>("WhatsApp_BusinessAccountId", cancellationToken),
                ApiVersion = await _globalSettingsService.GetValueAsync<string>("WhatsApp_ApiVersion", cancellationToken) ?? "v18.0",
                JobUpdateTemplate = await _globalSettingsService.GetValueAsync<string>("WhatsApp_Template_JobUpdate", cancellationToken),
                SiOnTheWayTemplate = await _globalSettingsService.GetValueAsync<string>("WhatsApp_Template_SiOnTheWay", cancellationToken),
                TtktTemplate = await _globalSettingsService.GetValueAsync<string>("WhatsApp_Template_Ttkt", cancellationToken)
            }
        };
    }

    public async Task UpdateMyInvoisSettingsAsync(MyInvoisSettingsDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        
        await UpdateOrCreateSettingAsync("MyInvois_Enabled", dto.IsEnabled.ToString(), "Bool", userId, cancellationToken);
        await UpdateOrCreateSettingAsync("MyInvois_BaseUrl", dto.BaseUrl, "String", userId, cancellationToken);
        await UpdateOrCreateSettingAsync("MyInvois_ClientId", dto.ClientId, "String", userId, cancellationToken);
        await UpdateOrCreateSettingAsync("MyInvois_ClientSecret", dto.ClientSecret, "String", userId, cancellationToken);
        await UpdateOrCreateSettingAsync("MyInvois_Environment", dto.Environment ?? "Sandbox", "String", userId, cancellationToken);
        await UpdateOrCreateSettingAsync("EInvoice_Provider", "MyInvois", "String", userId, cancellationToken);
        await UpdateOrCreateSettingAsync("EInvoice_Enabled", dto.IsEnabled.ToString(), "Bool", userId, cancellationToken);

        _logger.LogInformation("MyInvois settings updated. Enabled: {Enabled}", dto.IsEnabled);
    }

    public async Task UpdateSmsSettingsAsync(SmsSettingsDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        
        await UpdateOrCreateSettingAsync("SMS_Enabled", dto.IsEnabled.ToString(), "Bool", userId, cancellationToken);
        await UpdateOrCreateSettingAsync("SMS_Provider", dto.Provider, "String", userId, cancellationToken);
        
        if (!string.IsNullOrEmpty(dto.TwilioAccountSid))
            await UpdateOrCreateSettingAsync("SMS_Twilio_AccountSid", dto.TwilioAccountSid, "String", userId, cancellationToken);
        if (!string.IsNullOrEmpty(dto.TwilioAuthToken))
            await UpdateOrCreateSettingAsync("SMS_Twilio_AuthToken", dto.TwilioAuthToken, "String", userId, cancellationToken);
        if (!string.IsNullOrEmpty(dto.TwilioFromNumber))
            await UpdateOrCreateSettingAsync("SMS_Twilio_FromNumber", dto.TwilioFromNumber, "String", userId, cancellationToken);
        
        if (!string.IsNullOrEmpty(dto.GatewayUrl))
            await UpdateOrCreateSettingAsync("SMS_Gateway_Url", dto.GatewayUrl, "String", userId, cancellationToken);
        if (!string.IsNullOrEmpty(dto.GatewayApiKey))
            await UpdateOrCreateSettingAsync("SMS_Gateway_ApiKey", dto.GatewayApiKey, "String", userId, cancellationToken);
        if (!string.IsNullOrEmpty(dto.GatewaySenderId))
            await UpdateOrCreateSettingAsync("SMS_Gateway_SenderId", dto.GatewaySenderId, "String", userId, cancellationToken);

        _logger.LogInformation("SMS settings updated. Enabled: {Enabled}, Provider: {Provider}", dto.IsEnabled, dto.Provider);
    }

    public async Task UpdateWhatsAppSettingsAsync(WhatsAppSettingsDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        
        await UpdateOrCreateSettingAsync("WhatsApp_Enabled", dto.IsEnabled.ToString(), "Bool", userId, cancellationToken);
        await UpdateOrCreateSettingAsync("WhatsApp_Provider", dto.Provider, "String", userId, cancellationToken);
        
        if (!string.IsNullOrEmpty(dto.PhoneNumberId))
            await UpdateOrCreateSettingAsync("WhatsApp_PhoneNumberId", dto.PhoneNumberId, "String", userId, cancellationToken);
        if (!string.IsNullOrEmpty(dto.AccessToken))
            await UpdateOrCreateSettingAsync("WhatsApp_AccessToken", dto.AccessToken, "String", userId, cancellationToken);
        if (!string.IsNullOrEmpty(dto.BusinessAccountId))
            await UpdateOrCreateSettingAsync("WhatsApp_BusinessAccountId", dto.BusinessAccountId, "String", userId, cancellationToken);
        if (!string.IsNullOrEmpty(dto.ApiVersion))
            await UpdateOrCreateSettingAsync("WhatsApp_ApiVersion", dto.ApiVersion, "String", userId, cancellationToken);
        
        if (!string.IsNullOrEmpty(dto.JobUpdateTemplate))
            await UpdateOrCreateSettingAsync("WhatsApp_Template_JobUpdate", dto.JobUpdateTemplate, "String", userId, cancellationToken);
        if (!string.IsNullOrEmpty(dto.SiOnTheWayTemplate))
            await UpdateOrCreateSettingAsync("WhatsApp_Template_SiOnTheWay", dto.SiOnTheWayTemplate, "String", userId, cancellationToken);
        if (!string.IsNullOrEmpty(dto.TtktTemplate))
            await UpdateOrCreateSettingAsync("WhatsApp_Template_Ttkt", dto.TtktTemplate, "String", userId, cancellationToken);

        _logger.LogInformation("WhatsApp settings updated. Enabled: {Enabled}, Provider: {Provider}", dto.IsEnabled, dto.Provider);
    }

    public async Task<bool> TestMyInvoisConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _eInvoiceProviderFactory.GetProviderAsync(cancellationToken);
            if (provider is NullEInvoiceProvider)
            {
                return false;
            }

            // Try to get access token (basic connectivity test)
            var settings = await GetIntegrationSettingsAsync(cancellationToken);
            if (!settings.MyInvois.IsEnabled || string.IsNullOrEmpty(settings.MyInvois.ClientId))
            {
                return false;
            }

            // For now, just check if credentials are set
            // Full connection test would require actual API call
            return !string.IsNullOrEmpty(settings.MyInvois.ClientId) && 
                   !string.IsNullOrEmpty(settings.MyInvois.ClientSecret);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing MyInvois connection");
            return false;
        }
    }

    public async Task<bool> TestSmsConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _smsProviderFactory.CreateProviderAsync(cancellationToken);
            if (provider is NullSmsProvider)
            {
                return false;
            }

            // For now, just check if provider is configured
            // Full connection test would require actual SMS send
            var settings = await GetIntegrationSettingsAsync(cancellationToken);
            if (!settings.Sms.IsEnabled)
            {
                return false;
            }

            return settings.Sms.Provider switch
            {
                "Twilio" => !string.IsNullOrEmpty(settings.Sms.TwilioAccountSid) && 
                           !string.IsNullOrEmpty(settings.Sms.TwilioAuthToken),
                "SMS_Gateway" or "Gateway" => !string.IsNullOrEmpty(settings.Sms.GatewayUrl) && 
                                             !string.IsNullOrEmpty(settings.Sms.GatewayApiKey),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMS connection");
            return false;
        }
    }

    public async Task<bool> TestWhatsAppConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _whatsAppProviderFactory.CreateProviderAsync(cancellationToken);
            if (provider is NullWhatsAppProvider)
            {
                return false;
            }

            // For now, just check if credentials are set
            // Full connection test would require actual API call
            var settings = await GetIntegrationSettingsAsync(cancellationToken);
            if (!settings.WhatsApp.IsEnabled)
            {
                return false;
            }

            return settings.WhatsApp.Provider switch
            {
                "CloudApi" => !string.IsNullOrEmpty(settings.WhatsApp.PhoneNumberId) && 
                             !string.IsNullOrEmpty(settings.WhatsApp.AccessToken),
                "Twilio" => true, // Twilio WhatsApp uses same credentials as SMS
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing WhatsApp connection");
            return false;
        }
    }

    private async Task UpdateOrCreateSettingAsync(string key, string value, string valueType, Guid userId, CancellationToken cancellationToken)
    {
        var existing = await _globalSettingsService.GetByKeyAsync(key, cancellationToken);
        
        if (existing != null)
        {
            await _globalSettingsService.UpdateAsync(key, new UpdateGlobalSettingDto
            {
                Value = value,
                Description = existing.Description
            }, userId, cancellationToken);
        }
        else
        {
            await _globalSettingsService.CreateAsync(new CreateGlobalSettingDto
            {
                Key = key,
                Value = value,
                ValueType = valueType,
                Description = $"Integration setting: {key}",
                Module = "Integration"
            }, userId, cancellationToken);
        }
    }
}

