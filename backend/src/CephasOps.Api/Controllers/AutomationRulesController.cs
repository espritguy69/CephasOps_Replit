using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Automation Rules management endpoints
/// </summary>
[ApiController]
[Route("api/automation-rules")]
[Authorize]
public class AutomationRulesController : ControllerBase
{
    private readonly IAutomationRuleService _automationRuleService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<AutomationRulesController> _logger;

    public AutomationRulesController(
        IAutomationRuleService automationRuleService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<AutomationRulesController> logger)
    {
        _automationRuleService = automationRuleService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get automation rules
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AutomationRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AutomationRuleDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<AutomationRuleDto>>>> GetAutomationRules(
        [FromQuery] string? ruleType = null,
        [FromQuery] string? entityType = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var rules = await _automationRuleService.GetRulesAsync(
                companyId, ruleType, entityType, isActive, cancellationToken);
            return this.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting automation rules");
            return this.InternalServerError<List<AutomationRuleDto>>($"Failed to get automation rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Get automation rule by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AutomationRuleDto>>> GetAutomationRule(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var rule = await _automationRuleService.GetRuleByIdAsync(id, companyId, cancellationToken);
            if (rule == null)
            {
                return this.NotFound<AutomationRuleDto>($"Automation rule with ID {id} not found");
            }

            return this.Success(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting automation rule: {RuleId}", id);
            return this.InternalServerError<AutomationRuleDto>($"Failed to get automation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Get applicable automation rules for a context
    /// </summary>
    [HttpGet("applicable")]
    [ProducesResponseType(typeof(ApiResponse<List<AutomationRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AutomationRuleDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<AutomationRuleDto>>>> GetApplicableRules(
        [FromQuery] string entityType,
        [FromQuery] string? currentStatus = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? orderType = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<AutomationRuleDto>>("You do not have access to this department", 403);
        }

        try
        {
            var rules = await _automationRuleService.GetApplicableRulesAsync(
                companyId, entityType, currentStatus, partnerId, departmentScope, orderType, cancellationToken);
            return this.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applicable automation rules");
            return this.InternalServerError<List<AutomationRuleDto>>($"Failed to get applicable automation rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Create automation rule
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AutomationRuleDto>>> CreateAutomationRule(
        [FromBody] CreateAutomationRuleDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (userId == null)
        {
            return this.Unauthorized<AutomationRuleDto>("User context required");
        }

        try
        {
            var rule = await _automationRuleService.CreateRuleAsync(dto, companyId, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetAutomationRule), new { id = rule.Id }, rule, "Automation rule created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating automation rule");
            return this.InternalServerError<AutomationRuleDto>($"Failed to create automation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Update automation rule
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AutomationRuleDto>>> UpdateAutomationRule(
        Guid id,
        [FromBody] UpdateAutomationRuleDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (userId == null)
        {
            return this.Unauthorized<AutomationRuleDto>("User context required");
        }

        try
        {
            var rule = await _automationRuleService.UpdateRuleAsync(id, dto, companyId, userId.Value, cancellationToken);
            return this.Success(rule, "Automation rule updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<AutomationRuleDto>($"Automation rule with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating automation rule: {RuleId}", id);
            return this.InternalServerError<AutomationRuleDto>($"Failed to update automation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete automation rule
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteAutomationRule(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _automationRuleService.DeleteRuleAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Automation rule with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting automation rule: {RuleId}", id);
            return this.InternalServerError($"Failed to delete automation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle automation rule active status
    /// </summary>
    [HttpPost("{id}/toggle-active")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AutomationRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AutomationRuleDto>>> ToggleActive(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (userId == null)
        {
            return this.Unauthorized<AutomationRuleDto>("User context required");
        }

        try
        {
            var rule = await _automationRuleService.ToggleActiveAsync(id, companyId, userId.Value, cancellationToken);
            return this.Success(rule, "Automation rule active status toggled successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<AutomationRuleDto>($"Automation rule with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling automation rule active status: {RuleId}", id);
            return this.InternalServerError<AutomationRuleDto>($"Failed to toggle automation rule active status: {ex.Message}");
        }
    }
}

