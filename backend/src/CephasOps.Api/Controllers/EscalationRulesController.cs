using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Escalation Rules management endpoints
/// </summary>
[ApiController]
[Route("api/escalation-rules")]
[Authorize]
public class EscalationRulesController : ControllerBase
{
    private readonly IEscalationRuleService _escalationRuleService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<EscalationRulesController> _logger;

    public EscalationRulesController(
        IEscalationRuleService escalationRuleService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<EscalationRulesController> logger)
    {
        _escalationRuleService = escalationRuleService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get escalation rules
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EscalationRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<EscalationRuleDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<EscalationRuleDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<EscalationRuleDto>>>> GetEscalationRules(
        [FromQuery] string? entityType = null,
        [FromQuery] string? triggerType = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<List<EscalationRuleDto>>("Company context required");
        }

        try
        {
            var rules = await _escalationRuleService.GetRulesAsync(
                companyId, entityType, triggerType, isActive, cancellationToken);
            return this.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalation rules");
            return this.Error<List<EscalationRuleDto>>($"Failed to get escalation rules: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get escalation rule by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EscalationRuleDto>>> GetEscalationRule(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<EscalationRuleDto>("Company context required");
        }

        try
        {
            var rule = await _escalationRuleService.GetRuleByIdAsync(id, companyId, cancellationToken);
            if (rule == null)
            {
                return this.NotFound<EscalationRuleDto>($"Escalation rule with ID {id} not found");
            }

            return this.Success(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalation rule: {RuleId}", id);
            return this.Error<EscalationRuleDto>($"Failed to get escalation rule: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get applicable escalation rules for context
    /// </summary>
    [HttpGet("applicable")]
    [ProducesResponseType(typeof(ApiResponse<List<EscalationRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<EscalationRuleDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<EscalationRuleDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<EscalationRuleDto>>>> GetApplicableRules(
        [FromQuery] string entityType,
        [FromQuery] string? currentStatus = null,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? orderType = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return this.Unauthorized<List<EscalationRuleDto>>("Company context required");
        }

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<EscalationRuleDto>>("You do not have access to this department", 403);
        }

        try
        {
            var rules = await _escalationRuleService.GetApplicableRulesAsync(
                companyId, entityType, currentStatus, partnerId, departmentScope, orderType, cancellationToken);
            return this.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applicable escalation rules");
            return this.Error<List<EscalationRuleDto>>($"Failed to get applicable escalation rules: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create escalation rule
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EscalationRuleDto>>> CreateEscalationRule(
        [FromBody] CreateEscalationRuleDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (companyId == Guid.Empty || userId == null)
        {
            return this.Unauthorized<EscalationRuleDto>("Company and user context required");
        }

        try
        {
            var rule = await _escalationRuleService.CreateRuleAsync(dto, companyId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<EscalationRuleDto>.SuccessResponse(rule, "Escalation rule created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating escalation rule");
            return this.Error<EscalationRuleDto>($"Failed to create escalation rule: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update escalation rule
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EscalationRuleDto>>> UpdateEscalationRule(
        Guid id,
        [FromBody] UpdateEscalationRuleDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (companyId == Guid.Empty || userId == null)
        {
            return this.Unauthorized<EscalationRuleDto>("Company and user context required");
        }

        try
        {
            var rule = await _escalationRuleService.UpdateRuleAsync(id, dto, companyId, userId.Value, cancellationToken);
            return this.Success(rule, "Escalation rule updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<EscalationRuleDto>($"Escalation rule with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating escalation rule: {RuleId}", id);
            return this.Error<EscalationRuleDto>($"Failed to update escalation rule: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete escalation rule
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteEscalationRule(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        if (companyId == Guid.Empty)
        {
            return StatusCode(401, ApiResponse.ErrorResponse("Company context required"));
        }

        try
        {
            await _escalationRuleService.DeleteRuleAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Escalation rule deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Escalation rule with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting escalation rule: {RuleId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete escalation rule: {ex.Message}"));
        }
    }

    /// <summary>
    /// Toggle escalation rule active status
    /// </summary>
    [HttpPost("{id}/toggle-active")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EscalationRuleDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EscalationRuleDto>>> ToggleActive(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (companyId == Guid.Empty || userId == null)
        {
            return this.Unauthorized<EscalationRuleDto>("Company and user context required");
        }

        try
        {
            var rule = await _escalationRuleService.ToggleActiveAsync(id, companyId, userId.Value, cancellationToken);
            return this.Success(rule, "Escalation rule active status toggled successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<EscalationRuleDto>($"Escalation rule with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling escalation rule active status: {RuleId}", id);
            return this.Error<EscalationRuleDto>($"Failed to toggle escalation rule active status: {ex.Message}", 500);
        }
    }
}

