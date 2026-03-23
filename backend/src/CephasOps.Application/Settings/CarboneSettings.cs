namespace CephasOps.Application.Settings;

/// <summary>
/// Configuration settings for Carbone document rendering engine
/// </summary>
public class CarboneSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Carbone";

    /// <summary>
    /// Whether Carbone engine is enabled
    /// When false, any template using CarboneHtml/CarboneDocx will throw an error
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Carbone API base URL
    /// For Carbone Cloud: https://api.carbone.io
    /// For self-hosted: http://localhost:4000 (or your Docker container URL)
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.carbone.io";

    /// <summary>
    /// Carbone API key (required for Carbone Cloud, optional for self-hosted)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// API version (for Carbone Cloud)
    /// </summary>
    public string ApiVersion { get; set; } = "4";

    /// <summary>
    /// Validate configuration
    /// </summary>
    public bool IsValid()
    {
        if (!Enabled)
            return true; // Disabled is always valid

        if (string.IsNullOrWhiteSpace(BaseUrl))
            return false;

        // Carbone Cloud requires API key
        if (BaseUrl.Contains("carbone.io") && string.IsNullOrWhiteSpace(ApiKey))
            return false;

        return true;
    }

    /// <summary>
    /// Get validation error message
    /// </summary>
    public string? GetValidationError()
    {
        if (!Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(BaseUrl))
            return "Carbone BaseUrl is required when Carbone is enabled";

        if (BaseUrl.Contains("carbone.io") && string.IsNullOrWhiteSpace(ApiKey))
            return "Carbone ApiKey is required when using Carbone Cloud (api.carbone.io)";

        return null;
    }
}

