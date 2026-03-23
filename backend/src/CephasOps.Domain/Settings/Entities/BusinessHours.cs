using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Business Hours entity - defines operating hours for a company or department
/// </summary>
public class BusinessHours : CompanyScopedEntity
{
    /// <summary>
    /// Name (e.g. "Standard Business Hours", "GPON Department Hours")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Department ID (nullable) - if set, applies only to this department
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Timezone (e.g., "Asia/Kuala_Lumpur")
    /// </summary>
    public string Timezone { get; set; } = "Asia/Kuala_Lumpur";

    // ============================================
    // Day-specific hours
    // ============================================

    /// <summary>
    /// Monday start time (HH:mm format, e.g., "08:00")
    /// </summary>
    public string? MondayStart { get; set; }

    /// <summary>
    /// Monday end time (HH:mm format, e.g., "18:00")
    /// </summary>
    public string? MondayEnd { get; set; }

    /// <summary>
    /// Tuesday start time
    /// </summary>
    public string? TuesdayStart { get; set; }

    /// <summary>
    /// Tuesday end time
    /// </summary>
    public string? TuesdayEnd { get; set; }

    /// <summary>
    /// Wednesday start time
    /// </summary>
    public string? WednesdayStart { get; set; }

    /// <summary>
    /// Wednesday end time
    /// </summary>
    public string? WednesdayEnd { get; set; }

    /// <summary>
    /// Thursday start time
    /// </summary>
    public string? ThursdayStart { get; set; }

    /// <summary>
    /// Thursday end time
    /// </summary>
    public string? ThursdayEnd { get; set; }

    /// <summary>
    /// Friday start time
    /// </summary>
    public string? FridayStart { get; set; }

    /// <summary>
    /// Friday end time
    /// </summary>
    public string? FridayEnd { get; set; }

    /// <summary>
    /// Saturday start time
    /// </summary>
    public string? SaturdayStart { get; set; }

    /// <summary>
    /// Saturday end time
    /// </summary>
    public string? SaturdayEnd { get; set; }

    /// <summary>
    /// Sunday start time
    /// </summary>
    public string? SundayStart { get; set; }

    /// <summary>
    /// Sunday end time
    /// </summary>
    public string? SundayEnd { get; set; }

    /// <summary>
    /// Whether this is the default business hours for the company
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Effective from date
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective to date (nullable for ongoing configuration)
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// User ID who created this configuration
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this configuration
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

/// <summary>
/// Public Holiday entity - defines public holidays for SLA calculations
/// </summary>
public class PublicHoliday : CompanyScopedEntity
{
    /// <summary>
    /// Holiday name (e.g., "Hari Raya Aidilfitri", "Chinese New Year")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Holiday date
    /// </summary>
    public DateTime HolidayDate { get; set; }

    /// <summary>
    /// Holiday type: National, State, Regional, Custom
    /// </summary>
    public string HolidayType { get; set; } = "National";

    /// <summary>
    /// State/Region (nullable, for state-specific holidays)
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Whether this is a recurring holiday (same date every year)
    /// </summary>
    public bool IsRecurring { get; set; } = false;

    /// <summary>
    /// Whether this holiday is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// User ID who created this holiday
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this holiday
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}

