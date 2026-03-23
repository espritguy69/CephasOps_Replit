using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[ApiController]
[Route("api/workflow-definitions")]
[Authorize]
public class WorkflowDefinitionsController : ControllerBase
{
    private readonly IWorkflowDefinitionsService _workflowDefinitionsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<WorkflowDefinitionsController> _logger;

    public WorkflowDefinitionsController(
        IWorkflowDefinitionsService workflowDefinitionsService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        ILogger<WorkflowDefinitionsController> logger)
    {
        _workflowDefinitionsService = workflowDefinitionsService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all workflow definitions for the current company
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowDefinitionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowDefinitionDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<WorkflowDefinitionDto>>>> GetWorkflowDefinitions(
        [FromQuery] string? entityType = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("GET /api/workflow-definitions called by User {UserId} for Company {CompanyId}",
            _currentUserService.UserId, companyId);

        try
        {
            var definitions = await _workflowDefinitionsService.GetWorkflowDefinitionsAsync(
                companyId,
                entityType,
                isActive,
                cancellationToken);

            return this.Success(definitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow definitions");
            return this.Error<List<WorkflowDefinitionDto>>($"Failed to get workflow definitions: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get workflow definition by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> GetWorkflowDefinition(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("GET /api/workflow-definitions/{Id} called by User {UserId}",
            id, _currentUserService.UserId);

        try
        {
            var definition = await _workflowDefinitionsService.GetWorkflowDefinitionAsync(
                companyId,
                id,
                cancellationToken);

            if (definition == null)
            {
                return this.NotFound<WorkflowDefinitionDto>($"Workflow definition with ID '{id}' not found.");
            }

            return this.Success(definition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow definition {Id}", id);
            return this.Error<WorkflowDefinitionDto>($"Failed to get workflow definition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get effective workflow definition for an entity type (resolution: Partner → Department → OrderType → General).
    /// </summary>
    [HttpGet("effective")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> GetEffectiveWorkflowDefinition(
        [FromQuery] string? entityType,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? orderTypeCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return this.BadRequest<WorkflowDefinitionDto>("entityType is required.");
        }

        var entityTypeTrimmed = entityType!.Trim();

        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("GET /api/workflow-definitions/effective?entityType={EntityType} called by User {UserId}",
            entityTypeTrimmed, _currentUserService.UserId);

        try
        {
            var definition = await _workflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync(
                companyId,
                entityTypeTrimmed,
                partnerId,
                departmentId,
                orderTypeCode,
                cancellationToken);

            if (definition == null)
            {
                return this.NotFound<WorkflowDefinitionDto>($"No active workflow definition found for entity type '{entityTypeTrimmed}'.");
            }

            return this.Success(definition);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Multiple active"))
        {
            return this.Error<WorkflowDefinitionDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective workflow definition for entity type {EntityType}", entityTypeTrimmed);
            return this.Error<WorkflowDefinitionDto>($"Failed to get effective workflow definition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new workflow definition
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> CreateWorkflowDefinition(
        [FromBody] CreateWorkflowDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return this.Unauthorized<WorkflowDefinitionDto>("User ID not found.");
        }

        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("POST /api/workflow-definitions called by User {UserId} for Company {CompanyId}",
            _currentUserService.UserId, companyId);

        try
        {
            var definition = await _workflowDefinitionsService.CreateWorkflowDefinitionAsync(
                companyId,
                dto,
                _currentUserService.UserId.Value,
                cancellationToken);

            return this.StatusCode(201, ApiResponse<WorkflowDefinitionDto>.SuccessResponse(definition, "Workflow definition created successfully"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists") && ex.Message.Contains("scope"))
        {
            return this.Error<WorkflowDefinitionDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow definition");
            return this.Error<WorkflowDefinitionDto>($"Failed to create workflow definition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update an existing workflow definition
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> UpdateWorkflowDefinition(
        Guid id,
        [FromBody] UpdateWorkflowDefinitionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return this.Unauthorized<WorkflowDefinitionDto>("User ID not found.");
        }

        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("PUT /api/workflow-definitions/{Id} called by User {UserId}",
            id, _currentUserService.UserId);

        try
        {
            var definition = await _workflowDefinitionsService.UpdateWorkflowDefinitionAsync(
                companyId,
                id,
                dto,
                _currentUserService.UserId.Value,
                cancellationToken);

            return this.Success(definition, "Workflow definition updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<WorkflowDefinitionDto>(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists") && ex.Message.Contains("scope"))
        {
            return this.Error<WorkflowDefinitionDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow definition {Id}", id);
            return this.Error<WorkflowDefinitionDto>($"Failed to update workflow definition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a workflow definition
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteWorkflowDefinition(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("DELETE /api/workflow-definitions/{Id} called by User {UserId}",
            id, _currentUserService.UserId);

        try
        {
            await _workflowDefinitionsService.DeleteWorkflowDefinitionAsync(
                companyId,
                id,
                cancellationToken);

            return this.StatusCode(204, ApiResponse.SuccessResponse("Workflow definition deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow definition {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete workflow definition: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get transitions for a workflow definition
    /// </summary>
    [HttpGet("{id}/transitions")]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowTransitionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowTransitionDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<WorkflowTransitionDto>>>> GetTransitions(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("GET /api/workflow-definitions/{Id}/transitions called by User {UserId}",
            id, _currentUserService.UserId);

        try
        {
            var transitions = await _workflowDefinitionsService.GetTransitionsAsync(
                companyId,
                id,
                cancellationToken);

            return this.Success(transitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transitions for workflow definition {Id}", id);
            return this.Error<List<WorkflowTransitionDto>>($"Failed to get transitions: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Add a transition to a workflow definition
    /// </summary>
    [HttpPost("{id}/transitions")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTransitionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTransitionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTransitionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTransitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTransitionDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTransitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WorkflowTransitionDto>>> AddTransition(
        Guid id,
        [FromBody] CreateWorkflowTransitionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return this.Unauthorized<WorkflowTransitionDto>("User ID not found.");
        }

        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("POST /api/workflow-definitions/{Id}/transitions called by User {UserId}",
            id, _currentUserService.UserId);

        try
        {
            var transition = await _workflowDefinitionsService.AddTransitionAsync(
                companyId,
                id,
                dto,
                _currentUserService.UserId.Value,
                cancellationToken);

            return this.StatusCode(201, ApiResponse<WorkflowTransitionDto>.SuccessResponse(transition, "Transition added successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<WorkflowTransitionDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.Error<WorkflowTransitionDto>(ex.Message, 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transition to workflow definition {Id}", id);
            return this.Error<WorkflowTransitionDto>($"Failed to add transition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update a workflow transition
    /// </summary>
    [HttpPut("transitions/{transitionId}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTransitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTransitionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTransitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTransitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WorkflowTransitionDto>>> UpdateTransition(
        Guid transitionId,
        [FromBody] UpdateWorkflowTransitionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return this.Unauthorized<WorkflowTransitionDto>("User ID not found.");
        }

        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("PUT /api/workflow-definitions/transitions/{TransitionId} called by User {UserId}",
            transitionId, _currentUserService.UserId);

        try
        {
            var transition = await _workflowDefinitionsService.UpdateTransitionAsync(
                companyId,
                transitionId,
                dto,
                _currentUserService.UserId.Value,
                cancellationToken);

            return this.Success(transition, "Transition updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<WorkflowTransitionDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transition {TransitionId}", transitionId);
            return this.Error<WorkflowTransitionDto>($"Failed to update transition: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a workflow transition
    /// </summary>
    [HttpDelete("transitions/{transitionId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteTransition(
        Guid transitionId,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        _logger.LogInformation("DELETE /api/workflow-definitions/transitions/{TransitionId} called by User {UserId}",
            transitionId, _currentUserService.UserId);

        try
        {
            await _workflowDefinitionsService.DeleteTransitionAsync(
                companyId,
                transitionId,
                cancellationToken);

            return this.StatusCode(204, ApiResponse.SuccessResponse("Transition deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transition {TransitionId}", transitionId);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete transition: {ex.Message}"));
        }
    }
}

