namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// VIP group DTO
/// </summary>
public class VipGroupDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? NotifyDepartmentId { get; set; }
    public Guid? NotifyUserId { get; set; }
    public Guid? NotifyHodUserId { get; set; }
    public string? NotifyRole { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Related names for display
    public string? DepartmentName { get; set; }
    public string? NotifyUserName { get; set; }
    public string? HodUserName { get; set; }
    
    // Email addresses in this group
    public List<VipGroupEmailDto> EmailAddresses { get; set; } = new();
}

/// <summary>
/// VIP Group email DTO (for display in group)
/// </summary>
public class VipGroupEmailDto
{
    public Guid Id { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Create VIP group request DTO
/// </summary>
public class CreateVipGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? NotifyDepartmentId { get; set; }
    public Guid? NotifyUserId { get; set; }
    public Guid? NotifyHodUserId { get; set; }
    public string? NotifyRole { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Email addresses to add to this group (will create VipEmail records)
    /// Format: ["email1@example.com", "email2@example.com"] or [{"email": "email@example.com", "displayName": "Name"}]
    /// </summary>
    public List<string>? EmailAddresses { get; set; }
}

/// <summary>
/// Update VIP group request DTO
/// </summary>
public class UpdateVipGroupDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid? NotifyDepartmentId { get; set; }
    public Guid? NotifyUserId { get; set; }
    public Guid? NotifyHodUserId { get; set; }
    public string? NotifyRole { get; set; }
    public int? Priority { get; set; }
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Email addresses to sync with this group (will create/update/delete VipEmail records)
    /// Format: ["email1@example.com", "email2@example.com"]
    /// </summary>
    public List<string>? EmailAddresses { get; set; }
}

