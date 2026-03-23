using CephasOps.Domain.Common;
using CephasOps.Domain.ServiceInstallers.Enums;

namespace CephasOps.Domain.ServiceInstallers.Entities;

/// <summary>
/// Service installer entity - represents SIs (in-house and subcon)
/// </summary>
public class ServiceInstaller : CompanyScopedEntity
{
    public Guid? DepartmentId { get; set; } // Optional: department assignment
    public string Name { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    
    // Level: Senior or Junior only (Subcon is a Type, not a Level)
    public InstallerLevel SiLevel { get; set; } = InstallerLevel.Junior;
    
    // Type: In-House or Subcontractor
    public InstallerType InstallerType { get; set; } = InstallerType.InHouse;
    
    [Obsolete("Use InstallerType instead. This field will be removed in a future version.")]
    public bool IsSubcontractor { get; set; } // Kept for backward compatibility during migration
    
    public bool IsActive { get; set; } = true;
    public Guid? UserId { get; set; } // Link to User if SI has login access
    
    // Availability status
    public string? AvailabilityStatus { get; set; } // Available, Busy, OnLeave, etc.
    
    // ============================================
    // Conditional Fields - In-House Only
    // ============================================
    public DateTime? HireDate { get; set; } // For In-House employees
    public string? EmploymentStatus { get; set; } // Permanent, Probation, etc.
    
    // ============================================
    // Conditional Fields - Subcontractor Only
    // ============================================
    public string? ContractorId { get; set; } // For Subcontractors
    public string? ContractorCompany { get; set; } // Contractor company name (if applicable)
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    
    // ============================================
    // Additional Information Fields
    // ============================================
    public string? IcNumber { get; set; } // Malaysian IC/Identity Card Number
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContact { get; set; } // Emergency contact name/phone (can also use Contacts for detailed emergency contacts)

    // ============================================
    // Navigation Properties
    // ============================================
    public ICollection<ServiceInstallerContact> Contacts { get; set; } = new List<ServiceInstallerContact>();
    public ICollection<ServiceInstallerSkill> Skills { get; set; } = new List<ServiceInstallerSkill>();
}

