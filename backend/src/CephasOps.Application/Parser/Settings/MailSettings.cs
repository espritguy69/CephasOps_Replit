namespace CephasOps.Application.Parser.Settings;

/// <summary>
/// Configuration settings for Mail Viewer / Mail Storage lifecycle
/// </summary>
public class MailSettings
{
    public const string SectionName = "Mail";

    /// <summary>
    /// Email retention period in hours (default: 48 hours)
    /// </summary>
    public int RetentionHours { get; set; } = 48;

    /// <summary>
    /// Block external images by default for privacy/tracking (default: true)
    /// </summary>
    public bool BlockExternalImages { get; set; } = true;

    /// <summary>
    /// Auto-load CID (inline) images (default: false)
    /// </summary>
    public bool AutoLoadCidImages { get; set; } = false;

    /// <summary>
    /// Cleanup job configuration
    /// </summary>
    public CleanupJobSettings CleanupJob { get; set; } = new();

    /// <summary>
    /// Download URL configuration
    /// </summary>
    public DownloadUrlSettings DownloadUrl { get; set; } = new();
}

public class CleanupJobSettings
{
    /// <summary>
    /// Cleanup job interval in minutes (default: 60 minutes = 1 hour)
    /// </summary>
    public int IntervalMinutes { get; set; } = 60;
}

public class DownloadUrlSettings
{
    /// <summary>
    /// Download URL expiry time in minutes (default: 3 minutes)
    /// </summary>
    public int ExpiryMinutes { get; set; } = 3;
}

