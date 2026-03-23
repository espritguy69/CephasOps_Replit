using CephasOps.Domain.Common;

namespace CephasOps.Domain.ServiceInstallers.Entities;

/// <summary>
/// Join entity linking Service Installers to their Skills
/// Tracks when skill was acquired, verified, and by whom
/// </summary>
public class ServiceInstallerSkill : CompanyScopedEntity
{
    /// <summary>
    /// Service Installer ID
    /// </summary>
    public Guid ServiceInstallerId { get; set; }
    
    /// <summary>
    /// Skill ID
    /// </summary>
    public Guid SkillId { get; set; }
    
    /// <summary>
    /// Date when installer acquired this skill
    /// </summary>
    public DateTime? AcquiredAt { get; set; }
    
    /// <summary>
    /// Date when skill was verified/certified
    /// </summary>
    public DateTime? VerifiedAt { get; set; }
    
    /// <summary>
    /// User ID who verified this skill
    /// </summary>
    public Guid? VerifiedByUserId { get; set; }
    
    /// <summary>
    /// Notes about the skill acquisition or verification
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Whether this skill assignment is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Navigation property - Service Installer
    /// </summary>
    public ServiceInstaller ServiceInstaller { get; set; } = null!;
    
    /// <summary>
    /// Navigation property - Skill
    /// </summary>
    public Skill Skill { get; set; } = null!;
}

