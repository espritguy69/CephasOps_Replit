using CephasOps.Application.Orders.DTOs;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// Service implementation for managing order status checklist items and answers
/// </summary>
public class OrderStatusChecklistService : IOrderStatusChecklistService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderStatusChecklistService> _logger;

    public OrderStatusChecklistService(
        ApplicationDbContext context,
        ILogger<OrderStatusChecklistService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<OrderStatusChecklistItemDto>> GetChecklistItemsByStatusAsync(
        string statusCode,
        CancellationToken cancellationToken = default)
    {
        var items = await _context.OrderStatusChecklistItems
            .Where(c => c.StatusCode == statusCode && c.IsActive)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync(cancellationToken);

        // Build hierarchical structure
        var mainSteps = items.Where(c => c.ParentChecklistItemId == null)
            .OrderBy(c => c.OrderIndex)
            .ToList();

        var result = new List<OrderStatusChecklistItemDto>();

        foreach (var mainStep in mainSteps)
        {
            var dto = MapToDto(mainStep);
            
            // Get sub-steps
            var subSteps = items
                .Where(c => c.ParentChecklistItemId == mainStep.Id)
                .OrderBy(c => c.OrderIndex)
                .Select(MapToDto)
                .ToList();

            dto.SubSteps = subSteps;
            result.Add(dto);
        }

        return result;
    }

    public async Task<List<OrderStatusChecklistWithAnswersDto>> GetChecklistWithAnswersAsync(
        Guid orderId,
        string statusCode,
        CancellationToken cancellationToken = default)
    {
        var items = await _context.OrderStatusChecklistItems
            .Where(c => c.StatusCode == statusCode && c.IsActive)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync(cancellationToken);

        var answers = await _context.OrderStatusChecklistAnswers
            .Where(a => a.OrderId == orderId)
            .ToListAsync(cancellationToken);

        var answerLookup = answers.ToDictionary(a => a.ChecklistItemId);

        // Build hierarchical structure with answers
        var mainSteps = items.Where(c => c.ParentChecklistItemId == null)
            .OrderBy(c => c.OrderIndex)
            .ToList();

        var result = new List<OrderStatusChecklistWithAnswersDto>();

        foreach (var mainStep in items.Where(c => c.ParentChecklistItemId == null).OrderBy(c => c.OrderIndex))
        {
            var dto = MapToDtoWithAnswers(mainStep, answerLookup);
            
            // Get sub-steps
            var subSteps = items
                .Where(c => c.ParentChecklistItemId == mainStep.Id)
                .OrderBy(c => c.OrderIndex)
                .Select(c => MapToDtoWithAnswers(c, answerLookup))
                .ToList();

            dto.SubSteps = subSteps;
            result.Add(dto);
        }

        return result;
    }

    public async Task<OrderStatusChecklistItemDto> CreateChecklistItemAsync(
        CreateOrderStatusChecklistItemDto dto,
        Guid? companyId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        // Validate parent if provided
        if (dto.ParentChecklistItemId.HasValue)
        {
            var parent = await _context.OrderStatusChecklistItems
                .FirstOrDefaultAsync(c => c.Id == dto.ParentChecklistItemId.Value, cancellationToken);

            if (parent == null)
            {
                throw new InvalidOperationException($"Parent checklist item {dto.ParentChecklistItemId} not found.");
            }

            // Ensure parent doesn't have a parent (only one level deep)
            if (parent.ParentChecklistItemId.HasValue)
            {
                throw new InvalidOperationException("Cannot add sub-step to a sub-step. Only one level of nesting is allowed.");
            }

            // Ensure parent is for the same status
            if (parent.StatusCode != dto.StatusCode)
            {
                throw new InvalidOperationException("Parent checklist item must be for the same status.");
            }
        }

        var entity = new OrderStatusChecklistItem
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            StatusCode = dto.StatusCode,
            ParentChecklistItemId = dto.ParentChecklistItemId,
            Name = dto.Name,
            Description = dto.Description,
            OrderIndex = dto.OrderIndex,
            IsRequired = dto.IsRequired,
            IsActive = dto.IsActive,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OrderStatusChecklistItems.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created checklist item {ItemId} for status {StatusCode}", entity.Id, dto.StatusCode);

        return MapToDto(entity);
    }

    public async Task<OrderStatusChecklistItemDto> UpdateChecklistItemAsync(
        Guid id,
        UpdateOrderStatusChecklistItemDto dto,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.OrderStatusChecklistItems
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Checklist item {id} not found.");
        }

        if (dto.Name != null)
            entity.Name = dto.Name;
        if (dto.Description != null)
            entity.Description = dto.Description;
        if (dto.OrderIndex.HasValue)
            entity.OrderIndex = dto.OrderIndex.Value;
        if (dto.IsRequired.HasValue)
            entity.IsRequired = dto.IsRequired.Value;
        if (dto.IsActive.HasValue)
            entity.IsActive = dto.IsActive.Value;

        entity.UpdatedByUserId = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated checklist item {ItemId}", id);

        return MapToDto(entity);
    }

    public async Task DeleteChecklistItemAsync(
        Guid id,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.OrderStatusChecklistItems
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Checklist item {id} not found.");
        }

        // Check if it has sub-steps
        var hasSubSteps = await _context.OrderStatusChecklistItems
            .AnyAsync(c => c.ParentChecklistItemId == id && !c.IsDeleted, cancellationToken);

        if (hasSubSteps)
        {
            throw new InvalidOperationException("Cannot delete checklist item that has sub-steps. Delete sub-steps first.");
        }

        // Soft delete
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted checklist item {ItemId}", id);
    }

    public async Task SubmitChecklistAnswersAsync(
        Guid orderId,
        SubmitOrderStatusChecklistAnswersDto dto,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Verify order exists
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException($"Order {orderId} not found.");
        }

        foreach (var answerDto in dto.Answers)
        {
            // Check if answer already exists
            var existingAnswer = await _context.OrderStatusChecklistAnswers
                .FirstOrDefaultAsync(a => a.OrderId == orderId && a.ChecklistItemId == answerDto.ChecklistItemId, cancellationToken);

            if (existingAnswer != null)
            {
                // Update existing answer
                existingAnswer.Answer = answerDto.Answer;
                existingAnswer.Remarks = answerDto.Remarks;
                existingAnswer.AnsweredAt = DateTime.UtcNow;
                existingAnswer.AnsweredByUserId = userId;
                existingAnswer.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new answer
                var answer = new OrderStatusChecklistAnswer
                {
                    Id = Guid.NewGuid(),
                    CompanyId = order.CompanyId,
                    OrderId = orderId,
                    ChecklistItemId = answerDto.ChecklistItemId,
                    Answer = answerDto.Answer,
                    Remarks = answerDto.Remarks,
                    AnsweredAt = DateTime.UtcNow,
                    AnsweredByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.OrderStatusChecklistAnswers.Add(answer);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Submitted {Count} checklist answers for order {OrderId}", dto.Answers.Count, orderId);
    }

    public async Task<bool> ValidateChecklistCompletionAsync(
        Guid orderId,
        string statusCode,
        CancellationToken cancellationToken = default)
    {
        var errors = await GetChecklistValidationErrorsAsync(orderId, statusCode, cancellationToken);
        return errors.Count == 0;
    }

    public async Task<List<string>> GetChecklistValidationErrorsAsync(
        Guid orderId,
        string statusCode,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Get all required checklist items for this status
        var requiredItems = await _context.OrderStatusChecklistItems
            .Where(c => c.StatusCode == statusCode && c.IsRequired && c.IsActive)
            .ToListAsync(cancellationToken);

        // Get all answers for this order
        var answers = await _context.OrderStatusChecklistAnswers
            .Where(a => a.OrderId == orderId)
            .ToDictionaryAsync(a => a.ChecklistItemId, cancellationToken);

        // Group items by parent
        var mainSteps = requiredItems.Where(c => c.ParentChecklistItemId == null).ToList();
        var subSteps = requiredItems.Where(c => c.ParentChecklistItemId != null)
            .GroupBy(c => c.ParentChecklistItemId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var mainStep in mainSteps)
        {
            var hasSubSteps = subSteps.ContainsKey(mainStep.Id);

            if (hasSubSteps)
            {
                // Main step has sub-steps - check all required sub-steps
                var requiredSubSteps = subSteps[mainStep.Id];
                foreach (var subStep in requiredSubSteps)
                {
                    if (!answers.TryGetValue(subStep.Id, out var answer) || !answer.Answer)
                    {
                        errors.Add($"Required sub-step '{subStep.Name}' under '{mainStep.Name}' is not completed.");
                    }
                }

                // Optionally check main step itself if it's also required
                if (mainStep.IsRequired)
                {
                    if (!answers.TryGetValue(mainStep.Id, out var mainAnswer) || !mainAnswer.Answer)
                    {
                        errors.Add($"Required step '{mainStep.Name}' is not completed.");
                    }
                }
            }
            else
            {
                // Main step has no sub-steps - check main step itself
                if (!answers.TryGetValue(mainStep.Id, out var answer) || !answer.Answer)
                {
                    errors.Add($"Required step '{mainStep.Name}' is not completed.");
                }
            }
        }

        return errors;
    }

    public async Task ReorderChecklistItemsAsync(
        string statusCode,
        Dictionary<Guid, int> itemOrderMap,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        foreach (var (itemId, newOrderIndex) in itemOrderMap)
        {
            var item = await _context.OrderStatusChecklistItems
                .FirstOrDefaultAsync(c => c.Id == itemId && c.StatusCode == statusCode, cancellationToken);

            if (item != null)
            {
                item.OrderIndex = newOrderIndex;
                item.UpdatedByUserId = userId;
                item.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reordered {Count} checklist items for status {StatusCode}", itemOrderMap.Count, statusCode);
    }

    public async Task BulkUpdateChecklistItemsAsync(
        string statusCode,
        List<Guid> itemIds,
        UpdateOrderStatusChecklistItemDto updateDto,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var items = await _context.OrderStatusChecklistItems
            .Where(c => c.StatusCode == statusCode && itemIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            if (updateDto.Name != null)
                item.Name = updateDto.Name;
            if (updateDto.Description != null)
                item.Description = updateDto.Description;
            if (updateDto.OrderIndex.HasValue)
                item.OrderIndex = updateDto.OrderIndex.Value;
            if (updateDto.IsRequired.HasValue)
                item.IsRequired = updateDto.IsRequired.Value;
            if (updateDto.IsActive.HasValue)
                item.IsActive = updateDto.IsActive.Value;

            item.UpdatedByUserId = userId;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bulk updated {Count} checklist items for status {StatusCode}", items.Count, statusCode);
    }

    public async Task CopyChecklistFromStatusAsync(
        string sourceStatusCode,
        string targetStatusCode,
        Guid? companyId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        // Get all items from source status
        var sourceItems = await _context.OrderStatusChecklistItems
            .Where(c => c.StatusCode == sourceStatusCode && c.IsActive)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync(cancellationToken);

        if (sourceItems.Count == 0)
        {
            throw new InvalidOperationException($"No checklist items found for status {sourceStatusCode}");
        }

        // Create a map of old ID to new ID for parent relationships
        var idMap = new Dictionary<Guid, Guid>();

        // First pass: Create all main steps
        foreach (var sourceItem in sourceItems.Where(c => c.ParentChecklistItemId == null))
        {
            var newItem = new OrderStatusChecklistItem
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                StatusCode = targetStatusCode,
                ParentChecklistItemId = null,
                Name = sourceItem.Name,
                Description = sourceItem.Description,
                OrderIndex = sourceItem.OrderIndex,
                IsRequired = sourceItem.IsRequired,
                IsActive = sourceItem.IsActive,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            idMap[sourceItem.Id] = newItem.Id;
            _context.OrderStatusChecklistItems.Add(newItem);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Second pass: Create all sub-steps
        foreach (var sourceItem in sourceItems.Where(c => c.ParentChecklistItemId != null))
        {
            if (idMap.TryGetValue(sourceItem.ParentChecklistItemId!.Value, out var newParentId))
            {
                var newItem = new OrderStatusChecklistItem
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    StatusCode = targetStatusCode,
                    ParentChecklistItemId = newParentId,
                    Name = sourceItem.Name,
                    Description = sourceItem.Description,
                    OrderIndex = sourceItem.OrderIndex,
                    IsRequired = sourceItem.IsRequired,
                    IsActive = sourceItem.IsActive,
                    CreatedByUserId = userId,
                    UpdatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.OrderStatusChecklistItems.Add(newItem);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Copied {Count} checklist items from status {SourceStatusCode} to {TargetStatusCode}",
            sourceItems.Count,
            sourceStatusCode,
            targetStatusCode);
    }

    private static OrderStatusChecklistItemDto MapToDto(OrderStatusChecklistItem entity)
    {
        return new OrderStatusChecklistItemDto
        {
            Id = entity.Id,
            CompanyId = entity.CompanyId,
            StatusCode = entity.StatusCode,
            ParentChecklistItemId = entity.ParentChecklistItemId,
            Name = entity.Name,
            Description = entity.Description,
            OrderIndex = entity.OrderIndex,
            IsRequired = entity.IsRequired,
            IsActive = entity.IsActive,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedByUserId = entity.UpdatedByUserId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static OrderStatusChecklistWithAnswersDto MapToDtoWithAnswers(
        OrderStatusChecklistItem entity,
        Dictionary<Guid, OrderStatusChecklistAnswer> answerLookup)
    {
        var dto = new OrderStatusChecklistWithAnswersDto
        {
            Id = entity.Id,
            StatusCode = entity.StatusCode,
            ParentChecklistItemId = entity.ParentChecklistItemId,
            Name = entity.Name,
            Description = entity.Description,
            OrderIndex = entity.OrderIndex,
            IsRequired = entity.IsRequired,
            IsActive = entity.IsActive
        };

        if (answerLookup.TryGetValue(entity.Id, out var answer))
        {
            dto.Answer = new OrderStatusChecklistAnswerDto
            {
                Id = answer.Id,
                OrderId = answer.OrderId,
                ChecklistItemId = answer.ChecklistItemId,
                Answer = answer.Answer,
                AnsweredAt = answer.AnsweredAt,
                AnsweredByUserId = answer.AnsweredByUserId,
                Remarks = answer.Remarks,
                CreatedAt = answer.CreatedAt,
                UpdatedAt = answer.UpdatedAt
            };
        }

        return dto;
    }
}

