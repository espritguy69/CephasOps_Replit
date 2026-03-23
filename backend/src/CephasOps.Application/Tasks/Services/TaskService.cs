using CephasOps.Application.Tasks.DTOs;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Tasks.Services;

/// <summary>
/// Task service implementation
/// </summary>
public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ApplicationDbContext context,
        ILogger<TaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TaskDto>> GetTasksAsync(
        Guid companyId,
        Guid? assignedToUserId = null,
        Guid? requestedByUserId = null,
        Guid? departmentId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use LINQ query builder for better type safety and parameter handling
            var query = _context.Set<Domain.Tasks.Entities.TaskItem>().AsQueryable();

            // Apply filters
            if (companyId != Guid.Empty)
            {
                query = query.Where(t => t.CompanyId == companyId);
            }

            if (assignedToUserId.HasValue)
            {
                query = query.Where(t => t.AssignedToUserId == assignedToUserId.Value);
            }

            if (requestedByUserId.HasValue)
            {
                query = query.Where(t => t.RequestedByUserId == requestedByUserId.Value);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(t => t.DepartmentId == departmentId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            // Execute query and map to DTOs
            var tasks = await query.ToListAsync(cancellationToken);

            _logger.LogDebug("GetTasksAsync returned {Count} tasks. Filters: CompanyId={CompanyId}, AssignedToUserId={AssignedToUserId}, RequestedByUserId={RequestedByUserId}, DepartmentId={DepartmentId}, Status={Status}",
                tasks.Count, companyId, assignedToUserId, requestedByUserId, departmentId, status);

            // Map tasks with error handling for individual items
            var result = new List<TaskDto>();
            foreach (var task in tasks)
            {
                try
                {
                    result.Add(MapToTaskDtoFromEntity(task));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map task {TaskId}. Skipping.", task.Id);
                    // Skip invalid tasks rather than failing the entire request
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetTasksAsync. CompanyId={CompanyId}, DepartmentId={DepartmentId}", 
                companyId, departmentId);
            throw;
        }
    }

    public async Task<TaskDto?> GetTaskByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        CephasOps.Infrastructure.Persistence.TenantSafetyGuard.AssertTenantContext();
        var query = _context.Set<Domain.Tasks.Entities.TaskItem>()
            .Where(t => t.Id == id && t.CompanyId == companyId);
        var task = await query.FirstOrDefaultAsync(cancellationToken);

        return task != null ? MapToTaskDtoFromEntity(task) : null;
    }

    public async Task<TaskDto?> GetTaskByOrderIdAsync(Guid companyId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var task = await _context.Set<Domain.Tasks.Entities.TaskItem>()
            .Where(t => t.OrderId == orderId && t.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationToken);
        return task != null ? MapToTaskDtoFromEntity(task) : null;
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (dto.OrderId.HasValue)
        {
            var existing = await GetTaskByOrderIdAsync(companyId, dto.OrderId.Value, cancellationToken);
            if (existing != null)
            {
                _logger.LogDebug("Task already exists for order {OrderId}, returning existing task {TaskId}", dto.OrderId.Value, existing.Id);
                return existing;
            }
        }

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        object[] taskParams = {
            id, companyId, (object?)dto.DepartmentId ?? DBNull.Value, userId, dto.AssignedToUserId,
            dto.Title, (object?)dto.Description ?? DBNull.Value, now,
            (object?)dto.DueAt ?? DBNull.Value, dto.Priority, "New", now, userId, now, userId,
            (object?)dto.OrderId ?? DBNull.Value
        };
        await _context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""TaskItems"" (""Id"", ""CompanyId"", ""DepartmentId"", ""RequestedByUserId"", ""AssignedToUserId"", ""Title"", ""Description"", ""RequestedAt"", ""DueAt"", ""Priority"", ""Status"", ""CreatedAt"", ""CreatedByUserId"", ""UpdatedAt"", ""UpdatedByUserId"", ""OrderId"")
              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15})",
            taskParams);

        _logger.LogInformation("Task created: {TaskId}, Company: {CompanyId}", id, companyId);

        return await GetTaskByIdAsync(id, companyId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created task");
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetTaskByIdAsync(id, companyId, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found");
        }

        var updates = new List<string> { "\"UpdatedAt\" = {0}", "\"UpdatedByUserId\" = {1}" };
        var parameters = new List<object> { DateTime.UtcNow, userId };
        var paramIndex = 2;

        if (dto.AssignedToUserId.HasValue) { updates.Add($"\"AssignedToUserId\" = {{{paramIndex}}}"); parameters.Add(dto.AssignedToUserId.Value); paramIndex++; }
        if (dto.Title != null) { updates.Add($"\"Title\" = {{{paramIndex}}}"); parameters.Add(dto.Title); paramIndex++; }
        if (dto.Description != null) { updates.Add($"\"Description\" = {{{paramIndex}}}"); parameters.Add(dto.Description); paramIndex++; }
        if (dto.DueAt.HasValue) { updates.Add($"\"DueAt\" = {{{paramIndex}}}"); parameters.Add(dto.DueAt.Value); paramIndex++; }
        if (dto.Priority != null) { updates.Add($"\"Priority\" = {{{paramIndex}}}"); parameters.Add(dto.Priority); paramIndex++; }

        if (dto.Status != null)
        {
            updates.Add($"\"Status\" = {{{paramIndex}}}"); parameters.Add(dto.Status); paramIndex++;

            if (dto.Status == "InProgress" && existing.Status == "New")
                updates.Add("\"StartedAt\" = NOW()");
            if (dto.Status == "Completed" && existing.Status != "Completed")
                updates.Add("\"CompletedAt\" = NOW()");
        }

        if (updates.Count > 2)
        {
            parameters.Add(id);
            parameters.Add(companyId);
            var updateSql = $"UPDATE \"TaskItems\" SET {string.Join(", ", updates)} WHERE \"Id\" = {{{paramIndex}}} AND \"CompanyId\" = {{{paramIndex + 1}}}";
            await _context.Database.ExecuteSqlRawAsync(updateSql, parameters.ToArray());
        }

        _logger.LogInformation("Task updated: {TaskId}", id);

        return await GetTaskByIdAsync(id, companyId, cancellationToken) ?? existing;
    }

    public async Task DeleteTaskAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        var existing = await GetTaskByIdAsync(id, companyId, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found");
        }

        await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM ""TaskItems"" WHERE ""Id"" = {0} AND ""CompanyId"" = {1}",
            new object[] { id, companyId });

        _logger.LogInformation("Task deleted: {TaskId}", id);
    }

    private static TaskDto MapToTaskDtoFromEntity(Domain.Tasks.Entities.TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            CompanyId = task.CompanyId,
            DepartmentId = task.DepartmentId,
            RequestedByUserId = task.RequestedByUserId,
            AssignedToUserId = task.AssignedToUserId,
            Title = task.Title,
            Description = task.Description,
            RequestedAt = task.RequestedAt,
            DueAt = task.DueAt,
            Priority = task.Priority,
            Status = task.Status,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt,
            CreatedByUserId = task.CreatedByUserId,
            UpdatedAt = task.UpdatedAt,
            UpdatedByUserId = task.UpdatedByUserId,
            OrderId = task.OrderId
        };
    }

    private static TaskDto MapToTaskDto(dynamic t)
    {
        // Helper to safely parse GUID from dynamic (handles DBNull)
        Guid? ParseGuidOrNull(object? value)
        {
            if (value == null || value == DBNull.Value) return null;
            try
            {
                var str = value.ToString();
                return string.IsNullOrWhiteSpace(str) ? null : Guid.Parse(str);
            }
            catch
            {
                return null;
            }
        }

        // Helper to safely parse required GUID with fallback
        Guid ParseGuidRequired(object? value, string fieldName, Guid? fallback = null)
        {
            if (value == null || value == DBNull.Value)
            {
                if (fallback.HasValue)
                    return fallback.Value;
                throw new InvalidOperationException($"Required GUID field '{fieldName}' is null or DBNull");
            }
            try
            {
                return Guid.Parse(value.ToString()!);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse GUID field '{fieldName}': {ex.Message}", ex);
            }
        }

        // Helper to safely parse DateTime
        DateTime ParseDateTime(object? value, DateTime fallback)
        {
            if (value == null || value == DBNull.Value) return fallback;
            try
            {
                return (DateTime)value;
            }
            catch
            {
                return fallback;
            }
        }

        // Helper to safely parse nullable DateTime
        DateTime? ParseDateTimeOrNull(object? value)
        {
            if (value == null || value == DBNull.Value) return null;
            try
            {
                return (DateTime)value;
            }
            catch
            {
                return null;
            }
        }

        // Use a fallback GUID for required fields that might be NULL (shouldn't happen, but be defensive)
        var fallbackUserId = Guid.Parse("00000000-0000-0000-0000-000000000000");

        return new TaskDto
        {
            Id = ParseGuidRequired(t.Id, "Id"),
            CompanyId = ParseGuidOrNull(t.CompanyId),
            DepartmentId = ParseGuidOrNull(t.DepartmentId),
            RequestedByUserId = ParseGuidRequired(t.RequestedByUserId, "RequestedByUserId", fallbackUserId),
            AssignedToUserId = ParseGuidRequired(t.AssignedToUserId, "AssignedToUserId", fallbackUserId),
            Title = t.Title?.ToString() ?? string.Empty,
            Description = t.Description?.ToString(),
            RequestedAt = ParseDateTime(t.RequestedAt, DateTime.UtcNow),
            DueAt = ParseDateTimeOrNull(t.DueAt),
            Priority = t.Priority?.ToString() ?? "Normal",
            Status = t.Status?.ToString() ?? "New",
            StartedAt = ParseDateTimeOrNull(t.StartedAt),
            CompletedAt = ParseDateTimeOrNull(t.CompletedAt),
            CreatedAt = ParseDateTime(t.CreatedAt, DateTime.UtcNow),
            CreatedByUserId = ParseGuidRequired(t.CreatedByUserId, "CreatedByUserId", fallbackUserId),
            UpdatedAt = ParseDateTime(t.UpdatedAt, DateTime.UtcNow),
            UpdatedByUserId = ParseGuidRequired(t.UpdatedByUserId, "UpdatedByUserId", fallbackUserId),
            OrderId = ParseGuidOrNull(t.OrderId)
        };
    }
}

