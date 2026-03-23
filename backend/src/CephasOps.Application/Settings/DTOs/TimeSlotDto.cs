namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// Time slot DTO
/// </summary>
public class TimeSlotDto
{
    public Guid Id { get; set; }
    public string Time { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create time slot DTO
/// </summary>
public class CreateTimeSlotDto
{
    public string Time { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Update time slot DTO
/// </summary>
public class UpdateTimeSlotDto
{
    public string? Time { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Reorder time slots DTO
/// </summary>
public class ReorderTimeSlotsDto
{
    public List<Guid> TimeSlotIds { get; set; } = new();
}

