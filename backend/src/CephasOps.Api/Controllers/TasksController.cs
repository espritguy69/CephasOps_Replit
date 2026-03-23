using CephasOps.Application.Tasks.DTOs;
using CephasOps.Application.Tasks.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Task management endpoints
/// </summary>
[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        ITaskService taskService,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get tasks with filtering
    /// </summary>
    /// <param name="assignedToUserId">Filter by assigned user (use "current" for authenticated user)</param>
    /// <param name="requestedByUserId">Filter by requester (use "current" for authenticated user)</param>
    /// <param name="departmentId">Filter by department</param>
    /// <param name="status">Filter by status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tasks</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetTasks(
        [FromQuery] string? assignedToUserId = null,
        [FromQuery] string? requestedByUserId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        // Resolve "current" to the authenticated user's ID
        Guid? resolvedAssignedToUserId = ResolveUserIdParameter(assignedToUserId);
        Guid? resolvedRequestedByUserId = ResolveUserIdParameter(requestedByUserId);

        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<TaskDto>>("You do not have access to this department", 403);
        }

        try
        {
            var tasks = await _taskService.GetTasksAsync(
                companyId, resolvedAssignedToUserId, resolvedRequestedByUserId, departmentScope, status, cancellationToken);
            return this.Success(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tasks");
            return this.Error<List<TaskDto>>($"Failed to get tasks: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Resolves a user ID parameter, handling "current" as a special value for the authenticated user
    /// </summary>
    private Guid? ResolveUserIdParameter(string? userIdParam)
    {
        if (string.IsNullOrWhiteSpace(userIdParam))
        {
            return null;
        }

        // Handle "current" or "me" as special values for the authenticated user
        if (userIdParam.Equals("current", StringComparison.OrdinalIgnoreCase) ||
            userIdParam.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            return _currentUserService.UserId;
        }

        // Try to parse as GUID
        if (Guid.TryParse(userIdParam, out var guid))
        {
            return guid;
        }

        // Invalid value - return null (will show all tasks if no other filter)
        _logger.LogWarning("Invalid user ID parameter: {UserIdParam}", userIdParam);
        return null;
    }

    /// <summary>
    /// Get task by ID
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetTask(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            var task = await _taskService.GetTaskByIdAsync(id, companyId, cancellationToken);
            if (task == null)
            {
                return this.NotFound<TaskDto>($"Task with ID {id} not found");
            }

            return this.Success(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting task: {TaskId}", id);
            return this.Error<TaskDto>($"Failed to get task: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Create a new task
    /// </summary>
    /// <param name="dto">Task data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created task</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask(
        [FromBody] CreateTaskDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<TaskDto>("User context required");
        }

        try
        {
            var task = await _taskService.CreateTaskAsync(dto, companyId, userId.Value, cancellationToken);
            return this.StatusCode(201, ApiResponse<TaskDto>.SuccessResponse(task, "Task created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return this.Error<TaskDto>($"Failed to create task: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Update an existing task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="dto">Updated task data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated task</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(
        Guid id,
        [FromBody] UpdateTaskDto dto,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return this.Unauthorized<TaskDto>("User context required");
        }

        try
        {
            var task = await _taskService.UpdateTaskAsync(id, dto, companyId, userId.Value, cancellationToken);
            return this.Success(task, "Task updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound<TaskDto>($"Task with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task: {TaskId}", id);
            return this.Error<TaskDto>($"Failed to update task: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Delete a task
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> DeleteTask(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            await _taskService.DeleteTaskAsync(id, companyId, cancellationToken);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Task deleted successfully"));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Task with ID {id} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task: {TaskId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete task: {ex.Message}"));
        }
    }
}

