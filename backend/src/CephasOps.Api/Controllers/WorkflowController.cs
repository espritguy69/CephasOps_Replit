using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[ApiController]
[Route("api/workflow")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowEngineService _workflowEngineService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IWorkflowEngineService workflowEngineService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ICorrelationIdProvider correlationIdProvider,
        ILogger<WorkflowController> logger)
    {
        _workflowEngineService = workflowEngineService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _correlationIdProvider = correlationIdProvider;
        _logger = logger;
    }

    /// <summary>
    /// Execute a workflow transition for an entity
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowJobDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowJobDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WorkflowJobDto>>> ExecuteTransition(
        [FromBody] ExecuteTransitionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return this.Error<WorkflowJobDto>("User ID not found.", 401);
        }

        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        dto.CorrelationId ??= _correlationIdProvider.GetCorrelationId();

        _logger.LogInformation("POST /api/workflow/execute called by User {UserId} for Company {CompanyId}, entityType: {EntityType}, entityId: {EntityId}",
            _currentUserService.UserId, companyId, dto.EntityType, dto.EntityId);

        try
        {
            var job = await _workflowEngineService.ExecuteTransitionAsync(
                companyId,
                dto,
                _currentUserService.UserId.Value,
                cancellationToken);

            return this.Success(job, "Workflow transition executed successfully");
        }
        catch (InvalidWorkflowTransitionException ex)
        {
            return this.Error<WorkflowJobDto>(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<WorkflowJobDto>(ex.Message, 400);
        }
        catch (NotSupportedException ex)
        {
            return this.Error<WorkflowJobDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow transition");
            return this.Error<WorkflowJobDto>($"Failed to execute workflow transition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get allowed transitions for an entity in its current status
    /// </summary>
    [HttpGet("allowed-transitions")]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowTransitionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<WorkflowTransitionDto>>>> GetAllowedTransitions(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        [FromQuery] string currentStatus,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("GET /api/workflow/allowed-transitions called by User {UserId} for entityType: {EntityType}, entityId: {EntityId}, currentStatus: {CurrentStatus}",
            _currentUserService.UserId, entityType, entityId, currentStatus);

        // Get user roles from current user service
        var userRoles = _currentUserService.Roles;

        var transitions = await _workflowEngineService.GetAllowedTransitionsAsync(
            companyId,
            entityType,
            entityId,
            currentStatus,
            userRoles,
            cancellationToken);

        return this.Success(transitions);
    }

    /// <summary>
    /// Check if a transition is allowed
    /// </summary>
    [HttpGet("can-transition")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> CanTransition(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        [FromQuery] string fromStatus,
        [FromQuery] string toStatus,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("GET /api/workflow/can-transition called by User {UserId} for entityType: {EntityType}, entityId: {EntityId}, from: {FromStatus}, to: {ToStatus}",
            _currentUserService.UserId, entityType, entityId, fromStatus, toStatus);

        // Get user roles from current user service
        var userRoles = _currentUserService.Roles;

        var canTransition = await _workflowEngineService.CanTransitionAsync(
            companyId,
            entityType,
            entityId,
            fromStatus,
            toStatus,
            userRoles,
            cancellationToken);

        return this.Success(canTransition);
    }

    /// <summary>
    /// Get workflow job by ID
    /// </summary>
    [HttpGet("jobs/{id}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowJobDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WorkflowJobDto>>> GetWorkflowJob(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("GET /api/workflow/jobs/{Id} called by User {UserId}",
            id, _currentUserService.UserId);

        var job = await _workflowEngineService.GetWorkflowJobAsync(
            companyId,
            id,
            cancellationToken);

        if (job == null)
        {
            return this.NotFound<WorkflowJobDto>($"Workflow job with ID '{id}' not found.");
        }

        return this.Success(job);
    }

    /// <summary>
    /// Get workflow jobs for an entity
    /// </summary>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowJobDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<WorkflowJobDto>>>> GetWorkflowJobs(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        [FromQuery] string? state = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("GET /api/workflow/jobs called by User {UserId} for entityType: {EntityType}, entityId: {EntityId}, state: {State}",
            _currentUserService.UserId, entityType, entityId, state);

        List<WorkflowJobDto> jobs;

        if (!string.IsNullOrEmpty(state))
        {
            jobs = await _workflowEngineService.GetWorkflowJobsByStateAsync(
                companyId,
                state,
                cancellationToken);
        }
        else
        {
            jobs = await _workflowEngineService.GetWorkflowJobsAsync(
                companyId,
                entityType,
                entityId,
                cancellationToken);
        }

        return this.Success(jobs);
    }
}

