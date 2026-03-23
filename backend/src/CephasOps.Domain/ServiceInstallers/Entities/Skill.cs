using CephasOps.Domain.Common;

namespace CephasOps.Domain.ServiceInstallers.Entities;

/// <summary>
/// Skill master entity - defines available skills for service installers
/// Skills are organized by category: FiberSkills, NetworkEquipment, InstallationMethods, SafetyCompliance, CustomerService
/// </summary>
public class Skill : CompanyScopedEntity
{
    /// <summary>
    /// Skill name (e.g., "Fiber splicing (fusion)", "ONT installation and configuration")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Skill code (short identifier, e.g., "FIBER_SPLICE_FUSION", "ONT_INSTALL")
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Skill category: FiberSkills, NetworkEquipment, InstallationMethods, SafetyCompliance, CustomerService
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of the skill
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether this skill is active and available for assignment
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Display order within category (for UI sorting)
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
    
    /// <summary>
    /// Department ID - skills can be department-specific
    /// </summary>
    public Guid? DepartmentId { get; set; }
    
    /// <summary>
    /// Navigation property - installers who have this skill
    /// </summary>
    public ICollection<ServiceInstallerSkill> InstallerSkills { get; set; } = new List<ServiceInstallerSkill>();
}

