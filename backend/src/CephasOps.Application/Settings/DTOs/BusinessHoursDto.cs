namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// DTO for Business Hours
/// Time fields are stored in 24-hour format (HH:mm) but can be accepted in 12-hour format (8am, 8:00PM, etc.) when creating/updating
/// </summary>
public class BusinessHoursDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Timezone { get; set; } = "Asia/Kuala_Lumpur";
    public string? MondayStart { get; set; }
    public string? MondayEnd { get; set; }
    public string? TuesdayStart { get; set; }
    public string? TuesdayEnd { get; set; }
    public string? WednesdayStart { get; set; }
    public string? WednesdayEnd { get; set; }
    public string? ThursdayStart { get; set; }
    public string? ThursdayEnd { get; set; }
    public string? FridayStart { get; set; }
    public string? FridayEnd { get; set; }
    public string? SaturdayStart { get; set; }
    public string? SaturdayEnd { get; set; }
    public string? SundayStart { get; set; }
    public string? SundayEnd { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for Public Holiday
/// </summary>
public class PublicHolidayDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    public string HolidayType { get; set; } = "National";
    public string? State { get; set; }
    public bool IsRecurring { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating Business Hours
/// Time fields accept both 12-hour format (8am, 8:00PM, 8:00 PM) and 24-hour format (08:00, 20:00)
/// They will be automatically converted to 24-hour format for storage
/// </summary>
public class CreateBusinessHoursDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Timezone { get; set; } = "Asia/Kuala_Lumpur";
    public string? MondayStart { get; set; }
    public string? MondayEnd { get; set; }
    public string? TuesdayStart { get; set; }
    public string? TuesdayEnd { get; set; }
    public string? WednesdayStart { get; set; }
    public string? WednesdayEnd { get; set; }
    public string? ThursdayStart { get; set; }
    public string? ThursdayEnd { get; set; }
    public string? FridayStart { get; set; }
    public string? FridayEnd { get; set; }
    public string? SaturdayStart { get; set; }
    public string? SaturdayEnd { get; set; }
    public string? SundayStart { get; set; }
    public string? SundayEnd { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// DTO for updating Business Hours
/// Time fields accept both 12-hour format (8am, 8:00PM, 8:00 PM) and 24-hour format (08:00, 20:00)
/// They will be automatically converted to 24-hour format for storage
/// </summary>
public class UpdateBusinessHoursDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Timezone { get; set; }
    public string? MondayStart { get; set; }
    public string? MondayEnd { get; set; }
    public string? TuesdayStart { get; set; }
    public string? TuesdayEnd { get; set; }
    public string? WednesdayStart { get; set; }
    public string? WednesdayEnd { get; set; }
    public string? ThursdayStart { get; set; }
    public string? ThursdayEnd { get; set; }
    public string? FridayStart { get; set; }
    public string? FridayEnd { get; set; }
    public string? SaturdayStart { get; set; }
    public string? SaturdayEnd { get; set; }
    public string? SundayStart { get; set; }
    public string? SundayEnd { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>
/// DTO for creating Public Holiday
/// </summary>
public class CreatePublicHolidayDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    public string HolidayType { get; set; } = "National";
    public string? State { get; set; }
    public bool IsRecurring { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating Public Holiday
/// </summary>
public class UpdatePublicHolidayDto
{
    public string? Name { get; set; }
    public DateTime? HolidayDate { get; set; }
    public string? HolidayType { get; set; }
    public string? State { get; set; }
    public bool? IsRecurring { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

