namespace CephasOps.Application.Orders.DTOs;

/// <summary>
/// DTO for OrderStatusChecklistAnswer
/// </summary>
public class OrderStatusChecklistAnswerDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ChecklistItemId { get; set; }
    public bool Answer { get; set; }
    public DateTime AnsweredAt { get; set; }
    public Guid AnsweredByUserId { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Optional: Include checklist item details
    public OrderStatusChecklistItemDto? ChecklistItem { get; set; }
}

/// <summary>
/// DTO for submitting checklist answers
/// </summary>
public class SubmitOrderStatusChecklistAnswerDto
{
    public Guid ChecklistItemId { get; set; }
    public bool Answer { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// DTO for submitting multiple checklist answers at once
/// </summary>
public class SubmitOrderStatusChecklistAnswersDto
{
    public List<SubmitOrderStatusChecklistAnswerDto> Answers { get; set; } = new();
}

/// <summary>
/// DTO for checklist with answers (used in order detail view)
/// </summary>
public class OrderStatusChecklistWithAnswersDto
{
    public Guid Id { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public Guid? ParentChecklistItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }

    // Answer for this order (if provided)
    public OrderStatusChecklistAnswerDto? Answer { get; set; }

    // Nested sub-steps
    public List<OrderStatusChecklistWithAnswersDto> SubSteps { get; set; } = new();
}

