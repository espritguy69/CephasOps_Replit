using CephasOps.Application.Tasks.DTOs;

namespace CephasOps.Application.Tasks.Services;

/// <summary>
/// Task service interface
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Get tasks with filtering
    /// </summary>
    Task<List<TaskDto>> GetTasksAsync(
        Guid companyId,
        Guid? assignedToUserId = null,
        Guid? requestedByUserId = null,
        Guid? departmentId = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get task by ID
    /// </summary>
    Task<TaskDto?> GetTaskByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get task by order ID (for idempotent installer-task creation).
    /// </summary>
    Task<TaskDto?> GetTaskByOrderIdAsync(Guid companyId, Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new task
    /// </summary>
    Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing task
    /// </summary>
    Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a task
    /// </summary>
    Task DeleteTaskAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
}

