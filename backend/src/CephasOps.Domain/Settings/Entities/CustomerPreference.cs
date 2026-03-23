namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Customer preference entity - tracks customer communication preferences
/// Stores WhatsApp usage preference per phone number for smart routing
/// </summary>
public class CustomerPreference
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Customer phone number (normalized, E.164 format)
    /// </summary>
    public string CustomerPhone { get; set; } = string.Empty;

    /// <summary>
    /// Whether customer uses WhatsApp (null = unknown, true = yes, false = no)
    /// </summary>
    public bool? UsesWhatsApp { get; set; }

    /// <summary>
    /// Last time we checked/attempted WhatsApp (UTC)
    /// </summary>
    public DateTime? LastWhatsAppCheck { get; set; }

    /// <summary>
    /// Last time WhatsApp message was successfully sent (UTC)
    /// </summary>
    public DateTime? LastWhatsAppSuccess { get; set; }

    /// <summary>
    /// Last time WhatsApp message failed (UTC)
    /// </summary>
    public DateTime? LastWhatsAppFailure { get; set; }

    /// <summary>
    /// Number of consecutive WhatsApp failures
    /// </summary>
    public int ConsecutiveWhatsAppFailures { get; set; } = 0;

    /// <summary>
    /// Preferred communication channel (SMS, WhatsApp, Both)
    /// </summary>
    public string? PreferredChannel { get; set; }

    /// <summary>
    /// Additional notes about customer preferences
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Timestamp when preference was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when preference was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record successful WhatsApp message
    /// </summary>
    public void RecordWhatsAppSuccess()
    {
        UsesWhatsApp = true;
        LastWhatsAppCheck = DateTime.UtcNow;
        LastWhatsAppSuccess = DateTime.UtcNow;
        ConsecutiveWhatsAppFailures = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Record failed WhatsApp message
    /// </summary>
    public void RecordWhatsAppFailure()
    {
        LastWhatsAppCheck = DateTime.UtcNow;
        LastWhatsAppFailure = DateTime.UtcNow;
        ConsecutiveWhatsAppFailures++;

        // After 3 consecutive failures, mark as doesn't use WhatsApp
        if (ConsecutiveWhatsAppFailures >= 3)
        {
            UsesWhatsApp = false;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Manually set WhatsApp preference
    /// </summary>
    public void SetWhatsAppPreference(bool usesWhatsApp, string? notes = null)
    {
        UsesWhatsApp = usesWhatsApp;
        Notes = notes;
        ConsecutiveWhatsAppFailures = 0;
        UpdatedAt = DateTime.UtcNow;
    }
}

