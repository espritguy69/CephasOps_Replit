namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// SMS Gateway DTO
/// </summary>
public class SmsGatewayDto
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public DateTime LastSeenAtUtc { get; set; }
    public bool IsActive { get; set; }
    public string? AdditionalInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Register SMS Gateway request DTO
/// </summary>
public class RegisterSmsGatewayRequest
{
    public string DeviceName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? AdditionalInfo { get; set; }
}

