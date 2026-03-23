namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// Integration settings DTO
/// </summary>
public class IntegrationSettingsDto
{
    public MyInvoisSettingsDto MyInvois { get; set; } = new();
    public SmsSettingsDto Sms { get; set; } = new();
    public WhatsAppSettingsDto WhatsApp { get; set; } = new();
}

/// <summary>
/// MyInvois settings DTO
/// </summary>
public class MyInvoisSettingsDto
{
    public bool IsEnabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? Environment { get; set; } // Sandbox, Production
}

/// <summary>
/// SMS settings DTO
/// </summary>
public class SmsSettingsDto
{
    public bool IsEnabled { get; set; }
    public string Provider { get; set; } = string.Empty; // Twilio, SMS_Gateway, None
    
    // Twilio settings
    public string? TwilioAccountSid { get; set; }
    public string? TwilioAuthToken { get; set; }
    public string? TwilioFromNumber { get; set; }
    
    // SMS Gateway settings
    public string? GatewayUrl { get; set; }
    public string? GatewayApiKey { get; set; }
    public string? GatewaySenderId { get; set; }
}

/// <summary>
/// WhatsApp settings DTO
/// </summary>
public class WhatsAppSettingsDto
{
    public bool IsEnabled { get; set; }
    public string Provider { get; set; } = string.Empty; // CloudApi, Twilio, None
    
    // WhatsApp Cloud API settings
    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string? BusinessAccountId { get; set; }
    public string? ApiVersion { get; set; }
    
    // Template names
    public string? JobUpdateTemplate { get; set; }
    public string? SiOnTheWayTemplate { get; set; }
    public string? TtktTemplate { get; set; }
}

