namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// VIP email DTO
/// </summary>
public class VipEmailDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public Guid? VipGroupId { get; set; }
    public Guid? NotifyUserId { get; set; }
    public string? NotifyRole { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Related names for display
    public string? VipGroupName { get; set; }
    public string? NotifyUserName { get; set; }
    public string? DepartmentName { get; set; }
}

/// <summary>
/// Create VIP email request DTO
/// </summary>
public class CreateVipEmailDto
{
    public string EmailAddress { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public Guid? VipGroupId { get; set; }
    public Guid? NotifyUserId { get; set; }
    public string? NotifyRole { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Update VIP email request DTO
/// </summary>
public class UpdateVipEmailDto
{
    public string? EmailAddress { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public Guid? VipGroupId { get; set; }
    public Guid? NotifyUserId { get; set; }
    public string? NotifyRole { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
}

