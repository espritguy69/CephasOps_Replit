using CephasOps.Application.ServiceInstallers.DTOs;

namespace CephasOps.Application.ServiceInstallers.Services;

/// <summary>
/// Skill service interface
/// </summary>
public interface ISkillService
{
    /// <summary>
    /// Get all skills, optionally filtered by category and department
    /// </summary>
    Task<List<SkillDto>> GetSkillsAsync(Guid? companyId, Guid? departmentId = null, string? category = null, bool? isActive = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get skill by ID
    /// </summary>
    Task<SkillDto?> GetSkillByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all skill categories
    /// </summary>
    Task<List<string>> GetSkillCategoriesAsync(Guid? companyId, Guid? departmentId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get skills grouped by category
    /// </summary>
    Task<Dictionary<string, List<SkillDto>>> GetSkillsByCategoryAsync(Guid? companyId, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create a new skill
    /// </summary>
    Task<SkillDto> CreateSkillAsync(CreateSkillDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update an existing skill
    /// </summary>
    Task<SkillDto> UpdateSkillAsync(Guid id, UpdateSkillDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a skill
    /// </summary>
    Task DeleteSkillAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}

