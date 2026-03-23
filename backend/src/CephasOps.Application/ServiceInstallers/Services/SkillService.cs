using CephasOps.Application.ServiceInstallers.DTOs;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.ServiceInstallers.Services;

/// <summary>
/// Skill service implementation
/// </summary>
public class SkillService : ISkillService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SkillService> _logger;

    public SkillService(ApplicationDbContext context, ILogger<SkillService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SkillDto>> GetSkillsAsync(Guid? companyId, Guid? departmentId = null, string? category = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<SkillDto>();

        var query = _context.Skills.Where(s => s.CompanyId == effectiveCompanyId.Value);

        // Filter by department if provided
        if (departmentId.HasValue)
        {
            query = query.Where(s => s.DepartmentId == departmentId);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Category == category);
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        var skills = await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Category = s.Category,
                Description = s.Description,
                IsActive = s.IsActive,
                DisplayOrder = s.DisplayOrder,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.DepartmentId != null ? _context.Departments
                    .Where(d => d.Id == s.DepartmentId)
                    .Select(d => d.Name)
                    .FirstOrDefault() : null
            })
            .ToListAsync(cancellationToken);

        return skills;
    }

    public async Task<SkillDto?> GetSkillByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var skill = await _context.Skills
            .Where(s => s.Id == id && s.CompanyId == effectiveCompanyId.Value)
            .Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Category = s.Category,
                Description = s.Description,
                IsActive = s.IsActive,
                DisplayOrder = s.DisplayOrder,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.DepartmentId != null ? _context.Departments
                    .Where(d => d.Id == s.DepartmentId)
                    .Select(d => d.Name)
                    .FirstOrDefault() : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        return skill;
    }

    public async Task<List<string>> GetSkillCategoriesAsync(Guid? companyId, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<string>();

        var query = _context.Skills.Where(s => s.CompanyId == effectiveCompanyId.Value);

        if (departmentId.HasValue)
        {
            query = query.Where(s => s.DepartmentId == departmentId);
        }

        var categories = await query
            .Where(s => s.IsActive)
            .Select(s => s.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        return categories;
    }

    public async Task<Dictionary<string, List<SkillDto>>> GetSkillsByCategoryAsync(Guid? companyId, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var skills = await GetSkillsAsync(companyId, departmentId, null, isActive, cancellationToken);

        var grouped = skills
            .GroupBy(s => s.Category)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToList()
            );

        return grouped;
    }

    public async Task<SkillDto> CreateSkillAsync(CreateSkillDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a skill.");

        // Validate code uniqueness within company and department
        var existingSkill = await _context.Skills
            .Where(s => s.Code == dto.Code 
                && s.CompanyId == effectiveCompanyId.Value
                && (dto.DepartmentId == null || s.DepartmentId == dto.DepartmentId))
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSkill != null)
        {
            throw new InvalidOperationException($"A skill with code '{dto.Code}' already exists for this department.");
        }

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            DepartmentId = dto.DepartmentId,
            Name = dto.Name,
            Code = dto.Code,
            Category = dto.Category,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder
        };

        _context.Skills.Add(skill);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created skill: {SkillId} ({Code})", skill.Id, skill.Code);

        var departmentName = skill.DepartmentId != null
            ? await _context.Departments
                .Where(d => d.Id == skill.DepartmentId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return new SkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Code = skill.Code,
            Category = skill.Category,
            Description = skill.Description,
            IsActive = skill.IsActive,
            DisplayOrder = skill.DisplayOrder,
            DepartmentId = skill.DepartmentId,
            DepartmentName = departmentName
        };
    }

    public async Task<SkillDto> UpdateSkillAsync(Guid id, UpdateSkillDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to update a skill.");

        var skill = await _context.Skills
            .Where(s => s.Id == id && s.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (skill == null)
        {
            throw new KeyNotFoundException($"Skill with ID {id} not found.");
        }

        // Validate code uniqueness if code is being updated
        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != skill.Code)
        {
            var targetDepartmentId = dto.DepartmentId ?? skill.DepartmentId;
            var existingSkill = await _context.Skills
                .Where(s => s.Code == dto.Code 
                    && s.Id != id 
                    && s.CompanyId == effectiveCompanyId.Value
                    && (targetDepartmentId == null || s.DepartmentId == targetDepartmentId))
                .FirstOrDefaultAsync(cancellationToken);

            if (existingSkill != null)
            {
                throw new InvalidOperationException($"A skill with code '{dto.Code}' already exists for this department.");
            }

            skill.Code = dto.Code;
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            skill.Name = dto.Name;
        }

        if (!string.IsNullOrWhiteSpace(dto.Category))
        {
            skill.Category = dto.Category;
        }

        if (dto.Description != null)
        {
            skill.Description = dto.Description;
        }

        if (dto.IsActive.HasValue)
        {
            skill.IsActive = dto.IsActive.Value;
        }

        if (dto.DisplayOrder.HasValue)
        {
            skill.DisplayOrder = dto.DisplayOrder.Value;
        }

        if (dto.DepartmentId.HasValue)
        {
            skill.DepartmentId = dto.DepartmentId.Value;
        }
        else if (dto.DepartmentId == Guid.Empty) // Explicit null assignment
        {
            skill.DepartmentId = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated skill: {SkillId} ({Code})", skill.Id, skill.Code);

        var departmentName = skill.DepartmentId != null
            ? await _context.Departments
                .Where(d => d.Id == skill.DepartmentId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return new SkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Code = skill.Code,
            Category = skill.Category,
            Description = skill.Description,
            IsActive = skill.IsActive,
            DisplayOrder = skill.DisplayOrder,
            DepartmentId = skill.DepartmentId,
            DepartmentName = departmentName
        };
    }

    public async Task DeleteSkillAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete a skill.");

        var skill = await _context.Skills
            .Where(s => s.Id == id && s.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (skill == null)
        {
            throw new KeyNotFoundException($"Skill with ID {id} not found.");
        }

        // Check if skill is assigned to any installers
        var hasAssignments = await _context.ServiceInstallerSkills
            .AnyAsync(sis => sis.SkillId == id && sis.IsActive && !sis.IsDeleted, cancellationToken);

        if (hasAssignments)
        {
            throw new InvalidOperationException($"Cannot delete skill '{skill.Name}' because it is assigned to one or more service installers.");
        }

        // Soft delete
        skill.IsDeleted = true;
        skill.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted skill: {SkillId} ({Code})", skill.Id, skill.Code);
    }
}

