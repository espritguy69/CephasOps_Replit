namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// SMS Gateway entity - represents a registered Android SMS Gateway device
/// Only one gateway can be active at a time
/// </summary>
public class SmsGateway
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Device name (e.g., "Cephas Maxis SMS Gateway")
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the SMS Gateway (e.g., "http://192.168.0.50:8080")
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Last time the gateway registered/checked in (UTC)
    /// </summary>
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this gateway is currently active (only one can be active)
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Additional device information (Android device info, etc.)
    /// </summary>
    public string? AdditionalInfo { get; set; }

    /// <summary>
    /// Timestamp when the gateway was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the gateway was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Update gateway information
    /// </summary>
    public void Update(string deviceName, string baseUrl, string apiKey, string? additionalInfo = null)
    {
        DeviceName = deviceName;
        BaseUrl = baseUrl;
        ApiKey = apiKey;
        AdditionalInfo = additionalInfo;
        LastSeenAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate this gateway
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate this gateway and update last seen timestamp
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        LastSeenAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update last seen timestamp (heartbeat)
    /// </summary>
    public void Touch()
    {
        LastSeenAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

