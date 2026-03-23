using CephasOps.Application.ServiceInstallers.DTOs;
using CephasOps.Application.ServiceInstallers.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Skills endpoints
/// </summary>
[ApiController]
[Route("api/skills")]
[Authorize]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _skillService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<SkillsController> _logger;

    public SkillsController(
        ISkillService skillService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<SkillsController> logger)
    {
        _skillService = skillService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all skills, optionally filtered by category
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SkillDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<SkillDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<SkillDto>>>> GetSkills(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<SkillDto>>("You do not have access to this department", 403);
        }

        try
        {
            var skills = await _skillService.GetSkillsAsync(companyId, departmentScope, category, isActive, cancellationToken);
            return this.Success(skills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skills");
            return this.Error<List<SkillDto>>($"Failed to get skills: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get skills grouped by category
    /// </summary>
    [HttpGet("by-category")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, List<SkillDto>>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, List<SkillDto>>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<Dictionary<string, List<SkillDto>>>>> GetSkillsByCategory(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<Dictionary<string, List<SkillDto>>>("You do not have access to this department", 403);
        }

        try
        {
            var skillsByCategory = await _skillService.GetSkillsByCategoryAsync(companyId, departmentScope, isActive, cancellationToken);
            return this.Success(skillsByCategory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skills by category");
            return this.Error<Dictionary<string, List<SkillDto>>>($"Failed to get skills by category: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get all skill categories
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetSkillCategories(
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<string>>("You do not have access to this department", 403);
        }

        try
        {
            var categories = await _skillService.GetSkillCategoriesAsync(companyId, departmentScope, cancellationToken);
            return this.Success(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skill categories");
            return this.Error<List<string>>($"Failed to get skill categories: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get skill by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SkillDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SkillDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SkillDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SkillDto>>> GetSkill(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var skill = await _skillService.GetSkillByIdAsync(id, companyId, cancellationToken);
            if (skill == null)
            {
                return this.NotFound<SkillDto>($"Skill with ID {id} not found");
            }

            return this.Success(skill);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skill: {SkillId}", id);
            return this.Error<SkillDto>($"Failed to get skill: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new skill
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SkillDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SkillDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SkillDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SkillDto>>> CreateSkill(
        [FromBody] CreateSkillDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var skill = await _skillService.CreateSkillAsync(dto, companyId, cancellationToken);
            return this.Created($"/api/skills/{skill.Id}", skill);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating skill");
            return this.Error<SkillDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating skill");
            return this.Error<SkillDto>($"Failed to create skill: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update an existing skill
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SkillDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SkillDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SkillDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SkillDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SkillDto>>> UpdateSkill(
        Guid id,
        [FromBody] UpdateSkillDto dto,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            var skill = await _skillService.UpdateSkillAsync(id, dto, companyId, cancellationToken);
            return this.Success(skill);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Skill not found: {SkillId}", id);
            return this.NotFound<SkillDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation updating skill: {SkillId}", id);
            return this.Error<SkillDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating skill: {SkillId}", id);
            return this.Error<SkillDto>($"Failed to update skill: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a skill
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSkill(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;

        try
        {
            await _skillService.DeleteSkillAsync(id, companyId, cancellationToken);
            return this.Success<object>(new { }, "Skill deleted successfully");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Skill not found: {SkillId}", id);
            return this.NotFound<object>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation deleting skill: {SkillId}", id);
            return this.Error<object>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting skill: {SkillId}", id);
            return this.Error<object>($"Failed to delete skill: {ex.Message}", 500);
        }
    }
}

