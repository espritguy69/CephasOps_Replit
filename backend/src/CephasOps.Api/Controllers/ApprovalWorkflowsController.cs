using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Approval Workflows management endpoints
/// </summary>
[ApiController]
[Route("api/approval-workflows")]
[Authorize]
public class ApprovalWorkflowsController : ControllerBase
{
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<ApprovalWorkflowsController> _logger;

    public ApprovalWorkflowsController(
        IApprovalWorkflowService approvalWorkflowService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<ApprovalWorkflowsController> logger)
    {
        _approvalWorkflowService = approvalWorkflowService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get approval workflows
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ApprovalWorkflowDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ApprovalWorkflowDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<ApprovalWorkflowDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ApprovalWorkflowDto>>>> GetApprovalWorkflows(
        [FromQuery] string? workflowType = null,
        [FromQuery] string? entityType = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        
        try
        {
            var workflows = await _approvalWorkflowService.GetWorkflowsAsync(
                companyId, workflowType, entityType, isActive, cancellationToken);
            return this.Success(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approval workflows");
            return this.InternalServerError<List<ApprovalWorkflowDto>>($"Failed to get approval workflows: {ex.Message}");
        }
    }

    /// <summary>
    /// Get approval workflow by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ApprovalWorkflowDto>>> GetApprovalWorkflow(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var workflow = await _approvalWorkflowService.GetWorkflowByIdAsync(id, companyId, cancellationToken);
            if (workflow == null)
            {
                return this.NotFound<ApprovalWorkflowDto>($"Approval workflow with ID {id} not found");
            }

            return this.Success(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approval workflow: {WorkflowId}", id);
            return this.InternalServerError<ApprovalWorkflowDto>($"Failed to get approval workflow: {ex.Message}");
        }
    }

    /// <summary>
    /// Get effective approval workflow for context
    /// </summary>
    [HttpGet("effective")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ApprovalWorkflowDto>>> GetEffectiveWorkflow(
        [FromQuery] string workflowType,
        [FromQuery] string entityType,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? orderType = null,
        [FromQuery] decimal? value = null,
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
            return this.Error<ApprovalWorkflowDto>("You do not have access to this department", 403);
        }

        try
        {
            var workflow = await _approvalWorkflowService.GetEffectiveWorkflowAsync(
                companyId, workflowType, entityType, partnerId, departmentScope, orderType, value, cancellationToken);
            
            if (workflow == null)
            {
                return this.NotFound<ApprovalWorkflowDto>("No effective approval workflow found for the given context");
            }

            return this.Success(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective approval workflow");
            return this.InternalServerError<ApprovalWorkflowDto>($"Failed to get effective approval workflow: {ex.Message}");
        }
    }

    /// <summary>
    /// Create approval workflow
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ApprovalWorkflowDto>>> CreateApprovalWorkflow(
        [FromBody] CreateApprovalWorkflowDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (userId == null)
        {
            return this.Unauthorized<ApprovalWorkflowDto>("User context required");
        }

        try
        {
            var workflow = await _approvalWorkflowService.CreateWorkflowAsync(dto, companyId, userId.Value, cancellationToken);
            return this.CreatedAtAction(nameof(GetApprovalWorkflow), new { id = workflow.Id }, workflow, "Approval workflow created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating approval workflow");
            return this.InternalServerError<ApprovalWorkflowDto>($"Failed to create approval workflow: {ex.Message}");
        }
    }

    /// <summary>
    /// Update approval workflow
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ApprovalWorkflowDto>>> UpdateApprovalWorkflow(
        Guid id,
        [FromBody] UpdateApprovalWorkflowDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (userId == null)
        {
            return this.Unauthorized<ApprovalWorkflowDto>("User context required");
        }

        try
        {
            var workflow = await _approvalWorkflowService.UpdateWorkflowAsync(id, dto, companyId, userId.Value, cancellationToken);
            return this.Success(workflow, "Approval workflow updated successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ApprovalWorkflowDto>($"Approval workflow with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating approval workflow: {WorkflowId}", id);
            return this.InternalServerError<ApprovalWorkflowDto>($"Failed to update approval workflow: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete approval workflow
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteApprovalWorkflow(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _approvalWorkflowService.DeleteWorkflowAsync(id, companyId, cancellationToken);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"Approval workflow with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting approval workflow: {WorkflowId}", id);
            return this.InternalServerError($"Failed to delete approval workflow: {ex.Message}");
        }
    }

    /// <summary>
    /// Set approval workflow as default
    /// </summary>
    [HttpPost("{id}/set-default")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ApprovalWorkflowDto>>> SetAsDefault(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        
        if (userId == null)
        {
            return this.Unauthorized<ApprovalWorkflowDto>("User context required");
        }

        try
        {
            var workflow = await _approvalWorkflowService.SetAsDefaultAsync(id, companyId, userId.Value, cancellationToken);
            return this.Success(workflow, "Approval workflow set as default successfully.");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<ApprovalWorkflowDto>($"Approval workflow with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting approval workflow as default: {WorkflowId}", id);
            return this.InternalServerError<ApprovalWorkflowDto>($"Failed to set approval workflow as default: {ex.Message}");
        }
    }
}

