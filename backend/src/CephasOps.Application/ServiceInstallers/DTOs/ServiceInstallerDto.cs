using CephasOps.Domain.ServiceInstallers.Enums;

namespace CephasOps.Application.ServiceInstallers.DTOs;

/// <summary>
/// Service Installer DTO
/// </summary>
public class ServiceInstallerDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public InstallerLevel SiLevel { get; set; } = InstallerLevel.Junior;
    public InstallerType InstallerType { get; set; } = InstallerType.InHouse;
    [Obsolete("Use InstallerType instead. This field will be removed in a future version.")]
    public bool IsSubcontractor { get; set; } // Kept for backward compatibility
    public bool IsActive { get; set; }
    public Guid? UserId { get; set; }
    public string? IcNumber { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
    
    // Availability status
    public string? AvailabilityStatus { get; set; }
    
    // Conditional Fields - In-House Only
    public DateTime? HireDate { get; set; }
    public string? EmploymentStatus { get; set; }
    
    // Conditional Fields - Subcontractor Only
    public string? ContractorId { get; set; }
    public string? ContractorCompany { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    
    // Skills (will be populated separately via skills endpoint)
    public List<ServiceInstallerSkillDto>? Skills { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create service installer request DTO
/// </summary>
public class CreateServiceInstallerDto
{
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public InstallerLevel SiLevel { get; set; } = InstallerLevel.Junior;
    public InstallerType InstallerType { get; set; } = InstallerType.InHouse;
    [Obsolete("Use InstallerType instead. This field will be removed in a future version.")]
    public bool IsSubcontractor { get; set; } // Kept for backward compatibility
    public bool IsActive { get; set; } = true;
    public Guid? UserId { get; set; }
    public string? IcNumber { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
    
    // Availability status
    public string? AvailabilityStatus { get; set; }
    
    // Conditional Fields - In-House Only
    public DateTime? HireDate { get; set; }
    public string? EmploymentStatus { get; set; }
    
    // Conditional Fields - Subcontractor Only
    public string? ContractorId { get; set; }
    public string? ContractorCompany { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    
    // Skills (array of skill IDs for creation)
    public List<Guid>? SkillIds { get; set; }
}

/// <summary>
/// Update service installer request DTO
/// </summary>
public class UpdateServiceInstallerDto
{
    public Guid? DepartmentId { get; set; }
    public string? Name { get; set; }
    public string? EmployeeId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public InstallerLevel? SiLevel { get; set; }
    public InstallerType? InstallerType { get; set; }
    [Obsolete("Use InstallerType instead. This field will be removed in a future version.")]
    public bool? IsSubcontractor { get; set; } // Kept for backward compatibility
    public bool? IsActive { get; set; }
    public Guid? UserId { get; set; }
    public string? IcNumber { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }
    
    // Availability status
    public string? AvailabilityStatus { get; set; }
    
    // Conditional Fields - In-House Only
    public DateTime? HireDate { get; set; }
    public string? EmploymentStatus { get; set; }
    
    // Conditional Fields - Subcontractor Only
    public string? ContractorId { get; set; }
    public string? ContractorCompany { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    
    // Skills (array of skill IDs for update)
    public List<Guid>? SkillIds { get; set; }
}

public class ServiceInstallerContactDto
{
    public Guid Id { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string ContactType { get; set; } = "Backup";
    public bool IsPrimary { get; set; }
}

public class CreateServiceInstallerContactDto
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string ContactType { get; set; } = "Backup";
    public bool IsPrimary { get; set; }
}

public class UpdateServiceInstallerContactDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactType { get; set; }
    public bool? IsPrimary { get; set; }
}

/// <summary>
/// Skill DTO
/// </summary>
public class SkillDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}

/// <summary>
/// Create skill request DTO
/// </summary>
public class CreateSkillDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public Guid? DepartmentId { get; set; }
}

/// <summary>
/// Update skill request DTO
/// </summary>
public class UpdateSkillDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
    public Guid? DepartmentId { get; set; }
}

/// <summary>
/// Service Installer Skill DTO (join entity)
/// </summary>
public class ServiceInstallerSkillDto
{
    public Guid Id { get; set; }
    public Guid ServiceInstallerId { get; set; }
    public Guid SkillId { get; set; }
    public SkillDto? Skill { get; set; } // Populated skill details
    public DateTime? AcquiredAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

