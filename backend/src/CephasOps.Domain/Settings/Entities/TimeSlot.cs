using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Time slot entity - Available appointment time slots (e.g., "9:00 AM", "2:30 PM")
/// Time is stored as a string label (no timezone conversion needed)
/// </summary>
public class TimeSlot : CompanyScopedEntity
{
    /// <summary>
    /// Time display string (e.g., "9:00 AM", "2:30 PM")
    /// Stored as-is, no timezone conversion needed
    /// </summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// Sort order for display (lower numbers appear first)
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this time slot is active and available for selection
    /// </summary>
    public bool IsActive { get; set; } = true;
}

